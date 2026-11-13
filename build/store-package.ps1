<#
.SYNOPSIS
    Microsoft Store 제출용 MSIX(서명 없음)를 만든다: artifacts/store/<이름>-<버전>-x64.msix
.DESCRIPTION
    Store 가 인증 후 패키지에 서명하므로 코드 서명 인증서가 필요 없다.
    -p:StoreBuild=true 로 게시해 시작 프로그램 등록을 Windows StartupTask 로 처리하는 빌드를 만든다.
    Partner Center 에서 예약한 이름이 다르면 -IdentityName / -DisplayName 으로 바꾼다.
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File build/store-package.ps1
    powershell -ExecutionPolicy Bypass -File build/store-package.ps1 -IdentityName jacking75.JoyCMouse -DisplayName JoyCMouse
#>
[CmdletBinding()]
param(
    [string]$IdentityName = 'jacking75.JoyConMouse',
    [string]$DisplayName = 'JoyConMouse'
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
. (Join-Path $PSScriptRoot 'common.ps1')

$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot
try {
    & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'fetch-sdl.ps1')
    if ($LASTEXITCODE -ne 0) { throw "fetch-sdl.ps1 실패" }

    # 버전: 앱 csproj 의 <Version>(x.y.z) → MSIX x.y.z.0
    $appProj = Join-Path $repoRoot 'src\JoyConMouse.App\JoyConMouse.App.csproj'
    [xml]$proj = Get-Content $appProj -Raw -Encoding UTF8
    $version = ($proj.Project.PropertyGroup | ForEach-Object { $_.Version } | Where-Object { $_ } | Select-Object -First 1)
    if ($version -notmatch '^\d+\.\d+\.\d+$') { throw "Version 형식이 x.y.z 가 아닙니다: $version" }
    $msixVersion = "$version.0"

    $out     = Join-Path $repoRoot 'artifacts\store'
    $payload = Join-Path $out 'payload'
    if (Test-Path $payload) { Remove-Item $payload -Recurse -Force }
    New-Item -ItemType Directory -Force -Path $out | Out-Null

    # 1. Store 빌드 게시(self-contained, 폴더)
    #    PackagingBuild=true 면 bins\ 대신 .NET 기본 폴더(bin\, obj\ 아래 TFM·RID별)를 쓴다.
    #    출력 폴더를 명령줄 전역 속성으로 강제하면 참조 프로젝트의 RID·비RID 빌드가 같은 폴더를 공유해 서로 파일을 지운다.
    #    이전 게시·일반 빌드의 증분 상태가 남아 있으면 참조 프로젝트 출력이 지워져
    #    CS0006/MSB3030 으로 실패하므로, 매번 깨끗한 상태에서 빌드한다.
    Clear-ProjectBuildState $repoRoot
    & dotnet publish $appProj -c Release -r win-x64 --self-contained true -p:PublishReadyToRun=true `
        -p:StoreBuild=true -p:PackagingBuild=true -o $payload
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish 실패" }
    foreach ($f in 'JoyConMouse.exe', 'SDL3.dll') {
        if (-not (Test-Path (Join-Path $payload $f))) { throw "$f 가 없습니다." }
    }
    Copy-Item (Join-Path $repoRoot 'LICENSE') (Join-Path $payload 'LICENSE.txt')
    Copy-Item (Join-Path $repoRoot 'THIRD-PARTY-NOTICES.txt') $payload

    # 2. 매니페스트
    $manifest = Get-Content (Join-Path $repoRoot 'packaging\store\AppxManifest.xml') -Raw -Encoding UTF8
    $manifest = $manifest.Replace('{{IdentityName}}', $IdentityName).Replace('{{DisplayName}}', $DisplayName).Replace('{{Version}}', $msixVersion)
    [IO.File]::WriteAllText((Join-Path $payload 'AppxManifest.xml'), $manifest, (New-Object Text.UTF8Encoding $false))

    # 3. 로고(앱 아이콘 원본에서 생성, 타일은 여백을 둔다)
    Add-Type -AssemblyName System.Drawing
    $source = [Drawing.Image]::FromFile((Join-Path $repoRoot 'src\JoyConMouse.App\Assets\icon.png'))
    $assets = Join-Path $payload 'Assets'
    New-Item -ItemType Directory -Force -Path $assets | Out-Null
    try {
        foreach ($a in @(
            @{ Name = 'StoreLogo.png';          W = 50;  H = 50;  Scale = 1.0 },
            @{ Name = 'Square44x44Logo.png';    W = 44;  H = 44;  Scale = 1.0 },
            @{ Name = 'Square150x150Logo.png';  W = 150; H = 150; Scale = 0.66 },
            @{ Name = 'Wide310x150Logo.png';    W = 310; H = 150; Scale = 0.66 },
            @{ Name = 'StoreTile-300.png';      W = 300; H = 300; Scale = 0.8 }
        )) {
            $bmp = New-Object Drawing.Bitmap $a.W, $a.H
            $g = [Drawing.Graphics]::FromImage($bmp)
            $g.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $g.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::HighQuality
            $g.Clear([Drawing.Color]::Transparent)
            $s = [int]([Math]::Min($a.W, $a.H) * $a.Scale)
            $g.DrawImage($source, [int](($a.W - $s) / 2), [int](($a.H - $s) / 2), $s, $s)
            $g.Dispose()
            $target = if ($a.Name -like 'StoreTile*') { Join-Path $out $a.Name } else { Join-Path $assets $a.Name }
            $bmp.Save($target, [Drawing.Imaging.ImageFormat]::Png)
            $bmp.Dispose()
        }
    }
    finally { $source.Dispose() }

    # 4. MakeAppx 로 포장(매니페스트 의미 검사 포함)
    $sdkRoot = Join-Path ${env:ProgramFiles(x86)} 'Windows Kits\10\bin'
    $makeappx = Get-ChildItem -LiteralPath $sdkRoot -Filter makeappx.exe -Recurse -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -match '\\x64\\' } | Sort-Object FullName | Select-Object -Last 1
    if (-not $makeappx) { throw "Windows SDK 의 MakeAppx.exe 가 필요합니다." }
    $package = Join-Path $out "$DisplayName-$msixVersion-x64.msix"
    & $makeappx.FullName pack /d $payload /p $package /o | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "MakeAppx 포장 실패" }

    $hash = (Get-FileHash $package -Algorithm SHA256).Hash.ToLowerInvariant()
    [IO.File]::WriteAllText((Join-Path $out 'SHA256SUMS.txt'), "$hash  $(Split-Path $package -Leaf)`n", (New-Object Text.UTF8Encoding $false))
    Write-Host "패키지: $package ($([math]::Round((Get-Item $package).Length / 1MB, 1)) MB)"
    Write-Host "ID: $IdentityName, 표시 이름: $DisplayName, 버전: $msixVersion"
    Write-Host "SHA256: $hash"
}
finally {
    Pop-Location
}
