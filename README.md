# MultiDownloader
Files downloader for multiple protocols (currently supports FTP, HTTP, HTTPS).

## Getting Started
Follow these instructions to get this project up and running on your machine.

### Prerequisites
Microsoft Visual Studio Community 2019 16.3 with .NET Core 3.0 SDK.
```
Double click MultiDownloader.sln to open Visual Studio Solution.
```

### Configuration
Open Settings.json to modify the following settings:
* DownloadLocation - Location downloaded files will be saved to
* DefaultConnectionLimit - Maximum number of concurrent connections
* SegmentsPerFile - Splits file into number of segments to download concurrently (HTTP/HTTPS only)
* TimeoutSeconds - Seconds to connection timeout
* TimeoutRetries - Number of timeout retries
* LinearBackoffInterval - Linearly decrease the rate of retries
* URIs - List of files' URIs to download

### Documentation Generation
Download and install [Doxygen](http://www.doxygen.nl/download.html). After install, run Doxywizard and follow the wizard to generate documentation. HTML documentation is available [here](https://joelchen.github.io/MultiDownloader).

### Running MultiDownloader
Right click MultiDownloader project in Solution Explorer and select **Set as StartUp Project**, and click **Start** button to run with Settings.json configuration.

### Running MultiDownloaderUnitTests
Open Test Explorer from Test > Windows > Test Explorer, and click **Run All** to run all xUnit.net tests.

### Generate Standalone Executable
In Terminal or PowerShell, cd into MultiDownloader from root folder, and execute the following to generate standalone executable for deployment:

Windows 64-bit: `dotnet publish -r win-x64 -c Release /p:PublishSingleFile=true /p:PublishTrimmed=true`

Linux 64-bit: `dotnet publish -r linux-x64 -c Release /p:PublishSingleFile=true /p:PublishTrimmed=true`

macOS 64-bit: `dotnet publish -r osx-x64 -c Release /p:PublishSingleFile=true /p:PublishTrimmed=true`

You may download the latest releases [here](https://github.com/joelchen/MultiDownloader/releases/latest).
