<#
.SYNOPSIS
Installs the local image model (Z-Image-Turbo) into a self-contained folder.

.DESCRIPTION
Creates a Python venv, installs CUDA PyTorch and diffusers, and downloads the
Z-Image-Turbo weights (about 20 GB). Safe to re-run; finished steps are skipped
by pip and the Hugging Face cache.

.EXAMPLE
powershell -NoProfile -File tools/LocalImageGen/Setup-LocalImageGen.ps1
#>
param(
    [string]$ImageHome = $(if ($env:SWLOR_LOCAL_IMAGE_HOME) { $env:SWLOR_LOCAL_IMAGE_HOME } else { 'C:\AI\LocalImageGen' }),
    [string]$Python = 'py -3.13'
)

$ErrorActionPreference = 'Stop'
$venv = Join-Path $ImageHome 'venv'
$venvPython = Join-Path $venv 'Scripts\python.exe'

New-Item -ItemType Directory -Force $ImageHome | Out-Null
if (-not (Test-Path $venvPython)) {
    Write-Host "Creating venv in $venv"
    $exe, $pyArgs = $Python -split ' '
    & $exe @pyArgs -m venv $venv
}

& $venvPython -m pip install -q --upgrade pip
& $venvPython -m pip install -q torch torchvision --index-url https://download.pytorch.org/whl/cu128
& $venvPython -m pip install -q diffusers transformers accelerate safetensors huggingface_hub hf_xet sentencepiece pillow bitsandbytes truststore

$env:HF_HOME = Join-Path $ImageHome 'hf-cache'
$env:HF_HUB_DISABLE_TELEMETRY = '1'
Write-Host 'Downloading Tongyi-MAI/Z-Image-Turbo weights'
& $venvPython -c "import truststore; truststore.inject_into_ssl(); from huggingface_hub import snapshot_download; print(snapshot_download('Tongyi-MAI/Z-Image-Turbo'))"

& $venvPython -c "import torch; assert torch.cuda.is_available(), 'CUDA not available'; print('Ready:', torch.cuda.get_device_name(0))"
