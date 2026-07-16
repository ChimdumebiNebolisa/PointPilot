param([string]$Configuration = "Release")

$ErrorActionPreference = "Stop"
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
    dotnet restore PointPilot.sln --locked-mode
    dotnet test PointPilot.sln --configuration $Configuration --no-restore
    dotnet publish src/PointPilot.App/PointPilot.App.csproj --configuration $Configuration --runtime win-x64 --self-contained true --output $publish
    Copy-Item LICENSE,THIRD_PARTY_NOTICES.md -Destination $publish -Force
    if (Test-Path -LiteralPath $zip) { Remove-Item -LiteralPath $zip -Force }
    Compress-Archive -Path (Join-Path $publish "*") -DestinationPath $zip -CompressionLevel Optimal
    Write-Output $zip
}
finally { Pop-Location }
