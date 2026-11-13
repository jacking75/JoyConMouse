<p align="center">
  <img src="src/JoyConMouse.App/Assets/icon.png" width="96" alt="JoyConMouse 아이콘">
</p>

<h1 align="center">JoyConMouse</h1>

<p align="center">
  <b>조이콘 하나로, 한손 무선 마우스.</b><br>
  왼쪽이든 오른쪽이든 Joy-Con 한 개를 Bluetooth로 연결해 커서 이동·클릭·드래그·스크롤을 합니다.
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Windows-11%20x64-0078D4" alt="Windows 11 x64">
  <img src="https://img.shields.io/badge/.NET-11-512BD4" alt=".NET 11">
  <img src="https://img.shields.io/badge/license-MIT-green" alt="MIT License">
</p>

<p align="center">
  <img src="docs/images/hero.jpg" width="820" alt="소파에서 조이콘 하나로 노트북 커서를 움직이는 모습">
</p>

## 특징

- **설치 없이 실행** — zip을 풀고 `JoyConMouse.exe`만 실행하면 됩니다. .NET 설치도 필요 없습니다.
- **왼쪽·오른쪽 모두 지원** — 감도와 버튼 배치를 좌우별로 따로 저장합니다.
- **정밀한 커서** — 데드존과 속도 곡선으로 작은 버튼도 정확히 누를 수 있습니다.
- **네 방향 스크롤** — 가로 스크롤까지 앱 안에서 처리합니다. 다른 도구가 필요 없습니다.
- **버튼 자유 재지정** — 클릭·스크롤·일시정지를 원하는 버튼에 지정합니다.
- **트레이 상주** — 시작 프로그램 등록, 연결 끊김·재연결 알림, 연결 확인 화면을 제공합니다.
- **가볍고 조용함** — CPU 사용률 0.1% 미만. 자이로를 쓰지 않고, 계정·네트워크 통신이 없습니다.

## 시작하기

1. [Releases](../../releases)에서 `JoyConMouse-<버전>-win-x64.zip`을 받아 압축을 풉니다.
2. Windows **설정 → Bluetooth 및 장치 → 장치 추가**에서 Joy-Con을 페어링합니다. 레일의 작은 Sync 버튼을 LED가 깜박일 때까지 누르세요.
3. `JoyConMouse.exe`를 실행하고, 조이콘을 **세로로** 쥡니다(스틱이 위).

SmartScreen 경고가 뜨면 **추가 정보 → 실행**을 누르세요(코드 서명 전 버전). 자세한 설정과 문제 해결은 [사용 안내](docs/사용안내-3.md)를 참고하세요.

## 기본 버튼 배치

| 동작 | 왼쪽 조이콘 | 오른쪽 조이콘 |
|---|:---:|:---:|
| 커서 이동 | 스틱 | 스틱 |
| 왼쪽 클릭 · 드래그 | ZL | ZR |
| 오른쪽 클릭 | L | R |
| 위 / 아래 스크롤 | ↑ / ↓ | X / B |
| 왼쪽 / 오른쪽 스크롤 | ← / → | Y / A |
| 가운데 클릭 | 스틱 누르기 | 스틱 누르기 |
| 일시정지 / 재개 | − | + |

## 소스에서 빌드

```powershell
powershell -ExecutionPolicy Bypass -File build/fetch-sdl.ps1   # SDL3 바이너리 받기
dotnet build JoyConMouse.slnx -c Release                       # → bins\JoyConMouse.exe
powershell -ExecutionPolicy Bypass -File build/publish.ps1     # → artifacts\JoyConMouse-<버전>-win-x64.zip
```

Windows 11 x64와 .NET SDK 11이 필요합니다.

## 라이선스

[MIT](LICENSE). 포함된 SDL3(zlib), CommunityToolkit.Mvvm(MIT), .NET 런타임(MIT)의 고지는 [THIRD-PARTY-NOTICES.txt](THIRD-PARTY-NOTICES.txt)에 있습니다.

> JoyConMouse는 닌텐도와 관계없는 독립 프로젝트입니다. Nintendo Switch와 Joy-Con은 Nintendo의 상표입니다.
