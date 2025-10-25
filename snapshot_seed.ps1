dotnet build .\Xml2Doc\tests\Xml2Doc.Sample -c Release

$xml = Resolve-Path .\Xml2Doc\tests\Xml2Doc.Sample\bin\Release\net9.0\Xml2Doc.Sample.xml
$out = Join-Path $env:TEMP ("xml2doc-" + [Guid]::NewGuid())
New-Item -ItemType Directory -Path $out | Out-Null

dotnet run --project .\Xml2Doc\src\Xml2Doc.Cli -- --xml $xml --out $out --file-names clean --rootns Xml2Doc.Sample --lang csharp

New-Item -ItemType Directory -Force -Path .\Xml2Doc\tests\Xml2Doc.Tests\__snapshots__\PerType_CleanNames | Out-Null
Copy-Item (Join-Path $out index.md)                                  .\Xml2Doc\tests\Xml2Doc.Tests\__snapshots__\PerType_CleanNames\index.verified.md -Force
Copy-Item (Join-Path $out Xml2Doc.Sample.GenericPlayground.md)       .\Xml2Doc\tests\Xml2Doc.Tests\__snapshots__\PerType_CleanNames\Xml2Doc.Sample.GenericPlayground.verified.md -Force
Copy-Item (Join-Path $out Xml2Doc.Sample.Mathx.md)                   .\Xml2Doc\tests\Xml2Doc.Tests\__snapshots__\PerType_CleanNames\Xml2Doc.Sample.Mathx.verified.md -Force
Copy-Item (Join-Path $out Xml2Doc.Sample.XItem.md)                   .\Xml2Doc\tests\Xml2Doc.Tests\__snapshots__\PerType_CleanNames\Xml2Doc.Sample.XItem.verified.md -Force
