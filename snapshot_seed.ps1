# Build sample
dotnet build .\Xml2Doc\tests\Xml2Doc.Sample -c Release

# Single-file
$xml = Resolve-Path .\Xml2Doc\tests\Xml2Doc.Sample\bin\Release\net9.0\Xml2Doc.Sample.xml
$tmp = [System.IO.Path]::GetTempFileName()
dotnet run --project .\Xml2Doc\src\Xml2Doc.Cli -- --xml $xml --out $tmp --single --file-names clean --rootns Xml2Doc.Sample --lang csharp
Copy-Item $tmp .\Xml2Doc\tests\Xml2Doc.Tests\__snapshots__\SingleFile_CleanNames_Basic.verified.md -Force

# Per-type
$out = Join-Path $env:TEMP ("xml2doc-" + [Guid]::NewGuid())
New-Item -ItemType Directory -Path $out | Out-Null
dotnet run --project .\Xml2Doc\src\Xml2Doc.Cli -- --xml $xml --out $out --file-names clean --rootns Xml2Doc.Sample --lang csharp
Copy-Item (Join-Path $out index.md) .\Xml2Doc\tests\Xml2Doc.Tests\__snapshots__\PerType_CleanNames\index.verified.md -Force
Copy-Item (Join-Path $out Xml2Doc.Sample.Mathx.md) .\Xml2Doc\tests\Xml2Doc.Tests\__snapshots__\PerType_CleanNames\Xml2Doc.Sample.Mathx.verified.md -Force
