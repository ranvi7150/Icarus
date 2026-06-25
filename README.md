# Icarus

- 2D 플랫포머 조작감, 상태 기반 능력 전환, 스테이지 진행도 저장, 옵션/사운드/씬 전환, 에디터 검증 도구까지 직접 구현한 Unity 개인 포트폴리오 프로젝트입니다.

- 플레이어는 깃털을 수집하며 능력을 단계적으로 해금하고, `Wing Off` 상태의 대시와 `Wing On` 상태의 활공/기류 탑승을 상황에 맞게 전환하면서 스테이지를 진행합니다.

## 프로젝트 정보

- 개발 기간: 2026.04 ~ 2026.06
- 개발 인원: 1인 개발
- 엔진: Unity 6000.3.6f1
- 언어: C#
- 장르: 2D Platformer
- 주요 구현: Player Movement, Wing System, AirFlow, Interaction, Save/Load, Options, Sound, Scene Transition, Editor Tool
- 저장소: https://github.com/ranvi7150/Icarus
- 플레이 영상: 준비 중

## 기술 하이라이트

- `PlayerController`를 입력과 외부 API의 입구로 두고, 실제 이동/기류/상호작용/성장 상태를 전담 컴포넌트로 분리했습니다.
- Rigidbody2D 속도를 직접 덮어쓰기보다 목표 속도와 현재 속도의 차이를 Impulse로 적용해 일반 이동, 대시, 기류, 기류 이탈 관성을 하나의 흐름으로 처리했습니다.
- 깃털 수집량에 따라 대시, 날개 전환, 활공 시간이 단계적으로 해금되도록 `Progression` ScriptableObject로 성장 데이터를 분리했습니다.
- `SaveData`, `GameProgressState`, `SaveManager`를 분리해 JSON 파일 저장과 런타임 진행도 상태의 책임을 나눴습니다.
- `SettingsManager.SettingsApplied` 이벤트를 통해 옵션 UI와 사운드 시스템을 연결하고, 진행도 저장과 설정 저장을 별도 파일로 관리했습니다.
- 스테이지 필수 오브젝트 누락을 런타임 전에 확인할 수 있도록 Unity `EditorWindow` 기반 `StageValidator`를 구현했습니다.
- 주요 컴포넌트는 `Awake()`에서 필수 참조를 검증하고, 누락 시 `Debug.LogError(..., this)` 후 비활성화하는 fail-fast 방식으로 씬/프리팹 계약을 명확히 했습니다.

## 플레이어 시스템

플레이어 구조는 `PlayerController`에 모든 책임이 몰리지 않도록 여러 컴포넌트로 나눴습니다.

```text
PlayerController
├─ PlayerMotor
├─ PlayerAirFlow
├─ GroundSensor
├─ PlayerInteractor
├─ PlayerStats
└─ Wing
```

각 컴포넌트는 다음 역할을 맡습니다.

| 컴포넌트 | 역할 |
| --- | --- |
| `PlayerController` | 입력 수신, 외부 API 제공, 사망/리스폰/씬 전환 상태 조율 |
| `PlayerMotor` | 이동, 점프, 대시, 중력, 활공 낙하, 최종 속도 적용 |
| `PlayerAirFlow` | 기류 진입/이탈 상태, 기류 속도, carry velocity 관리 |
| `GroundSensor` | BoxCast 기반 지면 감지 |
| `PlayerInteractor` | 상호작용 대상 추적, Prompt 표시, Interact 실행 |
| `PlayerStats` | 깃털 수집량과 능력 해금 상태 관리 |
| `Wing` | 날개 On/Off 상태, 대시/활공/기류 사용 가능 여부 관리 |

초기에는 이동, 점프, 대시, 기류, 상호작용이 `PlayerController`에 몰려 있었지만, 기능이 늘면서 유지보수와 디버깅이 어려워졌습니다. 현재는 `PlayerController`가 플레이어 상태와 외부 호출의 입구 역할을 맡고, 실제 물리 계산과 개별 기능은 전담 컴포넌트가 처리합니다.

### 조작감 처리

플랫포머 조작감을 위해 다음 기능을 구현했습니다.

- Coyote Time
- Jump Buffer
- Low Jump / Fall Gravity Multiplier
- Max Fall Speed Clamp
- Ground Dash / Air Dash
- Wing On 상태의 Glide
- Wing Off 상태의 Dash

핵심 속도 적용 흐름은 다음과 같습니다.

```text
motorVelocity
+ airFlowVelocity
+ carryVelocity
-> targetVelocity
-> deltaVelocity
-> Rigidbody2D.AddForce(..., ForceMode2D.Impulse)
```

이 방식으로 일반 이동, 대시, 기류 탑승, 기류 이탈 후 관성을 각각 따로 덮어쓰지 않고 하나의 최종 목표 속도 계산으로 통합했습니다.

## Wing / Progression

Icarus의 핵심 조작은 날개 상태 전환입니다.

| 상태 | 사용 가능 능력 |
| --- | --- |
| `Wing Off` | Dash |
| `Wing On` | Glide, AirFlow |

날개를 펼치면 대시는 제한되고 활공과 기류 탑승이 가능해집니다. 반대로 날개를 접으면 대시가 가능해지지만 활공과 기류 탑승은 사용할 수 없습니다.

깃털 수집량에 따른 능력 해금은 `Progression` ScriptableObject가 담당합니다. 대시 해금, 날개 전환 해금, 활공 시간 증가 조건을 데이터로 분리했고, `OnValidate()`에서 단계 순서가 깨지지 않도록 보정합니다.

## AirFlow

`AirFlowZone`은 `Wing On` 상태에서만 사용할 수 있는 기류 영역입니다.

주요 구현 내용은 다음과 같습니다.

- `BoxCollider2D` 기반 기류 판정
- 기류 방향에 따른 플레이어 강제 이동
- 기류 진입 시 목표 속도까지 `MoveTowards`로 점진 가속
- 기류 이탈 후 carry velocity 감쇠
- 스위치에 의한 활성화/비활성화 또는 방향 반전
- `ParticleSystem` 기반 기류 시각화
- Collider 크기에 맞춘 비주얼/파티클 크기 동기화

방향 반전은 오브젝트 전체를 회전시키지 않고 내부 방향 부호를 바꾸며, `AirFlowZoneVisualizer`가 비주얼 루트만 뒤집는 방식으로 처리합니다. 덕분에 Collider와 Trigger 구조는 유지하면서 기류 방향 표현만 바꿀 수 있습니다.

## Interaction / Activation

상호작용 구조는 플레이어가 직접 만나는 대상과, 스위치 등에 의해 상태가 바뀌는 대상을 분리했습니다.

```text
PlayerInteractor
-> IInteractable
-> IActivatable
```

두 인터페이스의 역할은 다음과 같습니다.

| 인터페이스 | 역할 |
| --- | --- |
| `IInteractable` | 플레이어가 직접 상호작용하는 대상 |
| `IActivatable` | 스위치 등에 의해 상태가 변경되는 대상 |

예를 들어 플레이어가 `Switch`와 상호작용하면, `Switch`는 연결된 `Door`, `AirFlowZone`, `CameraFocusTrigger` 같은 대상의 `Activate()`를 호출합니다. 이 구조로 스위치가 여러 종류의 오브젝트를 같은 방식으로 제어할 수 있습니다.

## Save / Load

진행도 저장은 JSON 파일 기반으로 구현했습니다.

저장 데이터는 다음과 같습니다.

- 현재 스테이지
- 수집한 깃털 개수
- 수집한 깃털 ID 목록
- 열린 Door ID 목록

저장 구조는 세 책임으로 나눴습니다.

```text
SaveData
= JSON으로 직렬화되는 데이터 구조

GameProgressState
= 런타임에서 유지되는 현재 진행도 상태

SaveManager
= save.json 파일 로드/저장 담당
```

`FeatherPickup`과 `Door`는 `SceneName:LocalId` 형태의 ID를 사용합니다. 그래서 씬을 이동하거나 게임을 다시 불러와도 수집한 깃털과 열린 문 상태가 유지됩니다.

## Settings / Options / Sound

게임 설정은 진행도 저장과 분리해 `settings.json`으로 관리합니다.

저장되는 설정은 다음과 같습니다.

- Master Volume
- BGM Volume
- SFX Volume
- Fullscreen
- Windowed Resolution

구조는 진행도 저장과 비슷하게 `GameSettings`, `GameSettingsState`, `SettingsManager`로 나눴습니다. `OptionsPanel`은 UI 값을 읽어 현재 설정을 복사한 뒤 변경된 값만 반영하고, `SettingsManager.Apply()`가 실제 볼륨/화면 설정을 적용합니다.

사운드는 씬마다 하나의 `SoundManager`가 BGM과 SFX AudioSource를 관리합니다.

```text
SoundManager
├─ BgmSource
└─ SfxSource
```

`SoundManager`는 `SettingsManager.SettingsApplied` 이벤트를 구독해 BGM/SFX 볼륨 변경을 런타임에 반영합니다. 플레이어 점프 사운드는 `PlayerAudio`가 `PlayerMotor.JumpStarted` 이벤트를 구독해 재생하므로, 이동 로직과 사운드 재생 책임이 분리되어 있습니다.

## Scene Flow / Fade

현재 Build Settings에는 다음 씬이 등록되어 있습니다.

```text
Boot
MainMenu
Stage_01
Stage_02
```

런타임 흐름은 다음과 같습니다.

```text
Boot
-> MainMenu
-> Stage_01 / Stage_02
```

`Bootstrap`은 저장/설정을 먼저 로드한 뒤 MainMenu로 진입합니다. 새 게임과 이어하기, 스테이지 이동은 `ScreenFadeTransition.LoadScene()`을 통해 Fade Out -> LoadSceneAsync -> Fade In 순서로 처리됩니다.

전환 중에는 `CanvasGroup.blocksRaycasts`로 입력을 막아 중복 입력을 줄이고, `StageTransitionController`는 씬 이동 전에 현재 진행도를 저장합니다.

## Camera Feedback

스위치 조작 결과를 플레이어가 바로 인지할 수 있도록 `CameraFocusTrigger`와 `CameraFocusController`를 구현했습니다.

```text
Switch Activate
-> CameraFocusTrigger
-> CameraFocusController.Focus()
-> Focus Camera Priority 상승
-> 일정 시간 후 Follow Camera 복귀
```

이 기능은 Cinemachine 카메라의 Priority를 조정해 특정 위치를 잠깐 보여준 뒤 기존 Follow Camera로 돌아오는 방식입니다.

## Stage Validator Editor Tool

스테이지 제작 중 필수 오브젝트 누락을 에디터에서 확인할 수 있도록 `StageValidator`를 구현했습니다.

실행 경로:

```text
Tools > Icarus > Stage Validator
```

검사 항목은 다음과 같습니다.

- `PlayerController`가 정확히 1개 있는지
- `StageSpawnController`가 정확히 1개 있는지
- `StageTransitionController`가 정확히 1개 있는지
- `SoundManager`가 정확히 1개 있는지
- 루트 `HUD` 오브젝트가 정확히 1개 있는지
- `Portal`이 최소 1개 있는지
- 현재 Stage Scene이 Build Settings에 등록되어 있는지

검증 결과는 EditorWindow에 표시하고, 콘솔 로그와 Ping 버튼으로 문제 오브젝트를 빠르게 확인할 수 있게 했습니다.

## 트러블슈팅

### PlayerController 책임 비대화

처음에는 `PlayerController`가 이동, 점프, 대시, 기류, 상호작용, 사망, 리스폰을 모두 처리했습니다. 기능이 늘수록 수정 범위가 넓어져 `PlayerMotor`, `PlayerAirFlow`, `GroundSensor`, `PlayerInteractor`, `PlayerStats`, `Wing`으로 책임을 분리했습니다.

결과적으로 `PlayerController`는 외부 시스템이 접근하는 입구와 플레이어 상태 조율자 역할에 집중하고, 물리 계산과 상호작용 로직은 각각의 전담 컴포넌트로 분리되었습니다.

### Tilemap 경계 충돌

초기에는 발 밑에 별도 Feet Collider를 두고 지면을 감지했지만, Tilemap 경계에서 플레이어가 걸리는 문제가 있었습니다.

이를 해결하기 위해 물리 충돌은 Body Collider 중심으로 유지하고, 지면 감지는 `GroundSensor`의 BoxCast로 분리했습니다. 또한 Tilemap Collider에 CompositeCollider2D를 적용해 타일 경계 충돌을 줄였습니다.

### AirFlow 진입/이탈 속도 튐

기류 진입 시 즉시 강한 속도를 적용하면 플레이어가 갑자기 튀는 느낌이 있었습니다.

`AirFlowZone`은 목표 기류 속도까지 `MoveTowards`로 점진 가속하고, 이탈 후에는 `PlayerAirFlow`가 carry velocity를 감쇠시켜 자연스럽게 빠져나오도록 구성했습니다.

### 씬 이동 후 진행도 유지

스테이지 이동 시 깃털 수집 상태와 문 상태가 초기화되지 않도록 `SaveData`와 `GameProgressState`를 분리했습니다.

`GameProgressState`는 런타임 진행도를 유지하고, `SaveManager`는 JSON 파일 입출력만 담당합니다. 스테이지 이동 시 `StageTransitionController`가 현재 진행도를 저장한 뒤 다음 씬으로 전환합니다.

### 스테이지 제작 실수 검증

Stage Scene마다 Player, HUD, Portal, Stage Controller, SoundManager 같은 필수 오브젝트가 필요하지만, 누락되면 런타임에서야 문제가 드러납니다.

이를 줄이기 위해 `StageValidator` Editor Tool을 만들어 현재 Active Scene의 필수 구성 요소와 Build Settings 등록 여부를 에디터 단계에서 확인할 수 있게 했습니다.

## 코드에서 볼 만한 부분

| 경로 | 볼 내용 |
| --- | --- |
| `Assets/Scripts/Gameplay/Player/PlayerController.cs` | 플레이어 입력과 외부 API 입구 |
| `Assets/Scripts/Gameplay/Player/PlayerMotor.cs` | 점프/대시/활공/기류 속도 적용 |
| `Assets/Scripts/Gameplay/Player/Wing.cs` | 날개 상태 전환과 활공 가능 여부 |
| `Assets/Scripts/Gameplay/Item/Progression.cs` | 깃털 기반 능력 해금 데이터 |
| `Assets/Scripts/Gameplay/AirFlow/AirFlowZone.cs` | 기류 판정, 가속, 활성화/반전 |
| `Assets/Scripts/Gameplay/AirFlow/AirFlowZoneVisualizer.cs` | Collider 기반 기류 비주얼 동기화 |
| `Assets/Scripts/Gameplay/Interaction/Switch.cs` | `IActivatable` 대상 제어 |
| `Assets/Scripts/Core/Saving` | 진행도 저장 구조 |
| `Assets/Scripts/Core/Settings` | 옵션 저장/적용 구조 |
| `Assets/Scripts/Core/ScreenFadeTransition.cs` | 씬 전환 Fade 처리 |
| `Assets/Scripts/Core/Audio/SoundManager.cs` | 씬 단위 BGM/SFX 관리 |
| `Assets/Scripts/UI/StagePauseMenu.cs` | 일시정지, 옵션, 메인메뉴 복귀 |
| `Assets/Scripts/Editor/Validation/StageValidator.cs` | 스테이지 검증 EditorWindow |

## 사용 기술

- Unity 6000.3.6f1
- C#
- Unity Input System
- Cinemachine
- URP 2D
- Tilemap / CompositeCollider2D
- Unity UI / TextMeshPro
- Newtonsoft Json
- Unity EditorWindow

## 조작법

| 동작 | 키 |
| --- | --- |
| 이동 | 방향키 |
| 점프 | `C` / `Space` |
| 대시 | `X` / `Left Shift` |
| 날개 전환 | `Z` |
| 상호작용 | `F` |
| 일시정지 | `Esc` |

## 앞으로 개선할 부분

- Stage 추가 제작 및 난이도 곡선 정리
- Room 단위 카메라 전환 시스템
- 깃털 획득 시 능력 해금 안내 UI
- AirFlow / Door / Dash / Wing Toggle SFX 확장
- Stage Validator 검사 항목 확장
- Player 상태 표현 고도화

## 회고

Icarus는 단순히 2D 플랫포머 기능을 하나씩 붙이는 것보다, 기능이 늘어날수록 책임을 어떻게 나누고 Unity 씬/프리팹 계약을 어떻게 관리할지에 초점을 두고 진행한 프로젝트입니다.

플레이어 조작, 능력 해금, 상호작용 오브젝트, 저장/로드, 옵션, 사운드, 씬 전환, 에디터 검증 도구까지 하나의 플레이 루프에 필요한 요소를 직접 구현하며 클라이언트 개발에서 중요한 런타임 구조와 제작 편의성을 함께 다뤘습니다.
