param([int]$Skip = 0, [string]$Only = '')
$ErrorActionPreference = 'Stop'
$validationStarted = [DateTime]::UtcNow
$suites = @(
    @{ Name='CarVisualBatchTests'; Evidence='geometry-tests.json' },
    @{ Name='HeldCarFramingTests'; Evidence='held-tests.json' },
    @{ Name='CompactCollectionTests'; Evidence='collection-tests.json' },
    @{ Name='ItemMeshDetailTests'; Evidence='detail-mesh-tests.json' },
    @{ Name='DetailSelectionPlayTests'; Evidence='detail-play-tests.json' },
    @{ Name='UpgradeRegressionTests'; Evidence='powers-tests.json' },
    @{ Name='TutorialTests'; Evidence='tutorial-tests.json' },
    @{ Name='TutorialGuidanceTests'; Evidence='tutorial-guidance-tests.json' },
    @{ Name='CarVisualBatchTutorialTests'; Evidence='tutorial-gameplay-tests.json' },
    @{ Name='RenderingArtifactTests'; Evidence='rendering-artifact-tests.json' }
)
$selected = @($suites | Select-Object -Skip $Skip)
if ($Only) { $selected = @($Only.Split(',') | ForEach-Object { $name = $_; $suites | Where-Object { $_.Name -eq $name } }) }
foreach ($suite in $selected) {
    # The CLI uses substring filters: TutorialTests also selects the live car tutorial fixture.
    $tutorialReport = 'IntegrationEvidence/CarBatching/tutorial-tests.json'
    if ($suite.Name -eq 'CarVisualBatchTutorialTests' -and (Test-Path $tutorialReport) -and (Get-Item $tutorialReport).LastWriteTimeUtc -ge $validationStarted) {
        $tutorialRaw = Get-Content -Raw -LiteralPath $tutorialReport
        $tutorialResult = $tutorialRaw | ConvertFrom-Json
        if (@($tutorialResult.data.result.results | Where-Object { $_.FullName -like 'CarVisualBatchTutorialTests.*' -and $_.Status -eq 'Passed' }).Count -gt 0) {
            $tutorialRaw | Set-Content -LiteralPath ('IntegrationEvidence/CarBatching/' + $suite.Evidence)
            Write-Output 'Live car tutorial passed in the TutorialTests filter; no duplicate run needed.'
            continue
        }
    }
    Write-Output ('Starting ' + $suite.Name)
    if ($suite.Name -eq 'DetailSelectionPlayTests') {
        # Geometry/lighting assertions still run; the separate 192-pair review captures images.
        $fixture = unity command run_script --project-path . --caller plugin --skill optimize-web --format json -- --file IntegrationEvidence/CarBatching/FixtureSetup.cs --entry CarBatchFixtureSetup.Detail | ConvertFrom-Json
        if (!$fixture.success) { throw ($fixture | ConvertTo-Json -Depth 10) }
    }
    if (!$suite.AlreadyRunning) {
        $started = unity command run_tests --project-path . --caller plugin --skill optimize-web --format json -- --mode editor --filter $suite.Name --async_tests true --timeout 1200 | ConvertFrom-Json
        if (!$started.success) { throw ($started | ConvertTo-Json -Depth 10) }
    }
    $deadline = (Get-Date).AddMinutes(22)
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Seconds 15
        $raw = unity command test_status --project-path . --caller plugin --skill optimize-web --format json
        $status = $raw | ConvertFrom-Json
        if ($status.success -and $status.data.result.status -eq 'completed' -and $status.data.result.summary.total -gt 0) {
            $raw | Set-Content -LiteralPath ('IntegrationEvidence/CarBatching/' + $suite.Evidence)
            Write-Output ($suite.Name + ': ' + ($status.data.result.summary | ConvertTo-Json -Compress))
            if ($status.data.result.summary.failed -gt 0) { throw ($status.data.result.results | ConvertTo-Json -Depth 10) }
            if ($suite.Name -eq 'CarVisualBatchTests') {
                $preview = unity command eval --project-path . --caller plugin --skill optimize-web --timeout 600 --format json -- --code 'CarVisualBatchPreview.Capture(); return 1;' --timeout 600000 | ConvertFrom-Json
                if (!$preview.success -or !$preview.data.result.success) { throw ($preview | ConvertTo-Json -Depth 10) }
                $bounds = unity command run_script --project-path . --caller plugin --skill optimize-web --timeout 120 --format json -- --file IntegrationEvidence/CarBatching/BoundsCheck.cs --entry BoundsCheck.Run --timeout_ms 120000 | ConvertFrom-Json
                if (!$bounds.success) { throw ($bounds | ConvertTo-Json -Depth 10) }
                Write-Output 'Rendered comparisons and all600 bounds checked.'
            }
            break
        }
    }
    if ((Get-Date) -ge $deadline) { throw ('Timed out waiting for ' + $suite.Name) }
    if (Test-Path -LiteralPath 'IntegrationEvidence/CarBatching/pause-after-current') {
        Write-Output 'Validation queue paused for the coordinated candidate build.'
        exit 0
    }
}
Write-Output 'Selected car batching gameplay regressions passed.'
