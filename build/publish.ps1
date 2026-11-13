<#
.SYNOPSIS
    Windows x64 배포 zip 을 만든다: artifacts/JoyConMouse-<버전>-win-x64.zip
.DESCRIPTION
    self-contained 폴더 배포(단일 파일 아님)로 SDL3.dll 을 exe 옆에 그대로 둔다.
    LICENSE, THIRD-PARTY-NOTICES.txt, 사용 안내를 함께 넣는다.
    Windows PowerShell 5.1 과 PowerShell 7 모두에서 동작한다.
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File build/publish.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot
try {
    # 1. SDL3 바이너리 확보
    & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'fetch-sdl.ps1')
    if ($LASTEXITCODE -ne 0) { throw "fetch-sdl.ps1 실패" }

    # 2. 버전 (앱 csproj 의 <Version>)
    $appProj = Join-Path $repoRoot 'src\JoyConMouse.App\JoyConMouse.App.csproj'
    [xml]$proj = Get-Content $appProj -Raw -Encoding UTF8
    $version = ($proj.Project.PropertyGroup | ForEach-Object { $_.Version } | Where-Object { $_ } | Select-Object -First 1)
    if (-not $version) { throw "JoyConMouse.App.csproj 에 Version 이 없습니다." }

    $artifacts = Join-Path $repoRoot 'artifacts'
    $outDir    = Join-Path $artifacts 'JoyConMouse'
    $zipPath   = Join-Path $artifacts "JoyConMouse-$version-win-x64.zip"
    if (Test-Path $outDir) { Remove-Item $outDir -Recurse -Force }
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

    # 3. 게시. 중간 빌드 출력은 일반 빌드 폴더(bins\)와 섞이지 않게 artifacts\build 로 보낸다.
    $buildDir = Join-Path $artifacts 'build\'
    & dotnet publish src/JoyConMouse.App/JoyConMouse.App.csproj -c Release -r win-x64 --self-contained true `
        -p:PublishReadyToRun=true "-p:OutputPath=$buildDir" -o $outDir
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish 실패" }

    # 4. 산출물 확인: exe 이름 규칙(확장자 앞 본 이름에 점 없음)과 필수 파일
    $exe = Join-Path $outDir 'JoyConMouse.exe'
    if (-not (Test-Path $exe)) { throw "JoyConMouse.exe 가 없습니다." }
    if (-not (Test-Path (Join-Path $outDir 'SDL3.dll'))) { throw "SDL3.dll 이 없습니다." }
    $badExe = Get-ChildItem $outDir -Filter '*.exe' | Where-Object { $_.BaseName.Contains('.') }
    if ($badExe) { throw "exe 이름 규칙 위반: $($badExe.Name -join ', ')" }

    # 5. 고지·안내 문서
    Copy-Item (Join-Path $repoRoot 'LICENSE') (Join-Path $outDir 'LICENSE.txt')
    Copy-Item (Join-Path $repoRoot 'THIRD-PARTY-NOTICES.txt') $outDir
    $guide = Get-ChildItem (Join-Path $repoRoot 'docs') -Filter '사용안내-*.md' | Select-Object -First 1
    if (-not $guide) { throw "docs/사용안내-*.md 가 없습니다." }
    Copy-Item $guide.FullName (Join-Path $outDir '사용안내.md')

    # 6. 압축
    Compress-Archive -Path (Join-Path $outDir '*') -DestinationPath $zipPath
    $sizeMb = [math]::Round((Get-Item $zipPath).Length / 1MB, 1)
    Write-Host "완료: $zipPath ($sizeMb MB)"
}
finally {
    Pop-Location
}
