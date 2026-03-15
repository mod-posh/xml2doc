[CmdletBinding()]
param(
 [ValidateSet('Debug', 'Release')]
 [string] $Configuration = 'Release',

 [switch] $KeepArtifacts
)

$ErrorActionPreference = 'Stop'

function RepoRoot
{
 (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
}

function Write-Step([string] $Message)
{
 Write-Host ""
 Write-Host "==> $Message"
}

function Write-Detail([string] $Message)
{
 Write-Host "    $Message"
}

function Assert-True([bool]$cond, [string]$message)
{
 if (-not $cond) { throw $message }
}

function Run-Capture
{
 param(
  [Parameter(Mandatory = $true)][string] $FileName,
  [Parameter(Mandatory = $true)][string] $Arguments,
  [Parameter(Mandatory = $true)][string] $WorkingDirectory
 )

 $psi = [System.Diagnostics.ProcessStartInfo]::new()
 $psi.FileName = $FileName
 $psi.Arguments = $Arguments
 $psi.WorkingDirectory = $WorkingDirectory
 $psi.UseShellExecute = $false
 $psi.RedirectStandardOutput = $true
 $psi.RedirectStandardError = $true
 $psi.CreateNoWindow = $true

 $p = [System.Diagnostics.Process]::new()
 $p.StartInfo = $psi
 [void]$p.Start()

 $stdout = $p.StandardOutput.ReadToEnd()
 $stderr = $p.StandardError.ReadToEnd()
 $p.WaitForExit()

 [pscustomobject]@{
  ExitCode = $p.ExitCode
  StdOut   = $stdout
  StdErr   = $stderr
 }
}

function Run-OrThrow
{
 param(
  [Parameter(Mandatory = $true)][string] $File,
  [Parameter(Mandatory = $true)][string] $Args,
  [Parameter(Mandatory = $true)][string] $Cwd,
  [string] $ErrorPrefix = 'Command failed'
 )

 Write-Detail "CMD: $File $Args"

 $r = Run-Capture -FileName $File -Arguments $Args -WorkingDirectory $Cwd
 if ($r.ExitCode -ne 0)
 {
  throw "$ErrorPrefix`n`nCMD: $File $Args`n`nSTDOUT:`n$($r.StdOut)`n`nSTDERR:`n$($r.StdErr)"
 }

 if ($r.StdOut)
 {
  Write-Detail "STDOUT:"
  $r.StdOut.TrimEnd().Split("`n") | ForEach-Object {
   Write-Host "      $_".TrimEnd()
  }
 }

 if ($r.StdErr)
 {
  Write-Detail "STDERR:"
  $r.StdErr.TrimEnd().Split("`n") | ForEach-Object {
   Write-Host "      $_".TrimEnd()
  }
 }

 return $r
}

function Normalize([string]$s)
{
 if ($null -eq $s) { return '' }
 $s = $s.Replace("`r`n", "`n").Replace("`r", "`n")
 $lines = $s.Split("`n") | ForEach-Object { $_.TrimEnd() }
 return ($lines -join "`n").TrimEnd()
}

function Get-Tree([string] $root)
{
 if (-not (Test-Path $root)) { return '<missing>' }
 $files = Get-ChildItem -Path $root -Recurse -File -ErrorAction SilentlyContinue |
 Sort-Object FullName |
 ForEach-Object { $_.FullName.Substring($root.Length).TrimStart('\\', '/') -replace '\\', '/' }

 if (-not $files) { return '<empty>' }
 return ($files -join [Environment]::NewLine)
}

function Get-ExpectedSnapshotFiles([string] $snapDir)
{
 Assert-True (Test-Path $snapDir) "Snapshot directory missing: $snapDir`nRun scripts/seed-snapshots.ps1 and commit the results."

 Get-ChildItem -Path $snapDir -Filter '*.verified.md' -File |
 ForEach-Object { $_.Name.Replace('.verified.md', '.md') } |
 Sort-Object
}

function Assert-FileSet([string] $outDir, [string[]] $expected)
{
 $actual = Get-ChildItem -Path $outDir -Filter '*.md' -File |
 ForEach-Object { $_.Name } |
 Sort-Object

 $a = $actual -join "`n"
 $e = $expected -join "`n"
 Assert-True ($a -eq $e) "Output file set differs from snapshots.`n`nExpected:`n$e`n`nActual:`n$a"
}

function Assert-MatchesSnapshots([string] $outDir, [string] $snapDir, [string[]] $expectedFiles)
{
 foreach ($f in $expectedFiles)
 {
  $actualPath = Join-Path $outDir $f
  $expectedPath = Join-Path $snapDir ($f.Replace('.md', '.verified.md'))

  Assert-True (Test-Path $actualPath) "Missing output file: $actualPath"
  Assert-True (Test-Path $expectedPath) "Missing snapshot file: $expectedPath"

  $actual = Normalize (Get-Content $actualPath -Raw)
  $expected = Normalize (Get-Content $expectedPath -Raw)

  if ($actual -ne $expected)
  {
   $hint = "Mismatch in $f`n`nTip: run scripts/seed-snapshots.ps1 and commit updated snapshots."
   throw $hint
  }
 }
}

function Assert-DirsEqual([string] $aDir, [string] $bDir, [string[]] $files)
{
 foreach ($f in $files)
 {
  $aPath = Join-Path $aDir $f
  $bPath = Join-Path $bDir $f

  Assert-True (Test-Path $aPath) "Missing: $aPath"
  Assert-True (Test-Path $bPath) "Missing: $bPath"

  $a = Normalize (Get-Content $aPath -Raw)
  $b = Normalize (Get-Content $bPath -Raw)

  if ($a -ne $b)
  {
   throw "Cross-TFM mismatch in $f (net8 != net9)."
  }
 }
}

function Extract-Anchors([string] $mdPath)
{
 $rx = [regex]::new('<a id="([^"]+)"')
 $lines = Get-Content $mdPath
 $ids = New-Object System.Collections.Generic.HashSet[string]
 foreach ($line in $lines)
 {
  foreach ($m in $rx.Matches($line))
  {
   [void]$ids.Add($m.Groups[1].Value)
  }
 }
 [string[]]$ids | Sort-Object
}

# --------------------

Write-Step "Resolving repository paths"

$repo = RepoRoot

$sampleProj = Join-Path $repo 'Xml2Doc\tests\Xml2Doc.Sample\Xml2Doc.Sample.csproj'
$cliProj = Join-Path $repo 'Xml2Doc\src\Xml2Doc.Cli\Xml2Doc.Cli.csproj'

$fixtureXml = Join-Path $repo 'Xml2Doc\tests\Xml2Doc.Tests\Assets\Xml2Doc.Sample.xml'
$snapDir = Join-Path $repo 'Xml2Doc\tests\Xml2Doc.Tests\__snapshots__\PerType_CleanNames'

Write-Detail "Repo root: $repo"
Write-Detail "Sample project: $sampleProj"
Write-Detail "CLI project: $cliProj"
Write-Detail "Fixture XML: $fixtureXml"
Write-Detail "Snapshot dir: $snapDir"

Assert-True (Test-Path $sampleProj) "Sample project not found: $sampleProj"
Assert-True (Test-Path $cliProj) "CLI project not found: $cliProj"
Assert-True (Test-Path $fixtureXml) "Fixture XML missing: $fixtureXml"

$runId = [guid]::NewGuid().ToString('n')
$runRoot = Join-Path $repo "Xml2Doc\tests\Xml2Doc.Sample\obj\$Configuration\cli-it\$runId"
New-Item -ItemType Directory -Force -Path $runRoot | Out-Null

Write-Detail "Run root: $runRoot"

Write-Step "Building sample project to regenerate authoritative XML"

Run-OrThrow -File 'dotnet' -Args (
 "build `"$sampleProj`" -c $Configuration -f net9.0 -v minimal -m:1 -nr:false /p:Xml2Doc_Enabled=false"
) -Cwd $repo -ErrorPrefix 'Building sample failed.' | Out-Null

$sampleXml = Join-Path $repo "Xml2Doc\tests\Xml2Doc.Sample\bin\$Configuration\net9.0\Xml2Doc.Sample.xml"
Assert-True (Test-Path $sampleXml) "Sample XML not found at: $sampleXml"

Write-Detail "Generated sample XML: $sampleXml"

Write-Step "Validating committed fixture XML matches freshly generated sample XML"

$hSample = (Get-FileHash $sampleXml -Algorithm SHA256).Hash
$hFix = (Get-FileHash $fixtureXml -Algorithm SHA256).Hash

Write-Detail "Sample XML hash : $hSample"
Write-Detail "Fixture XML hash: $hFix"

if ($hSample -ne $hFix)
{
 throw (
  "Fixture XML is out of date.`n" +
  "  sample:  $sampleXml`n" +
  "  fixture: $fixtureXml`n`n" +
  "Hashes differ:`n  sample:  $hSample`n  fixture: $hFix`n`n" +
  "Run scripts/update-fixtures.ps1 and commit the updated Assets/Xml2Doc.Sample.xml"
 )
}

Write-Detail "Fixture XML is current."

Write-Step "Building CLI for both target frameworks"

foreach ($tfm in @('net8.0', 'net9.0'))
{
 Write-Detail "Building CLI for $tfm"
 Run-OrThrow -File 'dotnet' -Args (
  "build `"$cliProj`" -c $Configuration -f $tfm -v minimal -m:1 -nr:false"
 ) -Cwd $repo -ErrorPrefix "Building CLI ($tfm) failed." | Out-Null
}

$cli8 = Join-Path $repo "Xml2Doc\src\Xml2Doc.Cli\bin\$Configuration\net8.0\Xml2Doc.Cli.dll"
$cli9 = Join-Path $repo "Xml2Doc\src\Xml2Doc.Cli\bin\$Configuration\net9.0\Xml2Doc.Cli.dll"
Assert-True (Test-Path $cli8) "Missing CLI dll: $cli8"
Assert-True (Test-Path $cli9) "Missing CLI dll: $cli9"

Write-Detail "CLI net8 path: $cli8"
Write-Detail "CLI net9 path: $cli9"

Write-Step "Loading expected snapshot file set"

$expected = Get-ExpectedSnapshotFiles -snapDir $snapDir
Write-Detail "Expected markdown file count: $($expected.Count)"

$out8 = Join-Path $runRoot 'out-net8'
$out9 = Join-Path $runRoot 'out-net9'
New-Item -ItemType Directory -Force -Path $out8 | Out-Null
New-Item -ItemType Directory -Force -Path $out9 | Out-Null

Write-Step "Generating per-type output with CLI net8"
$r8 = Run-OrThrow -File 'dotnet' -Args (
 "`"$cli8`" --xml `"$fixtureXml`" --out `"$out8`" --file-names clean"
) -Cwd $repo -ErrorPrefix 'Running CLI net8 failed.'
Set-Content -Path (Join-Path $runRoot 'net8.stdout.txt') -Value $r8.StdOut
Set-Content -Path (Join-Path $runRoot 'net8.stderr.txt') -Value $r8.StdErr
Write-Detail "net8 output tree:"
Get-Tree $out8 | ForEach-Object { $_.Split([Environment]::NewLine) } | ForEach-Object { Write-Host "      $_" }

Write-Step "Generating per-type output with CLI net9"
$r9 = Run-OrThrow -File 'dotnet' -Args (
 "`"$cli9`" --xml `"$fixtureXml`" --out `"$out9`" --file-names clean"
) -Cwd $repo -ErrorPrefix 'Running CLI net9 failed.'
Set-Content -Path (Join-Path $runRoot 'net9.stdout.txt') -Value $r9.StdOut
Set-Content -Path (Join-Path $runRoot 'net9.stderr.txt') -Value $r9.StdErr
Write-Detail "net9 output tree:"
Get-Tree $out9 | ForEach-Object { $_.Split([Environment]::NewLine) } | ForEach-Object { Write-Host "      $_" }

Write-Step "Comparing per-type output file sets to expected snapshots"
Assert-FileSet -outDir $out8 -expected $expected
Assert-FileSet -outDir $out9 -expected $expected
Write-Detail "Output file sets match expected snapshots."

Write-Step "Comparing per-type output content to committed snapshots"
Assert-MatchesSnapshots -outDir $out8 -snapDir $snapDir -expectedFiles $expected
Assert-MatchesSnapshots -outDir $out9 -snapDir $snapDir -expectedFiles $expected
Write-Detail "Snapshot content matches for net8 and net9."

Write-Step "Comparing net8 and net9 output for cross-TFM stability"
Assert-DirsEqual -aDir $out8 -bDir $out9 -files $expected
Write-Detail "Cross-TFM output is identical."

Write-Step "Running single-file anchor algorithm smoke test"

$singleDir = Join-Path $runRoot 'single-file'
New-Item -ItemType Directory -Force -Path $singleDir | Out-Null

$algos = @('default', 'github', 'kramdown', 'gfm')
$anchorSets = @{}

foreach ($a in $algos)
{
 $dst = Join-Path $singleDir ("api-{0}.md" -f $a)
 Write-Detail "Generating single-file output with anchor algorithm '$a' -> $dst"

 Run-OrThrow -File 'dotnet' -Args (
  "`"$cli9`" --xml `"$fixtureXml`" --out `"$dst`" --single --anchor-algorithm $a --file-names clean --basename-only"
 ) -Cwd $repo -ErrorPrefix "Single-file run failed ($a)." | Out-Null

 $anchorSets[$a] = (Extract-Anchors -mdPath $dst)
 Write-Detail "Anchor count for '$a': $($anchorSets[$a].Count)"
}

$baseline = ($anchorSets['default'] -join '|')
$diff = $false
foreach ($a in $algos | Where-Object { $_ -ne 'default' })
{
 if (($anchorSets[$a] -join '|') -ne $baseline) { $diff = $true; break }
}
Assert-True $diff 'Expected at least one anchor algorithm to produce a different anchor set than default.'
Write-Detail "Anchor algorithm variation confirmed."

Write-Step "Running namespace-index / trim-rootns / basename-only smoke test"

$nsDir = Join-Path $runRoot 'namespace-index'
New-Item -ItemType Directory -Force -Path $nsDir | Out-Null

Run-OrThrow -File 'dotnet' -Args (
 "`"$cli9`" --xml `"$fixtureXml`" --out `"$nsDir`" --file-names clean " +
 "--rootns `"Xml2Doc.Sample`" --trim-rootns-filenames --namespace-index --basename-only --toc"
) -Cwd $repo -ErrorPrefix 'Namespace-index run failed.' | Out-Null

Assert-True (Test-Path (Join-Path $nsDir 'namespaces.md')) 'Expected namespaces.md to be created.'
Assert-True (Test-Path (Join-Path $nsDir 'namespaces')) 'Expected namespaces/ folder to be created.'

$top = Get-ChildItem -Path $nsDir -Filter '*.md' -File | ForEach-Object { $_.Name }
Assert-True ($top -contains 'index.md') 'Expected index.md at top-level.'
Assert-True ($top -contains 'AliasingPlayground.md') 'Expected trimmed filename AliasingPlayground.md at top-level.'
Assert-True (-not ($top -contains 'Xml2Doc.Sample.AliasingPlayground.md')) 'Did not expect untrimmed filename Xml2Doc.Sample.AliasingPlayground.md.'

$nsPage = Join-Path $nsDir 'namespaces\Xml2Doc.Sample.md'
Assert-True (Test-Path $nsPage) "Expected per-namespace page at $nsPage"

$nsContent = (Get-Content $nsPage -Raw).Replace("`r`n", "`n")
Assert-True ($nsContent -match '\.\./AliasingPlayground\.md') 'Expected basename-only relative link in namespace page.'

Write-Detail "Namespace-index output tree:"
Get-Tree $nsDir | ForEach-Object { $_.Split([Environment]::NewLine) } | ForEach-Object { Write-Host "      $_" }

Write-Step "CLI integration completed successfully"
Write-Host "CLI integration OK. Artifacts: $runRoot"

if (-not $KeepArtifacts)
{
 Write-Detail "Removing artifact directory: $runRoot"
 Remove-Item -Recurse -Force $runRoot
}
else
{
 Write-Detail "Keeping artifact directory: $runRoot"
}