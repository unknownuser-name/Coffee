# 커피 게임 작업 보관 — 2026-11-03

사용자 요청으로 커피 작업을 여기서 멈추고 반지 게임 조사로 전환합니다. 자동 백그라운드 제작은 진행하지 않습니다.

최신 프로젝트: UnityProject (Unity6000.6.5f1). 최신 실행: CoffeeCraftLatteMouse/Build/CoffeeCraftLatteMouse.exe.
전체 과정 실행: StartCoffeeCraft.cmd. 우유 붓기 즉시 연습: StartLattePractice.cmd (-latteDemo).
보관 압축본: CoffeeCraft_Latte_Windows.zip. 미리보기: CoffeeCraftLatte/Review/easy-latte-preview.gif.

완료: 분쇄·표면 정리·탬핑·머신 삽입/잠금·컵 자동 맞춤·에스프레소·쉬운 우유 붓기. 손 추적 카메라, 잡기/놓기 보정, 단일 도트 손 커서. 우유는 Space 누르고 붓기/놓기 정지, 마우스 위치, 휠 높이; 기본 높이/위치 자동 보정과 잔 자동 기울임. 우유 붓기는 준비된 스팀 우유로 시작합니다.

최근 검증: EasyLatteModel188개, 최종 전체 자동 실행 및 즉시 연습 자동 실행 통과. Review/result.json과 Review/Practice/result.json. 실제 OS 키보드/마우스 전체 검증은 미완료이며, 사용자 직접 피드백으로 판정을 반복 조정했습니다.

현재 에셋은 임시 픽셀 렌더 형상. 최종 작화, 실제 스티밍 조작, 복잡한 라테 아트, 손님 제공 단계는 남아 있습니다. 기존 MilkCraft와 이전 실행본은 보존했습니다.

핵심 신규 소스: Assets/Scripts/Crafting/EasyLatteModel.cs, FrontLatteInteraction.cs, LatteArtGeometry.cs, FrontHandInteraction.cs, PixelHandCursor.cs. 단계별 기록은 docs/superpowers/plans/2026-11-03-easy-latte-pour.md 등 참조.

## 2026-11-03 마우스 고정 수정

최신 실행본은 CoffeeCraftLatteMouse/Build/CoffeeCraftLatteMouse.exe. StartCoffeeCraft.cmd 및 StartLattePractice.cmd가 이 실행본을 가리킨다. 이전 CoffeeCraftLatte와 ZIP은 보존했다.

우유 화면 클릭 시 CursorLockMode.Locked로 고정하며 상대 마우스 이동으로 조작한다. 별도 OS 화살표는 숨기고 손잡이의 손을 포인터로 사용한다. 이동량을 65%로 줄이던 보정을 제거했고, 실제 목표 좌표 자체를 컵 안 원형 범위로 제한해 가장자리에서 반대로 움직이면 즉시 반응한다. 카메라 방향을 반영해 화면 방향으로 움직인다. Esc/포커스 이탈/종료 시 마우스 고정을 해제하고 붓기를 멈추며, 클릭으로 재개한다. 다른 제작 화면은 클릭 시 창 안에 마우스를 제한한다.

이번 검증: LattePointerTests 5개, EasyLatteTests 188개 통과. 최종 Unity 빌드 exit0. 최종 practice-final.log와 full-final.log는 각각 exit0이며 Review/Practice/result.json, Review/result.json에 통과 기록이 있다. 단독 연습 최초 검사는 초기 프레임 지연 중 고정 0.65초 대기와 모델의 프레임 50ms 제한이 달라 실패했다. 검사 대기를 모델과 같은 누적 시간으로 보완한 뒤 재검사 통과. 초기 실패 자료는 Review/Practice/failure-initial-startup.txt로 보관했다.

실제 OS 입력 확인은 Computer Use 창 캡처가 DX12와 DX11 모두 검게 나와 완료하지 못했다. 자동 컨트롤러 검사 통과를 실제 마우스 인수 완료로 표현하지 않는다. CoffeeCraftLatteMouse/Review/mouse-latte-preview.gif는 자동 검사의 연속 렌더 프레임이며 실제 OS 입력 녹화가 아니다.
