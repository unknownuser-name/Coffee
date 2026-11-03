# 실제 Unity 플러그인 도입 결과

확인: 2026-11-02 KST. 사용자 승인: 실제 사용할 플러그인을 선택해 사용해도 됨.

## 지금 적용한 것: DOTween Free 1.3.045

- 공식 ZIP: https://dotween.demigiant.com/downloads/DOTween_1_3_045.zip
- 공식 설치/호환 안내: https://dotween.demigiant.com/download.php
- 라이선스: https://dotween.demigiant.com/license.php — 상용/비상용 사용 허용. 원본 readme와 저작권 고지 보존. Pro는 도입하지 않음.
- 설치 위치: `UnityProject/Assets/Plugins/DOTween`. 원본 압축본·다운로드 해시와 파일별 해시는 `PluginSetup/dotween-install.json`에 보관.
- Unity 임포트 과정에서 API Updater가 DLL을 처리할 수 있으므로, 원본 보존 ZIP과 임포트 뒤 파일을 구분한다. 다운로드 해시는 제작사 디지털 서명을 대체하지 않는다.
- 공식 설정 창의 설정 생성/모듈 적용 루틴을 배치 실행해 `Assets/Resources/DOTweenSettings.asset` 생성. 이번 조작에 불필요한 Physics/Physics2D 트윈 모듈 비활성화. 오디오/스프라이트/UI 관련 설정은 공식 기본 설정 사용.
- 공급사 코드를 직접 편집하지 않았다. 추후 설정 재검증은 `CafePluginVerification.Run` 사용. 설정 호출은 고정 버전의 에디터 내부 진입점에 의존하므로 업그레이드할 때 재검증 필요.

### 실제 검사 결과

Unity 6000.6.5f1 에디터 배치 실행에서 5개 검사 통과:

1. Transform을 슬롯 목표 위치로 중간 보간.
2. 일시정지 중 위치 유지.
3. 최종 목표 위치 도달.
4. 되돌아가는 도중 `Pause → Kill` 후 추가 갱신해도 위치 유지.
5. 체결에 사용할 각도 값의 회전 보간 완료.

결과: `PluginSetup/verification.json`, 로그: `PluginSetup/verify.log`. 검사 코드: `Assets/Editor/CafePluginVerification.cs`.

초기 에디터 수동 갱신 검사에서는 Kill 단독 뒤 다음 갱신에서 위치가 바뀌는 결과가 나왔다. 이 버전/검사 환경에서 Pause 후 Kill은 통과했다. 일반적인 라이브러리 버그로 단정하지 않으며 실제 플레이모드에서도 다시 집기 검증을 해야 한다.

**범위:** 설치·설정·에디터 동작 검증 완료. 새 플레이어 빌드/실제 마우스 조작/기존 카페 공정 연결까지 검증한 것은 아니다. 우유 시험 스크립트와 기존 MilkCraft 실행본은 수정하지 않았다.

### G0/G1에서 사용할 곳

- 물건을 놓았을 때 슬롯 위치로 짧게 정렬.
- 잘못 놓았을 때 출발 위치로 복귀.
- 다시 잡으면 이전 보간을 정지/취소하여 직접 조작으로 전환.
- 포터필터 잠금 회전, 작업대 진입/복귀.
- 실제 가루/액체 총량 계산과 충돌 판정은 별도 모델이 담당한다. 트윈 완료 시간만으로 공정 성공을 판정하지 않는다.

## 도입 후보와 보류 기준

| 도구 | 용도 | 현재 상태/도입 조건 |
|---|---|---|
| Cinemachine | 대화·작업대·표면 상세 화면의 카메라 구성과 전환 | 공식 문서 확인, 미설치. G1 작업대 시점 검토 후 현재 카메라와 충돌 없이 시험 장면에서 검증 |
| Unity 2D Animation | 사신의 분리 파츠·리깅·마시기 자세 | 공식 문서 확인, 미설치. G7 첫 손님의 손/컵 접점과 원본 해상도 변형 검토 후 도입 판단 |
| Unity AudioSource/AudioMixer | 분쇄·펌프·스팀의 반복음과 실내음 제어 | 엔진 기능으로 구현 가능. 오디오 미들웨어 추가는 현재 불필요 |
| 별도 유체 플러그인 | 표면 유체/스팀/우유 | 현재 추가하지 않음. 기존 붓기 모델과 마스크 방식의 구체적 한계가 확인된 뒤 결정 |
| 유료 Spine/대화 시스템/종합 에셋 | 캐릭터·대화 등 | 구매/설치하지 않음. 현재 작업의 필수 조건으로 두지 않음 |

공식 참고:
- https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/index.html
- https://docs.unity3d.com/Packages/com.unity.2d.animation@10.0/manual/index.html

이들은 게임 프로젝트용 플러그인/패키지다. ChatGPT 또는 Codex의 Unity MCP 연결이 생긴 것은 아니다.
