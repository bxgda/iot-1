# Pokretanje svih k6 testova — sva 3 scenarija x 3 protokola x 3 VU nivoa
# Pokretanje: .\run-all.ps1
# Preduslovi: docker compose up --build (u drugom terminalu)

$scenarios = @("scenario-a-ingestion.js", "scenario-b-selective.js", "scenario-c-heavy-query.js")
$protocols = @("rest", "grpc", "graphql")
$vus = @(10, 100, 500)

# Kreiraj results folder
New-Item -ItemType Directory -Force -Path results | Out-Null

foreach ($scenario in $scenarios) {
    foreach ($protocol in $protocols) {
        foreach ($vu in $vus) {
            $scenarioName = $scenario -replace ".js", ""
            $outputFile = "results/${scenarioName}_${protocol}_${vu}vu.json"

            Write-Host ""
            Write-Host "========================================" -ForegroundColor Cyan
            Write-Host " $scenarioName | $protocol | ${vu} VUs" -ForegroundColor Cyan
            Write-Host "========================================" -ForegroundColor Cyan

            k6 run -e PROTOCOL=$protocol -e VUS=$vu -e DURATION=60s --summary-export=$outputFile $scenario

            Write-Host "Rezultati sacuvani u $outputFile" -ForegroundColor Green
            Write-Host ""

            # Pauza izmedju testova da se sistem oporavi
            Start-Sleep -Seconds 5
        }
    }
}

Write-Host ""
Write-Host "SVI TESTOVI ZAVRSENI!" -ForegroundColor Yellow
Write-Host "Rezultati su u k6-tests/results/ folderu" -ForegroundColor Yellow
