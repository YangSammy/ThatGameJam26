param([string]$Editor = 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe')
$projectRoot = $PSScriptRoot
if (-not (Test-Path -LiteralPath $Editor)) { throw '请使用 -Editor 指定 Unity 2022.3 编辑器。' }
$buildLog = Join-Path $projectRoot 'unity-build.log'
$buildProcess = Start-Process -FilePath $Editor -ArgumentList @('-batchmode','-nographics','-noUpm','-quit','-projectPath',('"'+$projectRoot+'"'),'-executeMethod','MemoryWhitebox.Editor.GameBuild.ValidateAndBuild','-logFile',('"'+$buildLog+'"')) -WindowStyle Hidden -PassThru
$buildProcess.WaitForExit()
if ($buildProcess.ExitCode -ne 0) { throw "Unity 构建失败，请查看 $buildLog" }
Write-Output 'HouseScene 与 ParkScene 的 Windows 构建通过。'
