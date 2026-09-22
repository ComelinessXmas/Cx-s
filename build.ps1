$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $compiler /nologo /target:winexe /platform:x64 /optimize+ /win32icon:AirLink.ico /out:AirLink.exe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll AirLink.cs WindowFit.cs Preferences.cs
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
Write-Host 'Built AirLink.exe'


