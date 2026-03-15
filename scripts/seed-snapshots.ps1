param(
  [string] $Configuration = "Release",
  [string] $CliTFM = "net9.0",
  [string] $SnapshotSet = "PerType_CleanNames"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot

$xml = Join-Path $repoRoot "Xml2Doc\tests\Xml2Doc.Tests\Assets\Xml2Doc.Sample.xml"
if (!(Test-Path $xml)) {
  throw "Fixture XML missing at: $xml`nRun scripts/update-fixtures.ps1 first."
}

# Build CLI (so we always seed with the current renderer behavior)
$cliProj = Join-Path $repoRoot "Xml2Doc\src\Xml2Doc.Cli\Xml2Doc.Cli.csproj"
Write-Host "Building CLI: $cliProj ($Configuration/$CliTFM)"
dotnet build $cliProj -c $Configuration -f $CliTFM -v minimal
if ($LASTEXITCODE -ne 0) { throw "dotnet build CLI failed." }

$cliExe = Join-Path $repoRoot "Xml2Doc\src\Xml2Doc.Cli\bin\$Configuration\$CliTFM\Xml2Doc.Cli.exe"
$cliDll = Join-Path $repoRoot "Xml2Doc\src\Xml2Doc.Cli\bin\$Configuration\$CliTFM\Xml2Doc.Cli.dll"

$useExe = Test-Path $cliExe
$useDll = Test-Path $cliDll
if (-not $useExe -and -not $useDll) { throw "CLI not found at exe or dll path." }

$out = Join-Path $env:TEMP ("xml2doc-seed-" + [guid]::NewGuid().ToString("n"))
New-Item -ItemType Directory -Force -Path $out | Out-Null

Write-Host "Generating markdown from fixture XML..."
if ($useExe) {
  & $cliExe --xml $xml --out $out --file-names clean
  if ($LASTEXITCODE -ne 0) { throw "CLI failed." }
} else {
  & dotnet $cliDll --xml $xml --out $out --file-names clean
  if ($LASTEXITCODE -ne 0) { throw "CLI dll run failed." }
}

$snapDir = Join-Path $repoRoot "Xml2Doc\tests\Xml2Doc.Tests\__snapshots__\$SnapshotSet"
New-Item -ItemType Directory -Force -Path $snapDir | Out-Null

Get-ChildItem -Path $snapDir -Filter "*.verified.md" -ErrorAction SilentlyContinue | Remove-Item -Force

Get-ChildItem -Path $out -Filter "*.md" -File | ForEach-Object {
  $dest = Join-Path $snapDir ($_.BaseName + ".verified.md")
  Copy-Item $_.FullName -Destination $dest -Force
}

Remove-Item $out -Recurse -Force

Write-Host "Snapshots updated:"
Write-Host " - $snapDir"