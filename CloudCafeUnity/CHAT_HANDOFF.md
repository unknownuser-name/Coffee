# 커피 게임 전용 대화 인계 — 2026-11-03

사용자는 반지 게임과 커피 게임을 두 대화에서 나눠 작업하려고 한다. 이 대화는 커피만 맡는다. 반지 작업은 별도 대화의 `C:/01_자동/codex/LittleGlintWorkshop`에 있으므로 그 폴더는 수정하지 않는다.

## 실제 현재 상태

- 먼저 같은 폴더의 RESUME_COFFEE.md를 읽는다.
- Unity 프로젝트: `CloudCafeUnity/UnityProject` (Unity6000.6.5f1).
- 최신 실행본: `CloudCafeUnity/CoffeeCraftLatteMouse/Build/CoffeeCraftLatteMouse.exe`.
- 전체 제작 실행: `CloudCafeUnity/StartCoffeeCraft.cmd`.
- 우유 붓기 연습: `CloudCafeUnity/StartLattePractice.cmd`.
- 보관 ZIP: CoffeeCraft_Latte_Windows.zip. GIF: CoffeeCraftLatte/Review/easy-latte-preview.gif.
- 분쇄→표면 정리→탬핑→포터필터 삽입/잠금→컵 자리 맞춤→추출→쉬운 우유 붓기까지 연결했다.
- 우유 붓기: 마우스로 위치, Space를 누르는 동안 붓기/놓으면 정지, 휠로 높이. 자동 위치·높이 보조와 잔 기울임이 있다. 준비된 스팀 우유로 시작하며 실제 스티밍은 아직 없다.
- 검증 기록: EasyLatteModel188개, 전체 흐름/즉시 연습 자동 실행 결과가 Review/result.json 및 Review/Practice/result.json에 있다. 이는 과거 기록이며 새 변경의 통과 증거로 대신 쓰지 않는다. 실제 OS 입력 전체 인수는 미완료다.

## 사용자가 원하는 방향

- 실제 커피 제작 동작을 순서대로 직접 조작하는 게임. 공정 순서를 버튼 하나로 생략하지 않는다.
- 앞에서 도구를 보고 집어서 이동한다. 손 커서로 잡은 위치가 보이고, 끼우는 자리와 컵 받침에는 넉넉한 판정·자리 맞춤을 제공한다.
- 우유는 피처 그림과 분리된 액체다. 안쪽 액면/잔량/밖으로 이어지는 줄기/컵이 채워짐이 연동돼야 한다. 단순2D 회전으로 기울임을 흉내내지 않는다.
- 우선 임시 도트·3D 형상으로 재미와 조작을 완성하고 나중에 최종 작화를 교체할 수 있다.
- 사용자는 현재 모바일에서도 확인하므로 로컬 MP4 링크만 보내지 말고 실제 플레이를 움직이는 GIF로 보여주는 편이 좋다. 정지 사진을 이어 붙인 것을 실제 플레이 영상이라고 부르지 않는다.
- 커피톡 등은 분위기·연출·구조 참고용이며 타 게임 자산·코드를 복제하지 않는다.
- 이미 승인한 작업은 반복 허락을 묻지 말고 구현·검증·재개 기록까지 이어서 한다. 새 유용한 외부 도구는 루트 AGENTS.md의 도구 목록/승인 규칙을 따른다.

## 재개할 때

핵심 소스는 UnityProject/Assets/Scripts/Crafting의 EasyLatteModel.cs, FrontLatteInteraction.cs, LatteArtGeometry.cs, FrontHandInteraction.cs, PixelHandCursor.cs다. UnityProject와 CloudCafeUnity/docs/superpowers의 기존 지침/계획을 읽고 실제 파일을 확인한다. 오래된 MilkCraft/FrontBrew/Build와 최신 CoffeeCraftLatte를 혼동하지 않는다.

최종 작화, 실제 스티밍, 복잡한 라테 아트, 손님 제공 단계가 남아 있다. 새 기능의 우선순위는 이 전용 대화에서 사용자의 후속 지시와 현재 상태를 기준으로 정한다. 새 대화 생성만으로 앞선 모든 게임 구현이 완료됐다고 주장하지 않는다.

## 동시 작업 경계

커피는 CloudCafeUnity 안에서만 수정하고 반지는 LittleGlintWorkshop 안에서만 수정한다. 다른 대화의 Unity/실행 중인 게임을 강제로 종료하거나 사용자의 미저장 장면을 덮어쓰지 않는다. 이미 동일 Unity 프로젝트가 열려 있으면 두 번째 Editor를 강제 실행하지 말고 프로젝트/프로세스 상태부터 확인한다.

## 2026-11-03 마우스 고정 수정

최신 실행본은 CoffeeCraftLatteMouse/Build/CoffeeCraftLatteMouse.exe. StartCoffeeCraft.cmd 및 StartLattePractice.cmd가 이 실행본을 가리킨다. 이전 CoffeeCraftLatte와 ZIP은 보존했다.

우유 화면 클릭 시 CursorLockMode.Locked로 고정하며 상대 마우스 이동으로 조작한다. 별도 OS 화살표는 숨기고 손잡이의 손을 포인터로 사용한다. 이동량을 65%로 줄이던 보정을 제거했고, 실제 목표 좌표 자체를 컵 안 원형 범위로 제한해 가장자리에서 반대로 움직이면 즉시 반응한다. 카메라 방향을 반영해 화면 방향으로 움직인다. Esc/포커스 이탈/종료 시 마우스 고정을 해제하고 붓기를 멈추며, 클릭으로 재개한다. 다른 제작 화면은 클릭 시 창 안에 마우스를 제한한다.

이번 검증: LattePointerTests 5개, EasyLatteTests 188개 통과. 최종 Unity 빌드 exit0. 최종 practice-final.log와 full-final.log는 각각 exit0이며 Review/Practice/result.json, Review/result.json에 통과 기록이 있다. 단독 연습 최초 검사는 초기 프레임 지연 중 고정 0.65초 대기와 모델의 프레임 50ms 제한이 달라 실패했다. 검사 대기를 모델과 같은 누적 시간으로 보완한 뒤 재검사 통과. 초기 실패 자료는 Review/Practice/failure-initial-startup.txt로 보관했다.

실제 OS 입력 확인은 Computer Use 창 캡처가 DX12와 DX11 모두 검게 나와 완료하지 못했다. 자동 컨트롤러 검사 통과를 실제 마우스 인수 완료로 표현하지 않는다. CoffeeCraftLatteMouse/Review/mouse-latte-preview.gif는 자동 검사의 연속 렌더 프레임이며 실제 OS 입력 녹화가 아니다.
