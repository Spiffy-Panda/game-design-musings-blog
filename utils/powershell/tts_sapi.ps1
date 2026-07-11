<#
  tts_sapi.ps1 — synthesize a single narration line to a WAV using Windows SAPI
  (System.Speech). Zero external deps: System.Speech ships with .NET on Windows,
  so this needs no pip/npm install and no network. Used by
  utils/python/build_exploration_video.py to make per-scene narration tracks.

  Usage:
    powershell -NoProfile -ExecutionPolicy Bypass -File tts_sapi.ps1 `
        -Text "..." -Out "C:\path\scene01.wav" [-Voice "Microsoft Zira Desktop"] [-Rate 0]

  -Rate is the SAPI rate (-10..10); 0 is natural. List voices with:
    Add-Type -AssemblyName System.Speech; (New-Object System.Speech.Synthesis.SpeechSynthesizer).GetInstalledVoices()
#>
param(
  [Parameter(Mandatory=$true)][string]$Text,
  [Parameter(Mandatory=$true)][string]$Out,
  [string]$Voice = "Microsoft Zira Desktop",
  [int]$Rate = 0
)
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Speech
$synth = New-Object System.Speech.Synthesis.SpeechSynthesizer
try {
  try { $synth.SelectVoice($Voice) } catch { Write-Host "voice '$Voice' unavailable; using default" }
  $synth.Rate = $Rate
  $dir = Split-Path -Parent $Out
  if ($dir -and -not (Test-Path $dir)) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }
  $synth.SetOutputToWaveFile($Out)
  $synth.Speak($Text)
} finally {
  $synth.Dispose()
}
if (-not (Test-Path $Out)) { throw "tts_sapi: no WAV written to $Out" }
