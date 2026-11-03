# 사실적인 산지별 커피 봉투 · Unity 2.5D

실제 포장 사진처럼 보이는 투명 배경 커피 봉투 이미지 18종입니다. 종이와 무광 필름의 질감, 불규칙한 주름, 눌린 모서리, 봉합선과 밸브, 포장 곡면을 따르는 인쇄를 이미지 전체에 함께 생성했습니다.

이 버전은 **2.5D용 PNG 스프라이트**입니다. 새 3D 메시나 PBR 재질 세트는 포함하지 않습니다. 따라서 자유 회전하는 3D 오브젝트 대신, 고정 시점의 매장 소품·진열·인벤토리 등에 사용합니다.

## 사용

1. `CoffeeBags_Realistic_Unity.unitypackage`를 Unity Project 창으로 끌어와 Import합니다. 또는 `unity/Assets/CoffeeBagsRealistic` 폴더를 프로젝트의 `Assets`에 복사합니다.
2. `Assets/CoffeeBagsRealistic/Sprites`에서 PNG를 Scene으로 끌어옵니다. Single Sprite, 투명 배경, Full Rect, 무압축, 밉맵 끄기가 설정되어 있습니다.
3. 각 이미지의 실제 보이는 봉투 하단에 피벗을 맞추고, 명목 높이에 맞는 PPU를 설정했습니다. 기본 Sprite Renderer로 배치할 수 있습니다.

## 파일

- `preview_realistic_18.png`: 전체 18종 미리보기
- `unity/Assets/CoffeeBagsRealistic/Sprites/*.png`: 이미지 생성 도구가 만든 최종 원본 PNG
- `CoffeeBags_Realistic_Unity.unitypackage`: Unity 패키지
- `catalog/products.json`: 산지·품종·향미·설명·조사 출처
- `catalog/sprite_manifest.json`: 이미지 크기·알파 경계·PPU·피벗·SHA256
- `generation_plan.json`: 전체 생성 계획과 프롬프트 목록
- `prompts/*.txt`: 이미지별 정확한 최종 생성 프롬프트와 수정 기록
- `validation.json`: 최종 PNG·패키지 검사 결과
- `tools/package_realistic.py`: 검증·미리보기·패키지 재생성

생성 방식은 기본 제공 `image_gen` 도구 모드입니다. 원본 PNG에는 후처리로 배경 제거, 그림 덧붙이기, 라벨 합성을 하지 않습니다. 미리보기에서는 원본을 복사해 축소하고 한 화면에 배치합니다.

산지별 향미와 1~5 지표는 대략적인 게임용 프로필이며 실제 로트·가공·로스팅에 따라 달라집니다. 특정 상업 브랜드나 인증 로트의 제품 복제품이 아닙니다. 실제 재배 조합의 근거는 `products.json`에 담았습니다. 이미지에 인쇄된 작은 글씨는 생성 특성상 시각 확인이 필요하므로 게임 로직이나 상세 설명 UI에는 함께 제공한 JSON 데이터를 사용하세요.

## 재패키징

Python 3.10 이상, Pillow, numpy가 필요합니다. 기존 생성 이미지를 다시 생성하지 않고 그대로 포장합니다.

```powershell
python assets/coffee_bags_realistic/tools/package_realistic.py
```

아직 이미지가 일부만 있을 때는 `--allow-partial`로 진행 중 미리보기만 만들 수 있습니다. 18개가 모두 있고 실제 알파 투명도 검사를 통과해야 Unity 패키지를 생성합니다. `--validate-only`는 파일만 검사합니다.

Unity Editor에서의 실제 가져오기·컴파일·실행 검증은 아직 수행하지 않았습니다.
