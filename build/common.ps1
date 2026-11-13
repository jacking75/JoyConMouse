# build/publish.ps1, build/store-package.ps1 이 함께 쓰는 함수.

<#
.SYNOPSIS
    게시 빌드 전에 src 프로젝트의 bin·obj 를 지우고 MSBuild·컴파일러 서버를 내린다.
.DESCRIPTION
    일반 빌드(bins\), zip 게시, Store 게시(-p:StoreBuild=true)는 전역 속성이 서로 달라
    같은 obj 의 증분 기록이 섞이면 참조 프로젝트 출력이 지워져 CS0006/MSB3030 으로 실패한다.
    게시는 드물게 하므로 매번 깨끗하게 빌드한다. bins\ 와 artifacts\ 는 건드리지 않는다.
#>
function Clear-ProjectBuildState([string]$RepoRoot) {
    & dotnet build-server shutdown | Out-Null
    foreach ($project in 'JoyConMouse.Core', 'JoyConMouse.Platform', 'JoyConMouse.App') {
        foreach ($dir in 'bin', 'obj') {
            $path = Join-Path $RepoRoot "src\$project\$dir"
            if (Test-Path $path) {
                Remove-Item $path -Recurse -Force
            }
        }
    }
}
