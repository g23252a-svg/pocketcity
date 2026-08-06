# 검증 도구

Unity 에디터 없이 코드를 검증하기 위한 것들입니다.

## 컴파일 검사

`UnityStubs.cs`는 이 프로젝트가 쓰는 UnityEngine API만 실제 시그니처대로 흉내 낸
최소 스텁입니다. 이걸로 전체 스크립트를 컴파일해보면 Unity를 켜지 않고도
오타·타입 불일치를 잡을 수 있습니다.

```bash
# mono (apt install mono-mcs) 또는 .NET SDK 필요
S=../PocketCity3D/Assets/Scripts
mcs -target:library -out:/tmp/full.dll UnityStubs.cs \
    $S/Core/Balance.cs $S/Core/CitySim.cs \
    $S/View/MeshFactory.cs $S/View/CityView.cs \
    $S/View/CameraRig.cs $S/View/TouchController.cs \
    $S/UI/GameHud.cs $S/GameBootstrap.cs
```

에러 없이 끝나면 통과입니다.

> 스텁은 실제 UnityEngine이 아니므로, Unity API 자체와의 미세한 차이는 잡지 못합니다.
> 어디까지나 "컴파일조차 안 되는" 부류의 오류를 걸러내는 용도입니다.

## 밸런스 검증

`BalanceTest.cs`는 신중한 플레이어를 흉내 내어 40년치를 돌리고 성장 곡선을 출력합니다.
`CitySim`이 UnityEngine에 의존하지 않아서 가능한 일입니다.

```bash
mcs -out:/tmp/balance.exe ../PocketCity3D/Assets/Scripts/Core/Balance.cs \
    ../PocketCity3D/Assets/Scripts/Core/CitySim.cs BalanceTest.cs
mono /tmp/balance.exe
```

인구가 꾸준히 늘고 적자에 빠지지 않으며 Lv.2 이상 건물이 생기면 통과(종료코드 0)입니다.
`Balance.cs`의 수치를 바꾼 뒤 이걸 돌려보면 게임을 켜지 않고도 영향을 확인할 수 있습니다.
