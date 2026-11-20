param(
    [Parameter(Mandatory)][string]$Editor,
    [Parameter(Mandatory)][ValidateSet('Windows', 'macOS')][string]$Platform,
    [Parameter(Mandatory)][string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$project = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$editorPath = (Resolve-Path -LiteralPath $Editor).Path
$output = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $output) {
    if (Get-ChildItem -LiteralPath $output -Force) {
        throw "Build output must start empty: $output"
    }
}
New-Item -Path $output -ItemType Directory -Force | Out-Null
$log = "$output.log"

if ($Platform -eq 'Windows') {
    $target = 'Win64'
    $buildArgument = '-buildWindows64Player'
    $player = Join-Path $output 'PicoPark.exe'
} else {
    $target = 'OSXUniversal'
    $buildArgument = '-buildOSXUniversalPlayer'
    $player = Join-Path $output 'PicoPark.app'
}

$arguments = @(
    '-batchmode', '-nographics', '-quit',
    '-projectPath', ('"{0}"' -f $project),
    '-buildTarget', $target,
    $buildArgument, ('"{0}"' -f $player),
    '-logFile', ('"{0}"' -f $log)
)
$process = Start-Process -FilePath $editorPath -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
if ($process.ExitCode -ne 0 -or !(Test-Path -LiteralPath $player)) {
    if (Test-Path -LiteralPath $log) { Get-Content -LiteralPath $log -Tail 45 }
    throw "Tuanjie did not produce $player (exit $($process.ExitCode)). See $log."
}

if ($Platform -eq 'Windows') {
    Copy-Item -LiteralPath (Join-Path $project 'Assets/Plugins/steam_appid.txt') -Destination $output
    Copy-Item -LiteralPath (Join-Path $project 'Assets/Plugins/x86_64/steam_api64.dll') -Destination $output
}
Write-Output $player
