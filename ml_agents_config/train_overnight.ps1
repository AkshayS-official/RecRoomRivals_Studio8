# ======================================================================
#  RecRoom Rivals - ShooterBot MAX SPEED Training
#  cd "F:\Projects\Unity\RecRoomRivals_Studio8"
#  .\ml_agents_config\train_overnight.ps1
# ======================================================================

$projectRoot = "F:\Projects\Unity\RecRoomRivals_Studio8"
$mlLearn     = "F:\Engines\Miniconda3\envs\mlagents\Scripts\mlagents-learn.exe"
$exePath     = "$projectRoot\TrainingBuild\RecRoomRivals_Studio8.exe"
$configPath  = "$projectRoot\ml_agents_config\shooter_bot.yaml"
$runId       = "ShooterBot_v1"
$unityModel  = "$projectRoot\Assets\Model"

# MAX SPEED settings
$numEnvs     = 8     # 8 exe instances x 24 agents = 192 agents total
$timeScale   = 20    # simulation runs 20x faster (no-graphics mode)
$basePort    = 5005

if (-not (Test-Path $mlLearn)) { Write-Host "ERROR: mlagents-learn not found" -ForegroundColor Red; exit 1 }
if (-not (Test-Path $exePath))  { Write-Host "ERROR: Training EXE not found at $exePath" -ForegroundColor Red; exit 1 }

Set-Location $projectRoot

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  ShooterBot Training  MAX GPU SPEED" -ForegroundColor Cyan
Write-Host "  $numEnvs envs x 24 agents = 192 total" -ForegroundColor Cyan
Write-Host "  Time scale: ${timeScale}x  |  CUDA GPU" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "TensorBoard (second terminal):" -ForegroundColor Yellow
Write-Host "  & `"F:\Engines\Miniconda3\envs\mlagents\Scripts\tensorboard.exe`" --logdir `"$projectRoot\results\$runId`"" -ForegroundColor White
Write-Host "  http://localhost:6006" -ForegroundColor White
Write-Host ""
Write-Host "Ctrl+C to stop and save model." -ForegroundColor Gray
Write-Host ""

$runCount  = 0
$startTime = Get-Date

while ($true) {
    $runCount++
    $resumeFlag = if ($runCount -eq 1) { "--force" } else { "--resume" }

    Write-Host "[$((Get-Date).ToString('HH:mm:ss'))]  Run #$runCount  $resumeFlag" -ForegroundColor Cyan

    $mlArgs = @(
        $configPath
        "--run-id=$runId"
        $resumeFlag
        "--env=$exePath"
        "--num-envs=$numEnvs"
        "--no-graphics"
        "--torch-device=cuda"
        "--base-port=$basePort"
        "--time-scale=$timeScale"
    )

    & $mlLearn @mlArgs
    $exitCode = $LASTEXITCODE

    if ($exitCode -eq 0) {
        Write-Host "Training finished (max_steps reached)." -ForegroundColor Green
        break
    }

    Write-Host "Exited (code $exitCode). Restarting in 10 s..." -ForegroundColor Yellow
    Start-Sleep -Seconds 10
}

# Copy best .onnx to Unity
Write-Host ""
Write-Host "Copying model to Unity..." -ForegroundColor Cyan
$onnxFile = Get-ChildItem "$projectRoot\results\$runId\*.onnx" -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $onnxFile) {
    $onnxFile = Get-ChildItem "$projectRoot\results\$runId\ShooterBot\*.onnx" -ErrorAction SilentlyContinue |
                Sort-Object LastWriteTime -Descending | Select-Object -First 1
}
if ($onnxFile) {
    if (-not (Test-Path $unityModel)) { New-Item -ItemType Directory -Path $unityModel | Out-Null }
    Copy-Item $onnxFile.FullName "$unityModel\ShooterBot.onnx" -Force
    Write-Host "  Saved: $unityModel\ShooterBot.onnx" -ForegroundColor Green
    Write-Host "  In Unity: ShooterBotAgent > Behavior Parameters > Model" -ForegroundColor Yellow
}

Write-Host "Done. Time: $((Get-Date) - $startTime)" -ForegroundColor Cyan
