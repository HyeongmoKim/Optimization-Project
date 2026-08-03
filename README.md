# RedRunner Optimization Project

기존 오픈소스 Unity 게임 [BayatGames/RedRunner](https://github.com/BayatGames/RedRunner)를 대상으로 병목을 측정하고, 모바일 환경에 맞게 CPU·메모리·렌더링·리소스 로딩 구조를 개선한 프로젝트입니다.

의도적으로 만든 최적화 예제가 아니라 다른 개발자가 작성한 프로젝트의 구조와 동작을 분석한 뒤, Unity Profiler와 Android 실기기 측정을 근거로 개선했습니다.

## 핵심 결과

| 항목 | 개선 전 | 개선 후 | 변화 |
| --- | ---: | ---: | ---: |
| `SoundManager.GetHandle` (선택한 최악 프레임) | 29.33 ms / 11회 | Terrain 활성화 경로의 반복 호출 제거 | Audio 초기화 병목 제거 |
| Texture2D Memory | 99.4 MB | 31.6 MB | **68.2% 감소** |
| Graphics Memory | 160.9 MB | 92.9 MB | **42.3% 감소** |
| Batches | 48 | 37 | **22.9% 감소** |
| SetPass Calls | 30 | 19 | **36.7% 감소** |

> 수치는 동일 프로젝트의 개선 전후 캡처를 기준으로 기록했습니다. Editor 수치와 Android 실기기 수치를 혼합해 평균값으로 사용하지 않았습니다. CPU의 29.33 ms는 평균값이 아니라 선택한 최악 프레임의 누적 시간이며, 동일 조건의 개선 후 수치를 다시 확보하기 전까지 감소율로 환산하지 않았습니다.

## 개발 및 측정 환경

- Unity 6000.3.14f1
- C#
- Android Development Build
- Unity Profiler
- Memory Profiler
- Frame Debugger
- Addressables Analyze / Build Report
- Git

## 최적화 내용

### 1. Audio 초기화 CPU 비용 개선

Profiler의 최악 프레임에서 다음 호출 흐름을 확인했습니다.

```text
TerrainGenerator.Update
└─ GameObject.Activate
   └─ GameObject.ActivateAwakeRecursively
      └─ SoundHandle.Instance.CreateChannel
         └─ SoundManager.GetHandle
```

Saw 오브젝트마다 AudioSource와 재생 상태를 관리하던 구조를 정리하고, 공용 `AudioManager`에 효과음 재생을 위임했습니다. 오브젝트 활성화 시 반복되던 Audio 채널 초기화 작업을 줄였으며 변경 전후 Profiler 캡처를 함께 보관했습니다.

선택한 최악 프레임에서는 `SoundManager.GetHandle`이 11회 호출되며 누적 29.33 ms를 사용했습니다. 개선 후 Terrain 활성화 호출 경로에서 해당 반복 초기화가 제거된 것을 확인했습니다. 단일 최악 프레임의 값이므로 평균 CPU Frame Time이나 전체 프레임 개선율로 표현하지 않았습니다.

- 변경 코드: [`AudioManager.cs`](Assets/Scripts/RedRunner/AudioManager.cs), [`Saw.cs`](Assets/Scripts/RedRunner/Enemies/Saw.cs)

### 2. Texture 및 Graphics Memory 절감

Memory Profiler에서 큰 Texture2D가 Graphics Memory 대부분을 차지하는 것을 확인했습니다. Android Override 압축 설정과 Sprite Atlas 구성을 적용해 모바일 런타임의 Texture 메모리를 줄였습니다.

- Android 텍스처 압축 포맷 적용
- 배경과 캐릭터 Sprite Atlas 구성
- 불필요하게 큰 원본 Texture의 Import Settings 조정
- 변경 전후 Memory Snapshot 비교

장시간 실행 및 반복 재시작 측정 결과는 다음과 같습니다.

| 항목 | Initial | After 3 min | 10 Restarts | 20 Restarts |
| --- | ---: | ---: | ---: | ---: |
| Resident Memory | 325.2 MB | 348.5 MB | 351.2 MB | 353.8 MB |
| Native Memory | 187.8 MB | 204.2 MB | 205.1 MB | 205.1 MB |
| Managed Memory | 18.6 MB | 19.3 MB | 19.4 MB | 19.4 MB |
| Graphics Memory | 160.6 MB | 162.8 MB | 160.8 MB | 160.9 MB |
| GameObjects | 1,957 | 2,530 | 2,055 | 2,162 |
| AudioSources | 95 | 121 | 106 | 108 |

Managed/Native Memory가 반복 횟수에 비례해 계속 증가하는 패턴은 관찰되지 않았습니다. 다만 Resident Memory만으로 누수 여부를 단정하지 않고 Unity 추적 영역과 오브젝트 수를 함께 비교했습니다.

### 3. Draw Call 및 배칭 개선

Frame Debugger에서 구름 Particle System의 배칭 중단 원인을 확인했습니다.

```text
Batch cause: Objects have different materials
Shader: Mobile/Particles/Alpha Blended
```

구름 Sprite를 Atlas로 통합하고 Particle System이 공용 Material을 사용하도록 변경했습니다. 캐릭터의 몸, 눈, 팔, 손, 발 파츠도 동일 Atlas와 Material을 사용하도록 정리했습니다.

결과적으로 Batches는 48에서 37로, SetPass Calls는 30에서 19로 감소했습니다.

### 4. Addressables 의존성 관리

Terrain 및 Background Prefab을 Scene과 설정 에셋이 직접 참조하던 구조를 Label 기반 비동기 로딩으로 변경했습니다.

```text
Addressables Group / Label
        ↓
LoadAssetsAsync<GameObject>()
        ↓
Block[] / BackgroundBlock[] 런타임 배열 생성
        ↓
기존 Terrain 선택 및 생성 로직에서 사용
        ↓
OnDestroy에서 AsyncOperationHandle Release
```

- Start/Middle Block Label 분리
- Far/Middle/Near Background Label 분리
- 로드 성공 여부와 컴포넌트 유효성 검사
- 로드 완료 전 Terrain 생성 방지
- Handle 생명주기 관리 및 Release
- Addressables Analyze로 중복 의존성 검사
- Build Report로 Bundle 포함 리소스 확인
- Editor와 Android 빌드에서 로딩 검증

관련 코드:

- [`TerrainGenerator.cs`](Assets/Scripts/RedRunner/TerrainGeneration/TerrainGenerator.cs)
- [`TerrainGenerationSettings.cs`](Assets/Scripts/RedRunner/TerrainGeneration/TerrainGenerationSettings.cs)
- [`BackgroundLayer.cs`](Assets/Scripts/RedRunner/TerrainGeneration/BackgroundLayer.cs)

## 핵심 코드 변경

아래 코드는 원본 프로젝트와 현재 구현의 핵심 차이를 발췌한 것입니다. 각 항목을 펼치면 변경 전후 코드를 확인할 수 있습니다.

<details>
<summary><strong>1. Prefab 직접 참조를 Addressables Label 참조로 변경</strong></summary>

### Before

`TerrainGenerationSettings`가 모든 Block Prefab을 직접 참조했습니다. 이 참조가 Scene 및 설정 에셋의 Bundle 의존성으로 이어졌습니다.

```csharp
[SerializeField]
protected Block[] m_StartBlocks;

[SerializeField]
protected Block[] m_MiddleBlocks;

[SerializeField]
protected Block[] m_EndBlocks;
```

### After

직접 참조 대신 Label만 직렬화하고, 실제 Block 배열은 런타임에 구성하도록 분리했습니다.

```csharp
[SerializeField]
protected AssetLabelReference StartBlocksLabel;

[SerializeField]
protected AssetLabelReference MiddleBlocksLabel;

[System.NonSerialized]
protected Block[] m_StartBlocks;

[System.NonSerialized]
protected Block[] m_MiddleBlocks;

public void SetBlocks(Block[] startBlocks, Block[] middleBlocks)
{
    m_StartBlocks = startBlocks;
    m_MiddleBlocks = middleBlocks;
}
```

이 변경으로 설정 에셋은 Prefab의 실제 위치를 알 필요가 없고, Addressables Group과 Label이 리소스 구성을 담당합니다.

</details>

<details>
<summary><strong>2. Label 기반 비동기 Block 로딩 추가</strong></summary>

### Before

기존 코드는 Inspector에 직렬화된 배열이 이미 채워져 있다는 전제에서 즉시 Terrain 생성을 시작했습니다.

```csharp
protected virtual void Update()
{
    if (m_Reset)
    {
        return;
    }

    Generate();
}
```

### After

게임 시작 시 Label별 Prefab을 비동기로 로드하고, 유효한 `Block` 컴포넌트만 런타임 배열에 저장합니다.

```csharp
protected virtual IEnumerator Start()
{
    yield return LoadBlocks();

    if (!m_BlocksReady)
    {
        Debug.LogError("블록 로드 실패");
        yield break;
    }

    yield return LoadBackgroundBlocks();
}

private IEnumerator LoadBlockSet(
    AssetLabelReference label,
    string setName,
    System.Action<Block[]> onLoaded)
{
    if (label == null || !label.RuntimeKeyIsValid())
    {
        Debug.LogError($"{setName} Label이 설정되지 않았습니다.");
        onLoaded?.Invoke(new Block[0]);
        yield break;
    }

    AsyncOperationHandle<IList<GameObject>> handle =
        Addressables.LoadAssetsAsync<GameObject>(label, null);

    m_BlockLoadHandles.Add(handle);
    yield return handle;

    if (handle.Status != AsyncOperationStatus.Succeeded)
    {
        Debug.LogError($"{setName} Block 로드 실패");
        onLoaded?.Invoke(new Block[0]);
        yield break;
    }

    List<Block> loadedBlocks = new List<Block>();

    IList<GameObject> loadedObjects = handle.Result;

    for (int i = 0; i < loadedObjects.Count; i++)
    {
        Block block = loadedObjects[i].GetComponent<Block>();

        if (block == null)
        {
            Debug.LogError(
                $"Block 컴포넌트가 없습니다: {loadedObjects[i].name}");
            continue;
        }

        loadedBlocks.Add(block);
    }

    onLoaded?.Invoke(loadedBlocks.ToArray());
}
```

`m_BlocksReady`와 `m_BackgroundReady`를 사용해 비동기 로딩이 끝나기 전에 생성 로직이 배열에 접근하지 않도록 했습니다.

</details>

<details>
<summary><strong>3. Background Prefab 배열을 Layer별 Label 로딩으로 변경</strong></summary>

### Before

각 Background Layer가 Prefab 배열을 직접 직렬화했습니다.

```csharp
[System.Serializable]
public struct BackgroundLayer
{
    public string name;
    public BackgroundBlock[] Blocks;
}
```

### After

Layer에는 Label을 저장하고, 로드된 배열은 직렬화하지 않는 런타임 데이터로 관리합니다.

```csharp
[System.Serializable]
public struct BackgroundLayer
{
    public string name;
    public AssetLabelReference BlocksLabel;

    [System.NonSerialized]
    public BackgroundBlock[] Blocks;
}
```

```csharp
AsyncOperationHandle<IList<GameObject>> handle =
    Addressables.LoadAssetsAsync<GameObject>(
        m_BackgroundLayers[i].BlocksLabel,
        null);

m_BackgroundLoadHandles.Add(handle);
yield return handle;

List<BackgroundBlock> loadedBlocks = new List<BackgroundBlock>();

IList<GameObject> loadedObjects = handle.Result;

for (int j = 0; j < loadedObjects.Count; j++)
{
    BackgroundBlock block =
        loadedObjects[j].GetComponent<BackgroundBlock>();

    if (block == null)
    {
        Debug.LogError(
            $"BackgroundBlock 컴포넌트가 없습니다: {loadedObjects[j].name}");
        continue;
    }

    loadedBlocks.Add(block);
}

m_BackgroundLayers[i].Blocks = loadedBlocks.ToArray();
```

Far, Middle, Near Layer가 각각 자신의 Label을 통해 필요한 Prefab 집합을 로드합니다.

</details>

<details>
<summary><strong>4. Addressables Handle 생명주기 관리 추가</strong></summary>

### Before

기존 종료 처리에는 Addressables 리소스 해제 과정이 없었습니다.

```csharp
protected virtual void OnDestroy()
{
    m_Singleton = null;
}
```

### After

로드 시 저장한 Handle을 오브젝트 생명주기가 끝날 때 Release합니다.

```csharp
protected virtual void OnDestroy()
{
    GameManager.OnReset -= Reset;

    for (int i = 0; i < m_BackgroundLoadHandles.Count; i++)
    {
        if (m_BackgroundLoadHandles[i].IsValid())
        {
            Addressables.Release(m_BackgroundLoadHandles[i]);
        }
    }

    for (int i = 0; i < m_BlockLoadHandles.Count; i++)
    {
        if (m_BlockLoadHandles[i].IsValid())
        {
            Addressables.Release(m_BlockLoadHandles[i]);
        }
    }

    m_BackgroundLoadHandles.Clear();
    m_BlockLoadHandles.Clear();

    if (m_Singleton == this)
    {
        m_Singleton = null;
    }
}
```

비동기 로드 성공 여부와 관계없이 유효한 Handle만 해제하고, 이벤트 구독도 함께 정리합니다.

</details>

<details>
<summary><strong>5. Saw별 AudioSource 관리에서 공용 AudioManager 호출로 변경</strong></summary>

### Before

Saw가 AudioClip, AudioSource, 충돌 중 재생 상태를 각각 관리했습니다.

```csharp
[SerializeField]
private AudioClip m_DefaultSound;

[SerializeField]
private AudioClip m_SawingSound;

[SerializeField]
private AudioSource m_AudioSource;
```

### After

Saw의 Audio 필드를 제거하고 효과음 재생 책임을 공용 `AudioManager`로 이동했습니다.

```csharp
// AudioManager.cs
[SerializeField]
protected AudioClip m_SawHitSound;

public void PlaySawHitSound(Vector3 position)
{
    PlaySoundOn(m_SoundAudioSource, m_SawHitSound);
}
```

```csharp
// Saw.cs
AudioManager.Singleton.PlaySawHitSound(transform.position);
```

이를 통해 Saw Prefab마다 AudioSource와 Audio 채널을 초기화하던 구조를 공용 재생 경로로 통합했습니다.

</details>

## Troubleshooting

### 1. 그룹을 분리했지만 Scene Bundle에 리소스가 남음

**증상**

Background Prefab을 별도 Addressables Group으로 이동했지만 Build Report의 Scene Bundle에도 Prefab과 PNG 의존성이 계속 포함됐습니다.

**원인 분석**

Addressables Group만 분리해도 Scene이나 ScriptableObject가 Prefab을 직접 참조하면 Unity는 해당 리소스를 Scene의 의존성으로 판단합니다. 따라서 물리적인 그룹 분리만으로는 Bundle 의존성이 분리되지 않았습니다.

**해결 및 검증**

- 설정 에셋의 직접 Prefab 배열을 `AssetLabelReference`로 변경
- 런타임에 Label로 Prefab을 로드해 배열 구성
- 직접 참조가 사라진 뒤 Addressables Analyze 다시 실행
- Build Report에서 Scene Bundle과 Background/Block Bundle의 포함 항목 재확인

### 2. Texture 최적화 후 Untracked Memory 증가

**증상**

Texture 설정 변경 후 Texture2D Memory는 `99.4 MB → 31.6 MB`로 감소했지만, Memory Profiler의 Untracked Memory는 약 `149.4 MB → 218.4 MB`로 증가했습니다.

**원인 분석**

Untracked Memory에는 Unity가 세부 카테고리로 분류하지 못한 플랫폼, 그래픽 드라이버 및 네이티브 할당이 포함될 수 있습니다. 따라서 Untracked 수치의 증가만으로 메모리 누수나 Texture 최적화 실패를 단정할 수 없었습니다.

**해결 및 검증**

- Untracked 수치만 보지 않고 Resident, Native, Managed, Graphics Memory를 함께 비교
- Texture2D 오브젝트별 메모리와 Import Settings 확인
- 동일 장면 상태에서 Snapshot을 다시 촬영
- 반복 재시작 시 Managed/Native Memory의 지속적인 단조 증가 여부 확인

재부팅 후 Texture 최적화 직후 218.4MB까지 증가했던 Untracked Memory가 170.3MB로 감소했고, 해당 libc_malloc 항목이 회수되는 것을 확인했습니다. 이를 통해 지속적인 누수보다 네이티브 allocator 또는 드라이버의 예약·캐시 메모리일 가능성이 높다고 판단했습니다.

## 실행 방법

1. 저장소를 Clone합니다.
2. Unity Hub에서 Unity `6000.3.14f1`로 프로젝트를 엽니다.
3. Addressables Group과 Profile 설정을 확인합니다.
4. `Assets/Scenes/Creation.unity` 또는 `Assets/Scenes/Play.unity`를 엽니다.
5. Android 측정 시 Development Build와 Autoconnect Profiler를 활성화합니다.

## 프로젝트 범위와 한계

- 이 저장소는 원작 전체를 새로 개발한 프로젝트가 아니라 기존 오픈소스 프로젝트를 분석하고 최적화한 사례입니다.
- 최적화 수치는 기기, 해상도, 실행 구간 및 빌드 설정에 따라 달라질 수 있습니다.

## 원작 및 라이선스

- Original project: [BayatGames/RedRunner](https://github.com/BayatGames/RedRunner)
- Original graphics: [Free Platform Game Assets](https://bayat.itch.io/platform-game-assets)
- License: [MIT](LICENSE)

원작자의 저작권과 라이선스를 유지하며, 이 저장소에서는 최적화 과정과 변경 사항을 중심으로 설명합니다.
