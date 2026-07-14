# REFACTOR GUIDE — Áp template NFramework (từ project ShibaSlide) vào Zombie War

> **Tài liệu này dành cho ai / AI nào sẽ thực hiện refactor project Zombie War.**
> Nó mô tả: (1) template kiến trúc của project ShibaSlide và **tại sao** nó được thiết kế như vậy, (2) bảng mapping chính xác từ code hiện tại của Zombie War sang kiến trúc mới, (3) các bước refactor theo thứ tự an toàn — mỗi phase xong đều compile và chạy được.
>
> **Nguồn template**: project ShibaSlide tại `d:\Game_anh_Q\ShibaSlide` (cần mở được folder này để copy `nframework`). Mọi đường dẫn ShibaSlide trong tài liệu tính từ root project đó.
>
> **Tất cả thông tin API trong tài liệu này đã được đối chiếu trực tiếp với source code thật + chạy thử game ShibaSlide trong editor** (flow đã xác nhận: `GameState: LOADING → FIRST → HOME → INGAME`, scene `Game` load additive và set active trong khi `Main` giữ nguyên).

---

## Mục lục

1. [Mục tiêu & nguyên tắc bất biến](#1-mục-tiêu--nguyên-tắc-bất-biến)
2. [Hiểu template ShibaSlide — từng khối và TẠI SAO](#2-hiểu-template-shibaslide)
3. [Hiện trạng Zombie War & bảng mapping tổng](#3-hiện-trạng-zombie-war--bảng-mapping)
4. [Phase 0 — Chuẩn bị project](#4-phase-0--chuẩn-bị)
5. [Phase A — Import NFramework](#5-phase-a--import-nframework)
6. [Phase B — Define, UserData, GameManager mới, scene Main](#6-phase-b--lõi-mới)
7. [Phase C — Chuyển UI sang BaseUIView prefabs](#7-phase-c--ui)
8. [Phase D — Thay singleton cũ + sửa call-sites](#8-phase-d--thay-singleton-cũ)
9. [Phase E — Additive scene flow + dọn code chết](#9-phase-e--additive-flow)
10. [Những bẫy đã biết (BẮT BUỘC ĐỌC)](#10-những-bẫy-đã-biết)
11. [Checklist nghiệm thu](#11-checklist-nghiệm-thu)
12. [Template hóa cho các game sau](#12-template-hóa)

---

## 1. Mục tiêu & nguyên tắc bất biến

**Mục tiêu**: refactor Zombie War về đúng template ShibaSlide (NFramework + hệ Manager + scene Main bootstrap + UI qua UIManager + save qua SaveManager) để code dễ đọc, dễ quản lý, dễ thêm feature, dễ làm việc nhóm — và template này tái dùng được cho mọi game sau.

**Nguyên tắc số 1 — GAMEPLAY KHÔNG ĐỔI.** Đây là refactor kiến trúc, không phải rework. Các thứ sau **tuyệt đối giữ nguyên từng con số, từng dòng logic**:

- `PlayerController` — di chuyển bằng joystick, xoay theo AutoAim target, animator params `Speed/MoveX/MoveY`.
- `AutoAim` — scan `OverlapSphereNonAlloc` mỗi 0.05s, line-of-sight check.
- `WeaponController` — toàn bộ ballistics, magazine/reserve ammo, reload timing, point-blank case, recoil, muzzle flash. (File này 334 dòng hơi to nhưng **KHÔNG tách trong đợt refactor này** — tách logic là việc khác, rủi ro khác.)
- `Bullet`, `Bomb`, `BombThrower`, `Tracer`, `Recoil`.
- `ZombieAI` — NavMesh chase, swing-prediction `WillConnect`, growl timing, speed multiplier.
- `ZombieHealth` (trừ chỗ dùng static `AliveCount` — xem [§10](#10-những-bẫy-đã-biết)), `ZombieRagdoll`, `DissolveEffect`.
- `ZombieSpawner` + `WaveConfig` — ring spawn, quadrant cycling, giant/runner chance, mọi số liệu phase.
- `PickupSpawner`, `Pickup`, `IDamageable` + `PendingDamage`.
- Luật chơi: sống sót `levelDuration = 180s` → Won; chết → Lost; win Level N → unlock Level N+1; `Time.timeScale = 0.4` slow-mo khi kết thúc; tutorial hiện 1 lần duy nhất ở Level 1.
- Âm thanh 3D positional (tiếng súng/zombie phát tại vị trí world) — xem bẫy [§10.1](#101-audio-3d).

**Nguyên tắc số 2 — mỗi phase xong phải compile sạch và chạy được.** Không gộp nhiều phase vào một lần sửa. Commit git sau mỗi phase.

**Nguyên tắc số 3 — không viết lại cái NFramework đã có.** Trước khi viết class/helper mới, kiểm tra `nframework/Assets/Scripts/Helper/`, `Extensions/`, `UtilsComponent/` xem đã có sẵn chưa.

---

## 2. Hiểu template ShibaSlide

### 2.1 NFramework — vị trí và module map

NFramework là framework nội bộ, nằm trọn trong **một folder tự chứa**: `Assets/ThirdParty/nframework/` (namespace `NFramework`, assembly definition riêng `NFramework.Runtime.asmdef`). Runtime code ở `nframework/Assets/Scripts/`:

| Module | Folder | Dùng để làm gì |
|---|---|---|
| Singleton | `Singleton/` | `Singleton<T>`, `SingletonMono<T>`, `LazySingletonMono<T>`, `LazyPersistentSingletonMono<T>` |
| UI framework | `UI/` | `UIManager`, `BaseUIView`, SafeArea, joystick, HoldButton... |
| Save | `Save/` | `SaveManager` + interface `ISaveable` |
| Pool | `Pool/` | `Pool`, `PooledObject` |
| Sound | `Sound/` | `SoundManager`, `SoundSO`, cần `AudioMixer` |
| Observer | `Observer/` | `EventManager` (event bus theo int id), `ObservableValue<T>`, `ObservableList<T>` |
| StateMachine | `StateMachine/` | `StateMachine` + `State` generic (ít dùng — manager thường tự viết FSM enum) |
| Helper | `Helper/` | `Logger`, `TimeUtils`, `PathUtils`, `WeightedList<T>`, `MathUtils`, `CoroutineHelper`, `DeviceInfo`... |
| Extensions | `Extensions/` | ~25 file extension method (Transform, Vector, GameObject, String...) |
| Attributes | `Attributes/` | `ConditionalField`, `ReadOnly`, `ButtonMethod`, `Dropdown`, `MinMaxRange`, `Foldout` — thay thế Odin cho nhu cầu cơ bản |
| UtilsComponent | `UtilsComponent/` | `DontDestroyOnLoad`, `FPSCounter`, `AdaptiveResolution`, `Invoker` |
| Vibration | `Vibration/` | `VibrationManager` (haptics, ISaveable) |
| Network | `Network/` | `InternetChecker` |
| **Module cần SDK ngoài** | `Ads/`, `IAP/`, `Tracking/`, `FirebaseService/`, `Review/` | Được bọc define `USE_ADMOB_ADS`, `USE_FIREBASE`, `USE_UNITY_PURCHASING`, `USE_IN_APP_REVIEW`, `USE_ADJUST_ANALYTICS`... — **không bật define thì compile sạch, không cần SDK**. Zombie War chưa có ads/IAP nên cứ để nguyên, sau này cần thì bật. |

`nframework/Assets/3rdParty/` bundle sẵn (đi kèm khi copy folder, không cần cài thêm): **DOTween + DOTweenPro** (dạng DLL precompiled ở `3rdParty/Plugins/Demigiant/`), **NiceVibrations**, **Rotary Heart SerializableDictionary**, **Mobile-Dialog-Unity** (native dialog `pingak9`), **IngameDebugConsole**, **MackySoft.SerializeReferenceExtensions**. Ngoài ra có `nframework/Assets/AudioMixer.mixer` dùng cho SoundManager.

Dependency NGOÀI folder mà NFramework cần (xem Phase A): **Newtonsoft Json** (package `com.unity.nuget.newtonsoft-json`). Chỉ vậy. (UniTask và Odin Inspector là dependency của *game code* ShibaSlide, KHÔNG phải của NFramework — Zombie War không cần.)

### 2.2 SingletonMono — tại sao

```csharp
// nframework/Assets/Scripts/Singleton/SingletonMono.cs — nguyên văn
public class SingletonMono<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T I { get; private set; }
    public static bool IsSingletonAlive => I != null;

    protected virtual void Awake()
    {
        if (I == null) I = this as T;
        else { Debug.LogError($"Duplicate singleton type of {typeof(T)}"); Destroy(gameObject); }
    }

    protected virtual void OnDestroy()
    {
        if (I == this) I = null;
    }
}
```

**Tại sao dùng nó thay vì tự viết `public static X Instance`:**
- Mọi manager viết giống hệt nhau → người mới đọc 1 lần hiểu cả hệ thống. Truy cập thống nhất `XxxManager.I`.
- Guard duplicate + tự null khi destroy — đúng cái boilerplate mà Zombie War đang lặp lại trong 4 class (`GameManager`, `AudioManager`, `SceneLoader`, `CameraShake`).
- **Lưu ý quan trọng**: `SingletonMono` KHÔNG có `DontDestroyOnLoad`. Nó sống được xuyên suốt là nhờ **mô hình scene** (mục 2.3): manager đặt trong scene `Main` và `Main` không bao giờ bị unload. Đây là chủ đích thiết kế — không cần DDOL, không có object "mồ côi" lơ lửng ngoài scene, hierarchy lúc nào cũng nhìn thấy đủ manager.
- Khi override `Awake`/`OnDestroy` trong class con, **bắt buộc** gọi `base.Awake()` / `base.OnDestroy()`.

### 2.3 Mô hình scene: Main bootstrap + gameplay additive — tại sao

```
Main.unity  (scene 0, load lúc mở app, KHÔNG BAO GIỜ unload)
├── GameManager          ← FSM app-level, điều phối mọi thứ
├── UIManager            ← Canvas gốc, mở mọi menu/popup
├── SaveManager
├── SoundManager
├── UserData             ← ví/tiến trình người chơi (ISaveable)
├── FactoryManager       ← chứa các Pool con (FX)
├── VibrationManager, InternetChecker (tùy chọn)
├── Main Camera (UI), EventSystem

Game.unity  (load ADDITIVE khi vào trận, SetActiveScene, unload khi thoát)
├── GameplayManager, GridManager, ...   ← manager CHỈ tồn tại trong trận
├── board, tiles, camera gameplay...
```

**Tại sao:**
- **Manager sống suốt vòng đời app** mà không cần DontDestroyOnLoad, không cần "re-find" reference sau mỗi lần load scene, không cần pattern "later duplicates self-destruct" (như `SceneLoader` của Zombie War hiện tại phải làm).
- **Tách trách nhiệm rõ**: manager nào phục vụ cả app (save, sound, UI, ví tiền) ở `Main`; manager nào chỉ có nghĩa trong một trận (spawner, level state) ở scene gameplay — vào trận thì có, thoát trận là được dọn sạch theo scene, không lo state cũ dính lại.
- **Restart cực rẻ và sạch**: unload scene gameplay + load lại = mọi object trong trận về trạng thái ban đầu, trong khi UI/save/sound không bị động đến. ShibaSlide làm restart bằng state `RESET` → `CRUnloadScene(Game)` → `EnterInGame()`.
- Flow load/unload nằm trong `GameManager` (2 coroutine, nguyên văn từ ShibaSlide):

```csharp
private IEnumerator CRLoadScene(string sceneName, Action callback = null)
{
    yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
    var gameScene = SceneManager.GetSceneByName(sceneName);
    SceneManager.SetActiveScene(gameScene);   // để lighting/Instantiate mặc định vào scene gameplay
    callback?.Invoke();
}

public IEnumerator CRUnloadScene(string sceneName, Action callback = null)
{
    yield return SceneManager.UnloadSceneAsync(sceneName, UnloadSceneOptions.None);
    callback?.Invoke();
}
```

### 2.4 UIManager + BaseUIView + Define.UIName — tại sao

**Cách hoạt động** (`nframework/Assets/Scripts/UI/UIManager.cs`, `BaseUIView.cs`):

- `UIManager : SingletonMono<UIManager>` gắn trên GameObject có `Canvas + CanvasScaler + GraphicRaycaster`. Ở `Awake`, nó tự tạo 4 canvas con theo enum `EUILayer { Background, Menu, Popup, AlwaysOnTop }` — view thuộc layer nào thì nằm dưới canvas đó, nên **popup luôn đè lên menu, loading luôn đè lên tất cả** mà không phải quản sorting order thủ công.
- Mỗi màn hình/popup là **một prefab có component kế thừa `BaseUIView`**, đặt trong `Resources/UI/` (đường dẫn root cấu hình bằng serialized field `_resourceRootPath` trên UIManager — ShibaSlide đặt là `UI`, trỏ vào `Assets/Game/Resources/UI/`). **Tên file prefab = identifier** truyền vào `Open`.
- API chính:

```csharp
UIManager.I.Open(Define.UIName.HOME_MENU);                       // mở, không cần type
UIManager.I.Open<ResultPopup>(Define.UIName.RESULT_POPUP);       // mở và lấy ref có type
UIManager.I.Open<LoadingPopup>(Define.UIName.LOADING_POPUP, v => { /* onOpened */ });
UIManager.I.Close(view);          // hoặc Close("Identifier"); view tự đóng: CloseSelf()
UIManager.I.GetOpenedView<T>(identifier);   UIManager.I.IsSpecificViewShown(id, out var v);
UIManager.I.CloseAllInLayer(EUILayer.Popup);
```

- `BaseUIView` có serialized `_uiLayer`, `_isUnique` (chỉ 1 instance), `_canDestroy`; lifecycle hook `OnOpen()` / `OnClose()` (override để subscribe/unsubscribe event, refresh data); `CloseSelf(bool destroy = false)`; `HandleOnKeyBack()` (UIManager tự route phím Escape/Back về view trên cùng).
- View đóng **không bị Destroy** mặc định — nó được ẩn đi và cache lại (per-identifier `Stack`), lần mở sau tái dùng → không tốn Instantiate. Static event `UIManager.OnOpenedView` / `OnClosedView` cho hệ thống khác lắng nghe.

**Tại sao mô hình này tốt hơn UI đặt cứng trong scene (cách Zombie War đang làm):**
- **UI không bị nhân bản qua từng scene.** Zombie War hiện phải copy HUD Canvas + PauseMenu + ResultPanel vào cả Level1 lẫn Level2 — sửa 1 nút phải sửa 2 nơi. Thành prefab mở qua UIManager thì chỉ có 1 nguồn.
- **Mở UI từ bất kỳ đâu bằng 1 dòng**, không cần kéo reference chéo scene, không cần `SetActive` chuỗi panel.
- **Vòng đời rõ ràng**: `OnOpen`/`OnClose` là chỗ chuẩn để subscribe/unsubscribe — hết leak event vì quên hủy đăng ký.
- **Back key, khóa input khi đang load (`DisableInteract`/`EnableInteract`), layer ordering** — được framework lo sẵn.
- Game còn có thêm class `Popup : BaseUIView` (ở game code, không phải framework — `Assets/Game/Scripts/UI/Popup/Popup.cs` của ShibaSlide): tự gắn close button + tween scale/fade khi mở + SFX mở popup. Mọi popup thật kế thừa `Popup` để đồng nhất cảm giác.

### 2.5 SaveManager + ISaveable — tại sao

```csharp
// nframework/Assets/Scripts/Save/SaveManager.cs
public interface ISaveable
{
    string SaveKey { get; }        // key duy nhất, vd "UserData"
    bool DataChanged { get; set; } // setter của data phải set true
    object GetData();              // trả về object SaveData (serializable)
    void SetData(string data);     // nhận JSON (hoặc "" lần đầu) → parse vào field
    void OnAllDataLoaded();        // gọi sau khi TẤT CẢ ISaveable đã SetData xong
}
```

- Mỗi manager tự định nghĩa một nested class `[Serializable] SaveData` chứa đúng phần data của mình, tự implement `ISaveable`.
- `GameManager` lúc boot gọi `SaveManager.I.RegisterSaveData(...)` cho từng manager rồi `SaveManager.I.Load()` **một lần duy nhất**.
- `SaveManager` serialize từng `GetData()` bằng Newtonsoft JSON thành `Dictionary<SaveKey, json>`, ghi ra **file** trong persistent path (XOR-encrypt bằng `deviceUniqueIdentifier`, có file backup; editor thì không encrypt; WebGL fallback PlayerPrefs).
- **Auto-save mỗi 5s** (chỉ ghi khi có `DataChanged == true`) + save khi mất focus/pause/quit. → gameplay code KHÔNG BAO GIỜ phải gọi save thủ công, chỉ cần set `DataChanged = true`.
- Editor menu **`NFramework > Delete Save`** xóa save để test.

**Tại sao tốt hơn `GameSettings` static + PlayerPrefs:**
- Data của ai người đó giữ — thêm hệ thống mới (vd inventory) chỉ cần class đó implement `ISaveable` và thêm 1 dòng register, không phải đụng vào class settings trung tâm.
- Save theo object JSON → thêm field mới không cần thêm key mới, dữ liệu cũ tự tương thích (field mới nhận default).
- Ghi file gộp 1 lần theo interval thay vì `PlayerPrefs.Save()` rải rác mỗi lần set.
- Chống sửa save cơ bản (encrypt + backup file).

### 2.6 SoundManager + Define.SoundName — tại sao

- `SoundManager : SingletonMono<SoundManager>, ISaveable` (framework, không phải game code): 1 `AudioSource` music + pool N AudioSource SFX (mặc định 10, cấu hình `_sfxAudioSourceCount`), route qua **AudioMixer** với 2 exposed param tên chính xác `"MusicVolume"` và `"SFXVolume"` (mixer có sẵn: `nframework/Assets/AudioMixer.mixer` — kéo vào 3 field `_audioMixer`, `_musicMixerGroup`, `_sfxMixerGroup`).
- API: `PlayMusic(clip/SoundSO)`, `StopMusic(fadeTime)`, `PlaySFX(clip/SoundSO)`, và bản load theo path `PlayMusicResource(path)` / `PlaySFXResource(path)` — path tính từ serialized `_resourceRootPath` (ShibaSlide để rỗng, path đầy đủ nằm trong `Define.SoundName`, vd `"Audio/Sfx/click_button"` → file `Assets/Game/Resources/Audio/Sfx/click_button.wav`).
- Properties `MusicStatus/SFXStatus` (bool bật tắt) và `MusicVolume/SFXVolume` (0–1, đổi ra dB log scale cho mixer) — **tự save** vì là ISaveable (SaveKey `"SoundManager"`), có static events `OnMusicVolumeChanged`... cho UI settings bind.

**Tại sao**: âm lượng chỉnh một chỗ ăn toàn cục qua mixer (kể cả AudioSource ngoài SoundManager, miễn là route vào đúng mixer group — điểm này quan trọng cho Zombie War, xem [§10.1](#101-audio-3d)); volume settings tự persist; gọi SFX 1 dòng từ bất cứ đâu.

### 2.7 Pool + FactoryManager — tại sao

- Framework: `Pool : MonoBehaviour` (serialized: `_initializeAtAwake`, `_autoExpandPool`, `_maxPoolSize`, `_initPoolSize`, `_objectToPool`) + `PooledObject : MonoBehaviour` (giữ back-ref `Pool`, UnityEvent hook `OnSpawnedFromPool`/`OnBeforeReturnToPool`, method `ReturnToPool()`).
- Game: `FactoryManager : SingletonMono<FactoryManager>` — GameObject trong `Main.unity`, mỗi **child** là một GameObject gắn `Pool` đã cấu hình prefab; `Awake` gom hết vào dictionary:

```csharp
public class FactoryManager : SingletonMono<FactoryManager>
{
    private Dictionary<PooledObject, Pool> _poolDic = new();
    protected override void Awake()
    {
        base.Awake();
        foreach (var pool in GetComponentsInChildren<Pool>())
        {
            pool.InitializePool();
            _poolDic.Add(pool.ObjectToPool, pool);
        }
    }
    public PooledObject GetObjectFromPool(PooledObject prefab) => _poolDic[prefab].GetPooledObject();
}
```

**Tại sao**: pool cấu hình bằng inspector (prefab nào, prewarm bao nhiêu, expand không) thay vì hard-code; caller chỉ giữ reference prefab `PooledObject` và gọi `FactoryManager.I.GetObjectFromPool(prefab)`; trả về pool bằng `pooledObject.ReturnToPool()`.

### 2.8 FSM hai tầng — tại sao

- **Tầng app** — `GameManager` với `enum EGameState { NONE, LOADING, FIRST, HOME, INGAME, RESET }`. Chuyển state qua một cổng duy nhất:

```csharp
private void SetGameState(EGameState state)
{
    if (_state != state) { _state = state; HandleGameStateChanged(_state); }
}
private void HandleGameStateChanged(EGameState state)
{
    Debug.Log($"GameState: {state}");
    switch (state) { /* mỗi case = toàn bộ việc phải làm khi vào state đó */ }
}
// public API chỉ là các hàm Enter*: EnterHome(), EnterInGame(), EnterReset()...
```

- **Tầng trận đấu** — manager riêng trong scene gameplay (ShibaSlide: `GameplayManager` + `EGameplayState`) quản state trong một trận. Với Zombie War tầng này là `LevelManager` (chính là `GameManager` cũ đổi tên) với `GameState { Playing, Won, Lost }` giữ nguyên.

**Tại sao**: nhìn `HandleGameStateChanged` là thấy TOÀN BỘ luồng game trong một hàm — mở UI gì, nhạc gì, load scene gì ở mỗi bước. Debug flow chỉ cần đọc log `GameState: ...`. App-flow (menu/loading/scene) và match-flow (đang chơi/thắng/thua) không trộn vào nhau.

### 2.9 ScriptableObject config — tại sao

Mọi số liệu tuning nằm trong `.asset` (ShibaSlide: `GameConfig`, `BoosterConfig`... trong `Assets/Game/ScriptableObjects/`), manager giữ `[SerializeField] private XxxConfig _config;`. Designer chỉnh số không đụng code, không đụng scene. **Zombie War đã làm đúng pattern này** với `WaveConfig` + `WeaponData` — giữ nguyên, chỉ bổ sung một `GameConfig` mới cho các hằng gameplay chung (xem Phase B).

### 2.10 Static events per manager — tại sao

Mỗi manager phát `public static event Action<T> OnXxxChanged` khi data đổi (vd `UserData.OnCoinChanged`). UI subscribe trong `OnOpen`, unsubscribe trong `OnClose`. **Tại sao**: UI không bao giờ poll trong Update; manager không cần biết UI nào tồn tại; static event nên không cần reference đến instance. Zombie War đã dùng events (instance-level: `PlayerHealth.OnHealthChanged`...) — giữ nguyên chúng, chỉ áp thêm quy ước subscribe/unsubscribe trong `OnOpen`/`OnClose` cho UI mới.

### 2.11 Define.cs — tại sao

Một file duy nhất `Assets/Game/Scripts/Utils/Define.cs` chứa **mọi string constant** dạng nested static class: `Define.UIName.*` (identifier UI = tên prefab), `Define.SceneName.*`, `Define.SoundName.*` (path Resources của SFX), `Define.SoundBG.*`. **Tại sao**: string gõ tay rải rác là nguồn bug thầm lặng số 1 (đổi tên scene/prefab là chết runtime không báo compile); gom một chỗ thì đổi tên chỉ sửa 1 dòng và mọi chỗ dùng đều là const có autocomplete. Zombie War hiện hard-code `"Level1"`, `"HomeMenu"`... ở ít nhất 4 file — sẽ dọn hết.

### 2.12 Folder convention

```
Assets/_Game/                     (Zombie War đã có sẵn _Game — GIỮ, tương đương Assets/Game của ShibaSlide)
├── Scripts/
│   ├── Manager/        ← các SingletonMono manager (GameManager, FactoryManager...)
│   ├── Data/           ← UserData và data-singleton ISaveable khác
│   ├── UI/
│   │   ├── Menu/       ← view full-screen (HomeMenu, GamePlayMenu)
│   │   ├── Popup/      ← Popup base + các popup
│   │   └── Item/       ← widget con tái dùng (bar, button item...)
│   ├── Utils/          ← Define.cs
│   ├── Player/ Weapons/ Zombie/ Core/   ← gameplay giữ nguyên chỗ cũ
│   └── Editor/
├── Resources/
│   ├── UI/             ← TẤT CẢ prefab BaseUIView (load bằng identifier)
│   └── Audio/Sfx, Audio/Bgm   ← clip load theo Define.SoundName
├── Prefabs/            ← prefab tham chiếu trực tiếp (KHÔNG load bằng string): zombie, gun, fx...
├── ScriptableObjects/  ← .asset config
└── Scenes/             ← Main.unity, Level1.unity, Level2.unity
```

Quy ước cốt lõi: **`Resources/` chỉ chứa thứ load động theo string** (UI prefab, audio). Mọi thứ khác reference trực tiếp qua inspector để còn hưởng ưu điểm asset dependency tracking của Unity.

---

## 3. Hiện trạng Zombie War & bảng mapping

Zombie War: Unity **2022.3.62f3**, Built-in RP, code namespace `ZombieWar.*` trong `Assets/_Game/Scripts/`, 3 scene thay thế nhau (`HomeMenu`, `Level1`, `Level2`). Code sạch, đã có events + `IDamageable`/`IPoolable` — thuận lợi.

### Bảng mapping tổng (cũ → mới)

| Hiện tại (Zombie War) | Sau refactor | Ghi chú |
|---|---|---|
| `Core/GameManager.cs` (`Instance`, per-level timer/kills/win-lose + scene load + unlock) | **Tách đôi**: ① `Manager/LevelManager.cs` = phần timer/kills/win-lose/layer-collision, sống trong Level scene, `SingletonMono<LevelManager>`; ② `Manager/GameManager.cs` MỚI = FSM app-level trong Main | Logic gameplay giữ nguyên 100%, chỉ chuyển nhà |
| `Core/GameSettings.cs` (static, PlayerPrefs) | `Data/UserData.cs : SingletonMono, ISaveable` (highestUnlockedLevel, hasSeenTutorial) + volume do NFramework `SoundManager` tự giữ | Xóa GameSettings ở cuối Phase D. Không migrate PlayerPrefs cũ (đã chốt) |
| `Core/AudioManager.cs` (SFX 3D positional + music) | **GIỮ phần 3D**, đổi tên `Manager/WorldSoundManager.cs : SingletonMono`, route qua mixer của NFramework; music chuyển cho NFramework `SoundManager` | ⚠️ KHÔNG map thẳng sang SoundManager — mất 3D positional. Xem §10.1 |
| `Core/SceneLoader.cs` (DDOL + overlay) | Xóa. Thay bằng `GameManager.CRLoadScene/CRUnloadScene` (additive) + `LoadingPopup : BaseUIView` | |
| `Core/ObjectPool.cs` static + `IPoolable` | **GIỮ NGUYÊN** trong đợt này | Xem §10.6 — chuyển sang NFramework Pool đụng vào Bullet/ZombieSpawner/VFX, rủi ro gameplay cao, để đợt sau |
| `Core/CameraShake.cs` | Đổi base class thành `SingletonMono<CameraShake>`, vẫn nằm trong Level scene (vì camera gameplay per-level) | 5 phút, chỉ đổi boilerplate |
| `UI/MenuController.cs` (scene HomeMenu) | `UI/Menu/HomeMenu.cs : BaseUIView` (layer Menu), prefab `Resources/UI/HomeMenu.prefab` | Scene HomeMenu.unity bị xóa |
| Level-select popup (panel trong HomeMenu scene) | `UI/Popup/LevelSelectPopup.cs : Popup`, prefab riêng | |
| `UI/SettingsPanel.cs` (panel dùng chung) | `UI/Popup/SettingsPopup.cs : Popup` — slider bind `SoundManager.I.MusicVolume/SFXVolume` | Dùng chung cho Home + pause, mở bằng `UIManager.I.Open` |
| `UI/HUDController.cs` (canvas trong Level scenes) | `UI/Menu/GamePlayMenu.cs : BaseUIView` (layer Menu), prefab `Resources/UI/GamePlayMenu.prefab`, chứa cả joystick | ⚠️ Cross-scene reference — xem §10.2 |
| `UI/PauseMenu.cs` | `UI/Popup/PausePopup.cs : Popup` | Nút pause nằm trên GamePlayMenu |
| `UI/ResultPanel.cs` | `UI/Popup/ResultPopup.cs : Popup` | |
| `UI/TutorialPanel.cs` | `UI/Popup/TutorialPopup.cs : Popup` | Điều kiện hiện chuyển vào flow GameManager/LevelManager |
| `UI/DamageFlashUI.cs`, `UI/SafeArea.cs` | Giữ — DamageFlash thành phần của GamePlayMenu prefab; SafeArea có thể thay bằng `NFramework` SafeArea hoặc giữ nguyên | |
| Scene `"Level1"`, `"HomeMenu"` string rải rác | `Define.SceneName.*` + `GameConfig.levels` | |
| Parse tên scene `sceneName.Substring(5)` để unlock | `LevelManager` có `[SerializeField] int levelNumber` set trong từng Level scene | Hết brittle |
| `GameObject.FindGameObjectWithTag("Player")` (ZombieAI, ZombieSpawner, PickupSpawner) | **GIỮ NGUYÊN đợt này** (hoạt động bình thường với additive load) | Ghi nhận cleanup đợt sau: LevelManager giữ ref Player |
| Editor menu `ZombieWar/Reset Level Progress` | Giữ menu, đổi ruột: gọi `SaveManager.DeleteSave()` hoặc reset UserData | |

---

## 4. Phase 0 — Chuẩn bị

1. **Commit git sạch** toàn bộ project Zombie War trước khi làm bất cứ gì. Tạo branch `refactor/nframework-template`.
2. **Bật Force Text serialization**: `Edit > Project Settings > Editor > Asset Serialization Mode = Force Text`. (Scene hiện đang binary/mixed — Force Text để diff/merge được trong lúc refactor.) Sau khi bật, mở từng scene và Save lại để scene được ghi lại dạng text. Commit riêng bước này.
3. Xác nhận project mở bằng Unity **2022.3.62f3** (đúng version hiện tại, không nâng cấp trong đợt refactor).

---

## 5. Phase A — Import NFramework

Mục tiêu phase: project compile sạch với NFramework bên trong, **chưa đổi bất kỳ dòng game code nào**.

### A1. Thêm Newtonsoft Json

Mở `Packages/manifest.json` của Zombie War, thêm vào `dependencies`:

```json
"com.unity.nuget.newtonsoft-json": "3.2.1",
```

### A2. Copy folder nframework

Copy **nguyên folder kèm toàn bộ file `.meta`** (để giữ GUID — các prefab/asset nội bộ nframework reference nhau bằng GUID):

```
TỪ:  d:\Game_anh_Q\ShibaSlide\Assets\ThirdParty\nframework
ĐẾN: D:\unity\MyGame\Zombie War\Assets\ThirdParty\nframework
```

(Copy cả `Assets\ThirdParty.meta` nếu Zombie War chưa có folder `ThirdParty`.)

### A3. Sửa 2 chỗ dính PlayAdSDK (BẮT BUỘC — không sửa là không compile)

NFramework trong ShibaSlide có đúng **một** dependency cứng vào PlayAd SDK (SDK riêng của ShibaSlide, không copy sang):

**(1) `nframework/Assets/Scripts/Save/SaveManager.cs`** — xóa 2 dòng:

```csharp
// dòng 6 — XÓA:
using PlayAd.SDK.Ads;

// trong OnApplicationQuit() — XÓA dòng này, GIỮ dòng Save():
private void OnApplicationQuit()
{
    Save();
    PlayAdSupport.GetUser().SaveAllAsync();   // ← XÓA
}
```

**(2) `nframework/Assets/Scripts/NFramework.Runtime.asmdef`** — trong mảng `references` có 4 GUID; xóa GUID của PlayAdSDK:

```json
"references": [
    "GUID:57a0b9bc628ab4740af4b6f1f0b2e134",   // Lofelt.NiceVibrations  — GIỮ
    "GUID:9166019891254ca498cc0bcad6c84540",   // pingak9.Mobile-Dialog  — GIỮ
    "GUID:3e489d156e241e9428b8aedd34f6a6dc",   // RotaryHeart.Runtime    — GIỮ
    "GUID:15cbcdca7da98924f8a67bcf159dfcb1"    // PlayAdSDK              — XÓA DÒNG NÀY
],
```

### A4. Kiểm tra compile

- Mở Unity, đợi import + compile. Console phải **0 error**.
- Các module `Ads/ IAP/ Tracking/ FirebaseService/ Review` không gây lỗi vì code SDK bọc trong define `USE_ADMOB_ADS`, `USE_FIREBASE`, `USE_UNITY_PURCHASING`, `USE_IN_APP_REVIEW`, `USE_ADJUST_ANALYTICS`... — **không bật define nào cả**. Kiểm tra `Project Settings > Player > Scripting Define Symbols` không chứa `USE_*`, `IS_SDK`, `ADDRESSABLE`.
- Nếu lỗi GUID trùng (hiếm — chỉ khi Zombie War đã có sẵn asset trùng GUID với đồ trong nframework, vd đã từng import DOTween): giữ bản trong nframework, xóa bản trùng bên ngoài.
- Chạy thử scene `HomeMenu` cũ — game phải chạy y như trước (chưa có gì đổi).
- **Commit: "Phase A: import NFramework"**.

---

## 6. Phase B — Lõi mới

Mục tiêu phase: có `Define.cs`, `UserData`, `GameManager` mới, scene `Main.unity` hoạt động — game cũ vẫn chạy song song (chưa xóa gì).

### B1. `Assets/_Game/Scripts/Utils/Define.cs`

```csharp
namespace ZombieWar
{
    public static class Define
    {
        public static class UIName
        {
            public const string HOME_MENU          = "HomeMenu";
            public const string GAMEPLAY_MENU      = "GamePlayMenu";
            public const string LOADING_POPUP      = "LoadingPopup";
            public const string LEVEL_SELECT_POPUP = "LevelSelectPopup";
            public const string SETTINGS_POPUP     = "SettingsPopup";
            public const string PAUSE_POPUP        = "PausePopup";
            public const string RESULT_POPUP       = "ResultPopup";
            public const string TUTORIAL_POPUP     = "TutorialPopup";
        }

        public static class SceneName
        {
            public const string MAIN = "Main";
            public const string LEVEL_PREFIX = "Level";           // Level1, Level2
            public static string Level(int number) => LEVEL_PREFIX + number;
        }

        public static class SoundName
        {
            // path tính từ Resources/, clip đặt tại Assets/_Game/Resources/Audio/Sfx/
            public const string CLICK_BUTTON = "Audio/Sfx/click_button";
            public const string OPEN_POPUP   = "Audio/Sfx/open_popup";
            public const string WIN          = "Audio/Sfx/win";
            public const string LOSE         = "Audio/Sfx/lose";
        }

        public static class SoundBG
        {
            public const string BGM_MAIN   = "Audio/Bgm/BGM_main";
            public const string BGM_INGAME = "Audio/Bgm/BGM_ingame";   // nếu không có nhạc ingame riêng thì bỏ
        }
    }
}
```

Ghi chú: move các AudioClip đang được kéo tay vào inspector (clickClip, winClip, loseClip, menu music...) vào `Assets/_Game/Resources/Audio/Sfx|Bgm/` với tên khớp const. Clip SFX **3D theo world** (tiếng súng, zombie) KHÔNG move — chúng vẫn là serialized ref của WeaponController/ZombieAI (xem §10.1).

### B2. `Assets/_Game/Scripts/Data/UserData.cs`

Thay vai trò của `GameSettings` (trừ volume — SoundManager tự lo):

```csharp
using System;
using NFramework;
using Newtonsoft.Json;
using UnityEngine;

namespace ZombieWar
{
    public class UserData : SingletonMono<UserData>, ISaveable
    {
        public static event Action<int> OnHighestUnlockedLevelChanged = delegate { };

        public const int MaxLevel = 2;   // giữ nguyên từ GameSettings.MaxLevel

        [SerializeField] private SaveData _saveData = new SaveData();

        public int HighestUnlockedLevel
        {
            get => Mathf.Clamp(_saveData.highestUnlockedLevel, 1, MaxLevel);
            private set
            {
                var clamped = Mathf.Clamp(value, 1, MaxLevel);
                if (_saveData.highestUnlockedLevel != clamped)
                {
                    _saveData.highestUnlockedLevel = clamped;
                    DataChanged = true;
                    OnHighestUnlockedLevelChanged?.Invoke(clamped);
                }
            }
        }

        public bool HasSeenTutorial
        {
            get => _saveData.hasSeenTutorial;
            set
            {
                if (_saveData.hasSeenTutorial != value)
                {
                    _saveData.hasSeenTutorial = value;
                    DataChanged = true;
                }
            }
        }

        public bool IsLevelUnlocked(int level) => level >= 1 && level <= HighestUnlockedLevel;

        /// <summary>Giữ nguyên semantics GameSettings.UnlockLevel: true CHỈ khi unlock mới.</summary>
        public bool UnlockLevel(int level)
        {
            if (level < 2 || level > MaxLevel) return false;
            if (level <= HighestUnlockedLevel) return false;
            HighestUnlockedLevel = level;
            return true;
        }

        #region ISaveable
        [Serializable]
        public class SaveData
        {
            public int highestUnlockedLevel = 1;
            public bool hasSeenTutorial;
        }

        public string SaveKey => "UserData";
        public bool DataChanged { get; set; }
        public object GetData() => _saveData;

        public void SetData(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                _saveData = new SaveData();
                DataChanged = true;
            }
            else
            {
                _saveData = JsonConvert.DeserializeObject<SaveData>(data);
            }
        }

        public void OnAllDataLoaded() { }
        #endregion
    }
}
```

### B3. `Assets/_Game/Scripts/Manager/GameManager.cs` (MỚI — thay file cũ ở Phase D)

Trong lúc Phase B, tạo file mới với **namespace/tên không đụng** file cũ, ví dụ tạm `ZombieWar.Manager.GameManager` hoặc đơn giản làm Phase B+C+D trên branch và chấp nhận 2 file: khuyến nghị **đổi tên file cũ trước** (`GameManager.cs` → `LevelManager.cs`, class `GameManager` → `LevelManager`, sửa các call-site `GameManager.Instance` → `LevelManager.Instance` bằng find-replace toàn project — đây là rename thuần, behavior không đổi) rồi mới tạo GameManager mới. Danh sách file đang gọi `GameManager.Instance`: `PlayerHealth`, `ZombieHealth` (RegisterKill), `HUDController`, `ResultPanel`, `PauseMenu`, `ZombieSpawner`/`PickupSpawner` (nếu đọc `TimeElapsed`), `WeaponController` (nếu check State) — **grep `GameManager.Instance` để bắt đủ 100%**.

GameManager mới:

```csharp
using System;
using System.Collections;
using NFramework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ZombieWar
{
    public enum EGameState { NONE, LOADING, HOME, INGAME, RESET }

    public class GameManager : SingletonMono<GameManager>
    {
        [SerializeField] private GameConfig _gameConfig;

        private EGameState _state;
        public EGameState GetGameState() => _state;

        /// <summary>Level đang chơi / sắp vào (1-based). Set bởi EnterInGame.</summary>
        public int CurrentLevel { get; private set; } = 1;

        private void Start()
        {
            EnterLoading();
        }

        private void SetGameState(EGameState state)
        {
            if (_state != state)
            {
                _state = state;
                HandleGameStateChanged(_state);
            }
        }

        private void RegisterAndLoadSave()
        {
            SaveManager.I.RegisterSaveData(UserData.I);
            SaveManager.I.RegisterSaveData(SoundManager.I);
            SaveManager.I.RegisterSaveData(VibrationManager.I);   // bỏ dòng này nếu không đặt VibrationManager
            SaveManager.I.Load();
        }

        private void HandleGameStateChanged(EGameState state)
        {
            Debug.Log($"GameState: {state}");
            switch (state)
            {
                case EGameState.LOADING:
                    Application.targetFrameRate = 60;
                    UIManager.I.Open<LoadingPopup>(Define.UIName.LOADING_POPUP)
                        .AssignEvent(LoadingComplete);
                    break;

                case EGameState.HOME:
                    SoundManager.I.StopMusic();
                    SoundManager.I.PlayMusicResource(Define.SoundBG.BGM_MAIN);
                    UIManager.I.Open(Define.UIName.HOME_MENU);
                    break;

                case EGameState.INGAME:
                    SoundManager.I.StopMusic();
                    UIManager.I.Open<LoadingPopup>(Define.UIName.LOADING_POPUP).AssignEvent(null);
                    StartCoroutine(CRLoadScene(Define.SceneName.Level(CurrentLevel), () =>
                    {
                        UIManager.I.Open(Define.UIName.GAMEPLAY_MENU);
                        UIManager.I.Close(Define.UIName.LOADING_POPUP);
                        LevelManager.I.Begin();   // xem B5 — tutorial gate + start trận
                    }));
                    break;

                case EGameState.RESET:
                    StartCoroutine(CRUnloadScene(Define.SceneName.Level(CurrentLevel), () => EnterInGame(CurrentLevel)));
                    break;
            }
        }

        private void LoadingComplete()
        {
            RegisterAndLoadSave();
            EnterHome();
        }

        private void EnterLoading() => SetGameState(EGameState.LOADING);

        public void EnterHome()
        {
            // gọi từ INGAME (nút Home): phải unload level trước
            if (_state == EGameState.INGAME)
            {
                Time.timeScale = 1f;
                UIManager.I.CloseAllInLayer(EUILayer.Popup);
                UIManager.I.Close(Define.UIName.GAMEPLAY_MENU);
                StartCoroutine(CRUnloadScene(Define.SceneName.Level(CurrentLevel), () => SetGameState(EGameState.HOME)));
            }
            else
            {
                SetGameState(EGameState.HOME);
            }
        }

        public void EnterInGame(int level)
        {
            CurrentLevel = level;
            Time.timeScale = 1f;
            SetGameState(EGameState.INGAME);
        }

        /// <summary>Restart level hiện tại (unload + load lại additive).</summary>
        public void EnterReset()
        {
            Time.timeScale = 1f;
            UIManager.I.CloseAllInLayer(EUILayer.Popup);
            UIManager.I.Close(Define.UIName.GAMEPLAY_MENU);
            SetGameState(EGameState.RESET);
        }

        /// <summary>Đi level tiếp theo từ ResultPopup.</summary>
        public void EnterNextLevel()
        {
            var next = CurrentLevel + 1;
            Time.timeScale = 1f;
            UIManager.I.CloseAllInLayer(EUILayer.Popup);
            UIManager.I.Close(Define.UIName.GAMEPLAY_MENU);
            StartCoroutine(CRUnloadScene(Define.SceneName.Level(CurrentLevel), () => EnterInGame(next)));
        }

        public GameConfig GetGameConfig() => _gameConfig;

        private IEnumerator CRLoadScene(string sceneName, Action callback = null)
        {
            yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
            callback?.Invoke();
        }

        private IEnumerator CRUnloadScene(string sceneName, Action callback = null)
        {
            yield return SceneManager.UnloadSceneAsync(sceneName, UnloadSceneOptions.None);
            callback?.Invoke();
        }
    }
}
```

Lưu ý so với ShibaSlide: bỏ state `FIRST` (chỉ phục vụ SDK login/banner ads — Zombie War chưa có); `EnterReset` quản lý `RESET` như ShibaSlide nhưng có thêm đóng UI vì HUD của Zombie War là view. `EnterInGame` nhận tham số level vì Zombie War có nhiều level (ShibaSlide endless chỉ 1 scene Game).

### B4. `GameConfig` ScriptableObject (mới, nhỏ)

`Assets/_Game/Scripts/Core/GameConfig.cs` — gom hằng gameplay chung, tạo asset `Assets/_Game/ScriptableObjects/GameConfig.asset`:

```csharp
using UnityEngine;

namespace ZombieWar
{
    [CreateAssetMenu(menuName = "ZombieWar/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        public float levelDuration = 180f;      // từ GameManager cũ
        public float endSlowMotionScale = 0.4f; // timeScale khi win/lose
    }
}
```

(Chỉ gom 2 hằng flow-level này. KHÔNG di chuyển các `[SerializeField]` tuning trong Player/Weapon/Zombie — đó là đổi data path, rủi ro sai số.)

### B5. `LevelManager` (đổi tên từ GameManager cũ) — nội dung sau refactor

Giữ nguyên toàn bộ logic timer/kills/win-lose/layer-collision, thay đổi đúng các điểm sau:

```csharp
using System;
using NFramework;
using UnityEngine;

namespace ZombieWar.Core
{
    public enum GameState { Playing, Won, Lost }

    /// <summary>Per-level state machine — sống trong Level scene. (Nguyên là GameManager cũ.)</summary>
    public class LevelManager : SingletonMono<LevelManager>
    {
        [SerializeField] private int levelNumber = 1;   // ⚠️ SET TRONG TỪNG SCENE: Level1=1, Level2=2

        public GameState State { get; private set; } = GameState.Playing;
        public float TimeRemaining { get; private set; }
        public float TimeElapsed => LevelDuration - TimeRemaining;
        public int Kills { get; private set; }
        public int JustUnlockedLevel { get; private set; }
        public int LevelNumber => levelNumber;

        private float LevelDuration => GameManager.I.GetGameConfig().levelDuration;

        public event Action<GameState> OnStateChanged;
        public event Action<int> OnKillsChanged;

        private bool _begun;

        protected override void Awake()
        {
            base.Awake();
            TimeRemaining = LevelDuration;
            // GIỮ NGUYÊN block Physics.IgnoreLayerCollision (Player/Zombie/ZombieRagdoll) từ code cũ
            int player = LayerMask.NameToLayer("Player");
            int zombie = LayerMask.NameToLayer("Zombie");
            int ragdoll = LayerMask.NameToLayer("ZombieRagdoll");
            if (ragdoll >= 0)
            {
                if (player >= 0) Physics.IgnoreLayerCollision(player, ragdoll, true);
                if (zombie >= 0) Physics.IgnoreLayerCollision(zombie, ragdoll, true);
            }
        }

        /// <summary>GameManager gọi sau khi scene load + HUD mở. Gate tutorial ở đây.</summary>
        public void Begin()
        {
            if (levelNumber == 1 && !UserData.I.HasSeenTutorial)
            {
                Time.timeScale = 0f;
                UIManager.I.Open<TutorialPopup>(Define.UIName.TUTORIAL_POPUP);
                // TutorialPopup khi bấm GOT IT: UserData.I.HasSeenTutorial = true; Time.timeScale = 1f; CloseSelf(); rồi gọi lại LevelManager.I.Begin()
                return;
            }
            _begun = true;
            Time.timeScale = 1f;
        }

        private void Update()
        {
            if (!_begun || State != GameState.Playing) return;
            TimeRemaining -= Time.deltaTime;           // GIỮ NGUYÊN
            if (TimeRemaining <= 0f) { TimeRemaining = 0f; SetState(GameState.Won); }
        }

        public void RegisterKill()                     // GIỮ NGUYÊN
        {
            Kills++;
            OnKillsChanged?.Invoke(Kills);
        }

        public void PlayerDied()                       // GIỮ NGUYÊN
        {
            if (State == GameState.Playing) SetState(GameState.Lost);
        }

        private void SetState(GameState state)
        {
            State = state;
            if (state == GameState.Won)
            {
                // THAY parse tên scene bằng levelNumber tường minh:
                if (UserData.I.UnlockLevel(levelNumber + 1))
                    JustUnlockedLevel = levelNumber + 1;
            }
            OnStateChanged?.Invoke(state);
            if (state != GameState.Playing)
            {
                Time.timeScale = GameManager.I.GetGameConfig().endSlowMotionScale;   // vẫn 0.4
                UIManager.I.Open<ResultPopup>(Define.UIName.RESULT_POPUP, p => p.Show(state == GameState.Won));
            }
        }
        // XÓA: RestartLevel(), LoadScene() — GameManager.I.EnterReset()/EnterNextLevel()/EnterHome() thay thế
    }
}
```

Diff so với bản cũ: đổi base class; `levelDuration` đọc từ GameConfig; unlock dùng `levelNumber` serialized thay vì parse tên scene; mở ResultPopup qua UIManager thay vì ResultPanel tự bật; thêm gate `Begin()` cho tutorial (thay `TutorialPanel.Start` tự freeze); `Application.targetFrameRate` chuyển lên GameManager. **Mọi công thức/số giữ nguyên.**

### B6. Dựng scene `Main.unity`

Tạo `Assets/_Game/Scenes/Main.unity` với các root GameObject (đối chiếu scene Main của ShibaSlide):

| GameObject | Components / setup |
|---|---|
| `GameManager` | script GameManager mới; kéo `GameConfig.asset` |
| `UIManager` | `Canvas` (**Screen Space – Overlay**), `CanvasScaler` (Scale With Screen Size, reference 1080×1920 hoặc theo UI hiện có), `GraphicRaycaster`, script `NFramework.UIManager`, field `_resourceRootPath` = `UI` |
| `SaveManager` | script `NFramework.SaveManager` (autoSave mặc định 5s) |
| `SoundManager` | script `NFramework.SoundManager`; kéo `nframework/Assets/AudioMixer.mixer` vào `_audioMixer`, group Music/SFX tương ứng vào `_musicMixerGroup`/`_sfxMixerGroup`; `_resourceRootPath` để rỗng |
| `UserData` | script UserData |
| `WorldSoundManager` | (Phase D — tạo trước GameObject cũng được) |
| `VibrationManager` | (tùy chọn) script `NFramework.VibrationManager` |
| `EventSystem` | EventSystem + StandaloneInputModule |
| `Main Camera` | camera trống render UI không cần (Canvas overlay) nhưng giữ 1 camera ở Main để không có frame "no camera" khi chưa load level; culling mask Nothing, clear flags Solid Color đen |

**Build Settings** (`File > Build Settings`): thứ tự mới — `Main` (index 0), `Level1`, `Level2`. **Bỏ `HomeMenu.unity`** (sẽ xóa scene này ở Phase E).

Phase B nghiệm thu: mở scene Main, Play — console hiện `GameState: LOADING` rồi lỗi thiếu prefab LoadingPopup là **đúng dự kiến** (prefab làm ở Phase C). Commit **"Phase B: core skeleton"**.

---

## 7. Phase C — UI

Mục tiêu: mọi màn hình/popup thành prefab `BaseUIView` trong `Assets/_Game/Resources/UI/`. Làm **từng view một**, mỗi view xong chạy thử.

### C0. Tạo `Popup` base (game-level) — port từ ShibaSlide

`Assets/_Game/Scripts/UI/Popup/Popup.cs` — port nguyên logic từ `ShibaSlide/Assets/Game/Scripts/UI/Popup/Popup.cs`:

```csharp
using DG.Tweening;
using NFramework;
using UnityEngine;
using UnityEngine.UI;

namespace ZombieWar.UI
{
    public class Popup : BaseUIView
    {
        public Transform _root;                       // panel giữa để tween scale
        [SerializeField] private Ease ease = Ease.OutBounce;
        [SerializeField] protected Button _closeButton;

        protected virtual void Awake()
        {
            _closeButton?.onClick.AddListener(() =>
            {
                SoundManager.I.PlaySFXResource(Define.SoundName.CLICK_BUTTON);
                CloseSelf();
            });
        }

        public override void OnOpen()
        {
            base.OnOpen();
            SoundManager.I.PlaySFXResource(Define.SoundName.OPEN_POPUP);
            if (_root != null)
            {
                _root.DOKill();
                _root.DOScale(1, 0.5f).From(0.5f).SetEase(ease).SetUpdate(true);
                CanvasGroup.DOFade(1f, 0.5f).From(0).SetEase(Ease.OutCirc).SetUpdate(true);
            }
        }
    }
}
```

> ⚠️ Khác bản gốc một chỗ **cố ý**: thêm `.SetUpdate(true)` (unscaled time) vì popup của Zombie War mở lúc `Time.timeScale = 0` (pause/tutorial) hoặc `0.4` (result) — không có nó tween sẽ đứng hình/chậm.

### C1. Cách chuyển một panel scene → prefab (quy trình chung, lặp cho từng view)

1. Trong scene cũ, tìm Canvas/panel gốc của view. Tạo GameObject rỗng `ViewName` (RectTransform stretch full), move toàn bộ visual con vào (KHÔNG mang theo component Canvas gốc — layer canvas do UIManager cấp).
2. Gắn script mới (kế thừa `BaseUIView` hoặc `Popup`), set inspector: `_uiLayer` (Menu cho HomeMenu/GamePlayMenu, Popup cho còn lại), `_isUnique = true`, `_canDestroy = false`.
3. Kéo thành prefab tại `Assets/_Game/Resources/UI/ViewName.prefab` — **tên file phải trùng const trong `Define.UIName`**.
4. Chuyển logic từ script cũ sang script mới: code trong `Start/OnEnable` → `OnOpen()`; hủy đăng ký event → `OnClose()`; các hàm `OnXxxPressed` giữ nguyên chữ ký để re-wire Button onClick trong prefab.
5. Play scene Main, mở view qua `UIManager.I.Open(...)`, so sánh trực quan với bản cũ.

### C2. Danh sách view cần tạo (theo thứ tự làm)

**① `LoadingPopup`** (`_uiLayer = AlwaysOnTop`) — thay overlay của `SceneLoader`. Port visual overlay + progress fill từ prefab SceneLoader cũ. Script:

```csharp
using System;
using NFramework;
using UnityEngine;
using UnityEngine.UI;

namespace ZombieWar.UI
{
    public class LoadingPopup : BaseUIView
    {
        [SerializeField] private Image progressFill;
        [SerializeField] private float minShowTime = 0.7f;   // giữ số cũ của SceneLoader

        private Action _onComplete;
        private float _startTime;

        public void AssignEvent(Action onComplete)
        {
            _onComplete = onComplete;
        }

        public override void OnOpen()
        {
            base.OnOpen();
            _startTime = Time.unscaledTime;
        }

        private void Update()
        {
            var t = Mathf.Clamp01((Time.unscaledTime - _startTime) / minShowTime);
            SetProgress(t);
            if (t >= 1f && _onComplete != null)
            {
                var cb = _onComplete;
                _onComplete = null;
                cb.Invoke();
                CloseSelf();
            }
        }

        private void SetProgress(float p)
        {
            if (progressFill == null) return;
            var rt = progressFill.rectTransform;   // giữ cách fill bằng anchor của SceneLoader cũ
            rt.anchorMax = new Vector2(Mathf.Lerp(0.03f, 1f, p), 1f);
        }
    }
}
```

(Trường hợp `AssignEvent(null)` khi vào INGAME: LoadingPopup chỉ hiển thị đè trong lúc `CRLoadScene` chạy, GameManager chủ động `Close` khi load xong — xem code GameManager B3.)

**② `HomeMenu : BaseUIView`** (`_uiLayer = Menu`) — port từ `MenuController` + scene HomeMenu:
- Visual: BG, title, PlayButton, SettingsButton, ExitButton (port từ scene HomeMenu cũ).
- `OnPlayPressed` → `PlaySfx click` + `UIManager.I.Open(Define.UIName.LEVEL_SELECT_POPUP)`.
- `OnSettingsPressed` → `UIManager.I.Open(Define.UIName.SETTINGS_POPUP)`.
- `OnExitPressed` → giữ nguyên `Application.Quit()` / editor stop.
- Click sound: thay `clickSource.PlayOneShot(clickClip, GameSettings.SfxVolume)` bằng `SoundManager.I.PlaySFXResource(Define.SoundName.CLICK_BUTTON)` — **xóa được hàm `Click()` duplicate ở 3 file**.
- Menu music (AudioSource `MenuMusic` trong scene cũ): xóa — GameManager đã `PlayMusicResource(Define.SoundBG.BGM_MAIN)` khi vào HOME.

**③ `LevelSelectPopup : Popup`** — port từ `levelSelectPopup` panel + logic `RefreshLevelLocks` của MenuController:
- `OnOpen()`: refresh lock (`UserData.I.IsLevelUnlocked(2)`) — giữ nguyên các `Color` dim hiện tại.
- Nút Level N → `GameManager.I.EnterInGame(n)` + `CloseSelf()` + `UIManager.I.Close(Define.UIName.HOME_MENU)`. Giữ guard `if (!UserData.I.IsLevelUnlocked(2)) return;`.

**④ `SettingsPopup : Popup`** — port từ `SettingsPanel`:
- `OnOpen()`: `musicSlider.SetValueWithoutNotify(SoundManager.I.MusicVolume)`; tương tự SFX.
- `OnMusicChanged(v)` → `SoundManager.I.MusicVolume = v;` (tự persist, tự ăn vào mixer — không cần `RefreshMusicVolume` hay `menuMusic.volume` nữa). `OnSfxChanged(v)` → `SoundManager.I.SFXVolume = v;` + preview sfx throttle giữ nguyên.

**⑤ `GamePlayMenu : BaseUIView`** (`_uiLayer = Menu`) — port từ HUD Canvas trong Level scene, gồm: health bar, timer, kills, ammo, weapon label, bomb count, DamageFlashUI, nút Pause, nút Switch weapon, nút Bomb, **và Fixed Joystick** (move joystick vào prefab này).
- Port logic `HUDController` vào: subscribe events trong `OnOpen`, unsubscribe trong `OnClose`.
- ⚠️ Không thể serialize ref tới `PlayerHealth/WeaponController/BombThrower` (khác scene). Giải pháp chuẩn: **LevelContext** — xem [§10.2](#102-cross-scene-references). `OnOpen` lấy ref qua `LevelManager.I` (được bảo đảm tồn tại vì GameManager chỉ mở GamePlayMenu sau khi level load xong).
- Expose joystick: `public Joystick Joystick => _joystick;` cho PlayerController resolve (xem §10.2).
- Nút Pause → `UIManager.I.Open(Define.UIName.PAUSE_POPUP)`.

**⑥ `PausePopup : Popup`** — port từ `PauseMenu`:

```csharp
using NFramework;
using UnityEngine;
using ZombieWar.Core;

namespace ZombieWar.UI
{
    public class PausePopup : Popup
    {
        private float _previousTimeScale = 1f;

        public override void OnOpen()
        {
            base.OnOpen();
            _previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;                        // giữ nguyên hành vi cũ
        }

        public override void OnClose()
        {
            base.OnClose();
            Time.timeScale = _previousTimeScale;        // Resume = CloseSelf
        }

        public void OnResumePressed() => CloseSelf();

        public void OnRestartPressed()
        {
            CloseSelf();
            GameManager.I.EnterReset();
        }

        public void OnHomePressed()
        {
            CloseSelf();
            GameManager.I.EnterHome();
        }
    }
}
```

Giữ guard cũ "không pause sau khi win/lose": nút Pause trên GamePlayMenu check `if (LevelManager.I.State != GameState.Playing) return;` trước khi Open.

**⑦ `ResultPopup : Popup`** — port từ `ResultPanel.Show(bool won)` giữ nguyên: title VICTORY/YOU DIED + màu, stats kills (đọc `LevelManager.I.Kills`), unlock text (đọc `LevelManager.I.JustUnlockedLevel`), logic ẩn/hiện + re-center nút (hasNext = `won && LevelManager.I.LevelNumber < UserData.MaxLevel`), win/lose stinger → `SoundManager.I.PlaySFXResource(Define.SoundName.WIN / LOSE)`. Nút: Restart → `GameManager.I.EnterReset()`; Next → `GameManager.I.EnterNextLevel()`; Home → `GameManager.I.EnterHome()`. (Nhớ `CloseSelf()` trước khi gọi — hoặc để GameManager `CloseAllInLayer(Popup)` lo, như code B3 đã làm.)

**⑧ `TutorialPopup : Popup`** — port từ `TutorialPanel`: nút GOT IT → `UserData.I.HasSeenTutorial = true;` + `Time.timeScale = 1f;` + `CloseSelf();` + `LevelManager.I.Begin();`. Việc "chỉ hiện Level 1 lần đầu" đã chuyển vào `LevelManager.Begin()` (B5) — popup không tự quyết định nữa.

Phase C nghiệm thu: từ scene Main chạy được flow đầy đủ bằng UI mới (miễn Phase D/E xong phần scene). Thực tế C và D/E đan nhau — cứ làm C xong prefab + script, việc nối dây cuối cùng nằm ở Phase E. Commit **"Phase C: UI views"** (có thể nhiều commit nhỏ theo từng view).

---

## 8. Phase D — Thay singleton cũ

### D1. `WorldSoundManager` (thay `AudioManager`) — GIỮ 3D positional

Tạo `Assets/_Game/Scripts/Manager/WorldSoundManager.cs`: **copy nguyên ruột `AudioManager.cs`**, chỉ đổi:

```csharp
public class WorldSoundManager : SingletonMono<WorldSoundManager>
{
    [SerializeField] private AudioMixerGroup _sfxMixerGroup;   // MỚI: kéo group SFX của nframework AudioMixer

    protected override void Awake()
    {
        base.Awake();
        // ... giữ nguyên vòng tạo 12 AudioSource, THÊM 1 dòng cho mỗi source:
        sources[i].outputAudioMixerGroup = _sfxMixerGroup;
    }

    public void PlaySfx(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f,
        bool spatial = true, float minDistance = 1.5f)
    {
        // giữ nguyên toàn bộ, CHỈ đổi dòng cuối:
        src.PlayOneShot(clip, volume);   // bỏ "* GameSettings.SfxVolume" — mixer đã nhân volume
    }
    // PlaySfxRandomPitch giữ nguyên. XÓA: musicSource + RefreshMusicVolume (music do SoundManager lo).
}
```

Đặt GameObject `WorldSoundManager` vào scene `Main`. **Tại sao tách đôi**: NFramework SoundManager phát SFX 2D tại chỗ (AudioSource nằm trên manager, không có position) — nếu ép tiếng súng/zombie qua đó sẽ **mất âm thanh 3D định hướng**, tức đổi trải nghiệm gameplay. Route AudioSource 3D qua mixer group SFX là cách đúng: volume slider của SoundManager vẫn điều khiển được chúng (mixer nhân toàn cục), mà positional giữ nguyên.

### D2. Sửa call-sites

Grep từng pattern và thay:

| Grep pattern | Thay bằng |
|---|---|
| `AudioManager.Instance.PlaySfx` / `.PlaySfxRandomPitch` | `WorldSoundManager.I.PlaySfx` / `.PlaySfxRandomPitch` (bỏ null-guard được vì manager chắc chắn sống trong Main; hoặc giữ `if (WorldSoundManager.I != null)` nếu muốn an toàn khi test scene lẻ) |
| `AudioManager.Instance.RefreshMusicVolume` | xóa (SettingsPopup set thẳng `SoundManager.I.MusicVolume`) |
| `GameSettings.SfxVolume` (nhân volume khi PlayOneShot) | xóa phép nhân (mixer lo) |
| `GameSettings.MusicVolume` | `SoundManager.I.MusicVolume` |
| `GameSettings.IsLevelUnlocked` / `.UnlockLevel` / `.HighestUnlockedLevel` / `.MaxLevel` | `UserData.I.` tương ứng / `UserData.MaxLevel` |
| `GameSettings.HasSeenTutorial` | `UserData.I.HasSeenTutorial` |
| `SceneLoader.Load(...)` / `SceneLoader.Instance` | `GameManager.I.EnterInGame/EnterReset/EnterHome/EnterNextLevel` tùy ngữ cảnh (đã xử lý hết trong các popup Phase C) |
| `GameManager.Instance` (nếu còn sót sau rename B3) | `LevelManager.I` (gameplay) — riêng `RegisterKill`, `PlayerDied`, `TimeElapsed`, `Kills`, `State` đều thuộc LevelManager |
| `CameraShake.Instance` | `CameraShake.I` (sau khi đổi base class thành `SingletonMono<CameraShake>` — xóa property `Instance` tự viết) |

Sau khi hết reference: **xóa file** `GameSettings.cs`, `AudioManager.cs`, `SceneLoader.cs` (+ prefab SceneLoader trong các scene). Sửa `Editor/ProgressTools.cs`: menu Reset gọi `NFramework.SaveManager.DeleteSave()` (hoặc giữ menu riêng thao tác UserData). Compile 0 error. Commit **"Phase D: managers swapped"**.

---

## 9. Phase E — Additive flow

### E1. Dọn Level1.unity / Level2.unity

Với từng level scene:
1. **Xóa**: HUD Canvas (đã thành GamePlayMenu prefab), PauseMenu, ResultPanel, TutorialPanel (Level1), SceneLoader instance, AudioManager, GameManager cũ (object), **EventSystem** (chỉ Main được có), Fixed Joystick (đã vào GamePlayMenu prefab).
2. **Thêm**: GameObject `LevelManager` gắn script LevelManager, **set `levelNumber` = 1 hoặc 2 đúng theo scene**, kéo các ref context (xem §10.2): Player, PlayerHealth, WeaponController, BombThrower.
3. **Giữ**: environment, Player, ZombieSpawner, PickupSpawner, camera gameplay + Cinemachine, Directional Light, CameraShake.

### E2. Xóa scene HomeMenu.unity

Toàn bộ chức năng đã nằm trong HomeMenu/LevelSelectPopup/SettingsPopup prefab. Xóa scene khỏi Build Settings và khỏi project.

### E3. Kiểm tra flow end-to-end

Chạy từ scene Main: LOADING → HOME → chọn level → INGAME (Level load additive, active scene = Level) → chơi → thắng/thua → ResultPopup → Restart (RESET unload/load lại) / Next / Home (unload level, về HOME). Commit **"Phase E: additive scene flow"**.

---

## 10. Những bẫy đã biết

### 10.1 Audio 3D

**Không** chuyển tiếng súng/zombie/pickup sang `SoundManager.PlaySFX` — mất positional 3D (xem D1). Quy tắc: **âm thanh UI + music = NFramework SoundManager; âm thanh world = WorldSoundManager**. Cả hai cùng route qua một AudioMixer nên slider volume vẫn thống nhất.

### 10.2 Cross-scene references

Prefab (GamePlayMenu trong Resources) **không thể** serialize reference tới object trong Level scene, và ngược lại PlayerController (Level scene) không thể serialize ref tới joystick (giờ nằm trong prefab UI). Giải pháp:

- **LevelManager = context của trận**: thêm serialized fields `public PlayerHealth PlayerHealth; public WeaponController Weapon; public BombThrower BombThrower; public Transform Player;` kéo trong Level scene. GamePlayMenu `OnOpen()` đọc `LevelManager.I.*` để subscribe events (an toàn vì GameManager mở GamePlayMenu **sau khi** scene load xong — LevelManager.Awake đã chạy).
- **Joystick chiều ngược lại**: PlayerController thay `[SerializeField] Joystick joystick` bằng resolve lười:

```csharp
private Joystick _joystick;
private Joystick Joystick
{
    get
    {
        if (_joystick == null)
        {
            var hud = UIManager.I.GetOpenedView<GamePlayMenu>(Define.UIName.GAMEPLAY_MENU);
            if (hud != null) _joystick = hud.Joystick;
        }
        return _joystick;
    }
}
// trong Update: if (Joystick == null) return;  → player đứng yên 1-2 frame đầu trước khi HUD mở, chấp nhận được
```

### 10.3 Time.timeScale

Zombie War thao túng timeScale ở 4 chỗ: pause (0), tutorial (0), end slow-mo (0.4), load scene (reset 1). Sau refactor, quyền reset về 1 thuộc về **GameManager** (mỗi `Enter*` đều set `Time.timeScale = 1f` — đã có trong code B3). Popup nào chạy tween lúc timeScale ≤ 0.4 phải dùng `.SetUpdate(true)` (đã đưa vào Popup base C0). LoadingPopup dùng `Time.unscaledTime`.

### 10.4 `ZombieHealth.AliveCount` static

Static counter này được reset trong `ZombieSpawner.Awake()` (`ResetAliveCount()`). Với additive load/unload, mỗi lần load lại Level scene thì `ZombieSpawner.Awake` chạy lại → **vẫn được reset đúng**. Nhưng kiểm tra kỹ: thứ tự Home→InGame→Home→InGame nhiều lần, count không được âm/lệch. Nếu thấy lệch, chuyển reset vào `LevelManager.Awake()` cho chắc chắn (chạy trước spawner nhờ Script Execution Order, hoặc gọi trong `Begin()`).

### 10.5 `FindGameObjectWithTag("Player")`

ZombieAI/ZombieSpawner/PickupSpawner tìm Player theo tag lúc Awake/spawn. Với additive, Player và các script này cùng scene Level nên **vẫn hoạt động**. Chú ý duy nhất: zombie pooled (`ObjectPool`) sống lại qua RESET — pool object nằm trong Level scene (pool tự tạo GameObject `Pool_...` trong active scene) nên unload level là pool chết theo, sạch sẽ. Không làm gì thêm.

### 10.6 KHÔNG chuyển ObjectPool sang NFramework Pool trong đợt này

`ObjectPool.Spawn/Release` static của Zombie War đang được Bullet, ZombieSpawner, VFX, Pickup dùng với semantics riêng (`IPoolable.OnSpawned/OnDespawned`, auto-create pool theo prefab). NFramework `Pool` yêu cầu prefab có sẵn component `PooledObject` + pool khai báo trước trong FactoryManager. Chuyển đổi = sửa hàng loạt prefab + timing spawn → rủi ro gameplay cao, lợi ích thấp (pool cũ chạy tốt). **Quyết định: giữ `ObjectPool` cũ.** Ghi vào backlog: nếu sau này cần FX mới thì dùng FactoryManager cho cái mới, hai hệ sống chung được.

### 10.7 EventSystem & Camera

- Chỉ **một** EventSystem — ở Main. Xóa khỏi mọi Level scene (2 EventSystem = warning + input lỗi).
- Canvas UIManager để **Screen Space – Overlay** → không phụ thuộc camera nào, không lo camera level load sau/unload trước.
- Camera gameplay (+ Cinemachine + CameraShake) ở Level scene — đúng chỗ, vì nó chỉ có nghĩa trong trận. Main giữ 1 camera "nền đen" culling Nothing để không có frame trống khi ở HOME (hoặc thêm background UI ở layer Background của UIManager).

### 10.8 Save đè PlayerPrefs cũ

Không migrate (đã chốt): người chơi cũ mất unlock Level 2 + volume. Key PlayerPrefs cũ (`MusicVolume`, `SFXVolume`, `HighestUnlockedLevel`, `HasSeenTutorial`) bỏ rơi, không cần xóa.

### 10.9 Thứ tự Awake trong Main

`SingletonMono.I` gán trong Awake — mọi truy cập chéo manager (`SaveManager.I`, `UIManager.I`...) phải diễn ra từ `Start()` trở đi (GameManager mới đã tuân thủ: `Start()` → `EnterLoading()`). Không gọi `Xxx.I` trong `Awake` của manager khác.

### 10.10 Không dùng Odin/UniTask trong code mới

ShibaSlide có Odin Inspector (`[Button]`, `[FoldoutGroup]`) và UniTask — Zombie War **không có** 2 package này và không cần. Khi port code mẫu, thay `[Button]` bằng `[NFramework.ButtonMethod]` hoặc `[ContextMenu]`, thay async UniTask bằng coroutine.

---

## 11. Checklist nghiệm thu

Chạy từ scene `Main`, tick từng dòng — **mọi hành vi phải giống bản trước refactor**:

- [ ] Boot: `GameState: LOADING` → loading bar chạy hết `minShowTime` → `GameState: HOME`, BGM main phát, HomeMenu hiện.
- [ ] Play → LevelSelectPopup mở; Level 2 khóa (dim + chain icon) khi chưa unlock; bấm vào không vào được.
- [ ] Settings (Home): kéo Music/SFX slider — nhạc đổi ngay, preview SFX kêu; **tắt app mở lại volume còn nguyên** (SaveManager persist).
- [ ] Vào Level 1 lần đầu: TutorialPopup hiện, game đứng hình; GOT IT → chạy; **restart level không hiện lại tutorial**; xóa save (`NFramework > Delete Save`) thì hiện lại.
- [ ] Gameplay Level 1: joystick di chuyển, auto-aim bắn, reload, switch weapon, bomb, pickup, zombie spawn theo wave, kills đếm trên HUD, timer đếm ngược từ 180 — **so sánh trực tiếp với build/commit cũ, mọi con số giống nhau**.
- [ ] Âm thanh: tiếng súng/zombie vẫn 3D (đứng xa nghe nhỏ, lệch trái phải); SFX slider chỉnh được cả tiếng súng lẫn tiếng UI.
- [ ] Pause: game đứng hình (timeScale 0), Resume trả về đúng; **không pause được sau khi win/lose**.
- [ ] Pause → Restart: level load lại từ đầu (RESET, unload+load additive), timer/kills/zombie reset, không double-spawn, `AliveCount` đúng.
- [ ] Pause → Home: về HomeMenu, vào lại level chạy bình thường.
- [ ] Thắng (sống hết 180s): slow-mo 0.4, ResultPopup VICTORY + kills + "LEVEL 2 UNLOCKED!" (chỉ lần đầu), nút Next hiện; thắng Level 2: không có Next, Restart tự căn giữa.
- [ ] Next Level: sang Level 2 sạch sẽ.
- [ ] Thua (chết): ResultPopup YOU DIED, không unlock text, Restart/Home hoạt động.
- [ ] Unlock persist: thắng Level 1 → thoát app → mở lại → Level 2 vẫn mở.
- [ ] Lặp Home↔Level 5+ lần liên tục: không leak error console, không duplicate-singleton error, memory không phình bất thường.
- [ ] Android back key: đóng popup trên cùng (HandleOnKeyBack mặc định của view — nếu muốn hành vi này, override `HandleOnKeyBack() => CloseSelf();` trong Popup base).
- [ ] Console 0 error, 0 warning mới trong toàn bộ flow trên.

---

## 12. Template hóa

Sau khi Zombie War xong, phần tái dùng cho **mọi game sau** gồm:

1. **Folder `Assets/ThirdParty/nframework`** (bản đã sạch PlayAd ở Phase A — lấy từ Zombie War, không lấy lại từ ShibaSlide).
2. **Skeleton scene `Main.unity`**: GameManager + UIManager + SaveManager + SoundManager + UserData + EventSystem + camera nền.
3. **Skeleton code**: `Define.cs` (UIName/SceneName/SoundName rỗng), `GameManager` FSM (`NONE, LOADING, HOME, INGAME, RESET`), `UserData : ISaveable`, `Popup : BaseUIView` base, `LoadingPopup`.
4. **Quy trình 8 bước cho game mới**:
   1. Tạo project → import nframework + Newtonsoft → tạo Main.unity theo skeleton.
   2. Khai `Define.UIName/SceneName/SoundName` cho game.
   3. Data người chơi → `UserData` + các manager `ISaveable`, register trong `GameManager.RegisterAndLoadSave`.
   4. Mỗi màn hình/popup = 1 prefab `BaseUIView/Popup` trong `Resources/UI`.
   5. Gameplay 1 scene riêng load additive; state trong trận = 1 manager riêng (LevelManager/GameplayManager) sống trong scene đó.
   6. Config số liệu = ScriptableObject; manager phát static event khi data đổi; UI subscribe trong OnOpen/unsubscribe trong OnClose.
   7. Sound UI/music qua SoundManager + mixer; sound 3D (nếu có) qua manager riêng route cùng mixer.
   8. Ads/IAP/Firebase khi cần: bật define `USE_*` tương ứng + import SDK, module nframework có sẵn adapter.

---

*Tài liệu sinh bởi Claude (đối chiếu source ShibaSlide + Zombie War ngày 2026-07-14, flow đã verify bằng Play Mode trong Unity Editor).*
