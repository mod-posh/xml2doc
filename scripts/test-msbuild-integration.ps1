[CmdletBinding()]
param(
  [ValidateSet("Debug", "Release")]
  [string] $Configuration = "Release",

  [switch] $KeepArtifacts
)

$ErrorActionPreference = "Stop"

function RepoRoot
{
  (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
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

function Assert-True($cond, [string]$message)
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
    [string] $ErrorPrefix = "Command failed"
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

function Get-Tree([string] $root)
{
  if (-not (Test-Path $root)) { return "<missing>" }

  $rootPath = (Resolve-Path $root).Path.TrimEnd('\', '/')

  $files = Get-ChildItem -Path $rootPath -Recurse -File -ErrorAction SilentlyContinue |
  Sort-Object FullName |
  ForEach-Object {
    $_.FullName.Substring($rootPath.Length).TrimStart('\', '/') -replace '\\', '/'
  }

  if (-not $files) { return "<empty>" }
  return ($files -join [Environment]::NewLine)
}

Write-Step "Resolving repository paths"

$repo = RepoRoot
$sampleProj = Join-Path $repo "Xml2Doc\tests\Xml2Doc.Sample\Xml2Doc.Sample.csproj"
Assert-True (Test-Path $sampleProj) "Sample project not found at: $sampleProj"

$sampleDir = Split-Path $sampleProj -Parent
$stampDefault = Join-Path $sampleDir "obj\$Configuration\net9.0\xml2doc.stamp"

$runId = [guid]::NewGuid().ToString("n")
$runRoot = Join-Path $sampleDir "obj\$Configuration\xml2doc-it\$runId"
$outDir = Join-Path $runRoot "docs"
$outFile = Join-Path $runRoot "api.md"
$report = Join-Path $runRoot "xml2doc-report.json"

New-Item -ItemType Directory -Force -Path $runRoot | Out-Null
New-Item -ItemType Directory -Force -Path $outDir  | Out-Null

Write-Detail "Repo root: $repo"
Write-Detail "Sample project: $sampleProj"
Write-Detail "Sample dir: $sampleDir"
Write-Detail "Stamp path: $stampDefault"
Write-Detail "Run root: $runRoot"
Write-Detail "Per-type output dir: $outDir"
Write-Detail "Single-file output: $outFile"
Write-Detail "Report path: $report"

function Invoke-SampleBuild([bool]$SingleFile, [string]$BinLogPath)
{
  $modeProps = if ($SingleFile)
  {
    "/p:Xml2Doc_SingleFile=true /p:Xml2Doc_OutputFile=`"$outFile`""
  }
  else
  {
    "/p:Xml2Doc_SingleFile=false /p:Xml2Doc_OutputDir=`"$outDir`""
  }

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
  $modeProps

  $args =
  "msbuild `"$sampleProj`" " +
  "/t:Build " +
  "/p:Configuration=$Configuration " +
  "/m:1 /nr:false /nodeReuse:false " +
  "/v:minimal " +
  "/bl:`"$BinLogPath`" " +
  $props

  Run-OrThrow -File "dotnet" -Args $args -Cwd $repo -ErrorPrefix "Sample build failed."
}

$bin1 = Join-Path $runRoot "build1.binlog"
$bin2 = Join-Path $runRoot "build2.binlog"
$bin3 = Join-Path $runRoot "build3.binlog"

Write-Step "Build 1: per-type output"
$r1 = Invoke-SampleBuild -SingleFile:$false -BinLogPath $bin1

Assert-True (Test-Path $stampDefault) "Expected stamp at $stampDefault`nrunRoot:`n$(Get-Tree $runRoot)"
Assert-True (Test-Path $report) "Expected report at $report`nrunRoot:`n$(Get-Tree $runRoot)"
Assert-True (Test-Path (Join-Path $outDir "index.md")) "Expected per-type index.md at $outDir"

$t1 = (Get-Item $stampDefault).LastWriteTimeUtc
Write-Detail "Stamp after build 1: $t1"

$rep1 = Get-Content $report -Raw | ConvertFrom-Json
Assert-True ($rep1.single -eq $false) "Expected report.single=false for per-type mode"
Assert-True ($rep1.files -and ($rep1.files | Where-Object { $_ -match "index\.md$" })) "Expected index.md in report.files"

Write-Detail "Build 1 output tree:"
(Get-Tree $runRoot).Split([Environment]::NewLine) | ForEach-Object {
  Write-Host "      $_"
}

Write-Step "Build 2: same properties, expect incremental no-op"
$r2 = Invoke-SampleBuild -SingleFile:$false -BinLogPath $bin2

$t2 = (Get-Item $stampDefault).LastWriteTimeUtc
Write-Detail "Stamp after build 2: $t2"

Assert-True ($t2 -eq $t1) ("Expected incremental no-op build (stamp unchanged).`n`t1=$t1`n`t2=$t2`n`nTip: check binlogs in $runRoot")

Start-Sleep -Milliseconds 600

Write-Step "Build 3: switch to single-file mode, expect re-run"
$r3 = Invoke-SampleBuild -SingleFile:$true -BinLogPath $bin3

$t3 = (Get-Item $stampDefault).LastWriteTimeUtc
Write-Detail "Stamp after build 3: $t3"

Assert-True ($t3 -gt $t2) ("Expected stamp to update after option change.`n`t2=$t2`n`t3=$t3")
Assert-True (Test-Path $outFile) "Expected single-file output at $outFile"
Assert-True (Test-Path $report) "Expected report to exist at $report"

$rep3 = Get-Content $report -Raw | ConvertFrom-Json
Assert-True ($rep3.single -eq $true) "Expected report.single=true after single-file build"

Write-Detail "Final output tree:"
(Get-Tree $runRoot).Split([Environment]::NewLine) | ForEach-Object {
  Write-Host "      $_"
}

Write-Step "MSBuild integration completed successfully"
Write-Host "MSBuild integration OK. Artifacts: $runRoot"

if (-not $KeepArtifacts)
{
  Write-Detail "Removing artifact directory: $runRoot"
  Remove-Item -Recurse -Force $runRoot
}
else
{
  Write-Detail "Keeping artifact directory: $runRoot"
}