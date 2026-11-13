<#
.SYNOPSIS
    고정 버전 SDL3 Windows x64 바이너리를 받아 native/win-x64 에 둔다.
.DESCRIPTION
    GitHub libsdl-org/SDL 공식 릴리스의 SDL3-<버전>-win32-x64.zip 을 내려받아
    SHA256 을 검증한 뒤 SDL3.dll 과 LICENSE.txt 만 native/win-x64 로 복사한다.
    Windows PowerShell 5.1 과 PowerShell 7 모두에서 동작한다.
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File build/fetch-sdl.ps1
#>
[CmdletBinding()]
param(
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

# 버전을 바꿀 때는 두 값을 함께 바꾸고, 문서 2번 T2 절의 확정값도 갱신한다.
$SdlVersion = '3.4.20'
$Sha256     = '368BED8B24E44AD9EA372990814F2343FCFB9E468ABB7F8F4A3BC146C7304752'

$repoRoot  = Split-Path -Parent $PSScriptRoot
$targetDir = Join-Path $repoRoot 'native\win-x64'
$dllPath   = Join-Path $targetDir 'SDL3.dll'
$stampPath = Join-Path $targetDir 'SDL3.version'

if (-not $Force -and (Test-Path $dllPath) -and (Test-Path $stampPath) -and ((Get-Content $stampPath -Raw).Trim() -eq $SdlVersion)) {
    Write-Host "SDL3 $SdlVersion 이(가) 이미 있습니다: $dllPath"
    exit 0
}

$zipName = "SDL3-$SdlVersion-win32-x64.zip"
$url     = "https://github.com/libsdl-org/SDL/releases/download/release-$SdlVersion/$zipName"
$work    = Join-Path ([System.IO.Path]::GetTempPath()) ("joyconmouse-sdl-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $work | Out-Null

try {
    [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
    $zipPath = Join-Path $work $zipName
    Write-Host "내려받는 중: $url"
    Invoke-WebRequest -Uri $url -OutFile $zipPath -UseBasicParsing

    $actual = (Get-FileHash -Path $zipPath -Algorithm SHA256).Hash
    if ($actual -ne $Sha256) {
        throw "SHA256 불일치. 기대값=$Sha256 실제값=$actual"
    }

    $extract = Join-Path $work 'x'
    Expand-Archive -Path $zipPath -DestinationPath $extract -Force

    $dll = Get-ChildItem -Path $extract -Recurse -Filter 'SDL3.dll' | Select-Object -First 1
    $lic = Get-ChildItem -Path $extract -Recurse -Filter 'LICENSE.txt' | Select-Object -First 1
    if (-not $dll) { throw "압축 안에 SDL3.dll 이 없습니다." }
    if (-not $lic) { throw "압축 안에 LICENSE.txt 가 없습니다." }

    New-Item -ItemType Directory -Force -Path $targetDir | Out-Null
    Copy-Item -Path $dll.FullName -Destination $dllPath -Force
    Copy-Item -Path $lic.FullName -Destination (Join-Path $targetDir 'SDL3-LICENSE.txt') -Force
    Set-Content -Path $stampPath -Value $SdlVersion -Encoding ASCII

    Write-Host "완료: SDL3 $SdlVersion -> $targetDir"
}
finally {
    Remove-Item -Path $work -Recurse -Force -ErrorAction SilentlyContinue
}
