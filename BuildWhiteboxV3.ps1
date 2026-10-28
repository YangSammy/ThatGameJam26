param(
    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe',
    [switch]$ValidateOnly
)
$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
if ([string]::IsNullOrEmpty($env:ALLUSERSPROFILE) -and -not [string]::IsNullOrEmpty($env:ProgramData)) { $env:ALLUSERSPROFILE = $env:ProgramData }
if (-not (Test-Path -LiteralPath $UnityPath)) { throw "Unity 2022.3 editor not found: $UnityPath" }
$qaFolder = Join-Path $projectRoot 'QA\WhiteboxV3'
New-Item -ItemType Directory -Path $qaFolder -Force | Out-Null
$logPath = Join-Path $qaFolder 'unity-build.log'
$method = if ($ValidateOnly) { 'MemoryWhitebox.V3.Editor.WhiteboxV3Build.Validate' } else { 'MemoryWhitebox.V3.Editor.WhiteboxV3Build.ValidateAndBuild' }
$arguments = @('-batchmode','-nographics','-quit','-projectPath',('"{0}"' -f $projectRoot),'-executeMethod',$method,'-logFile',('"{0}"' -f $logPath))
$editorProcess = Start-Process -FilePath $UnityPath -ArgumentList $arguments -WindowStyle Hidden -PassThru -Wait
if ($editorProcess.ExitCode -ne 0) { throw "Unity failed ($($editorProcess.ExitCode)). Read $logPath" }
Write-Output "Whitebox V3 completed. Log: $logPath"
