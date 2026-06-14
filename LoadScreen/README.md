# LoadScreen

`LoadScreen` is a global Unity loading screen coordinator. It displays a persistent loading UI, tracks multiple independent loading tasks, aggregates Addressables progress, rotates loading visuals, fades background images, and shows localized gameplay tips.

The system is used during boot, scene transitions, player spawn, NPC loading, tutorial validation, and other long-running operations where several subsystems may need to keep the loading screen open at the same time.

## Core Idea

The loading screen works like a reference-counted session manager.

Every subsystem that needs the loading screen calls:

```csharp
Guid taskId = await LoadScreenController.ShowAsync();
```

When that subsystem finishes its work, it must close only its own task:

```csharp
LoadScreenController.Hide(ref taskId);
```

The screen remains visible while at least one task is still active. This prevents one subsystem from accidentally hiding the loading screen while another subsystem is still loading.

## Main Components

| File | Responsibility |
|---|---|
| `LoadScreenController.cs` | Static public API, prefab loading, task ownership, Addressables progress aggregation, unload flow. |
| `LoadScreenMenu.cs` | Runtime UI behaviour: task list, slider smoothing, background images, tips, fade-out, loading icon rotation. |
| `LoadScreenAddressablePath.cs` | ScriptableObject config that maps scenes to loading background sprite references. |
| `LoadScreenTips.cs` | ScriptableObject config that stores localized loading tips by category. |

## Runtime Flow

1. A caller requests the loading screen through `LoadScreenController.ShowAsync()`.
2. The controller loads `Prefabs_LoadScreen` from Addressables if no instance exists.
3. The prefab is instantiated and marked as `DontDestroyOnLoad`.
4. `LoadScreenMenu.IncreaseLoadScreenTask()` creates a new `Guid` task id.
5. If this is the first task of a new session, the controller resets progress state and slider state.
6. The menu starts:
   - loading icon rotation;
   - slider update loop;
   - background image loading;
   - random tip display;
   - background fade animation.
7. Addressables handles can be registered through `UpdateLoadSliderView(handle)`.
8. When each caller finishes, it calls `Hide(ref taskId)`.
9. The screen starts closing only when:
   - all loading tasks are removed;
   - all tracked Addressables handles are complete;
   - the displayed slider reaches `100%`.
10. The menu fades out and the controller releases the loading screen prefab handle.

## Task Ownership

Each call to `ShowAsync()` returns a unique `Guid`.

This id is the caller's ownership token:

```csharp
private Guid _loadSceneTaskID;

private async UniTask LoadSomething()
{
	_loadSceneTaskID = await LoadScreenController.ShowAsync();

	await DoWork();

	LoadScreenController.Hide(ref _loadSceneTaskID);
}
```

`Hide(ref Guid taskID)` clears the id by reference after removing the task. This prevents accidental double-hide calls from removing work that no longer belongs to the caller.

## Addressables Progress

Addressables operations can be connected to the loading slider:

```csharp
var handle = Addressables.LoadAssetAsync<GameObject>(assetReference);
LoadScreenController.UpdateLoadSliderView(handle).Forget();
await handle.ToUniTask();
```

The controller tracks each handle as a `ProgressEntry`:

- current progress;
- downloaded bytes;
- total bytes;
- whether byte data is available;
- completion state.

The displayed aggregate progress is calculated as:

```text
sum(handleProgress) / handleCount
```

If byte information exists, the status text displays downloaded megabytes:

```text
12.45 / 30.00 MB
```

If byte information is unavailable, the slider still uses `AsyncOperationHandle.PercentComplete`.

## Slider Behaviour

The slider is intentionally smoothed.

While loading is still active, the displayed value moves toward the aggregate progress but is capped by `_softCeiling`, usually below `100%`.

Only after all tasks and tracked handles are complete can the slider reach `100%`.

This avoids UI states where the progress bar visually finishes before the actual loading work is done.

## Background Images

Loading backgrounds are configured in `LoadScreenAddressablePath`.

The config maps scene types to Addressable sprite references:

```text
LocationSceneType
  -> List<AssetReference background sprites>
```

When the loading screen opens, it:

1. waits for `ProjectContext`;
2. waits for `LoadScreenAddressablePath`;
3. gets the current scene;
4. loads all configured background sprites for that scene;
5. picks a random sprite;
6. periodically fades to another random sprite.

## Tips

Loading tips are configured in `LoadScreenTips`.

Tips are grouped by `TipType`:

- `Heroes`
- `PVE`
- `PVP`
- `Functions`
- `Narrative`

When `TipType.Any` is requested, the system picks a random concrete category and then a random localized tip from that category.

Tip text comes from `APILanguageData.GetTextByLanguage`.

## Integration Examples

Boot loading:

```csharp
_loadManagersID = await LoadScreenController.ShowAsync();
// load managers, initialize services, wait for local database
LoadScreenController.Hide(ref _loadManagersID);
```

Scene loading:

```csharp
_loadSceneTaskID = await LoadScreenController.ShowAsync();

_currentSceneHandle = Addressables.LoadSceneAsync(sceneReference);
LoadScreenController.UpdateLoadSliderView(_currentSceneHandle).Forget();

await _currentSceneHandle.Value.ToUniTask();

LoadScreenController.Hide(ref _loadSceneTaskID);
```

Force reset:

```csharp
LoadScreenController.ForceReset();
```

Use `ForceReset()` only as a recovery path when the screen must be cleared regardless of active tasks.

## What This System Does Well

- Supports several simultaneous loading owners.
- Keeps the screen visible until every owner releases its task.
- Aggregates progress from multiple Addressables handles.
- Shows download size when byte data is available.
- Smooths progress bar movement.
- Prevents the slider from reaching `100%` too early.
- Supports scene-specific loading backgrounds.
- Supports localized random tips.
- Logs task callers in the Unity Editor through caller info.
- Keeps loading UI alive across scene transitions with `DontDestroyOnLoad`.

## Performance Notes

The implementation is suitable for boot loading, scene transitions, and normal Addressables-driven loading flows.

Runtime work during loading includes:

- one per-frame slider update loop;
- one per-frame loading icon rotation coroutine;
- Addressables progress polling for registered handles;
- occasional background fade animation;
- occasional random background/tip selection.

---

# LoadScreen RU

`LoadScreen` - это глобальный координатор экрана загрузки в Unity. Он показывает persistent loading UI, отслеживает несколько независимых задач загрузки, агрегирует прогресс Addressables, крутит loading-иконку, плавно меняет фоновые изображения и показывает локализованные игровые подсказки.

Система используется во время boot-загрузки, смены сцен, spawn игрока, загрузки NPC, tutorial validation и других долгих операций, где несколько подсистем могут одновременно держать экран загрузки открытым.

## Основная идея

Loading screen работает как reference-counted session manager.

Каждая подсистема, которой нужен экран загрузки, вызывает:

```csharp
Guid taskId = await LoadScreenController.ShowAsync();
```

Когда эта подсистема завершила свою работу, она закрывает только свою задачу:

```csharp
LoadScreenController.Hide(ref taskId);
```

Экран остается видимым, пока активна хотя бы одна задача. Это не дает одной подсистеме случайно скрыть loading screen, пока другая подсистема еще грузится.

## Основные компоненты

| File | Responsibility |
|---|---|
| `LoadScreenController.cs` | Статический публичный API, загрузка prefab, владение задачами, агрегация Addressables progress, unload flow. |
| `LoadScreenMenu.cs` | Runtime UI behaviour: список задач, сглаживание slider, фоновые изображения, tips, fade-out, вращение loading icon. |
| `LoadScreenAddressablePath.cs` | ScriptableObject config, который связывает сцены с Addressable sprite references для loading backgrounds. |
| `LoadScreenTips.cs` | ScriptableObject config, который хранит локализованные loading tips по категориям. |

## Runtime Flow

1. Вызывающий код запрашивает экран через `LoadScreenController.ShowAsync()`.
2. Controller загружает `Prefabs_LoadScreen` из Addressables, если instance еще нет.
3. Prefab создается и помечается как `DontDestroyOnLoad`.
4. `LoadScreenMenu.IncreaseLoadScreenTask()` создает новый `Guid` задачи.
5. Если это первая задача новой loading-сессии, controller сбрасывает progress state и slider state.
6. Menu запускает:
   - вращение loading icon;
   - update loop для slider;
   - загрузку background images;
   - показ random tips;
   - fade animation для фона.
7. Addressables handles можно зарегистрировать через `UpdateLoadSliderView(handle)`.
8. Когда каждый caller завершает работу, он вызывает `Hide(ref taskId)`.
9. Экран начинает закрываться только когда:
   - все loading tasks удалены;
   - все отслеживаемые Addressables handles завершены;
   - отображаемый slider дошел до `100%`.
10. Menu плавно гасит экран, а controller release-ит handle prefab loading screen.

## Владение задачами

Каждый вызов `ShowAsync()` возвращает уникальный `Guid`.

Этот id является токеном владения caller-а:

```csharp
private Guid _loadSceneTaskID;

private async UniTask LoadSomething()
{
	_loadSceneTaskID = await LoadScreenController.ShowAsync();

	await DoWork();

	LoadScreenController.Hide(ref _loadSceneTaskID);
}
```

`Hide(ref Guid taskID)` после удаления задачи очищает id по ссылке. Это защищает от случайного повторного `Hide` тем же caller-ом.

## Addressables Progress

Addressables operations можно подключать к loading slider:

```csharp
var handle = Addressables.LoadAssetAsync<GameObject>(assetReference);
LoadScreenController.UpdateLoadSliderView(handle).Forget();
await handle.ToUniTask();
```

Controller отслеживает каждый handle как `ProgressEntry`:

- текущий progress;
- downloaded bytes;
- total bytes;
- доступна ли byte-информация;
- завершен ли handle.

Отображаемый aggregate progress считается так:

```text
sum(handleProgress) / handleCount
```

Если byte-информация доступна, status text показывает скачанные мегабайты:

```text
12.45 / 30.00 MB
```

Если byte-информации нет, slider все равно использует `AsyncOperationHandle.PercentComplete`.

## Поведение Slider

Slider намеренно сглаживается.

Пока загрузка активна, отображаемое значение движется к aggregate progress, но ограничивается `_softCeiling`, обычно ниже `100%`.

Только после завершения всех задач и всех tracked handles slider может дойти до `100%`.

Так UI не показывает завершенный progress bar раньше, чем реальная загрузка закончилась.

## Background Images

Loading backgrounds настраиваются в `LoadScreenAddressablePath`.

Config связывает scene types с Addressable sprite references:

```text
LocationSceneType
  -> List<AssetReference background sprites>
```

Когда loading screen открывается, он:

1. ждет `ProjectContext`;
2. ждет `LoadScreenAddressablePath`;
3. определяет текущую сцену;
4. загружает все background sprites для этой сцены;
5. выбирает случайный sprite;
6. периодически плавно переключается на другой случайный sprite.

## Tips

Loading tips настраиваются в `LoadScreenTips`.

Tips сгруппированы по `TipType`:

- `Heroes`
- `PVE`
- `PVP`
- `Functions`
- `Narrative`

Когда запрошен `TipType.Any`, система выбирает случайную конкретную категорию, а затем случайный локализованный tip из этой категории.

Текст подсказки берется из `APILanguageData.GetTextByLanguage`.

## Примеры интеграции

Boot loading:

```csharp
_loadManagersID = await LoadScreenController.ShowAsync();
// load managers, initialize services, wait for local database
LoadScreenController.Hide(ref _loadManagersID);
```

Scene loading:

```csharp
_loadSceneTaskID = await LoadScreenController.ShowAsync();

_currentSceneHandle = Addressables.LoadSceneAsync(sceneReference);
LoadScreenController.UpdateLoadSliderView(_currentSceneHandle).Forget();

await _currentSceneHandle.Value.ToUniTask();

LoadScreenController.Hide(ref _loadSceneTaskID);
```

Force reset:

```csharp
LoadScreenController.ForceReset();
```

`ForceReset()` стоит использовать только как recovery path, когда экран нужно очистить независимо от активных задач.

## Что система делает хорошо

- Поддерживает несколько одновременных владельцев loading screen.
- Держит экран открытым, пока каждый владелец не освободит свою задачу.
- Агрегирует progress нескольких Addressables handles.
- Показывает размер загрузки, если доступна byte-информация.
- Сглаживает движение progress bar.
- Не дает slider дойти до `100%` слишком рано.
- Поддерживает scene-specific loading backgrounds.
- Поддерживает локализованные random tips.
- В Unity Editor логирует callers через caller info.
- Сохраняет loading UI между сценами через `DontDestroyOnLoad`.

## Производительность

Реализация подходит для boot loading, scene transitions и обычных Addressables-driven loading flows.

Во время загрузки runtime-работа включает:

- один per-frame update loop для slider;
- одну coroutine для вращения loading icon;
- polling Addressables progress для зарегистрированных handles;
- периодический background fade;
- периодический random background/tip selection.