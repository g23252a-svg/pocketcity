# 포켓시티 3D — Unity 버전

같은 도시 건설 시뮬레이션을 Unity로 다시 만든 것입니다. 웹 버전의 2D 캔버스 대신
**실제 3D 메시 · 그림자 · 안개**로 그리고, 건물이 자랄 때 높이가 부드럽게 올라갑니다.

## 여는 방법

1. **Unity Hub** → `Add` → `Add project from disk`
2. 이 폴더 안의 **`PocketCity3D`** 를 선택
3. 에디터가 열리면 아무 씬에서나 **Play** 를 누르면 됩니다

`ProjectVersion.txt`는 2022.3 LTS로 적어두었습니다. 다른 버전(2021.3 이상, Unity 6 포함)에서
열면 업그레이드하겠냐고 묻는데 그대로 진행하면 됩니다. 렌더 파이프라인은 **Built-in**(기본값)
기준이라 별도 설정이 필요 없습니다.

### 씬 배치가 필요 없습니다

프리팹도, 배치된 오브젝트도, 인스펙터 배선도 없습니다. `GameBootstrap`이
`[RuntimeInitializeOnLoadMethod]`로 Play 시점에 **카메라·조명·지형·UI를 전부 코드로 생성**합니다.
그래서 빈 씬에서 Play만 눌러도 게임이 돌아가고, 씬 파일이 깨질 일도 없습니다.

## 조작

| 동작 | 방법 |
|---|---|
| 지도 이동 | 도구를 안 고른 상태에서 한 손가락 드래그 (마우스는 좌클릭 드래그) |
| 확대·축소 | 두 손가락 오므리기/벌리기 (마우스는 휠) |
| 건설 | 아래 팔레트에서 도구를 고르고 화면을 탭 |
| 연속 건설 | 도구를 고른 채 드래그 — 도로·구역을 한 번에 여러 칸 |
| 철거 | 도구 ▸ 철거 |
| 상태 겹쳐보기 | 우측 상단 전력 / 수도 / 오염 / 지가 |

두 손가락은 도구를 고른 상태에서도 항상 카메라 조작이라, 짓다가 화면을 옮기려고
도구를 해제할 필요가 없습니다.

## 폰으로 빌드하기

**Android** — Unity Hub에서 해당 에디터 버전에 `Android Build Support`
(+ OpenJDK, Android SDK/NDK) 모듈이 설치돼 있어야 합니다.

1. `File ▸ Build Settings` → Platform을 **Android**로 바꾸고 `Switch Platform`
2. `Player Settings`에서 아래를 권장합니다
   - Default Orientation: **Portrait**
   - Minimum API Level: **Android 7.0 (API 24)** 이상
   - Scripting Backend: **IL2CPP**, Target Architectures: **ARM64** (구글 플레이 필수 조건)
3. `Build And Run`

**iOS** — macOS + Xcode가 필요합니다. `iOS Build Support` 모듈을 설치한 뒤 Build하면
Xcode 프로젝트가 나오고, 거기서 실기기에 올리면 됩니다.

## 코드 구성

| 파일 | 역할 |
|---|---|
| `Core/Balance.cs` | 밸런스 상수와 건물 카탈로그. **난이도를 바꾸려면 이 파일만** 건드리면 됩니다 |
| `Core/CitySim.cs` | 시뮬레이션. `UnityEngine`을 전혀 참조하지 않습니다 |
| `View/MeshFactory.cs` | 큐브·평면 메시와 머티리얼을 코드로 생성 (임포트할 에셋 없음) |
| `View/CityView.cs` | 3D 렌더링. `Graphics.DrawMeshInstanced`로 종류별 일괄 렌더 |
| `View/CameraRig.cs` | 비스듬한 카메라. 지면 평면 교점으로 드래그를 계산 |
| `View/TouchController.cs` | 터치·마우스 입력, 드래그 연속 건설 |
| `UI/GameHud.cs` | HUD와 팔레트를 런타임 uGUI로 생성 |
| `GameBootstrap.cs` | 씬 구성과 게임 루프 |

### 왜 타일마다 GameObject를 만들지 않았나

64×64 = 4,096칸입니다. 칸마다 GameObject를 두면 드로우 콜과 Transform 갱신만으로
폰에서 프레임이 무너집니다. 대신 종류별로 변환 행렬을 모아
`Graphics.DrawMeshInstanced`로 한 번에 그립니다(1회 최대 1,023개씩 나눠서).
그래서 건물이 수천 개가 되어도 드로우 콜은 머티리얼 종류 수만큼만 늘어납니다.

### 시뮬레이션이 엔진과 분리된 이유

`CitySim`은 `UnityEngine`을 import하지 않습니다. 덕분에 에디터를 켜지 않고도
그대로 돌려 밸런스를 수치로 검증할 수 있습니다. 이 프로젝트의 밸런스 값들은
웹 프로토타입에서 40년치를 반복 시뮬레이션해 잡은 것을 그대로 옮긴 것입니다.

## 밸런스 조정

`Core/Balance.cs` 안:

- `LevelRequirement` — 레벨업에 필요한 지가. 낮추면 도시가 빨리 큽니다
- `ResidentCap` / `CommercialJobs` / `IndustrialJobs` — 칸당 수용량
- `TaxResidential` 등 — 세수 계수
- `IndustrialPollution` — 공업 오염 세기. 칸마다 누적되니 조금만 올려도 크게 체감됩니다
- `CommuteJobs` — 도시 밖 통근 보정. **0으로 두면 초기 도시의 실업률이 100%가 되어
  행복도가 즉사하고 도시가 시작조차 못 합니다**
- `TickSeconds` — 1개월의 실제 길이(초)
