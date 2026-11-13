# Microsoft Store 제출

MSIX로 포장해 무료로 배포한다. Store가 인증 후 패키지에 서명하므로 코드 서명 인증서가 필요 없다. GitHub 릴리스(zip) 배포는 그대로 유지한다.

## 식별자

| 항목 | 값 |
|---|---|
| 패키지 이름 | `jacking75.JoyConMouse` (예약 거절 시 `jacking75.JoyCMouse`) |
| 게시자 | `CN=E66A7C98-9426-41DB-9B55-AC993F05E1A3` |
| 게시자 표시 이름 | `jacking75` |

Partner Center **제품 ID** 화면의 값과 다르면 `-IdentityName`, `-DisplayName`으로 맞춘다.

## 패키지 만들기

```powershell
powershell -ExecutionPolicy Bypass -File build/store-package.ps1
# 이름이 JoyCMouse 로 바뀐 경우
powershell -ExecutionPolicy Bypass -File build/store-package.ps1 -IdentityName jacking75.JoyCMouse -DisplayName JoyCMouse
```

결과: `artifacts/store/<이름>-<버전>.0-x64.msix`, `StoreTile-300.png`, `SHA256SUMS.txt`.

Store 빌드(`-p:StoreBuild=true`)는 시작 프로그램 등록에 Windows StartupTask를 쓴다(패키지 앱은 Run 레지스트리를 쓸 수 없음). 로그인 시 StartupTask로 실행되면 창 없이 트레이로 시작한다.

## 제출 순서 (Partner Center)

1. 앱 이름 예약 → 제품 ID 값 확인
2. 가격: 무료, 공개 배포
3. 속성: 범주 유틸리티 + 도구, 개인정보처리방침·웹사이트·지원 URL (`listing.json`)
4. 연령 등급: IARC 설문(폭력·채팅·구매 없음)
5. 패키지: MSIX 업로드 → `runFullTrust` 사유 입력(`listing.json`의 `runFullTrustJustification`)
6. Store 목록(한국어·영어): 설명·짧은 설명·기능·키워드·스크린샷(`listing.json`, `packaging/store/screenshots/`)
7. 제출 옵션: 인증 메모(`notesForCertification`)
8. 인증 제출

## 주의

- `docs/privacy.md`가 GitHub main에 올라가 있어야 개인정보처리방침 URL이 동작한다.
- 이름에 "Joy-Con" 상표가 들어 있어 거절될 수 있다. 거절되면 `JoyCMouse`로 다시 예약하고 위 명령으로 패키지를 다시 만든다.
- 이 PC는 개발자 모드가 꺼져 있어 MSIX 설치 실행은 검증하지 않았다. Store 빌드 실행 파일 단독 실행(SDL 초기화)과 MakeAppx 검사는 통과했다.
