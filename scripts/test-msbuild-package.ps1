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

function Get-Xml2DocStatePaths
{
  param(
    [Parameter(Mandatory = $true)][string] $ProjectRoot,
    [Parameter(Mandatory = $true)][string] $Configuration,
    [Parameter(Mandatory = $true)][string[]] $FileNames
  )

  $configurationRoot = Join-Path $ProjectRoot "obj/$Configuration"
  if (-not (Test-Path -LiteralPath $configurationRoot -PathType Container))
  {
    return @()
  }

  @(
    Get-ChildItem -LiteralPath $configurationRoot -Recurse -File |
      Where-Object { $FileNames -contains $_.Name } |
      Sort-Object FullName |
      Select-Object -ExpandProperty FullName
  )
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
    "lib/net8.0/Xml2Doc.Core.dll",
    "build/Xml2Doc.MSBuild.props",
    "build/Xml2Doc.MSBuild.targets",
    "build/assets/Xml2Doc.MSBuild.Aggregation.targets"
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
  $generatedType = Join-Path $consumerRoot "docs/PackageConsumer.Example.md"
  Assert-True (Test-Path -LiteralPath $generatedIndex -PathType Leaf) "Clean consumer did not generate docs/index.md."
  Assert-True (Test-Path -LiteralPath $generatedReport -PathType Leaf) "Clean consumer did not generate xml2doc-report.json."
  Assert-True (Test-Path -LiteralPath $generatedType -PathType Leaf) "Clean consumer did not generate the expected type page."

  $stateFileNames = @(
    "xml2doc.stamp",
    "xml2doc.fingerprint.txt",
    "xml2doc.outputs.txt"
  )
  $activeStatePaths = @(Get-Xml2DocStatePaths -ProjectRoot $consumerRoot -Configuration $Configuration -FileNames $stateFileNames)
  Assert-True ($activeStatePaths.Count -eq $stateFileNames.Count) "Expected exactly one active-configuration file for each Xml2Doc state artifact."
  foreach ($statePath in $activeStatePaths)
  {
    Assert-True (Test-Path -LiteralPath $statePath -PathType Leaf) "Expected Xml2Doc state was not generated: $statePath"
  }

  $alternateConfiguration = if ($Configuration -eq "Release") { "Debug" } else { "Release" }
  Write-Step "Building clean consumer in $alternateConfiguration to verify configuration-scoped cleaning"
  Invoke-DotNet -WorkingDirectory $consumerRoot -Arguments @(
    "build", "PackageConsumer.csproj",
    "--configuration", $alternateConfiguration,
    "--no-restore",
    "--disable-build-servers"
  )

  $alternateStatePaths = @(Get-Xml2DocStatePaths -ProjectRoot $consumerRoot -Configuration $alternateConfiguration -FileNames $stateFileNames)
  Assert-True ($alternateStatePaths.Count -eq $stateFileNames.Count) "Expected exactly one alternate-configuration file for each Xml2Doc state artifact."
  foreach ($statePath in $alternateStatePaths)
  {
    Assert-True (Test-Path -LiteralPath $statePath -PathType Leaf) "Expected alternate-configuration Xml2Doc state was not generated: $statePath"
  }

  $updatedConsumerSource = $consumerSource.Replace(
    "Returns a deterministic value.",
    "Returns an updated deterministic value.")
  Set-Content -LiteralPath (Join-Path $consumerRoot "Example.cs") -Value $updatedConsumerSource -Encoding utf8

  Write-Step "Cleaning only the active consumer configuration"
  Invoke-DotNet -WorkingDirectory $consumerRoot -Arguments @(
    "clean", "PackageConsumer.csproj",
    "--configuration", $Configuration,
    "--disable-build-servers"
  )

  foreach ($statePath in $activeStatePaths)
  {
    Assert-True (-not (Test-Path -LiteralPath $statePath)) "Clean left active-configuration Xml2Doc state behind: $statePath"
  }
  foreach ($statePath in $alternateStatePaths)
  {
    Assert-True (Test-Path -LiteralPath $statePath -PathType Leaf) "Clean removed another configuration's Xml2Doc state: $statePath"
  }
  Assert-True (Test-Path -LiteralPath $generatedIndex -PathType Leaf) "Clean deleted generated docs/index.md."
  Assert-True (Test-Path -LiteralPath $generatedReport -PathType Leaf) "Clean deleted the generated report."
  Assert-True (Test-Path -LiteralPath $generatedType -PathType Leaf) "Clean deleted the generated type page."

  Write-Step "Rebuilding after Clean to verify documentation regeneration"
  Invoke-DotNet -WorkingDirectory $consumerRoot -Arguments @(
    "build", "PackageConsumer.csproj",
    "--configuration", $Configuration,
    "--no-restore",
    "--disable-build-servers"
  )

  $updatedMarkdown = Get-Content -LiteralPath $generatedType -Raw
  Assert-True ($updatedMarkdown.Contains("Returns an updated deterministic value.")) "Build after Clean did not regenerate the changed XML documentation."
  foreach ($statePath in $activeStatePaths)
  {
    Assert-True (Test-Path -LiteralPath $statePath -PathType Leaf) "Build after Clean did not recreate Xml2Doc state: $statePath"
  }

  $activeStamp = $activeStatePaths | Where-Object { [System.IO.Path]::GetFileName($_) -eq "xml2doc.stamp" } | Select-Object -First 1
  $stampBeforeNoOp = (Get-Item -LiteralPath $activeStamp).LastWriteTimeUtc
  Start-Sleep -Milliseconds 1100

  Write-Step "Rebuilding unchanged consumer to preserve incremental no-op behavior"
  Invoke-DotNet -WorkingDirectory $consumerRoot -Arguments @(
    "build", "PackageConsumer.csproj",
    "--configuration", $Configuration,
    "--no-restore",
    "--disable-build-servers"
  )

  $stampAfterNoOp = (Get-Item -LiteralPath $activeStamp).LastWriteTimeUtc
  Assert-True ($stampAfterNoOp -eq $stampBeforeNoOp) "Unchanged build rewrote the Xml2Doc stamp after clean-state regeneration."

  Write-Step "Creating clean packaged aggregation consumer"
  $aggregateRoot = Join-Path $runRoot "aggregate-consumer"
  $aggregateChildRoot = Join-Path $aggregateRoot "Child"
  $aggregateOwnerRoot = Join-Path $aggregateRoot "Owner"
  New-Item -ItemType Directory -Force -Path $aggregateChildRoot | Out-Null
  New-Item -ItemType Directory -Force -Path $aggregateOwnerRoot | Out-Null

  $aggregateChildProject = @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <Xml2Doc_Enabled>true</Xml2Doc_Enabled>
    <Xml2Doc_OutputDir>$(MSBuildProjectDirectory)/docs</Xml2Doc_OutputDir>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Xml2Doc.MSBuild" Version="__PACKAGE_VERSION__" PrivateAssets="all" />
  </ItemGroup>
</Project>
'@.Replace("__PACKAGE_VERSION__", $PackageVersion)

  $aggregateChildSource = @'
namespace PackageAggregateChild;

/// <summary>Type used to prove packaged repository aggregation can load and execute.</summary>
public sealed class Widget
{
    /// <summary>Returns a deterministic value.</summary>
    public int GetValue() => 7;
}
'@

  $aggregateOwnerProject = @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
    <Xml2Doc_Enabled>false</Xml2Doc_Enabled>
    <Xml2Doc_AggregateEnabled>true</Xml2Doc_AggregateEnabled>
    <Xml2Doc_OutputDir>$(MSBuildProjectDirectory)/docs</Xml2Doc_OutputDir>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="../Child/Child.csproj" />
    <PackageReference Include="Xml2Doc.MSBuild" Version="__PACKAGE_VERSION__" PrivateAssets="all" />
  </ItemGroup>
</Project>
'@.Replace("__PACKAGE_VERSION__", $PackageVersion)

  Set-Content -LiteralPath (Join-Path $aggregateChildRoot "Child.csproj") -Value $aggregateChildProject -Encoding utf8
  Set-Content -LiteralPath (Join-Path $aggregateChildRoot "Widget.cs") -Value $aggregateChildSource -Encoding utf8
  Set-Content -LiteralPath (Join-Path $aggregateOwnerRoot "Owner.csproj") -Value $aggregateOwnerProject -Encoding utf8
  Set-Content -LiteralPath (Join-Path $aggregateRoot "NuGet.Config") -Value $nugetConfig -Encoding utf8

  Write-Step "Restoring packaged aggregation consumer"
  Invoke-DotNet -WorkingDirectory $aggregateRoot -Arguments @(
    "restore", "Owner/Owner.csproj",
    "--configfile", "NuGet.Config"
  )

  Write-Step "Building packaged aggregation consumer"
  Invoke-DotNet -WorkingDirectory $aggregateRoot -Arguments @(
    "build", "Owner/Owner.csproj",
    "--configuration", $Configuration,
    "--no-restore",
    "--disable-build-servers"
  )

  $aggregateIndex = Join-Path $aggregateOwnerRoot "docs/index.md"
  $aggregateReport = Join-Path $aggregateOwnerRoot "docs/xml2doc-aggregate-report.json"
  Assert-True (Test-Path -LiteralPath $aggregateIndex -PathType Leaf) "Packaged aggregation consumer did not generate docs/index.md."
  Assert-True (Test-Path -LiteralPath $aggregateReport -PathType Leaf) "Packaged aggregation consumer did not generate xml2doc-aggregate-report.json."
  $aggregateIndexText = Get-Content -LiteralPath $aggregateIndex -Raw
  Assert-True ($aggregateIndexText.Contains("PackageAggregateChild.Widget")) "Packaged aggregation output did not contain the referenced child type."

  $aggregateStateFileNames = @(
    "xml2doc.aggregate.stamp",
    "xml2doc.aggregate.fingerprint.txt",
    "xml2doc.aggregate.outputs.txt"
  )
  $aggregateStatePaths = @(Get-Xml2DocStatePaths -ProjectRoot $aggregateOwnerRoot -Configuration $Configuration -FileNames $aggregateStateFileNames)
  Assert-True ($aggregateStatePaths.Count -eq $aggregateStateFileNames.Count) "Expected exactly one file for each aggregate Xml2Doc state artifact."
  foreach ($statePath in $aggregateStatePaths)
  {
    Assert-True (Test-Path -LiteralPath $statePath -PathType Leaf) "Expected aggregate Xml2Doc state was not generated: $statePath"
  }

  $aggregateChildStatePaths = @(Get-Xml2DocStatePaths -ProjectRoot $aggregateChildRoot -Configuration $Configuration -FileNames $stateFileNames)
  Assert-True ($aggregateChildStatePaths.Count -eq $stateFileNames.Count) "Expected the aggregate child to own an independent per-project state set."
  $aggregateChildIndex = Join-Path $aggregateChildRoot "docs/index.md"
  Assert-True (Test-Path -LiteralPath $aggregateChildIndex -PathType Leaf) "Aggregate child did not generate its independent documentation."

  Write-Step "Cleaning only the aggregate child project"
  Invoke-DotNet -WorkingDirectory $aggregateRoot -Arguments @(
    "clean", "Child/Child.csproj",
    "--configuration", $Configuration,
    "--disable-build-servers"
  )

  foreach ($statePath in $aggregateChildStatePaths)
  {
    Assert-True (-not (Test-Path -LiteralPath $statePath)) "Child clean left its per-project Xml2Doc state behind: $statePath"
  }
  foreach ($statePath in $aggregateStatePaths)
  {
    Assert-True (Test-Path -LiteralPath $statePath -PathType Leaf) "Child clean removed the owner's aggregate Xml2Doc state: $statePath"
  }
  Assert-True (Test-Path -LiteralPath $aggregateChildIndex -PathType Leaf) "Child clean deleted its generated Markdown."
  Assert-True (Test-Path -LiteralPath $aggregateIndex -PathType Leaf) "Child clean deleted the aggregate index."

  Write-Step "Cleaning packaged aggregation owner"
  Invoke-DotNet -WorkingDirectory $aggregateRoot -Arguments @(
    "clean", "Owner/Owner.csproj",
    "--configuration", $Configuration,
    "--disable-build-servers"
  )

  foreach ($statePath in $aggregateStatePaths)
  {
    Assert-True (-not (Test-Path -LiteralPath $statePath)) "Clean left aggregate Xml2Doc state behind: $statePath"
  }
  Assert-True (Test-Path -LiteralPath $aggregateIndex -PathType Leaf) "Clean deleted the aggregate index."
  Assert-True (Test-Path -LiteralPath $aggregateReport -PathType Leaf) "Clean deleted the aggregate report."

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
