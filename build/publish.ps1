<#
.SYNOPSIS
    Windows x64 배포 zip 을 만든다: artifacts/JoyConMouse-<버전>-win-x64.zip
.DESCRIPTION
    self-contained 단일 파일(JoyConMouse.exe) + ReadyToRun 으로 게시한다.
    WPF·WinForms 는 Native AOT 를 지원하지 않아 AOT 대신 ReadyToRun 으로 시작 속도를 확보한다.
    네이티브 DLL(SDL3.dll, WPF 네이티브 DLL)은 exe 에 넣지 않고 옆에 둔다(임시 폴더 추출 없음).
    LICENSE, THIRD-PARTY-NOTICES.txt, 사용 안내를 함께 넣는다.
    Windows PowerShell 5.1 과 PowerShell 7 모두에서 동작한다.
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File build/publish.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
. (Join-Path $PSScriptRoot 'common.ps1')

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

    # 3. 게시. PackagingBuild=true 면 bins\ 대신 .NET 기본 폴더(bin\, obj\ 아래 TFM·RID별)를 써서
    #    일반 빌드(bins\)와 섞이지 않는다. 출력 폴더를 명령줄 전역 속성으로 강제하면 참조 프로젝트의
    #    RID·비RID 빌드가 같은 폴더를 공유해 서로 파일을 지우므로 그렇게 하지 않는다.
    #    이전 게시(특히 Store 빌드)의 증분 상태가 남아 있으면 참조 프로젝트 출력이 지워져
    #    CS0006/MSB3030 으로 실패하므로, 매번 깨끗한 상태에서 빌드한다.
    Clear-ProjectBuildState $repoRoot
    & dotnet publish src/JoyConMouse.App/JoyConMouse.App.csproj -c Release -r win-x64 --self-contained true `
        -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=false `
        -p:PublishReadyToRun=true -p:PackagingBuild=true -o $outDir
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish 실패" }

    # 4. 산출물 확인: exe 이름 규칙(확장자 앞 본 이름에 점 없음)과 필수 파일
    $exe = Join-Path $outDir 'JoyConMouse.exe'
    if (-not (Test-Path $exe)) { throw "JoyConMouse.exe 가 없습니다." }
    if (-not (Test-Path (Join-Path $outDir 'SDL3.dll'))) { throw "SDL3.dll 이 없습니다." }
    $badExe = Get-ChildItem $outDir -Filter '*.exe' | Where-Object { $_.BaseName.Contains('.') }
    if ($badExe) { throw "exe 이름 규칙 위반: $($badExe.Name -join ', ')" }
    # 단일 파일 게시라면 관리 어셈블리(.dll)는 exe 에 들어가고, 옆에는 네이티브 DLL 만 남는다.
    if (Test-Path (Join-Path $outDir 'JoyConMouse.dll')) { throw "단일 파일로 게시되지 않았습니다(JoyConMouse.dll 이 따로 있음)." }
    # WPF 의 C++/CLI 어셈블리가 번들에 들어가면 실행 즉시 종료(BadImageFormatException)되므로 옆에 있어야 한다.
    if (-not (Test-Path (Join-Path $outDir 'DirectWriteForwarder.dll'))) { throw "DirectWriteForwarder.dll 이 exe 옆에 없습니다(번들에 묶이면 실행되지 않음)." }
    # 디버그 기호는 배포본에 넣지 않는다.
    Get-ChildItem $outDir -Filter '*.pdb' | Remove-Item -Force

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
