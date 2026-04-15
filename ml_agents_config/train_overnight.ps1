# Overnight Training Script — Auto-restarts if crashed, stops at reward 0.95
# Run this in PowerShell: .\train_overnight.ps1

$projectPath = "F:\Projects\Unity\RecRoomRivals_Studio8"
$exePath = "TrainingBuild\RecRoomRivals_Studio8.exe"
$configPath = "ml_agents_config\shooter_bot.yaml"
$runId = "ShooterBot_FINAL"
$logFile = "training_log.txt"
$targetReward = 0.95
$runCount = 0

Set-Location $projectPath

# Activate conda mlagents env
& "F:\Engines\Miniconda3\Scripts\activate.bat" mlagents

Write-Host "🏀 Starting overnight training. Target reward: $targetReward" -ForegroundColor Green
Write-Host "Log file: $projectPath\$logFile" -ForegroundColor Yellow

while ($true) {
    $runCount++
    Write-Host "`n[Run #$runCount] Starting training at $(Get-Date -Format 'HH:mm:ss')" -ForegroundColor Cyan
    
    # First run uses --force, subsequent runs use --resume
    if ($runCount -eq 1) {
        $resumeFlag = "--force"
    } else {
        $resumeFlag = "--resume"
    }
    
    # Run training and capture output
    $process = Start-Process -FilePath "F:\Engines\Miniconda3\envs\mlagents\Scripts\mlagents-learn.exe" `
        -ArgumentList "$configPath --run-id=$runId $resumeFlag --env=$exePath --num-envs=8 --no-graphics --torch-device cuda" `
        -PassThru -NoNewWindow `
        -RedirectStandardOutput $logFile

    # Monitor log file for reward and completion
    $maxWaitSeconds = 28800 # 8 hours max
    $elapsed = 0
    $lastReward = 0.0
    $reachedTarget = $false

    while (-not $process.HasExited -and $elapsed -lt $maxWaitSeconds) {
        Start-Sleep -Seconds 30
        $elapsed += 30

        # Read last few lines of log for current reward
        if (Test-Path $logFile) {
            $lastLines = Get-Content $logFile -Tail 20
            foreach ($line in $lastLines) {
                if ($line -match "Mean Reward: ([0-9.]+)") {
                    $lastReward = [float]$matches[1]
                }
                if ($line -match "Step: ([0-9]+)") {
                    $currentStep = $matches[1]
                }
            }
        }

        Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Step: $currentStep | Mean Reward: $lastReward" -ForegroundColor White

        # Check if target reached
        if ($lastReward -ge $targetReward) {
            Write-Host "`n🎉 TARGET REWARD $targetReward REACHED! Stopping training." -ForegroundColor Green
            $process.Kill()
            $reachedTarget = $true
            break
        }
    }

    if ($reachedTarget) { break }

    if ($process.HasExited) {
        Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Process exited. Last reward: $lastReward. Restarting in 10 seconds..." -ForegroundColor Yellow
        Start-Sleep -Seconds 10
    }
}

Write-Host "`n✅ Training complete! Model saved in results\$runId\" -ForegroundColor Green
Write-Host "Import the .onnx file into Unity to use the trained bot." -ForegroundColor Cyan