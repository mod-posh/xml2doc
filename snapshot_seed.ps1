param(
 [string] $Configuration = "Release",
 [string] $CliTFM = "net8.0",   # CLI runs on net8.0
 [string] $SampleTFM = "net9.0"    # Sample builds on net9.0
)

$ErrorActionPreference = "Stop"

# 1) Build the sample assembly that will produce XML doc
dotnet build "Xml2Doc\tests\Xml2Doc.Sample\Xml2Doc.Sample.csproj" -c $Configuration -v minimal

# 2) Locate the XML doc produced by the sample
$xml = "Xml2Doc\tests\Xml2Doc.Sample\bin\$Configuration\$SampleTFM\Xml2Doc.Sample.xml"
if (!(Test-Path $xml)) { throw "XML doc not found at $xml" }

# 3) Create a temp output folder
$out = Join-Path $env:TEMP ("xml2doc-" + [guid]::NewGuid().ToString())
New-Item -ItemType Directory -Path $out | Out-Null

# 4) Run the CLI to render docs to a directory
.\Xml2Doc\src\Xml2Doc.Cli\bin\Release\net9.0\Xml2Doc.Cli.exe `
 --xml $xml `
 --out $out

# 5) Copy a stable subset to snapshots
$dest = "Xml2Doc\tests\Xml2Doc.Tests\__snapshots__"
New-Item -ItemType Directory -Force -Path $dest | Out-Null

Copy-Item (Join-Path $out "index.md")                                 -Destination (Join-Path $dest "index.md") -Force
Copy-Item (Join-Path $out "Xml2Doc.Sample.GenericPlayground.md")      -Destination (Join-Path $dest "Xml2Doc.Sample.GenericPlayground.md") -Force
Copy-Item (Join-Path $out "Xml2Doc.Sample.Mathx.md")                  -Destination (Join-Path $dest "Xml2Doc.Sample.Mathx.md") -Force
Copy-Item (Join-Path $out "Xml2Doc.Sample.XItem.md")                  -Destination (Join-Path $dest "Xml2Doc.Sample.XItem.md") -Force
Copy-Item (Join-Path $out "Xml2Doc.Sample.AliasingPlayground.md")     -Destination (Join-Path $dest "Xml2Doc.Sample.AliasingPlayground.md") -Force

Write-Host "Snapshots updated from $xml into $dest"

remove-item $out -recurse -force