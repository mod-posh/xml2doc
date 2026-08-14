[CmdletBinding()]
param(
  [ValidateSet("Debug", "Release")]
  [string] $Configuration = "Release",

  [string] $PackageDirectory,

  [string] $PackageVersion,

  [switch] $UseExistingPackage,

  [switch] $KeepArtifacts
)

$ErrorActionPreference = "Stop"

function Write-Step([string] $Message)
{
  Write-Host ""
  Write-Host "==> $Message"
}

function Assert-True($Condition, [string] $Message)
{
  if (-not $Condition) { throw $Message }
}

function Invoke-DotNet
{
  param(
    [Parameter(Mandatory = $true)][string[]] $Arguments,
    [Parameter(Mandatory = $true)][string] $WorkingDirectory
  )

  Write-Host "    dotnet $($Arguments -join ' ')"
  Push-Location -LiteralPath $WorkingDirectory
  try
  {
    & dotnet @Arguments 2>&1 | ForEach-Object { Write-Host $_ }
    $exitCode = $LASTEXITCODE
    if ($exitCode -ne 0)
    {
      throw "dotnet $($Arguments -join ' ') failed with exit code $exitCode."
    }
  }
  finally
  {
    Pop-Location
  }
}

$repo = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$propsPath = Join-Path $repo "Directory.Build.props"
$taskProject = Join-Path $repo "Xml2Doc/src/Xml2Doc.MSBuild/Xml2Doc.MSBuild.csproj"

[xml] $props = Get-Content -LiteralPath $propsPath -Raw
$versionPrefix = $props.SelectSingleNode("/Project/PropertyGroup/VersionPrefix").InnerText
Assert-True (-not [string]::IsNullOrWhiteSpace($versionPrefix)) "VersionPrefix was not found in Directory.Build.props."

if ([string]::IsNullOrWhiteSpace($PackageVersion))
{
  $PackageVersion = "$versionPrefix-package-test"
}

$runRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("xml2doc-package-test-" + [guid]::NewGuid().ToString("n"))
$consumerRoot = Join-Path $runRoot "consumer"

if ([string]::IsNullOrWhiteSpace($PackageDirectory))
{
  $PackageDirectory = Join-Path $runRoot "packages"
}
elseif (-not [System.IO.Path]::IsPathRooted($PackageDirectory))
{
  $PackageDirectory = Join-Path $repo $PackageDirectory
}

New-Item -ItemType Directory -Force -Path $PackageDirectory | Out-Null
New-Item -ItemType Directory -Force -Path $consumerRoot | Out-Null

try
{
  if (-not $UseExistingPackage)
  {
    Write-Step "Building Xml2Doc.MSBuild and its project dependencies"
    Invoke-DotNet -WorkingDirectory $repo -Arguments @(
      "build", $taskProject,
      "--configuration", $Configuration,
      "--disable-build-servers"
    )

    Write-Step "Packing with the release workflow's --no-build sequence"
    Invoke-DotNet -WorkingDirectory $repo -Arguments @(
      "pack", $taskProject,
      "--configuration", $Configuration,
      "--output", $PackageDirectory,
      "--no-build",
      "-p:PackageVersion=$PackageVersion"
    )
  }

  $packagePath = Join-Path $PackageDirectory "Xml2Doc.MSBuild.$PackageVersion.nupkg"
  Assert-True (Test-Path -LiteralPath $packagePath -PathType Leaf) "Expected package was not found: $packagePath"

  Write-Step "Inspecting package contents"
  try
  {
    Add-Type -AssemblyName System.IO.Compression.ZipFile -ErrorAction Stop
  }
  catch
  {
    # Windows PowerShell on .NET Framework exposes ZipFile through this assembly.
    Add-Type -AssemblyName System.IO.Compression.FileSystem
  }
  $archive = [System.IO.Compression.ZipFile]::OpenRead($packagePath)
  try
  {
    $entries = @($archive.Entries | ForEach-Object { $_.FullName -replace '\\', '/' })
  }
  finally
  {
    $archive.Dispose()
  }

  $requiredEntries = @(
    "lib/net472/Xml2Doc.MSBuild.dll",
    "lib/net472/Xml2Doc.Core.dll",
    "lib/net472/System.Text.Json.dll",
    "lib/net8.0/Xml2Doc.MSBuild.dll",
    "lib/net8.0/Xml2Doc.Core.dll"
  )

  foreach ($entry in $requiredEntries)
  {
    Assert-True ($entries -contains $entry) "Package is missing required entry '$entry'."
    Write-Host "    Found: $entry"
  }

  $forbiddenEntries = @(
    "lib/net472/Microsoft.Build.Framework.dll",
    "lib/net472/Microsoft.Build.Utilities.Core.dll",
    "lib/net8.0/Microsoft.Build.Framework.dll",
    "lib/net8.0/Microsoft.Build.Utilities.Core.dll"
  )

  foreach ($entry in $forbiddenEntries)
  {
    Assert-True ($entries -notcontains $entry) "Package must not contain MSBuild-owned assembly '$entry'."
  }

  Write-Step "Creating clean net9.0 consumer"
  $consumerProject = @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <Xml2Doc_Enabled>true</Xml2Doc_Enabled>
    <Xml2Doc_OutputDir>$(MSBuildProjectDirectory)/docs</Xml2Doc_OutputDir>
    <Xml2Doc_ReportPath>$(MSBuildProjectDirectory)/docs/xml2doc-report.json</Xml2Doc_ReportPath>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Xml2Doc.MSBuild" Version="__PACKAGE_VERSION__" PrivateAssets="all" />
  </ItemGroup>
</Project>
'@.Replace("__PACKAGE_VERSION__", $PackageVersion)

  $consumerSource = @'
namespace PackageConsumer;

/// <summary>Type used to prove the packaged MSBuild task can load and execute.</summary>
public sealed class Example
{
    /// <summary>Returns a deterministic value.</summary>
    public int GetValue() => 42;
}
'@

  $nugetConfig = @'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="__PACKAGE_DIRECTORY__" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
'@.Replace("__PACKAGE_DIRECTORY__", [System.Security.SecurityElement]::Escape($PackageDirectory))

  Set-Content -LiteralPath (Join-Path $consumerRoot "PackageConsumer.csproj") -Value $consumerProject -Encoding utf8
  Set-Content -LiteralPath (Join-Path $consumerRoot "Example.cs") -Value $consumerSource -Encoding utf8
  Set-Content -LiteralPath (Join-Path $consumerRoot "NuGet.Config") -Value $nugetConfig -Encoding utf8

  Write-Step "Restoring clean consumer from the local package"
  Invoke-DotNet -WorkingDirectory $consumerRoot -Arguments @(
    "restore", "PackageConsumer.csproj",
    "--configfile", "NuGet.Config"
  )

  Write-Step "Building clean consumer"
  Invoke-DotNet -WorkingDirectory $consumerRoot -Arguments @(
    "build", "PackageConsumer.csproj",
    "--configuration", $Configuration,
    "--no-restore",
    "--disable-build-servers"
  )

  $generatedIndex = Join-Path $consumerRoot "docs/index.md"
  $generatedReport = Join-Path $consumerRoot "docs/xml2doc-report.json"
  Assert-True (Test-Path -LiteralPath $generatedIndex -PathType Leaf) "Clean consumer did not generate docs/index.md."
  Assert-True (Test-Path -LiteralPath $generatedReport -PathType Leaf) "Clean consumer did not generate xml2doc-report.json."

  Write-Step "MSBuild package integration completed successfully"
  Write-Host "Package: $packagePath"
}
finally
{
  if ($KeepArtifacts)
  {
    Write-Host "Keeping test artifacts: $runRoot"
  }
  elseif (Test-Path -LiteralPath $runRoot)
  {
    Remove-Item -LiteralPath $runRoot -Recurse -Force
  }
}
