# Coffee

직접 원두를 갈고, 표면을 정리해 탬핑한 뒤 에스프레소를 추출하고 우유를 붓는 Unity 카페 게임입니다. 원본 커피 제작 프로젝트에 **사실적인 원두 봉투 18종**을 등록했습니다.

![산지별 사실적인 원두 봉투 18종](assets/coffee_bags_realistic/preview_realistic_18.png)

## 시작하기

1. 이 저장소를 내려받습니다.
2. Unity Hub에서 **`CloudCafeUnity/UnityProject`** 폴더를 추가합니다.
3. **Unity 6000.6.5f1**로 열고 패키지 가져오기가 끝날 때까지 기다립니다.
4. `Assets/Scenes/FrontGrind.unity`를 열고 Play합니다.
5. **B** 또는 **원두 선택** 버튼으로 봉투 사진·산지·향미를 확인하고 사용할 원두를 고릅니다. 원두 변경은 분쇄를 시작하기 전, 바스켓이 비어 있고 도구를 잡고 있지 않을 때 가능합니다.

별도 `.unitypackage`를 다시 가져올 필요 없이 이미지·카탈로그·원두 데이터가 프로젝트 안에 등록돼 있습니다.

## 커피 제작

**분쇄 → 표면 정리 → 탬핑 → 머신 삽입·잠금 → 컵 자리 맞춤 → 에스프레소 추출 → 우유 붓기**를 이어서 조작합니다.

| 조작 | 동작 |
| --- | --- |
| 마우스 드래그 | 포터필터·정리 도구·탬퍼·컵 이동 |
| 기계의 금색 버튼 / Space | 현재 단계의 분쇄·추출 또는 우유 붓기 |
| 우유 화면의 마우스 이동·휠 | 피처 위치·높이 조절 |
| B / 원두 선택 | 원두 18종 사진과 정보를 보고 선택 |
| R | 제작 다시 시작 |
| Esc | 선택창 닫기 또는 제작 일시 정지·마우스 해제 |

우유는 준비된 스팀 우유로 시작합니다. 실제 스티밍, 복잡한 라테 아트와 손님 제공은 후속 작업입니다.

### 원본 제작 흐름 미리보기

![커피 게임의 우유 붓기 자동 실행 미리보기](CloudCafeUnity/Review/mouse-latte-preview.gif)

위 GIF는 가져온 원본 프로젝트의 자동 조작 기록입니다. 이번 원두 등록 변경의 실제 마우스 입력 검증 영상은 아닙니다.

## 원두 에셋 등록

- **이미지:** 사실적인 고정 시점의 **1024 × 1536 투명 PNG 18종**. 크라프트지·무광 필름, 자연스러운 주름, 틴 타이·지퍼·봉합선과 산지별 인쇄를 표현했습니다.
- **Unity 등록:** PNG의 Sprite 설정, 원두별 `CoffeeBeanDefinition` 18개와 전체 `CoffeeBeanCatalog`를 `.meta`와 함께 포함합니다.
- **게임 연결:** `Resources/CoffeeBags/CoffeeBeanCatalog`를 읽어 실제 봉투 사진과 산지·품종·향미·산미·단맛·바디·설명을 표시합니다.
- **표현 범위:** 사진처럼 보이는 2.5D 스프라이트입니다. 자유 회전하는 3D 메시나 새 PBR 재질 세트는 포함하지 않습니다.
- **향미:** 1~5 지표는 산지 자료를 참고한 대략적인 게임용 프로필입니다. 실제 맛은 로트·가공·로스팅에 따라 달라지며 현재 분쇄·추출 물리 수치를 변경하지 않습니다.

원본 PNG와 프롬프트·출처·별도 Unity 패키지는 [`assets/coffee_bags_realistic`](assets/coffee_bags_realistic/)에서 확인할 수 있습니다. 이전 카툰 시안은 작업 PC에 보존하고 이 저장소에서는 제외했습니다.

## 등록된 산지 18종

| 국가 · 산지 | 품종 | 대표 향미 | 산미 / 단맛 / 바디 |
| --- | --- | --- | --- |
| 에티오피아 예가체프 | Kurume | 홍차 · 핵과일 · 시트러스 | 5 / 4 / 2 |
| 콜롬비아 안티오키아 | Caturra | 붉은 베리 · 초콜릿 · 크림 | 3 / 4 / 3 |
| 탄자니아 킬리만자로 | Bourbon | 시트러스 · 코코아 · 홍차 | 4 / 3 / 4 |
| 르완다 니야마셰케 | Bourbon | 붉은 베리 · 시트러스 · 달콤한 향신료 | 4 / 4 / 3 |
| 에콰도르 로하 | Typica | 핵과일 · 시트러스 · 초콜릿 | 3 / 4 / 3 |
| 브라질 세하두 미네이루 | Catuai | 밀크 초콜릿 · 구운 견과 · 캐러멜 | 2 / 4 / 4 |
| 브라질 만치케이라 지 미나스 | Mundo Novo | 코코아 · 구운 견과 · 말린 과일 | 2 / 4 / 4 |
| 엘살바도르 찰라테낭고 | Pacamara | 자두 · 청포도 · 흑설탕 | 4 / 4 / 3 |
| 과테말라 코반 | Maragogipe | 들꽃 · 오렌지 · 초콜릿 | 3 / 3 / 3 |
| 과테말라 산타로사 | Pache | 붉은 사과 · 캐러멜 · 코코아 | 3 / 4 / 3 |
| 케냐 키리냐가 | SL28 | 블랙커런트 · 시트러스 · 홍차 | 5 / 4 / 3 |
| 케냐 니에리 | SL34 | 블랙베리 · 적포도 · 흑설탕 | 4 / 4 / 4 |
| 케냐 키암부 | Ruiru 11 | 블랙커런트 · 달콤한 꽃향 · 시트러스 | 4 / 4 / 3 |
| 케냐 메루 | Batian | 시트러스 · 청포도 · 홍차 | 4 / 3 / 3 |
| 파나마 보케테 | Gesha | 재스민 · 베르가모트 · 복숭아 | 5 / 4 / 2 |
| 코스타리카 웨스트 밸리 | Villa Sarchi | 꿀 · 살구 · 시트러스 | 3 / 5 / 3 |
| 멕시코 치아파스 | Marsellesa | 흑설탕 · 다크 초콜릿 · 시트러스 | 3 / 4 / 4 |
| 온두라스 엘 파라이소 | Parainema | 시트러스 · 캐러멜 · 열대과일 | 4 / 4 / 3 |

모든 지표는 5점 기준의 게임용 상대 프로필입니다. 각 재배 조합의 근거와 가공·로스팅 선택은 [원두 원본 데이터](assets/coffee_bags_realistic/catalog/products.json)에 담았습니다.

## 프로젝트 구성

| 경로 | 내용 |
| --- | --- |
| [`CloudCafeUnity/UnityProject`](CloudCafeUnity/UnityProject/) | Unity에서 여는 카페 게임 |
| [`Assets/Scenes/FrontGrind.unity`](CloudCafeUnity/UnityProject/Assets/Scenes/FrontGrind.unity) | 전체 제작 과정의 시작 씬 |
| [`Assets/Scripts/Crafting`](CloudCafeUnity/UnityProject/Assets/Scripts/Crafting/) | 분쇄·탬핑·추출·우유 조작과 원두 선택 |
| [`Assets/Scripts/Coffee`](CloudCafeUnity/UnityProject/Assets/Scripts/Coffee/) | 원두 카탈로그·정의 |
| [`Assets/Resources/CoffeeBags`](CloudCafeUnity/UnityProject/Assets/Resources/CoffeeBags/) | 등록된 봉투 이미지·원두 18개·카탈로그·원본 정보 |
| [`assets/coffee_bags_realistic`](assets/coffee_bags_realistic/) | 최종 원본 이미지, 프롬프트, 미리보기, `.unitypackage` |
| [`CloudCafeUnity/Tests`](CloudCafeUnity/Tests/) | 기존 커피 제작 모델 회귀 테스트 |
| [`tools`](tools/) | 에셋 재등록 및 검증 도구 |
| [`docs/SOURCE_IMPORT.md`](docs/SOURCE_IMPORT.md) | 원본 브랜치·커밋 및 가져오기 범위 |

## 원본과 검증 범위

[SDHQN의 `coffee-crafting-20261103` 브랜치](https://github.com/blueanggel10004-tech/SDHQN/tree/coffee-crafting-20261103)의 커밋 [`76d52d0`](https://github.com/blueanggel10004-tech/SDHQN/commit/76d52d0db9cbea0084a4ac480a96df566d5aff9f)에서 독립 카페 프로젝트 `CloudCafeUnity/` 전체를 가져왔습니다. 목적지 저장소의 기존 `main` 이력을 보존했습니다.

원본의 Unity `.meta`, DOTween 및 FluidSim 파일과 관련 라이선스, 패키지·프로젝트 설정, 작업 문서와 테스트를 함께 보존합니다. Unity 버전은 원본 `ProjectVersion.txt`를 따릅니다.

이번 변경의 재현 가능한 검증은 다음 명령으로 실행합니다.

```powershell
pwsh -NoProfile -File tools/test_coffee_project.ps1
python tools/register_coffee_assets.py --validate-only
```

기존 제작 모델 8개 테스트 프로그램의 518개 항목과 PNG·Unity 원두 데이터 18종의 참조·GUID·원본 해시 검사를 수행합니다. 결과는 [`tools/validation.json`](tools/validation.json)에 저장합니다.

이번 작업에서 **Unity 6000.6.5f1 실제 컴파일, 18종 Resources 로드, Windows 빌드, 전체 제작 흐름·우유 연습·원두 선택 API 자동 실행**을 확인했습니다. 새 결과는 [`Review/CoffeeAssets`](CloudCafeUnity/Review/CoffeeAssets/)에 모았습니다. 원두 선택 검사에는 기본 예가체프, 안티오키아·킬리만자로 선택, 잘못된 ID·중간 투입 변경 차단, 커서 복구가 포함됩니다.

```powershell
# 로컬 Unity 설치 경로를 지정해 Windows 실행본 생성
./CloudCafeUnity/BuildCoffeeCraft.ps1 -UnityEditor 'E:/6000.6.5f1/Editor/Unity.exe'
# 빌드된 게임의 런타임 API 및 기존 제작 흐름 검사
./tools/run_coffee_smoke.ps1 -Mode coffeeBeanSmoke -SkipCapture
./tools/run_coffee_smoke.ps1 -Mode frontSmoke
./tools/run_coffee_smoke.ps1 -Mode latteSmoke
```

실제 OS 마우스·키보드 입력 인수와 원두 선택창의 시각 캡처는 미완료입니다. 숨긴 창에서 캡처가 검게 나오는 환경이어서 원두 선택 API 검사와 화면 검사를 분리했습니다. `-SkipCapture`를 생략하면 검은 화면을 실패로 판정합니다. README의 봉투 이미지는 생성 원본 갤러리이고, 위 GIF와 기존 `Review` 결과는 가져온 원본 프로젝트의 기록입니다.
