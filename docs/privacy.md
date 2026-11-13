# JoyConMouse 개인정보 처리방침 / Privacy policy

최종 갱신일 / Last updated: 2026-11-13

## 한국어

JoyConMouse는 Choi HeungBae(`jacking75`)가 제공하는 무료 오픈소스 Windows 앱입니다. 앱 계정, 구독, 앱 내 구매가 없습니다.

**처리하는 정보.** 사용자가 Bluetooth로 연결한 Joy-Con의 버튼·스틱 입력을 PC에서 읽어 마우스 이동·클릭·스크롤로 바꿉니다. 입력값은 저장하거나 전송하지 않습니다. 장치를 구분하기 위해 장치 시리얼(Bluetooth 주소)을 해시한 짧은 식별자만 사용하며, 원본 시리얼·Bluetooth 주소는 저장하지 않습니다.

**PC에 저장하는 정보.** 감도·버튼 배치 등 설정과 마지막으로 사용한 장치의 해시 식별자를 앱 데이터 폴더(`settings.json`)에 저장합니다. 동작 기록(앱 시작·장치 연결·오류)을 로그 폴더에 저장하며, 7일이 지난 로그는 자동으로 지웁니다. 로그에는 장치 이름(예: "Nintendo Switch Joy-Con (L)")과 해시 식별자가 포함될 수 있습니다.

**전송하지 않음.** JoyConMouse에는 원격 분석, 광고, 자동 업데이트 확인, 로그 업로드 등 네트워크 통신 기능이 없습니다. Microsoft Store의 다운로드·업데이트·서비스 통계는 Microsoft의 정책에 따라 처리될 수 있습니다.

**진단 정보 내보내기.** 사용자가 메뉴를 누를 때만 설정·최근 로그·버전 정보를 담은 zip 파일을 바탕화면에 만듭니다. 자동으로 전송하지 않습니다.

**삭제.** 앱 설정과 로그는 앱을 제거하면 함께 삭제됩니다(Microsoft Store 설치판). 내보낸 진단 zip은 사용자가 직접 지웁니다.

**문의.** [GitHub Issues](https://github.com/jacking75/JoyConMouse/issues)

## English

JoyConMouse is a free, open-source Windows app provided by Choi HeungBae (`jacking75`). It has no app account, subscription, or in-app purchase.

**Information processed.** The app reads button and stick input from a Joy-Con that you connect over Bluetooth and turns it into mouse movement, clicks, and scrolling on your PC. Input is neither stored nor transmitted. To tell devices apart it uses only a short hashed identifier derived from the device serial (Bluetooth address); the original serial and Bluetooth address are not stored.

**Stored on your PC.** Settings such as sensitivity and button layout, and the hashed identifier of the last used device, are saved in the app data folder (`settings.json`). An activity log (app start, device connection, errors) is saved in a log folder; logs older than 7 days are deleted automatically. Logs may contain device names (for example "Nintendo Switch Joy-Con (L)") and hashed identifiers.

**Nothing is sent.** JoyConMouse has no analytics, advertising, update checks, log upload, or any other network communication. Microsoft Store downloads, updates, and service statistics may be processed under Microsoft's policies.

**Export diagnostics.** Only when you choose it from the menu, the app creates a zip file on your desktop with settings, recent logs, and version information. It is never sent automatically.

**Deletion.** App settings and logs are removed when you uninstall the app (Microsoft Store version). Delete exported diagnostics zip files yourself.

**Contact.** [GitHub Issues](https://github.com/jacking75/JoyConMouse/issues)

JoyConMouse is an independent project and is not affiliated with Nintendo. Nintendo Switch and Joy-Con are trademarks of Nintendo.
