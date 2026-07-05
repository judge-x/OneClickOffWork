$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root "src\OneClickOffWork.App\OneClickOffWork.App.csproj"
$output = Join-Path $root "publish\win-x64"

if (Test-Path $output) {
  try {
    Remove-Item -LiteralPath $output -Recurse -Force
  }
  catch {
    Write-Host ""
    Write-Host "Cannot update publish folder. Please close OneClickOffWork.exe first."
    Write-Host "Locked folder: $output"
    throw
  }
}

dotnet publish $project `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:UseAppHost=true `
  -p:PublishSingleFile=true `
  -p:EnableCompressionInSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:DebugType=None `
  -p:DebugSymbols=false `
  -p:SatelliteResourceLanguages=zh-Hans `
  -o $output

if ($LASTEXITCODE -ne 0) {
  throw "dotnet publish failed with exit code $LASTEXITCODE"
}

Write-Host ""
Write-Host "Compressed self-contained build output: $output"
Write-Host "Target PCs do not need to install .NET 8 Desktop Runtime."
Write-Host "Double-click: $output\OneClickOffWork.exe"
