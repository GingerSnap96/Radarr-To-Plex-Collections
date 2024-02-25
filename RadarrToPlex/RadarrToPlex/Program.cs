using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections;
using System.Xml;

namespace RadarrToPlex
{
    class Program
    {
        // Initialize variables for connection information
        static string serverId = "";
        static string radarrURL = "";
        static string radarrAPIKey = "";
        static string plexURL = "";
        static string plexToken = "";
        static int minForCollection = 0;
        static string libraryName = "";
        static int libraryId = 0;
        static bool delExistingCollections = false;

        // Define class for Plex collections
        class PlexCollections
        {
            public int CollectionId { get; set; }
            public string CollectionName { get; set; }
        }

        // Initialize dictionary to store Plex collections
        static Dictionary<string, PlexCollections> plexcollectionsDictionary = new Dictionary<string, PlexCollections>();

        // Define class for Plex movie details
        class PlexMovieDetails
        {
            public int RatingKey { get; set; }
            public string Key { get; set; }
            public string Title { get; set; }
            public string File { get; set; }
            public string[] Collections { get; set; }
        }

        // Initialize dictionary to store Plex movie details
        static Dictionary<string, PlexMovieDetails> plexMovieDetailsDictionary = new Dictionary<string, PlexMovieDetails>();

        // Define class for Radarr movie details
        class RadarrMovieDetails
        {
            public int tmdbid { get; set; }
            public string Title { get; set; }
            public string File { get; set; }
            public int CollectionId { get; set; }
            public string CollectionName { get; set; }
        }

        // Initialize dictionary to store Radarr movie details
        static Dictionary<string, RadarrMovieDetails> radarrMovieDetailsDictionary = new Dictionary<string, RadarrMovieDetails>();

        // Initialize dictionary to store collection exclusions
        static Dictionary<string, string> exclusions = new Dictionary<string, string>();

        // Main method
        static async Task Main()
        {
            try
            {
                // Display progress indicator
                Console.WriteLine("Progress:");
                Console.CursorVisible = false;

                // Step 1: Configure and establish the output log
                ConfigureLogging();

                // Step 2: Fetch config settings from config.json
                Log.Information("Fetching config settings from config.json");
                UpdateProgressBar(1, "Fetching config settings from config.json");
                GetConnectionInfo();

                // Step 3: Fetch server info from Plex
                Log.Information("Fetching server info from Plex");
                UpdateProgressBar(3, "Fetching server info from Plex");
                await GetServerInfo();

                // Step 4: Fetch library info from Plex
                Log.Information($"Fetching library {libraryName} info from Plex");
                UpdateProgressBar(5, $"Fetching library {libraryName} info from Plex");
                await GetPlexLibraries(libraryName);

                // Step 5: Fetch Collection info from Plex
                Log.Information("Fetching Collection info from Plex");
                UpdateProgressBar(6, "Fetching Collection info from Plex");
                await GetPlexCollections();
                
                // Optional Step: Delete the collections that already exist in plex.
                if (delExistingCollections)
                {
                    Log.Information("Deleting collections that already exist in plex.");
                    UpdateProgressBar(6, "Deleting collections that already exist in plex.");
                    await DeletePlexCollections();
                }

                // Step 6: Fetch movie data from Plex
                await GetPlexMovieData();

                // Step 7: Fetch collection data from Radarr
                Log.Information("Fetching collection data from Radarr");
                UpdateProgressBar(60, "Fetching collection data from Radarr");
                await GetRadarrCollectionInfo();

                // Step 8: Process collections to Plex
                Log.Information("Processing collections to Plex");
                UpdateProgressBar(65, "Processing collections to Plex");
                await ProcessCollectionMovies();

                // Completion message
                Log.Information("Collection info successfully synced with Plex");
                UpdateProgressBar(100, "Collection info successfully synced with Plex");
            }
            catch (Exception ex)
            {
                // Log error
                Log.Error(ex, "An error occurred");
                Console.WriteLine("Error: Please review the most recent log file errors and make required corrections.");
                // Terminate the application
                Environment.Exit(1);
            }
        }

        static void ConfigureLogging()
        {
            // Configure Serilog for file logging
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File($"logs/log-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt")
                .CreateLogger();

            // Log application started
            Log.Information("Application started.");
        }

        static void GetConnectionInfo()
        {
            // Read configuration from config.json
            string configFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
            if (!File.Exists(configFile))
            {
                // Log error
                Log.Error("Config file not found.");
                Console.WriteLine("Error: Please review the most recent log file errors and make required corrections.");
                // Terminate the application
                Environment.Exit(1);
            }

            try
            {
                string json = File.ReadAllText(configFile);
                dynamic config = JsonConvert.DeserializeObject(json);
                radarrURL = ValidateAndCleanUrl(config.RadarrURL.ToString());
                radarrAPIKey = config.RadarrAPIKey;
                plexURL = ValidateAndCleanUrl(config.PlexURL.ToString());
                plexToken = config.PlexToken;
                libraryName = config.LibraryName;
                minForCollection = config.MinForCollection;
                delExistingCollections = config.DeleteExistingPlexCollections;

                // Parse exclusions
                foreach (var exclusion in config.Exclusions)
                {
                    string collectionName = exclusion.CollectionName.ToString();
                    // Store exclusion in dictionary
                    exclusions[collectionName] = collectionName;
                }

                if (radarrAPIKey == "your_radarr_api_key_here" || plexToken == "your_plex_token_here" || libraryName == "library_name_here")
                {
                    // Log error
                    Log.Error("Error reading configuration file, default values still present. Please update the config.json");
                    Console.WriteLine("Error: Please review the most recent log file errors and make required corrections.");
                    // Terminate the application
                    Environment.Exit(1);
                }
            }
            catch (Exception ex)
            {
                // Log error
                Log.Error(ex, "Error reading configuration file");
                Console.WriteLine("Error: Please review the most recent log file errors and make required corrections.");
                // Terminate the application
                Environment.Exit(1);
            }
        }

        static string ValidateAndCleanUrl(string url)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out Uri validatedUrl))
            {
                return validatedUrl.ToString().TrimEnd('/');
            }
            else
            {
                // Log error
                Log.Error("Invalid URL format: " + url);
                Console.WriteLine("Error: Please review the most recent log file errors and make required corrections.");
                // Terminate the application
                Environment.Exit(1);
                return null; // This return statement is added to satisfy the compiler, it won't be reached
            }
        }

        static async Task GetServerInfo()
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync($"{plexURL}/identity");
                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(responseBody);
                        string machineIdentifier = xmlDoc.DocumentElement.GetAttribute("machineIdentifier");
                        serverId = machineIdentifier;
                    }
                    else
                    {
                        // Log error
                        Log.Error($"Failed to get server identity. Status code: {response.StatusCode}");
                        Console.WriteLine("Error: Please review the most recent log file errors and make required corrections.");
                        // Terminate the application
                        Environment.Exit(1);
                    }
                }
                catch (Exception ex)
                {
                    // Log error
                    Log.Error(ex, "An error occurred while fetching server info");
                    Console.WriteLine("Error: Please review the most recent log file errors and make required corrections.");
                    // Terminate the application
                    Environment.Exit(1);
                }
            }
        }

        // Method to fetch Plex libraries
        static async Task GetPlexLibraries(string libraryName)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    // Doesnt allow token as a header for some reason on this request
                    HttpResponseMessage response = await client.GetAsync($"{plexURL}/library/sections?X-Plex-Token={plexToken}");
                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();

                        // Load XML response into XmlDocument
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(responseBody);

                        // Find the Directory element with matching title attribute
                        XmlNodeList directoryNodes = xmlDoc.SelectNodes($"//Directory[@title='{libraryName}']");
                        if (directoryNodes.Count > 0)
                        {
                            // Retrieve the key attribute of the first matching Directory element and store it in global variable
                            libraryId = int.Parse(directoryNodes[0].Attributes["key"].Value);
                        }
                        else
                        {
                            // Log error
                            Log.Error($"Library '{libraryName}' not found.");
                            Console.WriteLine("Error: Please review the most recent log file errors and make required corrections.");
                            Environment.Exit(1);
                        }
                    }
                    else
                    {
                        // Log error
                        Log.Error($"Failed to get library identity. Status code: {response.StatusCode}");
                        Console.WriteLine("Error: Please review the most recent log file errors and make required corrections.");
                        Environment.Exit(1);
                    }
                }
                catch (Exception ex)
                {
                    // Log error
                    Log.Error($"An error occurred while fetching Plex libraries: {ex.Message}");
                    Console.WriteLine("Error: Please review the most recent log file errors and make required corrections.");
                    // Terminate the application
                    Environment.Exit(1);
                }
            }
        }

        // Method to fetch Plex collections
        static async Task GetPlexCollections()
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    // Get collections from Plex server
                    HttpResponseMessage response = await client.GetAsync($"{plexURL}/library/sections/{libraryId}/collections/?X-Plex-Token={plexToken}");
                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();

                        // Load XML response into XmlDocument
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(responseBody);

                        // Find all Directory elements
                        XmlNodeList directoryNodes = xmlDoc.SelectNodes("//Directory");
                        if (directoryNodes != null)
                        {
                            foreach (XmlNode directoryNode in directoryNodes)
                            {
                                string title = directoryNode.Attributes["title"].Value;
                                int ratingKey = int.Parse(directoryNode.Attributes["ratingKey"].Value);

                                // Create a new PlexCollections object
                                PlexCollections collectionDetails = new PlexCollections
                                {
                                    CollectionId = ratingKey,
                                    CollectionName = title
                                };
                                plexcollectionsDictionary.Add(title, collectionDetails);
                            }
                        }
                        else
                        {
                            // Log error
                            Log.Error("No collection directories found.");
                            Console.WriteLine("Error: Please review the most recent log file errors and make required corrections.");
                            Environment.Exit(1);
                        }
                    }
                    else
                    {
                        // Log error
                        Log.Error($"Failed to get library identity. Status code: {response.StatusCode}");
                        Console.WriteLine("Error: Please review the most recent log file errors and make required corrections.");
                        Environment.Exit(1);
                    }
                }
                catch (Exception ex)
                {
                    // Log error
                    Log.Error($"An error occurred while fetching Plex collections: {ex.Message}");
                    Console.WriteLine("Error: Please review the most recent log file errors and make required corrections.");
                    // Terminate the application
                    Environment.Exit(1);
                }
            }
        }

        // Method to delete existing plex collections
        static async Task DeletePlexCollections()
        {
            int totalPlexCollections = plexcollectionsDictionary.Count;
            int currentCollection = 0;
            foreach (var entry in plexcollectionsDictionary)
            {
                currentCollection++;

                // Get the collection name & the CollectionId
                string collectionName = entry.Key;
                PlexCollections plexCollection = entry.Value;
                int collectionId = plexCollection.CollectionId;

                try
                {
                    // Log collection that will be deleted
                    Log.Information($"Attempting to delete collection ({currentCollection}/{totalPlexCollections}): {collectionName}");

                    // Construct the URL for the DELETE request
                    string deleteUrl = $"{plexURL}/library/collections/{collectionId}?X-Plex-Product=Plex%20Web&X-Plex-Version=4.124.1&X-Plex-Client-Identifier=1mjve8qjaa7j8aodxmk18eo5&X-Plex-Platform=Microsoft%20Edge&X-Plex-Platform-Version=121.0&X-Plex-Features=external-media%2Cindirect-media%2Chub-style-list&X-Plex-Model=hosted&X-Plex-Device=Windows&X-Plex-Device-Name=Microsoft%20Edge&X-Plex-Device-Screen-Resolution=2505x1289%2C2561x1440&X-Plex-Token={plexToken}&X-Plex-Language=en&X-Plex-Drm=playready&X-Plex-Text-Format=plain&X-Plex-Provider-Version=5.1";

                    // Create HttpRequestMessage with HttpMethod.Delete
                    var request = new HttpRequestMessage(HttpMethod.Delete, deleteUrl);

                    // Create HttpClient
                    using (HttpClient client = new HttpClient())
                    {
                        // Send DELETE request
                        HttpResponseMessage response = client.SendAsync(request).Result;

                        // Check if request was successful
                        if (response.IsSuccessStatusCode)
                        {
                            Log.Information($"Successfully deleted collection ({currentCollection}/{totalPlexCollections}): {collectionName}");
                            // Remove the collection from the dictionary
                            plexcollectionsDictionary.Remove(collectionName);
                        }
                        else
                        {
                            // Log error
                            Log.Error($"Failed to delete collection. Status code: {response.StatusCode}");
                            Console.WriteLine("Error: Please review the most recent log file errors and make required corrections.");
                            Environment.Exit(1);
                        }
                    }
                }

                catch (HttpRequestException e)
                {
                    // Log error
                    Log.Error($"Error deleting collection {collectionName} ({collectionId}) : {e.Message}");
                    Console.WriteLine("Error: Please review the most recent log file errors and make required corrections.");
                    Environment.Exit(1);
                }
            }
        }

        // Method to fetch movie data from Plex
        static async Task GetPlexMovieData()
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync($"{plexURL}/library/sections/{libraryId}/all?X-Plex-Token={plexToken}");
                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(responseBody);

                        XmlNodeList videoNodes = xmlDoc.SelectNodes("//Video");
                        if (videoNodes.Count > 0)
                        {
                            int totalMovies = videoNodes.Count;
                            int currentMovie = 0;
                            int percentIncrement = (int)Math.Round((double)totalMovies / 54);
                            int percentIncrease = 0;
                            foreach (XmlNode videoNode in videoNodes)
                            {
                                currentMovie++;
                                int remainder = currentMovie % percentIncrement;
                                bool isWholeNumber = remainder == 0;
                                if (isWholeNumber)
                                {
                                    percentIncrease++;
                                }

                                int ratingKey = int.Parse(videoNode.Attributes["ratingKey"].Value);
                                string key = videoNode.Attributes["key"].Value;
                                string title = videoNode.Attributes["title"].Value;

                                XmlNode partNode = videoNode.SelectSingleNode("Media/Part");
                                string filePath = partNode.Attributes["file"].Value;

                                // Standardize the movie path
                                string movieFile = StandarizeMovieFilePath(filePath);

                                UpdateProgressBar(6 + percentIncrease, $"Fetching movie data from Plex ({currentMovie}/{totalMovies}): {title}");
                                Log.Information($"Fetching movie data from Plex ({currentMovie}/{totalMovies}): {title}");
                                // Get collection info
                                HttpResponseMessage collectionResponse = await client.GetAsync($"{plexURL}/library/metadata/{ratingKey}?X-Plex-Token={plexToken}");
                                if (collectionResponse.IsSuccessStatusCode)
                                {
                                    string collectionResponseBody = await collectionResponse.Content.ReadAsStringAsync();
                                    XmlDocument collectionXmlDoc = new XmlDocument();
                                    collectionXmlDoc.LoadXml(collectionResponseBody);

                                    List<string> collections = new List<string>();
                                    XmlNodeList collectionNodes = collectionXmlDoc.SelectNodes("//Collection");
                                    if (collectionNodes != null)
                                    {
                                        foreach (XmlNode collectionNode in collectionNodes)
                                        {
                                            // Check if the "tag" attribute exists
                                            if (collectionNode.Attributes["tag"] != null)
                                            {
                                                string collectionName = collectionNode.Attributes["tag"].Value;
                                                collections.Add(collectionName);
                                            }
                                        }
                                    }

                                    PlexMovieDetails movieDetails = new PlexMovieDetails
                                    {
                                        RatingKey = ratingKey,
                                        Key = key,
                                        Title = title,
                                        File = movieFile,
                                        Collections = collections.ToArray()
                                    };

                                    plexMovieDetailsDictionary[movieFile] = movieDetails;
                                }
                                else
                                {
                                    // Log error
                                    Log.Error($"Failed to get collection for rating key {ratingKey}. Status code: {collectionResponse.StatusCode}");
                                    Console.WriteLine("Error: Please review the most recent log file errors and make required corrections.");
                                    Environment.Exit(1);
                                }
                            }
                        }
                        else
                        {
                            // Log error
                            Log.Error("No videos found in the response.");
                            Console.WriteLine("Error: Please review the most recent log file errors and make required corrections.");
                            Environment.Exit(1);
                        }
                    }
                    else
                    {
                        // Log error
                        Log.Error($"Failed to get library contents. Status code: {response.StatusCode}");
                        Console.WriteLine("Error: Please review the most recent log file errors and make required corrections.");
                        Environment.Exit(1);
                    }
                }
                catch (Exception ex)
                {
                    // Log error
                    Log.Error($"An error occurred while fetching Plex movie data: {ex.Message}");
                    Console.WriteLine("Error: Please review the most recent log file errors and make required corrections.");
                    // Terminate the application
                    Environment.Exit(1);
                }
            }
        }


        // Method to fetch collection info from Radarr
        static async Task GetRadarrCollectionInfo()
        {
            // Create HttpClient instance
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.Add("X-Api-Key", radarrAPIKey);

                    // Retrieve collections from Radarr
                    HttpResponseMessage response = client.GetAsync($"{radarrURL}/api/v3/collection").Result;
                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = response.Content.ReadAsStringAsync().Result;
                        JArray collections = JArray.Parse(responseBody);

                        // Iterate through collections again to fetch movies
                        foreach (JObject collection in collections)
                        {
                            int collectionId = collection["id"].ToObject<int>();
                            int tmdbId = collection["tmdbId"].ToObject<int>();
                            string collectionName = collection["title"].ToString();

                            await GetRadarrMoviesInCollection(collectionId, collectionName);
                        }
                    }
                    else
                    {
                        // Log error
                        Log.Error($"Failed to get collections from Radarr. Status code: {response.StatusCode}");
                        Console.WriteLine("Error: Please review the most recent log file errors and make required corrections.");
                        Environment.Exit(1);
                    }
                }
                catch (HttpRequestException e)
                {
                    // Log error
                    Log.Error($"Error getting radarr collections: {e.Message}");
                    Console.WriteLine("Error: Please review the most recent log file errors and make required corrections.");
                    Environment.Exit(1);
                }
            }
        }

        // Method to fetch movies in a Radarr collection
        static async Task GetRadarrMoviesInCollection(int collectionId, string collectionName)
        {
            // Create HttpClient instance
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.Add("X-Api-Key", radarrAPIKey);
                    // Fetch movies within the collection
                    HttpResponseMessage moviesResponse = client.GetAsync($"{radarrURL}/api/v3/collection/{collectionId}").Result;
                    if (moviesResponse.IsSuccessStatusCode)
                    {
                        JObject moviesResponseBody = JObject.Parse(moviesResponse.Content.ReadAsStringAsync().Result);
                        JArray movies = (JArray)moviesResponseBody["movies"];

                        // Iterate through movies
                        foreach (JObject movie in movies)
                        {
                            // Extract movie details
                            int movieTmdbId = movie["tmdbId"].ToObject<int>();
                            string movieTitle = movie["title"].ToString();

                            await GetRadarrMovieDetails(movieTmdbId, collectionId, collectionName, movieTitle);
                        }
                    }
                    else
                    {
                        // Log error
                        Log.Error($"Failed to get movies in collections from Radarr. Status code: {moviesResponse.StatusCode}");
                        Console.WriteLine("Error: Please review the most recent log file errors and make required corrections.");
                        Environment.Exit(1);
                    }
                }
                catch (HttpRequestException e)
                {
                    // Log error
                    Log.Error($"Error getting radarr movies in collection: {e.Message}");
                    Console.WriteLine("Error: Please review the most recent log file errors and make required corrections.");
                    Environment.Exit(1);
                }
            }
        }

        // Method to fetch details of a movie in Radarr collection
        static async Task GetRadarrMovieDetails(int movieTmdbId, int collectionId, string collectionName, string title)
        {
            // Create HttpClient instance
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.Add("X-Api-Key", radarrAPIKey);
                    // Fetch movies within the collection
                    HttpResponseMessage moviedetailsResponse = await client.GetAsync($"{radarrURL}/api/v3/movie?tmdbid={movieTmdbId}");

                    if (moviedetailsResponse.IsSuccessStatusCode)
                    {
                        string responseBody = await moviedetailsResponse.Content.ReadAsStringAsync();

                        // Check if the response body is empty
                        if (string.IsNullOrEmpty(responseBody) || responseBody == "[]")
                        {
                            // No movies found for the given TMDb ID, bail out of the method
                            Log.Information($"Movie {title} is not downloaded as part of collection {collectionName} in Radarr, skipping.");
                            return;
                        }

                        JArray moviesResponseBody = JArray.Parse(responseBody);

                        if (moviesResponseBody.Count == 0)
                        {
                            return; // Bail out of the method
                        }

                        JObject movie = (JObject)moviesResponseBody.First;

                        string movieTitle = movie["title"].ToString();

                        // Check if movie file path exists
                        if (movie["movieFile"] == null || movie["movieFile"]["path"] == null)
                        {
                            // Movie file path not found bail out of the method
                            Log.Information($"Movie {title} is not downloaded as part of collection {collectionName} in Radarr, skipping.");
                            return;
                        }

                        string movieFilePath = movie["movieFile"]["path"].ToString();

                        // Standardize the movie path
                        string radarrPath = StandarizeMovieFilePath(movieFilePath);

                        // Create a new MovieDetails object
                        RadarrMovieDetails radarrmovieDetails = new RadarrMovieDetails
                        {
                            tmdbid = movieTmdbId,
                            Title = movieTitle,
                            File = radarrPath,
                            CollectionId = collectionId,
                            CollectionName = collectionName,
                        };

                        // Store the movie details in the dictionary
                        radarrMovieDetailsDictionary[radarrPath] = radarrmovieDetails;
                    }
                    else
                    {
                        // Log error
                        Log.Error($"Failed to get Radarr movie details. Status code: {moviedetailsResponse.StatusCode}");
                        Console.WriteLine("Error: Please review the most recent log file errors and make required corrections.");
                        Environment.Exit(1);
                    }
                }
                catch (HttpRequestException e)
                {
                    // Log error
                    Log.Error($"Error getting radarr movies in collection: {e.Message}");
                    Console.WriteLine("Error: Please review the most recent log file errors and make required corrections.");
                    Environment.Exit(1);
                }
            }
        }

        // Method to process movies in collections
        static async Task ProcessCollectionMovies()
        {
            // Group movies by collection name and count the number of movies in each group
            var groupedMovies = radarrMovieDetailsDictionary.Values
                .GroupBy(movie => movie.CollectionName)
                .Where(group => group.Count() >= minForCollection);

            // Initialize variables for progress tracking
            int percentIncrease = 0;
            int currentCollection = 0;
            int totalCollections = groupedMovies.Count();
            int percentIncrement = (int)Math.Round((double)totalCollections / 35);

            // Iterate over each group of movies by collection
            foreach (var group in groupedMovies)
            {
                try
                {
                    currentCollection++;
                    var collectionName = group.Key;

                    // Check to see if the collection is set to be excluded
                    if (exclusions.ContainsKey(collectionName))
                    {
                        // Skip to the next iteration if the collection is excluded
                        continue;
                    }

                    // Update progress bar to indicate the current collection being processed
                    UpdateProgressBar(65 + percentIncrease, $"Processing plex collection ({currentCollection}/{totalCollections}): {collectionName}");
                    Log.Information($"Processing plex collection ({currentCollection}/{totalCollections}): {collectionName}");

                    // Update percentIncrease if a whole increment of movies has been processed
                    int remainder = currentCollection % percentIncrement;
                    bool isWholeNumber = remainder == 0;
                    if (isWholeNumber)
                    {
                        percentIncrease++;
                    }

                    // Process movies in the current collection group
                    foreach (var radarrMovie in group)
                    {
                        // Standardize the movie path
                        string movieFile = StandarizeMovieFilePath(radarrMovie.File);

                        if (plexMovieDetailsDictionary.TryGetValue(movieFile, out var plexMovie))
                        {
                            int ratingKey = plexMovie.RatingKey;
                            string movieTitle = plexMovie.Title;

                            bool inCollection = plexMovie.Collections.Contains(collectionName);
                            bool collectionExists = plexcollectionsDictionary.ContainsKey(collectionName);

                            // Add the movie to the collection if it's not already in it and the collection exists
                            if (!inCollection && collectionExists)
                            {
                                await AddMovieToCollection(collectionName, ratingKey, movieTitle);
                                Log.Information($"Successfully added {movieTitle} to collection: {collectionName}");
                            }
                            // Create the collection and add the movie if the collection doesn't exist
                            else if (!inCollection && !collectionExists)
                            {
                                await CreateCollection(collectionName, ratingKey);
                                Log.Information($"Successfully created collection: {collectionName} and added {movieTitle} to collection");
                            }
                            Log.Information($"Movie {movieTitle} already exists in collection {collectionName}, skipping.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log error
                    Log.Error($"Error processing Radarr movies in collection: {group.Key}. Details: {ex.Message}");
                    Console.WriteLine("Error: Please review the most recent log file errors and make required corrections.");
                    Environment.Exit(1);
                }
            }
        }


        // Method to create a new collection in Plex
        static async Task CreateCollection(string collectionName, int ratingKey)
        {
            try
            {
                if (collectionName != "")
                {
                    // Convert collection name to URL format
                    string title = Uri.EscapeDataString(collectionName);

                    string collectionURL = $"{plexURL}/library/collections?type=1&title={title}&smart=0&uri=server%3A%2F%2F{serverId}%2Fcom.plexapp.plugins.library%2Flibrary%2Fmetadata%2F{ratingKey}&sectionId=1&X-Plex-Product=Plex%20Web&X-Plex-Version=4.123.2&X-Plex-Client-Identifier=1mjve8qjaa7j8aodxmk18eo5&X-Plex-Platform=Microsoft%20Edge&X-Plex-Platform-Version=121.0&X-Plex-Features=external-media%2Cindirect-media%2Chub-style-list&X-Plex-Model=hosted&X-Plex-Device=Windows&X-Plex-Device-Name=Microsoft%20Edge&X-Plex-Device-Screen-Resolution=2505x1289%2C2561x1440&X-Plex-Token={plexToken}&X-Plex-Provider-Version=6.5&X-Plex-Text-Format=plain&X-Plex-Drm=playready&X-Plex-Language=en";

                    // Create HttpRequestMessage with HttpMethod.Post
                    var request = new HttpRequestMessage(HttpMethod.Post, collectionURL);

                    // Create HttpClient
                    using (HttpClient client = new HttpClient())
                    {
                        // Send POST request
                        HttpResponseMessage response = client.SendAsync(request).Result;

                        // Check if request was successful
                        if (response.IsSuccessStatusCode)
                        {
                            // Parse and store the index from the XML response
                            string responseBody = await response.Content.ReadAsStringAsync();
                            int index = ParseIndex(responseBody);

                            // Create a new PlexCollections object
                            PlexCollections collectionDetails = new PlexCollections
                            {
                                CollectionId = index,
                                CollectionName = collectionName
                            };
                            // Add to the collection
                            plexcollectionsDictionary.Add(collectionName, collectionDetails);
                        }
                        else
                        {
                            // Log error
                            Log.Error($"Failed to create collection. Status code: {response.StatusCode}");
                            Console.WriteLine("Error: Please review the most recent log file errors and make required corrections.");
                            Environment.Exit(1);
                        }
                    }
                }

            }
            catch (HttpRequestException e)
            {
                // Log error
                Log.Error($"Error creating collection {collectionName} : {e.Message}");
                Console.WriteLine("Error: Please review the most recent log file errors and make required corrections.");
                Environment.Exit(1);
            }
        }

        // Method to add a movie to a collection in Plex
        static async Task AddMovieToCollection(string collectionName, int ratingKey, string movieTitle)
        {
            try
            {
                string title = Uri.EscapeDataString(collectionName);
                string collectionURL = $"{plexURL}/library/sections/{libraryId}/all?type=1&id={ratingKey}&includeExternalMedia=1&collection[0].tag.tag={title}&X-Plex-Token={plexToken}";

                // Create HttpRequestMessage with HttpMethod.Post
                var request = new HttpRequestMessage(HttpMethod.Put, collectionURL);

                // Create HttpClient
                using (HttpClient client = new HttpClient())
                {
                    // Send POST request
                    HttpResponseMessage response = client.SendAsync(request).Result;

                    // Check if request was successful
                    if (!response.IsSuccessStatusCode)
                    {
                        // Log error
                        Log.Error($"Error adding movie {movieTitle} in collection: {collectionName}");
                        Console.WriteLine("Error: Please review the most recent log file errors and make required corrections.");
                        Environment.Exit(1);
                    }
                }
            }
            catch (HttpRequestException e)
            {
                // Log error
                Log.Error($"Error adding movie {movieTitle} in collection: {e.Message}");
                Console.WriteLine("Error: Please review the most recent log file errors and make required corrections.");
                Environment.Exit(1);
            }
        }

        // Method to parse index from XML response
        static int ParseIndex(string xmlResponse)
        {
            try
            {
                // Load XML response into an XmlDocument
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xmlResponse);

                // Get the index attribute value from the first Directory element
                XmlNode directoryNode = xmlDoc.SelectSingleNode("//Directory");
                if (directoryNode != null)
                {
                    int index;
                    if (int.TryParse(directoryNode.Attributes["index"]?.Value, out index))
                    {
                        return index;
                    }
                }

                // Return -1 if index attribute not found or cannot be parsed
                return -1;
            }
            catch (Exception e)
            {
                // Log error
                Log.Error($"Failed to parse Index in collection. {e.Message}");
                Console.WriteLine("Error: Please review the most recent log file errors and make required corrections.");
                Environment.Exit(1);
                return -1;
            }

        }

        // Method to get the filename and the folder above it to match plex movies to radarr movies (done to prevent issues if UNC is used for one but not the other)
        static string StandarizeMovieFilePath(string filePath)
        {
            // Finding the last occurrence of directory separator character
            int lastIndex = filePath.LastIndexOfAny(new[] { '\\', '/' });

            // Extracting the parent folder and its name, had to be done this way to make it work across os's
            string fileName = lastIndex != -1 ? filePath.Substring(lastIndex + 1, filePath.Length - lastIndex - 1) : "";
            string parentFolder = lastIndex != -1 ? filePath.Substring(0, lastIndex) : "";
            int secondLastIndex = parentFolder.LastIndexOfAny(new[] { '\\', '/' });
            string folderAbove = secondLastIndex != -1 ? parentFolder.Substring(secondLastIndex + 1) : parentFolder;

            // Combining folderAbove and fileName
            string standarizedMovieFilePath = Path.Combine(folderAbove, fileName);
            return standarizedMovieFilePath;
        }

        // Method to update progress bar
        static void UpdateProgressBar(int percent, string message)
        {
            int totalBars = 50;
            int numBars = (int)Math.Round((double)percent / 100 * totalBars);

            string progressBar = $"[{new string('#', numBars)}{new string(' ', totalBars - numBars)}] {percent}% : ";

            // Pad or truncate the message to fit within the existing line
            if (message.Length > Console.WindowWidth - progressBar.Length)
            {
                message = message.Substring(0, Console.WindowWidth - progressBar.Length);
            }
            else
            {
                message = message.PadRight(Console.WindowWidth - progressBar.Length);
            }

            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(progressBar + message);
        }


    }
}