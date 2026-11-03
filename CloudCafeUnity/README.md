# 구름 위의 카페 — 커피 제작 공동 작업

준수가 이어서 작업할 수 있는 Unity 소스입니다. 최신 작업은 **분쇄부터 추출, 쉬운 우유 붓기까지 직접 조작하는 커피 제작 시제품**입니다. 기존 저장소의 옥상 게임과는 별도 프로젝트입니다.

## 원두 봉투 18종 등록

사실적인 원두 봉투 PNG와 산지·품종·향미 데이터를 `UnityProject/Assets/Resources/CoffeeBags`에 등록했습니다. `FrontGrind`를 실행한 뒤 **B / 원두 선택**에서 사진과 정보를 확인하고 분쇄 시작 전 원두를 고릅니다. 원두별 ScriptableObject 18개와 카탈로그, Sprite 설정이 함께 포함돼 있어 별도 패키지 가져오기는 필요하지 않습니다. 등록 원두와 이미지·검증·원본 출처는 [저장소 README](../README.md)를 참고하세요.

## Unity에서 바로 열기

1. 이 브랜치를 내려받습니다.
2. Unity Hub에서 **이 폴더 안의 `UnityProject`** 를 추가합니다. 저장소 루트의 Unity 프로젝트를 열지 않습니다.
3. Unity **6000.6.5f1**로 열고 패키지 가져오기가 끝날 때까지 기다립니다.
4. `Assets/Scenes/FrontGrind.unity`를 열어 Play를 누릅니다.

`.meta` 파일과 DOTween 라이브러리·라이선스를 함께 포함했습니다. Library와 이전 실행본·대형 압축본은 제외했습니다. 에셋이 일반 파일로 포함돼 있어 이 커피 폴더 자체에는 별도 Git LFS 다운로드가 필요하지 않습니다.

## Windows 실행본 만들기

Unity Editor를 닫은 상태에서 이 폴더의 `BuildCoffeeCraft.ps1`을 실행합니다. 기본 Unity 설치 경로가 다르면 `-UnityEditor`로 Unity.exe 위치를 지정합니다. 빌드가 끝나면 `StartCoffeeCraft.cmd`로 전체 제작 과정, `StartLattePractice.cmd`로 우유 붓기만 시작합니다. 생성 위치는 `CoffeeCraftLatteMouse/Build/CoffeeCraftLatteMouse.exe`입니다.

## 지금 가능한 제작과 조작

분쇄 → 표면 정리 → 탬핑 → 머신 삽입/잠금 → 컵 자리 맞춤 → 에스프레소 추출 → 완성 컵 잡기 → 우유 붓기가 연결돼 있습니다. 도구는 마우스로 잡아 옮기며, 가까운 자리는 넉넉하게 맞춰 줍니다.

우유 화면을 클릭하면 마우스가 고정되고 피처 손잡이의 손이 포인터가 됩니다. 마우스로 위치, Space를 누르는 동안 붓기, 놓으면 정지, 휠로 높이를 조절합니다. Esc 또는 다른 앱으로 전환하면 멈추고 마우스를 풀며, 게임을 클릭하면 이어집니다. R은 다시 연습입니다. 컵 가장자리에서 반대로 움직이면 즉시 반응하도록 수정했습니다.

## 먼저 볼 코드

| 파일 (`UnityProject/Assets/Scripts/Crafting/`) | 역할 |
|---|---|
| `FrontGrindController.cs` | 제작 흐름과 공통 입력 |
| `FrontLatteInteraction.cs` | 우유 조작, 마우스 고정/해제 |
| `EasyLatteModel.cs` | 우유 잔량·낙하·컵 양·붓기 위치 제한 |
| `LatteArtGeometry.cs` | 피처·분리된 액면·줄기·컵 표현 |
| `FrontHandInteraction.cs`, `PixelHandCursor.cs` | 도구 손잡이의 손 커서 |
| `FrontBrewInteraction.cs` | 머신 체결·컵·추출 |

## 남은 작업과 확인 범위

우유는 준비된 스팀 우유로 시작합니다. 실제 스티밍, 복잡한 라테 아트, 손님 제공, 최종 작화가 남아 있습니다. 임시 도트·3D 형상으로 조작감을 먼저 완성하는 방향입니다.

2026-11-03 검증: 우유 모델 188개, 위치 이동 회귀 검사 5개, Unity Windows 빌드, 전체 제작과 우유 단독 자동 검사 통과. 결과는 `Review/result.json`과 `Review/Practice/result.json`입니다. 실제 OS 마우스 입력 인수는 화면 캡처 도구가 검게 나와 미완료입니다. 자동 검사 통과를 실제 입력 검증 완료로 해석하지 않습니다.

`Review/mouse-latte-preview.gif`는 자동 조작 실행 중 렌더한 연속 화면이며 실제 OS 입력 녹화는 아닙니다.

## 공동 작업

커피 수정은 `CloudCafeUnity/`에서만 진행하고 다른 게임 폴더는 건드리지 않습니다. 기능별 브랜치에서 수정하고 `.meta` 파일을 함께 커밋합니다. 씬·ProjectSettings는 동시 수정 전에 담당을 맞춥니다. 상세 기록은 `CHAT_HANDOFF.md`, `RESUME_COFFEE.md`, `docs/superpowers/plans/2026-11-03-latte-mouse-lock.md`를 참고하세요.
