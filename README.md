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

## 핵심 구현

- `PlayerController`를 플레이어의 입력과 외부 API의 입구로 두고, 실제 이동/기류/상호작용/성장 상태는 각각의 전담 컴포넌트로 분리했습니다.
- Rigidbody2D 속도를 직접 덮어쓰기보다 목표 속도와 현재 속도의 차이를 Impulse로 적용해 일반 이동, 대시, 기류, 기류 이탈 관성을 하나의 흐름으로 처리했습니다.
- 깃털 수집량에 따라 대시 기능 추가, 기류 기믹을 수행할 수 있는 Wing 기능 추가, 활공 시간 증가 등이 단계적으로 해금되도록 `Progression` ScriptableObject를 통해 성장 데이터를 분리했습니다.
- `SaveData`, `GameProgressState`, `SaveManager`를 분리해 JSON 파일 저장과 런타임 진행도 저장의 책임을 나눴습니다.
- `SettingsManager.SettingsApplied` 이벤트를 통해 옵션 UI와 사운드 시스템을 연결하고, 진행도 저장과 설정 저장을 별도 파일로 관리했습니다.
- DontDestroyOnLoad 오브젝트 사용을 줄이고 씬 단위 소유를 우선했기 때문에, 각 스테이지가 필수적으로 가지고 있어야 하는 오브젝트가 존재했습니다. 에디터를 활용하는 누구나 필수 오브젝트의 누락을 런타임 전에 확인할 수 있도록 Unity `EditorWindow` 기반 `StageValidator`를 구현했습니다.
- 주요 컴포넌트는 `Awake()`에서 필수 참조를 검증하고, 누락 시 `Debug.LogError(..., this)` 후 비활성화하는 fail-fast 방식을 활용해 씬/프리팹 계약을 명확히 했습니다.

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
motorVelocity (플레이어의 조작, 상태를 통해 계산된 목표 속도)
+ airFlowVelocity (기류 안에서 적용되는 목표 속도)
+ carryVelocity (기류 이탈 후 남는 관성 속도)
-> targetVelocity (최종 목표 속도)
-> deltaVelocity (targetVelocity - _rb.linearVelocity, 최종 목표 속도와 현재 속도의 차이)
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
- 스위치에 의한 활성화/비활성화 또는 기류 방향 반전
- `ParticleSystem` 기반 기류 시각화
- Collider 크기에 맞춘 비주얼/파티클 크기 동기화

기류 방향 반전은 `AirFlowZone`의 루트 Transform을 직접 회전시키지 않고, 내부 방향 값인 `_flowDirectionSign`을 수정해 같은 영역 안에서 흐름만 반대로 계산되도록 설계했습니다. `Orb` 같은 상태 표시 요소까지 함께 회전하지 않도록, 기류 영역 비주얼과 파티클이 포함된 `VisualRoot`만 회전시켜 실제 기류 방향과 맞췄습니다.

## Interaction / Activation

상호작용 구조는 2개의 인터페이스 스크립트를 활용하여 플레이어와 직접 상호작용하는 대상과, 스위치 등에 의해 상태가 바뀌는 대상을 분리했습니다.

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

예를 들어 플레이어가 `Switch`와 상호작용하면, `Switch`는 연결된 `Door`, `AirFlowZone`, `CameraFocusTrigger` 같은 대상의 `Activate()`를 호출합니다. 이 구조로 하나의 스위치 프리팹이 여러 종류의 오브젝트를 같은 방식으로 제어할 수 있습니다.

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

현재 Build Settings에 따른 런타임 흐름은 다음과 같습니다.

```text
Boot
-> MainMenu
-> Stage_01 / Stage_02
```

`Bootstrap`은 JSON 파일에 저장된 저장/설정값을 먼저 로드한 뒤 MainMenu로 진입합니다. 새 게임과 이어하기, 스테이지 이동은 `ScreenFadeTransition.LoadScene()`을 통해 Fade Out -> LoadSceneAsync -> Fade In 순서로 처리됩니다. `ScreenFadeTransition`은 씬 전환 흐름을 유지해야 하는 예외적인 전역 전환 객체로 사용했습니다.

전환 중에는 `CanvasGroup.blocksRaycasts`로 입력을 막아 중복 입력을 줄이고, `StageTransitionController`는 씬 이동 전에 현재 진행도를 저장합니다.

## Camera Feedback

스위치 조작의 결과가 멀리 떨어진 오브젝트에 적용될 때 플레이어가 바로 인지할 수 있도록 `CameraFocusTrigger`와 `CameraFocusController`를 구현했습니다.

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

초기 구상에서는 `PlayerController`가 이동, 점프, 대시, 기류, 상호작용, 사망, 리스폰을 모두 처리했습니다. 기능이 늘어날수록 플레이어 상태 수정의 범위가 넓어져 `PlayerMotor`, `PlayerAirFlow`, `GroundSensor`, `PlayerInteractor`, `PlayerStats`, `Wing`으로 책임을 분리했습니다.

결과적으로 `PlayerController`는 외부 시스템이 접근하는 입구와 플레이어 상태 조율자 역할에 집중하고, 물리 계산과 상호작용 로직은 각각의 전담 컴포넌트로 분리되었습니다.

### Tilemap 경계 충돌

초기에는 발 밑에 별도 Feet Collider를 두고 지면을 감지했지만, Tilemap 경계에서 플레이어가 걸리는 문제가 있었습니다.

이를 해결하기 위해 물리 충돌은 Body Collider 중심으로 유지하고, 지면 감지는 `GroundSensor`의 BoxCast로 분리했습니다. 또한 Tilemap Collider에 CompositeCollider2D를 적용하여 개별 타일의 경계면을 하나로 병합함으로써, 물리 엔진이 순간적으로 경계면을 벽으로 인지해 멈추는 '고스트 충돌(Ghost Collision)' 현상을 해결했습니다.

### AirFlow 진입/이탈 속도 튐

기류 진입 시 즉시 강한 속도를 적용하면 플레이어가 갑자기 튀는 느낌이 있었습니다.

`AirFlowZone`은 목표 기류 속도까지 `Vector2.MoveTowards`를 활용해 프레임 독립적으로 점진 가속시켰으며, 이탈 시에는 외부 관성 속도(carryVelocity)를 감쇠시켜 부드러운 이탈 조작감을 구현했습니다.

### 스테이지 제작 실수 검증

DontDestroyOnLoad 오브젝트 사용을 줄이고 씬마다 명확한 소유 오브젝트를 두는 방식을 우선했기 때문에 스테이지마다 Player, HUD, Portal, Stage Controller, SoundManager 같은 필수 오브젝트가 필요합니다. 하지만 필수 오브젝트가 누락되면 런타임에서야 문제가 드러납니다.

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

- Stage 추가 제작 및 레벨 디자인
- 깃털 획득 시 능력 해금 안내 UI
- AirFlow / Door / Dash / Wing Toggle SFX 확장
- Stage Validator 검사 항목 확장

## 회고

- Icarus는 단순하게 2D 플랫포머 게임의 구성요소들을 계속해서 추가하는 것보다는 게임의 테마가 되는 핵심 기능들에 초점을 맞추었습니다.
- 기능이 늘어날수록 책임을 어떻게 나누고 Unity 씬/프리팹 계약을 어떻게 관리할지에 초점을 두고 진행했습니다. 결과적으로 저를 포함한 에디터를 조작하는 인원이 이미 만들어진 스테이지를 유지보수하거나, 이후 새로운 스테이지를 제작할 때 필요한 오브젝트와 구조를 더 쉽게 확인할 수 있도록 설계했습니다.
- 생산성 향상을 위해 AI 툴(Codex)을 코드 리뷰 및 교차 검증 도구로 활용했습니다. 이 과정에서 AI가 제안한 코드를 무조건 수용하지 않고, 런타임 상의 엣지 케이스(예: 중복 Fail 체크, 예외 흐름)를 주도적으로 분석하고 검증하며 아키텍처의 일관성을 유지하는 능력을 길렀습니다.
- 아키텍처 설계와 코드 전체에 적용되어야 할 방향성과 규칙을 프로젝트 진행 초기부터 설정하고 글로 정리해두는 것으로 스크립트의 양이 늘어남에도 프로젝트 전체에 일관성 있는 흐름을 유지할 수 있었습니다.
