param([string]$EnvironmentFile = ".env.local")

$ErrorActionPreference = "Stop"
$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$path = [System.IO.Path]::GetFullPath((Join-Path $root $EnvironmentFile))
if (-not $path.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase) -or -not (Test-Path -LiteralPath $path)) {
    throw "The local environment file is missing or outside the repository."
}

$values = @{}
foreach ($line in [System.IO.File]::ReadLines($path)) {
    $value = $line.Trim()
    if ($value.Length -eq 0 -or $value.StartsWith("#")) { continue }
    $separator = $value.IndexOf("=")
    if ($separator -gt 0) { $values[$value.Substring(0, $separator).Trim()] = $value.Substring($separator + 1).Trim().Trim('"') }
}
$key = $values["OPENAI_API_KEY"]
if ([string]::IsNullOrWhiteSpace($key)) { throw "OPENAI_API_KEY is not configured in the local environment file." }
$responsesModel = if ($values["POINTPILOT_RESPONSES_MODEL"]) { $values["POINTPILOT_RESPONSES_MODEL"] } else { "gpt-5.6" }
$realtimeModel = if ($values["POINTPILOT_REALTIME_MODEL"]) { $values["POINTPILOT_REALTIME_MODEL"] } else { "gpt-realtime-2.1" }
$baseUrl = if ($values["OPENAI_BASE_URL"]) { $values["OPENAI_BASE_URL"].TrimEnd('/') } else { "https://api.openai.com/v1" }
$headers = @{ Authorization = "Bearer $key"; "OpenAI-Safety-Identifier" = "pointpilot-local-smoke" }

try {
    $realtimeBody = @{ session = @{ type = "realtime"; model = $realtimeModel; audio = @{ output = @{ voice = "marin" } } } } | ConvertTo-Json -Depth 8 -Compress
    $realtime = Invoke-RestMethod -Method Post -Uri "$baseUrl/realtime/client_secrets" -Headers $headers -ContentType "application/json" -Body $realtimeBody
    if ([string]::IsNullOrWhiteSpace($realtime.value)) { throw "Realtime endpoint returned no client secret." }

    $responsesBody = @{ model = $responsesModel; input = "Reply with the single word ready."; max_output_tokens = 16 } | ConvertTo-Json -Depth 5 -Compress
    $response = Invoke-RestMethod -Method Post -Uri "$baseUrl/responses" -Headers $headers -ContentType "application/json" -Body $responsesBody
    if ([string]::IsNullOrWhiteSpace($response.id)) { throw "Responses endpoint returned no response id." }
    [pscustomobject]@{ RealtimeClientSecret = "PASS"; Responses = "PASS"; ResponsesModel = $responsesModel; RealtimeModel = $realtimeModel }
}
catch {
    $status = if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { 0 }
    throw "OpenAI smoke check failed (HTTP status $status). No credential or response body was printed."
}
finally {
    $key = $null
    $headers = $null
}
