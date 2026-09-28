param(
    [string]$BaseUrl = "http://localhost:5019",
    [string]$Path = "/api/system/status",
    [ValidateSet("GET", "POST", "PUT", "DELETE")]
    [string]$Method = "GET",
    [int]$Requests = 200,
    [int]$Concurrency = 10,
    [int]$ExpectedStatus = 200,
    [string]$BodyFile,
    [string]$BearerTokenEnvironmentVariable
)

$ErrorActionPreference = "Stop"

if ($Requests -lt 1) { throw "Requests must be at least 1." }
if ($Concurrency -lt 1) { throw "Concurrency must be at least 1." }
if ($Concurrency -gt $Requests) { $Concurrency = $Requests }

$requestBody = $null
if ($BodyFile) {
    $resolvedBodyFile = Resolve-Path -LiteralPath $BodyFile
    $requestBody = Get-Content -Raw -LiteralPath $resolvedBodyFile
}

$bearerToken = $null
if ($BearerTokenEnvironmentVariable) {
    $bearerToken = [Environment]::GetEnvironmentVariable($BearerTokenEnvironmentVariable)
    if ([string]::IsNullOrWhiteSpace($bearerToken)) {
        throw "Environment variable '$BearerTokenEnvironmentVariable' is empty or unavailable."
    }
}

$requestUri = "{0}/{1}" -f $BaseUrl.TrimEnd("/"), $Path.TrimStart("/")
$baseCount = [math]::Floor($Requests / $Concurrency)
$remainder = $Requests % $Concurrency

$jobs = 0..($Concurrency - 1) | ForEach-Object {
    $workerCount = $baseCount + $(if ($_ -lt $remainder) { 1 } else { 0 })
    Start-Job -ScriptBlock {
        param($uri, $method, $count, $expectedStatus, $body, $token)

        $headers = @{}
        if ($token) { $headers.Authorization = "Bearer $token" }

        1..$count | ForEach-Object {
            $watch = [Diagnostics.Stopwatch]::StartNew()
            try {
                $parameters = @{
                    Uri = $uri
                    Method = $method
                    Headers = $headers
                    UseBasicParsing = $true
                }
                if ($null -ne $body) {
                    $parameters.Body = $body
                    $parameters.ContentType = "application/json"
                }

                $response = Invoke-WebRequest @parameters
                $watch.Stop()
                [pscustomobject]@{
                    DurationMs = $watch.Elapsed.TotalMilliseconds
                    StatusCode = [int]$response.StatusCode
                    Succeeded = [int]$response.StatusCode -eq $expectedStatus
                    Error = $null
                }
            }
            catch {
                $watch.Stop()
                $responseStatus = 0
                if ($_.Exception.Response -and $_.Exception.Response.StatusCode) {
                    $responseStatus = [int]$_.Exception.Response.StatusCode
                }
                [pscustomobject]@{
                    DurationMs = $watch.Elapsed.TotalMilliseconds
                    StatusCode = $responseStatus
                    Succeeded = $responseStatus -eq $expectedStatus
                    Error = $_.Exception.Message
                }
            }
        }
    } -ArgumentList $requestUri, $Method, $workerCount, $ExpectedStatus, $requestBody, $bearerToken
}

$results = $jobs | Receive-Job -Wait -AutoRemoveJob
$durations = @($results.DurationMs | Sort-Object)
$successCount = @($results | Where-Object Succeeded).Count
$failureCount = $results.Count - $successCount

function Get-Percentile([double[]]$Values, [double]$Percentile) {
    if ($Values.Count -eq 0) { return 0 }
    $index = [math]::Min($Values.Count - 1, [math]::Floor($Values.Count * $Percentile))
    return [math]::Round($Values[$index], 2)
}

[pscustomobject]@{
    Uri = $requestUri
    Method = $Method
    Requests = $results.Count
    Succeeded = $successCount
    Failed = $failureCount
    AverageMs = [math]::Round(($durations | Measure-Object -Average).Average, 2)
    P50Ms = Get-Percentile $durations 0.50
    P95Ms = Get-Percentile $durations 0.95
    P99Ms = Get-Percentile $durations 0.99
    MaximumMs = [math]::Round(($durations | Measure-Object -Maximum).Maximum, 2)
    StatusCodes = ($results | Group-Object StatusCode | Sort-Object Name | ForEach-Object { "{0}:{1}" -f $_.Name, $_.Count }) -join ", "
}

if ($failureCount -gt 0) {
    $sampleErrors = $results | Where-Object { -not $_.Succeeded } | Select-Object -First 5 StatusCode, Error
    $sampleErrors | Format-Table -AutoSize | Out-String | Write-Error
}
