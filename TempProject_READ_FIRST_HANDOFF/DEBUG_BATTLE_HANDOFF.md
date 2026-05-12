# Debug Battle Handoff

## 목적

디버그 전투 씬과 Unity MCP 작업 흐름을 별도 브랜치에서 관리하기 위한 인수인계 문서.

메인 전투 씬을 직접 수정하지 않고, `DebugBattle` 씬에서 4:4 전투 구성, 모션, 피격 반응, 공용 이펙트, 스킬 연결, 레벨/돌파 테스트를 검증하는 것이 목적이다.

## 작업 원칙

- 커밋, 스테이징, 푸시는 명시 요청 전 금지.
- `.meta` 파일은 커밋 대상에서 제외.
- 원본 게임 스크립트 수정 필요 시 백업 후 진행.
- 디버그 전용 기능은 가능한 한 아래 경로에 격리.
  - `Assets/Scripts/Debug/`
  - `Assets/Scenes/DebugBattle.unity`
  - `Assets/Prefabs/Effects/Debug/`
  - `.codex_tmp/`
- 팀원이 올린 변경을 받을 때는 먼저 `git status --short --branch`로 충돌 가능성 확인.
- Unity Editor가 열려 있는 상태에서는 batchmode 대신 Unity MCP 또는 에디터 내부 명령 사용.

## Unity MCP 작업 흐름

1. 프로젝트 루트 확인.
   - `C:\UnityProject\NeoFinalProject_SRPG`
2. Git 상태 확인.
   - `git status --short --branch`
3. Unity MCP 연결 확인.
   - `Unity_ManageEditor GetState`
4. `Unity not detected`가 나오면 `Unity_ManageEditor GetState` 재시도.
5. C# 수정 후 빌드 확인.
   - `dotnet build NeoFinalProject_SRPG.sln --no-restore`
6. Play Mode 진입 후 콘솔 에러 확인.
   - `Unity_GetConsoleLogs`의 Error 로그 확인.
7. 검증 후 Play Mode 정지.

## 현재 디버그 전투 기능

### 씬

- 디버그 씬: `Assets/Scenes/DebugBattle.unity`
- 원본 전투 씬과 유사한 4:4 전투 테스트용 구성.
- 메인 전투 씬은 직접 수정하지 않는 방향.

### 주요 스크립트

- `Assets/Scripts/Debug/DebugBattleSceneController.cs`
- `Assets/Scripts/Debug/DebugBattleSceneController.UI.cs`
- `Assets/Scripts/Debug/DebugBattleSceneController.Picker.cs`
- `Assets/Scripts/Debug/DebugBattleSceneController.Capture.cs`
- `Assets/Scripts/Debug/DebugBattleHitEffectUI.cs`

### 기본 배치

아군 기본 배치:

- 암흑 사제
- 주인공
- 그림자 무희
- 흑기사

적군 기본 배치:

- 농부
- 경비병
- 석궁병
- 사제

아군 진형은 화면상 가장 왼쪽이 4열이다.
전투 시작 시 아군 슬롯 매핑은 시각적 순서와 런타임 슬롯 순서를 반대로 맞춘다.

## 스킬 규칙

### 주인공

주인공 스킬은 고정이며 변경 불가.

- 수확
- 붕괴의 유성
- 차원 균열
- 영혼 착취

수확은 주인공의 기본 평타다.

### 아군

아군은 역할군별 스킬 풀을 사용한다.
주인공을 제외한 아군은 3개 스킬 슬롯을 사용하고, 4번째 슬롯은 비활성 `-` 표시를 유지한다.
이미 선택된 스킬은 같은 유닛의 선택 풀에서 빠져야 한다.

### 적군

적군 스킬은 유닛별 고정.
디버그 UI에서는 버튼 형태로 표시하되 선택 불가.
스킬 수가 4개 미만이면 남은 칸은 비활성 `-` 표시를 유지한다.

## 레벨 및 돌파 테스트

디버그 UI에서 아군/적군 전체 레벨 조정 가능.

- `-10`
- `-5`
- `-1`
- `Reset`
- `+1`
- `+5`
- `+10`

레벨 변경 시 런타임 전투 스탯에 반영되어야 한다.
`currentLevel`만 바꾸면 스탯이 오르지 않을 수 있으므로, 디버그 생성 시 성장치도 함께 채워야 한다.

확인 대상 필드:

- `levelGrowthMaxHp`
- `levelGrowthDmg`
- `promotionRank`
- `promotionBonusPercentPerRank`

아군 포트레이트에는 돌파/등급 버튼을 반투명하게 배치한다.
등급 이미지는 `Assets/Image/UI/Unit_Rank1.png`부터 `Unit_Rank9.png`까지 사용한다.

## 전투 중 디버그 기능

전투 중에도 다음 기능을 사용할 수 있어야 한다.

- 전투 중단
- 아군 무적 모드
- 스킬 노쿨 모드

전투 중단 시 씬이 초기화되어야 한다.
필드 위 유닛, 전투 UI, 플로팅 텍스트, 디버그 이펙트 오브젝트가 남으면 안 된다.

## 캡처 규칙

게임 뷰 캡처 경로:

- `.codex_tmp/captures/debugbattle_gameview.png`
- `.codex_tmp/captures/debugbattle_gameview_yyyyMMdd_HHmmss_fff.png`

타임스탬프 캡처는 최근 3개만 유지한다.
고정 파일명 캡처는 최신 상태 확인용으로 덮어쓴다.

## 브랜치 운용

디버그 전투 작업은 별도 브랜치에서 관리한다.

권장 브랜치명:

- `debug-battle-tooling`

팀 변경을 받는 방식:

1. 현재 브랜치 확인.
2. 작업 중인 변경 상태 확인.
3. `main` 또는 `origin/main` 최신 변경 확인.
4. 필요 시 디버그 브랜치에서 `main` 변경을 merge 또는 rebase.
5. 충돌 발생 시 디버그 경로와 원본 경로를 분리해서 해결.

주의:

- 브랜치 생성만으로는 공유되지 않는다.
- GitHub에 공유하려면 명시적으로 커밋과 푸시가 필요하다.
- 로컬 선택 공유가 목적이면 zip 또는 patch 산출물이 더 안전할 수 있다.

## 다른 컴퓨터 또는 새 채팅 인수인계 문구

다음 문구를 새 작업자에게 전달한다.

```text
Unity AI MCP 스킬을 사용해 C:\UnityProject\NeoFinalProject_SRPG 프로젝트를 이어서 작업한다.

커밋, 스테이징, 푸시는 명시 요청 전 금지.
.meta 파일은 커밋 금지.
원본 게임 스크립트 수정 필요 시 백업 후 진행.

디버그 전투 작업은 가능한 한 Assets/Scripts/Debug, Assets/Scenes/DebugBattle.unity, Assets/Prefabs/Effects/Debug, .codex_tmp 안에 격리한다.

DebugBattle 씬은 원본 4:4 전투 테스트용이다.
아군 기본 배치는 암흑 사제, 주인공, 그림자 무희, 흑기사.
적군 기본 배치는 농부, 경비병, 석궁병, 사제.
아군 화면상 가장 왼쪽은 4열이다.

주인공 스킬은 수확, 붕괴의 유성, 차원 균열, 영혼 착취 고정이며 변경 불가.
수확은 기본 평타다.

C# 수정 후 dotnet build NeoFinalProject_SRPG.sln --no-restore 실행.
Unity Play Mode에서 콘솔 Error 확인.
검증 후 Play Mode를 반드시 정지한다.
```

## 커밋 전 확인 목록

- `git status --short --branch`로 변경 파일 확인.
- `.meta` 포함 여부 확인.
- 원본 스크립트 변경 여부 확인.
- 디버그 전용 변경과 팀 공용 변경 분리.
- `dotnet build NeoFinalProject_SRPG.sln --no-restore` 성공 여부 확인.
- Unity Play Mode 콘솔 Error 0개 확인.
- Play Mode 정지 확인.
