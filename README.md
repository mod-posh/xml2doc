# C# Project

I have enclosed the files I use to build and deploy C# Projects. GitHub Workflows now manage all the project automation that used to be handled locally by Psake. This document outlines everything you will need to use these for your projects.

> [!Caution]
> Please make sure you modify the.CSPROJ files to suit your project needs; what is defined in this template is
> common to most of my projects and shouldn't be assumed to be required by yours.

## Workflows

Most of the content that used to be in the makefile and support JSON files has been moved into Github Action workflows. These workflows handle all the tasks previously handled by the makefile.

### Repository Secrets/Variables

You will need to define the following variables or secrets; these are referenced by one or more of the workflows listed below.

**Variables**

- DOTNET_VERSION - This is the version of Dotnet your application supports
- PROJECT_NAME - This is the name of your project
- PROJECT_NAMESPACE - This is the namespace of your project

**Secrets**

- BLUESKY_API_KEY - This is the Bluesky API key associated with your account
- BLUESKY_IDENTIFIER - This is your Bluesky identifier "myhandle. bsky.social"
- DISCORD_WEBHOOK - This is generated in the Discord channel you wish to announce the release
- GALLERY_API_KEY - The API key for PowerShell Gallery
- NUGET_API_KEY - The API key for nuget.org

### Merge Test Workflow

**Overview**

This workflow is triggered on pull requests targeting the `main` branch. It ensures code quality by running a series of automated tests.

**Steps**

1. **Checkout Repository** - Fetches the latest code from the repository.
2. **Setup .NET** - Installs the specified .NET version.
3. **Restore Dependencies** - Restores project dependencies using `dotnet restore.`
4. **Build** - Compiles the project in Release configuration.
5. **Run Tests** - Executes unit tests and generates a TRX test results file.
6. **Publish Test Results** - Upload the test results as an artifact for review.

**Execution Environment**

- Runs on `windows-latest`.
- Uses GitHub-hosted runners with `.NET` installed.

**Purpose**

Performs a full build and test cycle before merging into `main` to ensure that new changes do not introduce errors.

### Build and Package

**Overview**

This workflow is triggered on pushes to the `main` branch. It automates the process of building and packaging the project for deployment.

**Steps**

1. **Checkout Repository** - Fetches the latest code from the repository.
2. **Setup .NET** - Installs the specified .NET version.
3. **Restore Dependencies** - Restores project dependencies using `dotnet restore`.
4. **Clean** - Cleans the project build artifacts.
5. **Build** - Compiles the project in Release configuration.
6. **Package** - Creates a NuGet package (`.nupkg`) for distribution.

**Execution Environment**

- Runs on `windows-latest`.
- Uses GitHub-hosted runners with `.NET` installed.

**Purpose**

Automates the build and packaging process to ensure a clean, reproducible package is created for deployment or distribution.

### New Release

**Overview**

This workflow is triggered when a milestone is closed. It automates the creation of a new release, generation of release notes, construction of project documentation, and updating of the repository.

**Steps**

1. **Checkout Repository** - Fetches the latest code from the repository.
2. **Get Project Version** - Extract the current project version from the `.csproj` file.
3. **Create Release Notes** - Generates release notes from closed milestone issues.
4. **Pull Latest Changes** - Ensures the repository is current with the latest changes.
5. **Create GitHub Release** - Tags the new release with the extracted version.
6. **Install PowerShell & XMLDocMD Tool** - Prepares the environment for documentation generation.
7. **Build & Publish Project** - Compiles and prepares the project for release.
8DocumentationDocumentation** - Creates API documentation from XML comments.
9. **Lint Markdown Files** - Ensures all markdown files adhere to best practices.
10. **Commit Documentation Changes** - Updates and commits new documentation.
11. **Update README** - Uses a custom action to refresh the project README.

**Execution Environment**

- Runs on `ubuntu-latest`.
- Uses GitHub-hosted runners with installed `.NET`, PowerShell, and markdown linting tools.

**Purpose**

Automates the release process by tagging a new version and ensuring the latest project details are reflected in the repository.

### Announce New Release

**Overview**

This workflow is triggered after completing the **New Release** workflow. It announces the latest release by retrieving the newest GitHub tag and posting notifications to various platforms.

**Steps**

1. **Checkout Repository** - Fetches the latest code from the repository.
2. **Get Latest Tag** - Retrieves GitHub's most recent release tag.
3. **Post to Bluesky** - Publishes a release announcement to Bluesky.
4. **Post to Discord** - Sends a release notification to a designated Discord channel.

**Execution Environment**

- Runs on `ubuntu-latest`.
- It uses GitHub-hosted runners with `gh` CLI to retrieve release tags.

**Purpose**

Automatically announces new releases to external platforms, ensuring users are informed of the latest version and updates.
