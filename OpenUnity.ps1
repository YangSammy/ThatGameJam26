param([string]$Editor = 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe')
$projectRoot = $PSScriptRoot
if (-not (Test-Path -LiteralPath $Editor)) { throw '请使用 -Editor 指定已安装的 Unity 2022.3 编辑器。' }
Start-Process -FilePath $Editor -ArgumentList @('-projectPath', ('"' + $projectRoot + '"'), '-noUpm')
