# Effect Handoff

작성 기준: 2026-05-13

## 목적

전투 피격 이펙트를 공용 타입 기반으로 제작하고 연결하기 위한 인수인계 문서.

현재 목표는 스킬별 개별 프리팹 직접 연결 방식에서 벗어나, 스킬 데이터가 이펙트 타입을 지정하고 중앙 레지스트리가 실제 프리팹을 제공하는 구조로 전환하는 것이다.

## 현재 브랜치

- 브랜치: `debug-battle-tooling`
- 메인 브랜치 직접 커밋 금지.
- 현재 브랜치는 디버그 전투 및 이펙트 작업 공간으로 사용 중.

## 문서 위치 규칙

- 이 프로젝트의 인수인계/분류/작업 보고 문서는 `TempProject_READ_FIRST_HANDOFF/` 폴더에 둔다.
- 루트 경로에는 신규 문서를 만들지 않는다.
- 루트에 있던 `EFFECT_HANDOFF.md`, `EFFECT_TYPE_CLASSIFICATION.md`는 현재 기준 문서 위치가 아니다.
- 스킬별 피격 이펙트 분류표는 `TempProject_READ_FIRST_HANDOFF/EFFECT_TYPE_CLASSIFICATION.md`를 기준으로 한다.

## 구현된 구조

### 타입

파일:

- `Assets/Scripts/Battle/HitEffectType.cs`
- `Assets/Scripts/Battle/HitEffectAnchorType.cs`

현재 피격 이펙트 타입:

- `None`
- `Slashing`
- `Piercing`
- `Blunt`
- `Magic`
- `Blessing`
- `SlashingBlood`
- `ArcaneMagic`
- `FireMagic`
- `HolyMagic`
- `Shield`

정리 기준:

- 기존 `Magic` 값은 보존한다.
- `이펙트/` 폴더 이미지 기준으로 `Magic`을 `ArcaneMagic`, `FireMagic`, `HolyMagic`으로 세분화했다.
- 보호막은 `Blessing`과 시각 성격이 달라 `Shield` 타입으로 분리했다.
- `Shield`는 보라색 구체 보호막만 사용한다.
- `Blessing`은 노란색 블레싱 계열만 사용한다.
- 후보 이미지 묶음은 선택지 기준이다. 한 프리팹 안에서 후보 이미지를 동시에 겹쳐 사용하지 않는다.
- `찍는 느낌1~3` 스프라이트는 `SlashingBlood`로 분류한다.
- `SlashingBlood`는 피가 튀는 찌르기, 갈고리, 출혈성 근접 관통 계열에 사용한다.

현재 앵커 타입:

- `Default`
- `Center`
- `Overhead`
- `Feet`

### 레지스트리

파일:

- `Assets/Scripts/Battle/HitEffectRegistry.cs`
- `Assets/Resources/Battle/HitEffectRegistry.asset`

역할:

- `HitEffectType`별 실제 프리팹 매핑.
- 타입별 앵커, 재생 시간, 추가 오프셋 관리.
- `Resources.Load("Battle/HitEffectRegistry")`로 런타임 자동 로드.

현재 연결:

- `Slashing` → `Assets/Prefabs/Effects/Common/Common_Slashing_HitEffect.prefab`
- `SlashingBlood` → `Assets/Prefabs/Effects/Common/Common_SlashingBlood_HitEffect.prefab`
- `Piercing` → `Assets/Prefabs/Effects/Common/Common_Piercing_HitEffect.prefab`
- `Blunt` → `Assets/Prefabs/Effects/Common/Common_Blunt_HitEffect.prefab`
- `Blessing` → `Assets/Prefabs/Effects/Common/Common_Blessing_HitEffect.prefab`
- `ArcaneMagic` → `Assets/Prefabs/Effects/Common/Common_ArcaneMagic_HitEffect.prefab`
- `FireMagic` → `Assets/Prefabs/Effects/Common/Common_FireMagic_HitEffect.prefab`
- `HolyMagic` → `Assets/Prefabs/Effects/Common/Common_HolyMagic_HitEffect.prefab`
- `Shield` → `Assets/Prefabs/Effects/Common/Common_Shield_HitEffect.prefab`

공통 설정:

- 공격형 기본 앵커: `Default`
- 충격/버프/보호막형 앵커: `Center`
- 재생 시간: 약 `1.05`~`1.55`
- offset: `(0, 0)`

### 중앙 재생 서비스

파일:

- `Assets/Scripts/Battle/BattleEffectManager.cs`

역할:

1. `SkillDefinition.hitEffectPrefab`이 있으면 해당 스킬 전용 이펙트 사용.
2. 없으면 `SkillDefinition.hitEffectType`으로 `HitEffectRegistry` 조회.
3. 조회 성공 시 `BattleViewManager.PlayEffect(...)` 호출.
4. 위치는 `BattleUnitView.GetHitEffectAnchorPosition(...)` 기준.

### 스킬 데이터 확장

파일:

- `Assets/Scripts/Battle/SkillDefinition.cs`
- `Assets/Scripts/Battle/SkillDefinition.cs.meta`

추가 필드:

- `hitEffectType`
- `hitEffectAnchorType`
- `hitEffectDurationOverride`

주의:

- 기존 `hitEffectPrefab`은 스킬 전용 override로 유지.
- `SkillDefinition.cs.meta`에 들어 있던 `hitEffectPrefab` 기본 참조는 제거됨.
- 이 기본 참조가 남아 있으면 새 스킬 생성 시 자동으로 특정 프리팹이 들어가 레지스트리 기반 구조가 작동하지 않는다.

### 전투 처리 연결

파일:

- `Assets/Scripts/Battle/BattleActionController.cs`

현재 처리:

- 성공 판정형 스킬은 대상 처리 시 `BattleEffectManager.PlayHitEffect(...)` 호출.
- 공격 판정형 스킬은 실제 명중 후 `ResolveAndApplyAttack(...)` 안에서 호출.
- 따라서 빗나감에는 피격 이펙트가 재생되지 않는다.
- 추가타, 관통, 연쇄타처럼 `ResolveAndApplyAttack(...)`을 타는 공격은 같은 구조를 공유한다.

### 피격 위치 계산

파일:

- `Assets/Scripts/Battle/BattleUnitView.cs`

추가 메서드:

- `GetHitEffectAnchorPosition(HitEffectAnchorType anchorType, Vector2 additionalOffset)`

기존 속성:

- `HitEffectAnchorPosition`

기존 속성은 `Default` 앵커와 동일하게 유지되어 기존 호출과 호환된다.

## 현재 공용 피격 이펙트 상태

처음 제작 당시 흑기사 테스트에 사용한 슬래싱 계열은 현재 흑기사 전용이 아니라 공용 `Slashing` 피격 이펙트다.

현재 기준으로 9개 공용 피격 프리팹 제작 완료 상태다.

현재 경로:

- `Assets/Image/Effects/Common/`
- `Assets/Image/Effects/Handoff/`
- `Assets/Materials/Effects/Common/`
- `Assets/Prefabs/Effects/Common/`

현재 프리팹:

- `Assets/Prefabs/Effects/Common/Common_Slashing_HitEffect.prefab`
- `Assets/Prefabs/Effects/Common/Common_SlashingBlood_HitEffect.prefab`
- `Assets/Prefabs/Effects/Common/Common_Piercing_HitEffect.prefab`
- `Assets/Prefabs/Effects/Common/Common_Blunt_HitEffect.prefab`
- `Assets/Prefabs/Effects/Common/Common_Blessing_HitEffect.prefab`
- `Assets/Prefabs/Effects/Common/Common_ArcaneMagic_HitEffect.prefab`
- `Assets/Prefabs/Effects/Common/Common_FireMagic_HitEffect.prefab`
- `Assets/Prefabs/Effects/Common/Common_HolyMagic_HitEffect.prefab`
- `Assets/Prefabs/Effects/Common/Common_Shield_HitEffect.prefab`

구성:

- 타입별 대표 피격 스프라이트 1장 기반 UI 이미지 레이어.
- 동일 스프라이트의 약한 글로우 레이어.
- `BattleRichHitEffectUI` 기반 스케일, 회전, 페이드 애니메이션.
- `ParticleSystem` 3종: `SparkParticles`, `MoteParticles`, `AfterParticles`.
- 후보 이미지 여러 장을 한 프리팹에 겹쳐 넣지 않음.

생성 메뉴:

- `Tools/Battle/Effects/Generate Rich Hit Effect Prefabs`

확인용 씬:

- `Assets/Scenes/HitEffectPreview.unity`
- 9개 공용 피격 프리팹을 3x3 격자로 배치.
- Play Mode 진입 시 모든 이펙트를 동시에 무한 반복 재생.
- 수정 컨펌용 시각 확인 씬으로 사용.

## 신규 피격 이펙트 이미지 분류

원본 이미지 폴더:

- `이펙트/`

분류표:

| 권장 타입 | 이미지 묶음 | 용도 |
|---|---|---|
| `ArcaneMagic` | `마법1`, `마법2`, `마법3` | 보라/청색 계열 마법, 어둠, 표식, 비전, 영혼 |
| `FireMagic` | `붉은계열 마법1`, `붉은계열 마법2`, `붉은계열 마법3` | 화염, 폭발, 유성, 화상 |
| `HolyMagic` | `신성마법1`, `신성마법2`, `신성마법3` | 신성 빛 피해, 성광 타격 |
| `Shield` | `보라색 보호막(아군 버프 및 보호막 부여)` | 아군용 보호막, 방어막 부여 |
| `Blessing` | `블레싱1`, `블레싱2` | 회복, 버프, 정화, 자기 강화 |
| `Slashing` | `슬래싱1`, `슬래싱2`, `슬래싱3` | 검, 절단, 그림자 근접 |
| `SlashingBlood` | `찍는 느낌1`, `찍는 느낌2`, `찍는 느낌3` | 피가 튀는 찌르기, 갈고리, 출혈성 근접 관통 |
| `Blunt` | `타격1`, `타격2`, `타격3` | 둔기, 돌진, 투석, 짓밟기, 충격 |
| `Piercing` | `피어싱1`, `피어싱2`, `피어싱3` | 화살, 석궁, 창 투사체, 원거리 관통 |
| `None` | 없음 | 패시브, 소환, 도주, 증원 등 피격 이펙트 미적용 |

기존 디버그 경로는 삭제 상태로 표시됨.

- `Assets/Image/Effects/Debug/BlackKnight/...`
- `Assets/Materials/Effects/Debug/BlackKnight/...`
- `Assets/Prefabs/Effects/Debug/BlackKnight/...`

## 제작 완료 타입

피격 이펙트는 공용 이펙트로 제작한다.

현재 제작 완료 타입:

1. `ArcaneMagic`
   - 기존 `Magic`을 세분화한 기본 마법 피격.
   - 대상: 어둠, 표식, 비전, 영혼, 보라/청색 계열 마법.
   - 이미지 기준: `마법1~3`.
   - 위치 권장: `Default` 또는 `Center`.

2. `FireMagic`
   - 화염, 폭발, 유성, 화상 계열.
   - 이미지 기준: `붉은계열 마법1~3`.
   - 위치 권장: `Default`.

3. `HolyMagic`
   - 신성 빛 피해, 성광 타격 계열.
   - 이미지 기준: `신성마법1~3`.
   - 위치 권장: `Default` 또는 `Center`.

4. `Shield`
   - 보호막 생성, 방어막 부여 계열.
   - 이미지 기준: `보라색 보호막(아군 버프 및 보호막 부여)`.
   - 노란색 블레싱 계열과 혼합하지 않는다.
   - 위치 권장: `Center`.

5. `Blessing`
   - 회복, 버프, 정화, 자기 강화 계열.
   - 이미지 기준: `블레싱1~2`.
   - 위치 권장: `Overhead` 또는 `Center`.

6. `Piercing`
   - 화살, 석궁, 창 투사체, 원거리 관통 계열.
   - 이미지 기준: `피어싱1~3`.
   - 위치 권장: `Default`.

7. `Blunt`
   - 둔기, 충격, 밀쳐내기 계열.
   - 이미지 기준: `타격1~3`.
   - 위치 권장: `Center`.

8. `Slashing`
   - 베기 계열.
   - 이미지 기준: `슬래싱1~3`.

9. `SlashingBlood`
   - 피가 튀는 찌르기, 갈고리, 출혈성 근접 관통 계열.
   - 이미지 기준: `찍는 느낌1~3`.

## 권장 폴더 구조

공용 이펙트:

- `Assets/Image/Effects/Common/`
- `Assets/Materials/Effects/Common/`
- `Assets/Prefabs/Effects/Common/`

규칙:

- 파일명에 `Debug` 사용 지양.
- 공용 이펙트는 타입명 중심.

예시:

- `Common_Slashing_HitEffect.prefab`
- `Common_SlashingBlood_HitEffect.prefab`
- `Common_Piercing_HitEffect.prefab`
- `Common_Blunt_HitEffect.prefab`
- `Common_ArcaneMagic_HitEffect.prefab`
- `Common_FireMagic_HitEffect.prefab`
- `Common_HolyMagic_HitEffect.prefab`
- `Common_Blessing_HitEffect.prefab`
- `Common_Shield_HitEffect.prefab`

## 남은 작업 순서

1. `TempProject_READ_FIRST_HANDOFF/EFFECT_TYPE_CLASSIFICATION.md` 기준으로 스킬별 `hitEffectType` 지정.
2. 스킬 에셋에서 `hitEffectPrefab`이 비어 있는지 확인.
3. 디버그 전투에서 타입별 테스트.
4. 연출 과다/부족 타입 조정.
5. 콘솔 Error 0개 확인.

## 검증 상태

마지막 확인:

- `dotnet build NeoFinalProject_SRPG.sln --no-restore` 성공.
- 오류 0개.
- 기존 경고 존재.
- Unity Console Error 0개.
- Play Mode 정지 상태.

## 주의할 변경 상태

현재 작업 트리에는 이펙트 구조 변경 외의 변경도 보일 수 있다.

주의 대상:

- `Assets/Fonts/malgun SDF.asset`
  - Unity 저장/리프레시 과정에서 dirty로 표시됨.
  - 이펙트 구조 작업 범위 아님.

- `Assets/AI Toolkit/`
  - Unity AI 관련 로컬 상태일 수 있음.
  - 임의 삭제 금지.

- `.codex_tmp/`
  - 백업 및 캡처 폴더.
  - 중요한 백업 여부 확인 전 삭제 금지.

백업 위치:

- `.codex_tmp/backups/hit-effect-registry-20260513_163850/`
- `.codex_tmp/backups/rich-hit-effects-20260513_222820/`

## 새 채팅방 첫 요청 예시

새 채팅방에서 다음 문장으로 시작하면 된다.

> `TempProject_READ_FIRST_HANDOFF/EFFECT_HANDOFF.md`, `TempProject_READ_FIRST_HANDOFF/EFFECT_TYPE_CLASSIFICATION.md`, `TempProject_READ_FIRST_HANDOFF/DEBUG_BATTLE_HANDOFF.md`를 먼저 읽고, 현재 `debug-battle-tooling` 브랜치의 이펙트 구조와 작업 트리 상태를 확인해줘. 그 다음 `이펙트/` 폴더 이미지 기준 공용 피격 이펙트 타입 제작과 스킬 연결을 이어가자.
