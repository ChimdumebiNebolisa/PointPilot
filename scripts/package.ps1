param([string]$Configuration = "Release")

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$artifacts = [System.IO.Path]::GetFullPath((Join-Path $root "artifacts"))
$publish = [System.IO.Path]::GetFullPath((Join-Path $artifacts "publish/win-x64"))
$zip = Join-Path $root "artifacts/PointPilot-win-x64.zip"

if (-not $publish.StartsWith($artifacts + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Publish output must remain inside the repository artifacts directory."
}

Push-Location $root
try {
    npm ci
    npm run build:web
    if (Test-Path -LiteralPath $publish) { Remove-Item -LiteralPath $publish -Recurse -Force }
    dotnet clean PointPilot.sln --configuration $Configuration
    dotnet restore PointPilot.sln --runtime win-x64 --locked-mode
    dotnet test PointPilot.sln --configuration $Configuration --no-restore
    dotnet publish src/PointPilot.App/PointPilot.App.csproj --configuration $Configuration --runtime win-x64 --self-contained true --no-restore --output $publish
    Copy-Item LICENSE,THIRD_PARTY_NOTICES.md -Destination $publish -Force
    Get-ChildItem -LiteralPath $publish -Recurse -File -Filter "*.pdb" | Remove-Item -Force
    if (Test-Path -LiteralPath $zip) { Remove-Item -LiteralPath $zip -Force }
    $timestamp = [DateTimeOffset]::new(2000, 1, 1, 0, 0, 0, [TimeSpan]::Zero)
    $archive = [System.IO.Compression.ZipFile]::Open($zip, [System.IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($file in Get-ChildItem -LiteralPath $publish -Recurse -File | Sort-Object FullName) {
            $relative = $file.FullName.Substring($publish.Length).TrimStart([System.IO.Path]::DirectorySeparatorChar).Replace('\', '/')
            $entry = $archive.CreateEntry($relative, [System.IO.Compression.CompressionLevel]::Optimal)
            $entry.LastWriteTime = $timestamp
            $source = $file.OpenRead()
            $destination = $entry.Open()
            try { $source.CopyTo($destination) }
            finally { $destination.Dispose(); $source.Dispose() }
        }
    }
    finally { $archive.Dispose() }
    Write-Output $zip
}
finally { Pop-Location }
