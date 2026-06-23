# Icarus

취업 포트폴리오용으로 제작 중인 Unity 2D 플랫폼 액션 게임입니다. 깃털을 수집하며 이동 능력을 확장하고, 날개/활공/AirFlow를 활용해 스테이지를 돌파하는 구조를 목표로 합니다.

이 프로젝트는 단순히 기능을 붙이는 것보다, 플레이어 조작감과 Unity 씬 제작 흐름을 유지하기 쉬운 코드 구조로 만드는 데 초점을 두었습니다.

## 포트폴리오 핵심 포인트

- `PlayerController`를 입력과 생명주기 중심의 facade로 두고, 실제 이동/점프/대시/중력/AirFlow 처리는 `PlayerMotor`와 전용 helper로 분리했습니다.
- 깃털 수집량에 따라 대시, 날개 토글, 활공 시간 증가가 순차적으로 해금되는 성장 구조를 구현했습니다.
- `SaveData`, `GameProgressState`, `SaveManager`를 분리해 저장 DTO, 런타임 상태, 파일 입출력 책임을 명확히 나눴습니다.
- `ScreenFadeTransition`을 통해 메뉴와 스테이지 전환을 비동기 로드와 페이드 연출로 통일했습니다.
- `SoundManager`는 씬 단위 오디오 소유권을 갖고, 설정 시스템의 볼륨 변경을 구독해 반영합니다.
- `StageValidator` 에디터 도구를 만들어 스테이지 씬의 필수 오브젝트 계약을 빠르게 점검할 수 있게 했습니다.

## 빠르게 볼 코드

| 관심 영역 | 주요 파일 |
| --- | --- |
| 플레이어 입력/상태 조정 | `Assets/Scripts/Gameplay/Player/PlayerController.cs` |
| 이동, 점프, 대시, 활공 물리 | `Assets/Scripts/Gameplay/Player/PlayerMotor.cs` |
| 날개 상태와 활공 시간 | `Assets/Scripts/Gameplay/Player/Wing.cs` |
| AirFlow 진입/유지/이탈 | `Assets/Scripts/Gameplay/Player/PlayerAirFlow.cs`, `Assets/Scripts/Gameplay/AirFlow/AirFlowZone.cs` |
| 성장 데이터 | `Assets/Scripts/Gameplay/Item/Progression.cs` |
| 저장 구조 | `Assets/Scripts/Core/Saving/SaveData.cs`, `GameProgressState.cs`, `SaveManager.cs` |
| 씬 전환 | `Assets/Scripts/Core/ScreenFadeTransition.cs`, `StageTransitionController.cs` |
| 설정/오디오 연동 | `Assets/Scripts/Core/Settings`, `Assets/Scripts/Core/Audio/SoundManager.cs` |
| 스테이지 검증 도구 | `Assets/Scripts/Editor/Validation/StageValidator.cs` |

## 게임 시스템

### 플레이어

플레이어는 점프 버퍼, 코요테 타임, 대시 쿨타임, 공중 대시 제한, 날개 활공, AirFlow 이동을 조합해 조작합니다. `PlayerController`는 외부 시스템이 플레이어와 대화하는 입구 역할을 하고, 세부 이동 계산은 `PlayerMotor`가 담당합니다.

### 성장

깃털을 수집하면 `Progression` ScriptableObject 기준에 따라 능력이 해금됩니다.

- 깃털 1개: 대시 해금
- 깃털 3개: 날개 토글 해금
- 깃털 5개: 활공 시간 증가

수집한 깃털은 저장 데이터에 기록되어 스테이지 전환 후에도 유지됩니다.

### 상호작용과 스테이지

`Switch`, `Door`, `Portal`, `CameraFocusTrigger`는 작은 인터페이스와 명확한 컴포넌트 책임으로 연결됩니다. 포탈을 이용한 스테이지 이동은 현재 스테이지와 도착 포탈 정보를 저장한 뒤 페이드 전환으로 처리합니다.

### 저장과 설정

저장 시스템은 다음 세 가지 책임으로 나뉩니다.

- `SaveData`: 파일로 직렬화되는 데이터
- `GameProgressState`: 현재 런타임 진행 상태
- `SaveManager`: 저장 파일 입출력과 JSON 변환

설정 시스템도 `GameSettings`, `GameSettingsState`, `SettingsManager`로 같은 형태를 따릅니다. 런타임 파일은 Unity의 `Application.persistentDataPath` 아래에 `save.json`, `settings.json`으로 생성됩니다.

## 에디터 작업 흐름

스테이지 씬은 다음 메뉴에서 검증할 수 있습니다.

```text
Tools/Icarus/Stage Validator
```

`StageValidator`는 `Stage_*` 씬에 대해 다음 항목을 확인합니다.

- `PlayerController` 정확히 하나
- `StageSpawnController` 정확히 하나
- `StageTransitionController` 정확히 하나
- `SoundManager` 정확히 하나
- 루트 `HUD` 정확히 하나
- `Portal` 하나 이상
- 현재 스테이지 씬이 Build Settings에 활성화되어 있음

세부 Inspector 연결 오류는 각 런타임 컴포넌트의 `Awake()`에서 fail-fast 방식으로 검출합니다.

## 실행 방법

1. Unity Hub에서 Unity `6000.3.6f1`로 프로젝트를 엽니다.
2. `Assets/Scenes/Boot.unity`를 엽니다.
3. Play 버튼을 누릅니다.

기본 실행 흐름은 다음과 같습니다.

```text
Boot -> MainMenu -> Stage_01 / Stage_02
```

현재 Build Settings에는 다음 씬이 활성화되어 있습니다.

- `Assets/Scenes/Boot.unity`
- `Assets/Scenes/MainMenu.unity`
- `Assets/Scenes/Stage_01.unity`
- `Assets/Scenes/Stage_02.unity`

`Boot`는 저장 데이터와 설정을 초기화하므로 첫 번째 씬으로 유지해야 합니다.

## 조작법

| 동작 | 입력 |
| --- | --- |
| 이동 | 방향키 |
| 점프 | `C` 또는 `Space` |
| 대시 | `X` 또는 `Left Shift` |
| 날개 토글 | `Z` |
| 상호작용 | `F` |

입력 액션은 `Assets/Prefabs/Character/Player/Controls.inputactions`에 있습니다.

## 개발 환경

- Unity: `6000.3.6f1`
- Render Pipeline: Universal Render Pipeline 2D
- Input: Unity Input System
- JSON: Unity Newtonsoft Json
- Solution: `Icarus.sln`

저장소 루트에서 C# 스크립트 빌드를 확인할 수 있습니다.

```powershell
dotnet build Icarus.sln --no-restore
```

`Switch.ActivationTarget.targetObject`의 `CS0649` 경고는 Unity가 Inspector 직렬화로 값을 채우는 필드 때문에 나타날 수 있습니다.

## 코드 작성 기준

- 외부 씬/프리팹 참조는 `[SerializeField]`로 드러냅니다.
- 같은 오브젝트나 안정적인 자식 의존성은 `Awake()`에서 캐싱합니다.
- 필수 참조가 없으면 `Debug.LogError(..., this)`를 남기고 컴포넌트를 비활성화합니다.
- `Update()`와 `FixedUpdate()`에서는 초기화가 성공했다고 보고 반복적인 null 검사를 피합니다.
- 씬/프리팹 계약은 Inspector에서 보이는 계층과 연결로 유지합니다.

## 폴더 구조

```text
Assets/Scenes                         런타임 씬
Assets/Scripts/Core                   Boot, 저장, 설정, 오디오, 씬 전환
Assets/Scripts/Gameplay               플레이어, AirFlow, 상호작용, 카메라, 월드 오브젝트
Assets/Scripts/UI                     메인 메뉴, 옵션, HUD
Assets/Scripts/Editor/Validation      Stage Validator 에디터 창
Assets/Prefabs                        플레이어, UI, 오디오, 게임플레이 프리팹
ProjectSettings                       Unity 프로젝트 설정과 Build Settings
Packages                              Unity 패키지 manifest
```
