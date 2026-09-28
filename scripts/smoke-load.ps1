param(
    [string]$BaseUrl = "http://localhost:5019",
    [int]$Requests = 200,
    [int]$Concurrency = 10
)
$ErrorActionPreference = "Stop"
$jobs = 1..$Concurrency | ForEach-Object {
    $count = [math]::Ceiling($Requests / $Concurrency)
    Start-Job -ScriptBlock {
        param($url,$count)
        $times = @()
        for($i=0;$i -lt $count;$i++) {
            $watch=[Diagnostics.Stopwatch]::StartNew()
            $response=Invoke-WebRequest -UseBasicParsing "$url/api/system/status"
            $watch.Stop()
            if($response.StatusCode -ne 200){throw "HTTP $($response.StatusCode)"}
            $times += $watch.Elapsed.TotalMilliseconds
        }
        $times
    } -ArgumentList $BaseUrl,$count
}
$values = $jobs | Receive-Job -Wait -AutoRemoveJob
$ordered = $values | Sort-Object
[pscustomobject]@{
    Requests = $values.Count
    AverageMs = [math]::Round(($values | Measure-Object -Average).Average,2)
    P95Ms = [math]::Round($ordered[[math]::Min($ordered.Count-1,[math]::Floor($ordered.Count*.95))],2)
    MaximumMs = [math]::Round(($values | Measure-Object -Maximum).Maximum,2)
}
