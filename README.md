# 🚀 Radarr to Plex Collections 🍿

Welcome to Radarr to Plex Collections! Say goodbye to scattered movies and hello to organized collections, all with the click of a button (or command)!

## 🎥 About

Plex attempts automatically create collections (if you have that setting enabled), but is inconsistent at best and does nothing at worst. Radarr to Plex Collections automates the process of creating Plex movie collections directly from your existing Radarr movie collections, streamlining your media management experience. 


## ❓ What are plex collections?

Plex collections allow you to organize your media library by grouping related movies together. These collections are visible in the Plex interface under the "Collections" tab within a movie library, making it easy to browse and access movies within each collection. Additionally, within each movie's details in Plex, the collection it belongs to is displayed, providing a convenient way to watch movies in order or explore related content.

## 🌟 Features

- **Automated Collection Creation**: Radarr to Plex Collections seamlessly generates movie collections in Plex based on your existing collections in Radarr.

    > **Note:** This application will not work for movies that are not managed in Radarr. For example, if you have three Harry Potter movies in Plex that are not managed by Radarr, they will not be added to the Harry Potter Collection.

- **Effortless Automation**: With Radarr to Plex Collections, automate the entire process effortlessly. Set it up once, and let the tool handle the rest. Save time and effort by automating the management of your collections seamlessly.

## 🛠️ Prerequisites

Before setting up Radarr to Plex Collections, make sure you have the following:

1. **Radarr and Plex Servers**: Ensure you have both Radarr and Plex servers set up, you will need administrator access to both Radarr and Plex.

2. **Radarr API Version**: This application was built using v3 of the Radarr API, ensure you're able to use version 3 of the API for your Radarr install.

Once you have these prerequisites in place, you'll be ready to set up Radarr to Plex Collections seamlessly.

## 🖥️ Supported Operating Systems

Radarr to Plex Collections is compatible with the following operating systems and architectures:

| Operating System | Architecture | Compatibility |
|------------------|--------------|---------------|
| Windows 11       | x64          | ✅            |
| Windows 10       | x86, x64     | ✅            |
| Linux (Ubuntu)   | x86, x64, ARM| ✅            |
| Linux (Debian)   | x86, x64, ARM| ✅            |
| macOS            | x64, ARM     | ✅            |

## 💡 Getting Started

To get started with Radarr to Plex Collections, follow these simple steps:

1. **Download the Latest Release**: Visit the [Releases](https://github.com/gingersnap96/radarr-to-plex-collections/releases) page and download the latest release of Radarr to Plex Collections. Look for the release package named something like `radarr-to-plex-vX.X.X-win-x64.zip`. Make sure to select the release package appropriate for your operating system (Windows, Linux or macOS) and architecture (x86, x64, arm).

2. **Extract the Files**: After downloading the zip file, extract its contents to a directory of your choice on your machine.

3. **Navigate to the Directory**: Open the folder where you extracted the files.

## ⚙️ Usage

Using Radarr to Plex Collections is a breeze:

1. **Configure Settings**: Open the `config.json` file and provide your Radarr and Plex server details.
    - **RadarrURL**: Replace `"your_radarr_url_here"` with the URL of your Radarr instance. This typically looks like `http://localhost:7878` unless you've configured it differently.
    - **RadarrAPIKey**: Replace `"your_radarr_api_key_here"` with your Radarr API key. You can find this in Radarr under Settings > General > Security.
    - **PlexURL**: Replace `"your_plex_url_here"` with the URL of your Plex instance. This typically looks like `http://localhost:32400`.
    - **PlexToken**: Replace `"your_plex_token_here"` with your Plex authentication token. To obtain the token, follow these steps:
        1. Log in to your Plex account in the Plex Web App.
        2. Browse to any library item in your Plex server.
        3. Select "Get Info" on the library item.
        4. Select View XML button on the Get Info page.
        5. View the URL of the xml and locate the value associated with the `X-Plex-Token` parameter (usually at the end of the URL).
        6. Copy the value of the `X-Plex-Token` parameter (value after =) and paste it into the `PlexToken` field in your `config.json` file. 
        7. Save and close the confif file.
    - **MinForCollection**: Set `"MinForCollection"` to the minimum number of movies required to create a collection. For example, if you set it to `2`, collections will only be created when you have 2 or more movies in that collection in your Plex library.
    - **LibraryName**: Replace `"library_name_here"` with the name of the library in Plex for which you want to create collections. This will most likely be named "Movies".
    - **DeleteExistingPlexCollections**: Acceptable values are `true` or `false`. If set to true the application will delete all pre-existing collections within plex. It is advised to set to true if Plex auto-generated some collections as this application may generate a duplicate collection with a slightly different name. Keep in Mind this will delete any collections you created manually as well.
    - **Exclusions**: Optionally, fill out the `"Exclusions"` array with the names of collections you want to exclude from synchronization. Do not delete the placeholders if you don't need to exclude any collections. Ensure to follow the format mentioned in the Exclusions Configuration section below.

2. **Run the Application**:
    - **For Windows**:
        - Double-click the `RadarrToPlex.exe` executable to launch the application.
        - To add it as a scheduled task, open Task Scheduler, click "Create Basic Task", and follow the wizard to schedule the `RadarrToPlex.exe` to run at your desired intervals. Set the "Start in" field to the directory where the `RadarrToPlex.exe` file is located.
    - **For Linux**:
        - Open a terminal and navigate to the directory containing the `RadarrToPlex` executable (It will have no extension).
        - Ensure that the `config.json` file is also located in this directory.
        - Run the following command to add execute permissions to the executable: (you only need to do this once)
            ```bash
            chmod +x RadarrToPlex
            ```
        - Run the following command to add execute the application:
            ```bash
            ./RadarrToPlex
            ```
            
    - To add it as a scheduled task using cron jobs:
        1. Open the crontab editor by running `crontab -e`. 
        2. Add a new line specifying the desired schedule and command to run the `RadarrToPlex` executable. Make sure to specify the full path to the directory containing the executable.

    - **For macOS**:
        - Double-click the `RadarrToPlex` executable to launch the application(It will have no extension). 
        - You may get a security warning and your OS will prevent the app from running, if so follow the steps below.
            - Open Settings > Security and Privacy > General Tab 
            - If you just tried to run the application there should be an option to allow the application, select allow.
        - Double-click the `RadarrToPlex` executable to launch the application

3. **Review Log File:** Each time the application runs it will create a log file for you to review the output of the application. 
    - For Windows and Linux this folder will be created in the folder the executable is in. 
    - For macOS the log file will be created in `{Mac HD}/users/{user}/logs`
4. **Sit Back and Enjoy**: Once the application has finished running, navigate to your Plex server to enjoy your newly organized movie collections!

## 🛑 Exclusions Configuration

In the `config.json` file, you have the option to specify collections that you want to exclude from the synchronization process. The collection name can be found in Radarr in the collections tab. Here's how to fill out the exclusions section:

```json
  "Exclusions": [
    {
      "CollectionName": "Harry Potter Collection"
    },
    {
      "CollectionName": "Star Wars Collection"
    }
  ]
```

## ℹ️ Additional Information

### Showing or Hiding Plex Collections

In Plex, you have the option to show or hide collections within a library. Here's how you can manage this:

1. **Log in to Plex**: Open the Plex Web App and log in to your Plex account.

2. **Access Library Settings**: Navigate to the library where you want to manage collections.

3. **Click on the Library**: Hover over the library in the sidebar and click on the ellipsis (`...`) icon that appears.

4. **Select Library Settings**: From the dropdown menu, select "Manage Library" then "Edit" (the exact wording may vary depending on the Plex version).

5. **Toggle Collection Visibility**: In the Library Settings under advanced, you'll find an option related to collections. Toggle the setting to either show or hide collections within this library.

6. **Save Changes**: Once you've adjusted the collection visibility setting to your preference, make sure to save your changes.

By managing collection visibility, you can customize the presentation of your library to suit your preferences and browsing habits.

## 🔍 Troubleshooting

Encountering issues? Here are some common problems and suggested solutions:

- **401 Unauthorized Error**: If you encounter a 401 Unauthorized error, double-check your API key for Radarr and Plex. Ensure that the API keys provided in the `config.json` file are correct and properly configured with the required permissions.

- **Application Not Running**: If the application fails to run, verify that the executable file (`RadarrToPlex` or `RadarrToPlex.exe`) has the appropriate permissions to execute. You may need to adjust the file permissions using the `chmod` command on Linux/macOS.

- **Empty Plex Collections**: If Plex collections are not created or appear empty, ensure that your Radarr library contains movies with associated collections. Collections will only be created in Plex for movies that are managed within Radarr.

- **Schedule Not Working**: If the scheduled task fails to run as expected, review the task scheduler settings to ensure that the task is configured correctly with the appropriate start time, frequency, and working directory.

If you continue to experience issues after troubleshooting, feel free to open an issue for further assistance.

## 📄 License

This project is licensed under the GNU General Public License v3.0. See the [LICENSE](LICENSE) file for details.

## 🤝 Contributing

Contributions are welcome from everyone! Whether you're a seasoned developer or simply have an idea to share!

### Contribution Guidelines:

1. Fork the repository.
2. Create a new branch for your feature or bug fix.
3. Make your changes.
4. Submit a pull request with a clear description of your changes and their purpose.
5. Your pull request will be reviewed by the maintainers, and feedback may be provided for further improvements.
6. Once approved, your changes will be merged into the main branch.

Happy contributing!

## 📧 Contact

Have questions or suggestions? Feel free to open an issue!
