param([string]$OutputPath = (Join-Path $PSScriptRoot 'CodexKnob.exe'))
$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (Test-Path -LiteralPath $OutputPath) { throw 'Output already exists; use a new OutputPath or preserve the existing file first.' }
$sources = @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter '*.cs' | ForEach-Object FullName)
& $compiler /nologo /target:winexe /platform:x64 /optimize+ /codepage:65001 /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll "/win32icon:$(Join-Path $PSScriptRoot 'assets\knob.ico')" "/out:$OutputPath" @sources
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
