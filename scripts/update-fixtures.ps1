param(
  [string] $Configuration = "Release",
  [string] $SampleTFM = "net9.0"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$sampleProj = Join-Path $repoRoot "Xml2Doc\tests\Xml2Doc.Sample\Xml2Doc.Sample.csproj"
$assetsDir  = Join-Path $repoRoot "Xml2Doc\tests\Xml2Doc.Tests\Assets"
$destXml    = Join-Path $assetsDir "Xml2Doc.Sample.xml"

Write-Host "Building sample: $sampleProj"
dotnet build $sampleProj -c $Configuration -v minimal
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed." }

$xml = Join-Path $repoRoot "Xml2Doc\tests\Xml2Doc.Sample\bin\$Configuration\$SampleTFM\Xml2Doc.Sample.xml"
if (!(Test-Path $xml)) { throw "Sample XML not found at: $xml" }

New-Item -ItemType Directory -Force -Path $assetsDir | Out-Null
Copy-Item $xml -Destination $destXml -Force

Write-Host "Updated fixture XML:"
Write-Host " - $destXml"