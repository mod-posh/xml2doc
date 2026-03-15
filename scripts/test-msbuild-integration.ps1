[CmdletBinding()]
param(
  [ValidateSet("Debug","Release")]
  [string] $Configuration = "Release",

  [switch] $KeepArtifacts
)

$ErrorActionPreference = "Stop"

function RepoRoot {
  # script is /scripts/, repo root is parent
  return (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
}

function Run-Capture {
  param(
    [Parameter(Mandatory=$true)][string] $FileName,
    [Parameter(Mandatory=$true)][string] $Arguments,
    [Parameter(Mandatory=$true)][string] $WorkingDirectory
  )

  $psi = New-Object System.Diagnostics.ProcessStartInfo
  $psi.FileName = $FileName
  $psi.Arguments = $Arguments
  $psi.WorkingDirectory = $WorkingDirectory
  $psi.UseShellExecute = $false
  $psi.RedirectStandardOutput = $true
  $psi.RedirectStandardError = $true
  $psi.CreateNoWindow = $true

  $p = New-Object System.Diagnostics.Process
  $p.StartInfo = $psi
  [void]$p.Start()

  $stdout = $p.StandardOutput.ReadToEnd()
  $stderr = $p.StandardError.ReadToEnd()
  $p.WaitForExit()

  return [pscustomobject]@{
    ExitCode = $p.ExitCode
    StdOut   = $stdout
    StdErr   = $stderr
  }
}

function Assert-True($cond, [string]$message) {
  if (-not $cond) { throw $message }
}

function Get-Tree([string] $root) {
  if (-not (Test-Path $root)) { return "<missing>" }
  $files = Get-ChildItem -Path $root -Recurse -File -ErrorAction SilentlyContinue |
    Sort-Object FullName |
    ForEach-Object { $_.FullName.Substring($root.Length).TrimStart('\','/') -replace '\\','/' }

  if (-not $files) { return "<empty>" }
  return ($files -join [Environment]::NewLine)
}

$repo = RepoRoot

$sampleProj = Join-Path $repo "Xml2Doc\tests\Xml2Doc.Sample\Xml2Doc.Sample.csproj"
Assert-True (Test-Path $sampleProj) "Sample project not found at: $sampleProj"

$sampleDir = Split-Path $sampleProj -Parent

# Sample is net9.0 today; stamp should land here (default behavior)
$stampDefault = Join-Path $sampleDir "obj\$Configuration\net9.0\xml2doc.stamp"

$runId  = [guid]::NewGuid().ToString("n")
$runRoot = Join-Path $sampleDir "obj\$Configuration\xml2doc-it\$runId"
$outDir  = Join-Path $runRoot "docs"
$outFile = Join-Path $runRoot "api.md"
$report  = Join-Path $runRoot "xml2doc-report.json"

New-Item -ItemType Directory -Force -Path $runRoot | Out-Null
New-Item -ItemType Directory -Force -Path $outDir  | Out-Null

function Invoke-SampleBuild([bool]$SingleFile, [string]$BinLogPath) {
  $props =
    "/p:Xml2Doc_Enabled=true " +
    "/p:GenerateDocumentationFile=true " +
    "/p:Xml2Doc_ReportIncludeTimestamp=false " +
    "/p:Xml2Doc_ReportPath=`"$report`" " +
    "/p:Xml2Doc_FileNameMode=clean " +
    "/p:Xml2Doc_RootNamespaceToTrim=Xml2Doc.Sample " +
    "/p:Xml2Doc_TrimRootNamespaceInFileNames=true " +
    "/p:Xml2Doc_CodeBlockLanguage=csharp " +
    "/p:Xml2Doc_DryRun=false " +
    (
      if ($SingleFile) {
        "/p:Xml2Doc_SingleFile=true /p:Xml2Doc_OutputFile=`"$outFile`""
      } else {
        "/p:Xml2Doc_SingleFile=false /p:Xml2Doc_OutputDir=`"$outDir`""
      }
    )

  $args =
    "msbuild `"$sampleProj`" " +
    "/t:Build " +
    "/p:Configuration=$Configuration " +
    "/m:1 /nr:false /nodeReuse:false " +
    "/v:minimal " +
    "/bl:`"$BinLogPath`" " +
    $props

  $r = Run-Capture -FileName "dotnet" -Arguments $args -WorkingDirectory $repo
  return $r
}

$bin1 = Join-Path $runRoot "build1.binlog"
$bin2 = Join-Path $runRoot "build2.binlog"
$bin3 = Join-Path $runRoot "build3.binlog"

# --- 1) First build (per-type) ---
$r1 = Invoke-SampleBuild -SingleFile:$false -BinLogPath $bin1
Assert-True ($r1.ExitCode -eq 0) ("First build failed.`n`nSTDOUT:`n{0}`n`nSTDERR:`n{1}`n`nrunRoot:`n{2}" -f $r1.StdOut,$r1.StdErr,(Get-Tree $runRoot))

Assert-True (Test-Path $stampDefault) "Expected stamp at $stampDefault`nrunRoot:`n$(Get-Tree $runRoot)"
Assert-True (Test-Path $report) "Expected report at $report`nrunRoot:`n$(Get-Tree $runRoot)"
Assert-True (Test-Path (Join-Path $outDir "index.md")) "Expected per-type index.md at $outDir"

$t1 = (Get-Item $stampDefault).LastWriteTimeUtc

# Parse and sanity-check report
$rep1 = Get-Content $report -Raw | ConvertFrom-Json
Assert-True ($rep1.single -eq $false) "Expected report.single=false for per-type mode"
Assert-True ($rep1.files -and ($rep1.files | Where-Object { $_ -match "index\.md$" })) "Expected index.md in report.files"

# --- 2) Second build, same props (should be incremental, stamp unchanged) ---
$r2 = Invoke-SampleBuild -SingleFile:$false -BinLogPath $bin2
Assert-True ($r2.ExitCode -eq 0) ("Second build failed.`n`nSTDOUT:`n{0}`n`nSTDERR:`n{1}" -f $r2.StdOut,$r2.StdErr)

$t2 = (Get-Item $stampDefault).LastWriteTimeUtc
Assert-True ($t2 -eq $t1) ("Expected incremental no-op build (stamp unchanged).`n`t1=$t1`n`t2=$t2`n`nTip: check binlogs in $runRoot")

Start-Sleep -Milliseconds 600

# --- 3) Change options (single-file) => should re-run (stamp updated) ---
$r3 = Invoke-SampleBuild -SingleFile:$true -BinLogPath $bin3
Assert-True ($r3.ExitCode -eq 0) ("Third build failed.`n`nSTDOUT:`n{0}`n`nSTDERR:`n{1}" -f $r3.StdOut,$r3.StdErr)

$t3 = (Get-Item $stampDefault).LastWriteTimeUtc
Assert-True ($t3 -gt $t2) ("Expected stamp to update after option change.`n`t2=$t2`n`t3=$t3")

Assert-True (Test-Path $outFile) "Expected single-file output at $outFile"
Assert-True (Test-Path $report) "Expected report to exist at $report"

$rep3 = Get-Content $report -Raw | ConvertFrom-Json
Assert-True ($rep3.single -eq $true) "Expected report.single=true after single-file build"

Write-Host "MSBuild integration OK. Artifacts: $runRoot"
if (-not $KeepArtifacts) {
  Remove-Item -Recurse -Force $runRoot
}