# ObstacleGenerator

`ObstacleGenerator` is a Unity Editor tool for creating and preparing battle obstacles for hex-grid combat scenes. It supports obstacle prefab creation, obstacle view setup, visual placement on a hex grid, and JSON preset export for runtime battle loading.

The tool is editor-only and is wrapped in `#if UNITY_EDITOR`.

## Purpose

`ObstacleGenerator` connects Unity content authoring with the battle obstacle runtime format.

It helps designers and developers:

- create new `BattleObstacle` objects from a base prefab;
- create matching visual views for existing obstacles;
- place obstacles directly on a hex battlefield;
- snap obstacle positions and rotations to the grid;
- visualize occupied and blocked cells;
- save obstacle layouts as JSON presets;
- load existing presets back into the editor;
- use Addressables addresses that match runtime loading.

## Main Components

| File | Responsibility |
|---|---|
| `ObstacleGenerator.cs` | Main `ExecuteAlways` component that switches between generator tools. |
| `ObstacleGeneratorEditorSettings.cs` | Shared editor references: base obstacle prefab, base view prefab, hex tile, grid renderer, camera. |
| `ObstacleGenerator_BaseTool.cs` | Base class for all generator modes. |
| `ObstacleGenerator_CreateNewObstacle.cs` | Tool mode for spawning a new obstacle from the base prefab. |
| `ObstacleGenerator_CreateNewView.cs` | Tool mode for creating a visual view for an existing obstacle prefab. |
| `ObstacleGenerator_CreateObstaclePreset.cs` | Tool mode for building, loading, and saving obstacle presets on a hex grid. |
| `ObstaclePaletteWindow.cs` | Odin EditorWindow that displays draggable obstacle prefabs in a visual palette. |
| `ToolBattleSceneDTO.cs` | DTO classes used to read available battle locations from server data. |

## Tool Modes

`ObstacleGenerator` exposes three modes:

```csharp
public enum ObstacleGeneratorTool
{
	CreateNewObstacle,
	CreateNewView,
	CreateObstaclePreset,
}
```

The main component tracks the active mode in editor update. When the selected tool changes, the previous tool is disabled and the new one is enabled.

## Create New Obstacle

`CreateNewObstacle` is used to spawn a new obstacle from the configured base obstacle prefab.

The spawned obstacle follows the position of the `ObstacleGenerator` object in the scene. This gives a quick setup workflow for creating and validating a new obstacle prefab.

Typical use:

1. Select `CreateNewObstacle`.
2. Click `SpawnNewObstacle`.
3. Configure the spawned `BattleObstacle`.
4. Validate or adjust its bound points.
5. Save the result as a prefab.

## Create New View

`CreateNewView` is used to create a visual representation for an existing obstacle.

The tool lets the user select an obstacle prefab from:

```text
Assets/Core/Prefabs/Game/Battle/Obstacles/ObstacleBlockers
```

After spawning the obstacle for view creation, the tool creates a base `BattleObstacleView` and shows a temporary hex footprint based on the obstacle's `BoundsPoints`.

This makes it easier to align the visual model with the obstacle's gameplay footprint.

Typical use:

1. Select `CreateNewView`.
2. Choose an existing obstacle prefab.
3. Click `SpawnObstacleToCreateVisual`.
4. Adjust or replace the generated visual view.
5. Save the view prefab.

## Create Obstacle Preset

`CreateObstaclePreset` is the main layout authoring mode.

When enabled, it creates a temporary hex grid in the scene and switches to the preset editing camera. The grid uses separate materials for:

- available cells;
- closed cells;
- cells occupied by placed obstacles.

The tool can load available battle locations from server data and uses those locations when saving a preset.

## Obstacle Palette

The palette window is opened from the preset tool.

`ObstaclePaletteWindow` scans a prefab folder and displays obstacle prefabs as a visual grid. Each item draws the obstacle footprint from its `BoundsPoints`, so the user can understand the shape before placing it.

Palette features:

- configurable obstacle prefab folder;
- searchable prefab list;
- adjustable icon size and visual scale;
- click to select prefab;
- drag prefab into SceneView to place it on the hex grid.

Default prefab folder:

```text
Assets/Core/Prefabs/Game/Battle/Obstacles/ObstacleBlockers
```

## SceneView Placement

In preset mode, obstacles can be dragged from the palette into the SceneView.

Placement flow:

1. The tool receives the drag event in `SceneView.duringSceneGui`.
2. The mouse position is projected onto the battlefield plane.
3. The world position is converted into a hex grid coordinate.
4. The obstacle footprint is checked through `BattleObstacle.BoundsPoints`.
5. The prefab is instantiated through `PrefabUtility.InstantiatePrefab`.
6. The object is snapped to the target hex cell.
7. Occupied cells are updated and rendered on the grid.
8. The spawned obstacle is selected in the editor.

Existing obstacles can also be moved and rotated in the SceneView. The tool snaps position to the nearest hex cell and rounds Y rotation to 60-degree steps.

## Preset Loading

`LoadPreset` reads a JSON preset and reconstructs the obstacle layout in the editor.

The loaded preset contains:

- active battle locations;
- obstacle prefab Addressables addresses;
- obstacle positions;
- obstacle rotations;
- occupied cells;
- attack-through flags;
- optional view override Addressables addresses.

For each obstacle, the tool loads the matching prefab through its Addressables address, places it on the temporary grid, restores its settings, and updates the occupied cell visualization.

## Preset Saving

`SavePreset` exports the current obstacle layout to JSON.

The output format is `WSPackageObstaclePreset`.

Each obstacle is saved as `WSPackageBattleObstacle` with:

- `mAttackIsPossibleThrough`;
- `pAttackIsPossibletThrough`;
- `skillAttackIsPossibleThrough`;
- grid `position`;
- Y-axis `rotation`;
- obstacle `type`;
- `obstacleBundleName`;
- optional `obstacleViewOverride`;
- occupied `cells`.

The preset also stores the selected active battle locations in `locationIDs`.

## Runtime Connection

The exported JSON matches the format used by the runtime battle obstacle system.

At runtime, `BattleObstaclesController` reads obstacle package data, loads obstacle prefabs through Addressables, places them on the battle grid, and applies optional view overrides.

This keeps the editor workflow and runtime spawning path aligned:

```text
Editor placement
  -> JSON preset
    -> WSPackageObstaclePreset
      -> Runtime Addressables loading
        -> BattleObstacle instances on grid
```

## Data Model

Obstacle prefabs are based on `BattleObstacle`.

Important obstacle data:

- `BoundsPoints` define the obstacle footprint on the hex grid.
- `MAttackIsPossibleThrough` defines whether melee attacks can pass through.
- `PAttackIsPossibleThrough` defines whether physical/projectile attacks can pass through.
- `SkillAttackIsPossibleThrough` defines whether skill attacks can pass through.
- `BattleObstacleView` can be attached as a visual override.

The tool uses the same data that runtime gameplay uses, so the authored layout reflects real battle obstacle behavior.

## Workflow Summary

1. Create or prepare obstacle prefabs.
2. Create matching visual views if needed.
3. Open `CreateObstaclePreset`.
4. Open the obstacle palette.
5. Drag obstacles onto the hex grid.
6. Move and rotate placed obstacles in SceneView.
7. Select active locations for the preset.
8. Save the layout as JSON.
9. Use the exported preset in the battle loading pipeline.

---

# ObstacleGenerator RU

`ObstacleGenerator` - это Unity Editor инструмент для создания и подготовки battle obstacles для hex-grid боевых сцен. Он поддерживает создание obstacle prefabs, настройку obstacle views, визуальную расстановку на hex grid и экспорт JSON-presets для runtime battle loading.

Инструмент работает только в editor и обернут в `#if UNITY_EDITOR`.

## Назначение

`ObstacleGenerator` связывает Unity content authoring с runtime-форматом battle obstacles.

Он помогает дизайнерам и разработчикам:

- создавать новые `BattleObstacle` объекты из base prefab;
- создавать visual views для существующих obstacles;
- расставлять obstacles прямо на hex battlefield;
- снапить позиции и повороты obstacles к grid;
- визуализировать занятые и закрытые клетки;
- сохранять obstacle layouts как JSON presets;
- загружать существующие presets обратно в editor;
- использовать Addressables addresses, совпадающие с runtime loading.

## Основные компоненты

| File | Responsibility |
|---|---|
| `ObstacleGenerator.cs` | Главный `ExecuteAlways` component, который переключает generator tools. |
| `ObstacleGeneratorEditorSettings.cs` | Общие editor references: base obstacle prefab, base view prefab, hex tile, grid renderer, camera. |
| `ObstacleGenerator_BaseTool.cs` | Base class для всех режимов generator. |
| `ObstacleGenerator_CreateNewObstacle.cs` | Режим для spawn нового obstacle из base prefab. |
| `ObstacleGenerator_CreateNewView.cs` | Режим для создания visual view под существующий obstacle prefab. |
| `ObstacleGenerator_CreateObstaclePreset.cs` | Режим для сборки, загрузки и сохранения obstacle presets на hex grid. |
| `ObstaclePaletteWindow.cs` | Odin EditorWindow, который показывает draggable obstacle prefabs в визуальной palette. |
| `ToolBattleSceneDTO.cs` | DTO classes для чтения доступных battle locations из server data. |

## Режимы инструмента

`ObstacleGenerator` предоставляет три режима:

```csharp
public enum ObstacleGeneratorTool
{
	CreateNewObstacle,
	CreateNewView,
	CreateObstaclePreset,
}
```

Главный component отслеживает активный режим в editor update. Когда выбранный tool меняется, предыдущий tool выключается, а новый включается.

## Create New Obstacle

`CreateNewObstacle` используется, чтобы заспавнить новый obstacle из настроенного base obstacle prefab.

Spawned obstacle следует за позицией объекта `ObstacleGenerator` в сцене. Это дает быстрый workflow для создания и проверки нового obstacle prefab.

Типичный сценарий:

1. Выбрать `CreateNewObstacle`.
2. Нажать `SpawnNewObstacle`.
3. Настроить spawned `BattleObstacle`.
4. Проверить или поправить bound points.
5. Сохранить результат как prefab.

## Create New View

`CreateNewView` используется для создания визуального представления существующего obstacle.

Tool позволяет выбрать obstacle prefab из:

```text
Assets/Core/Prefabs/Game/Battle/Obstacles/ObstacleBlockers
```

После spawn obstacle для view creation инструмент создает base `BattleObstacleView` и показывает временный hex footprint на основе `BoundsPoints` obstacle.

Так проще совместить visual model с gameplay footprint obstacle.

Типичный сценарий:

1. Выбрать `CreateNewView`.
2. Выбрать существующий obstacle prefab.
3. Нажать `SpawnObstacleToCreateVisual`.
4. Настроить или заменить generated visual view.
5. Сохранить view prefab.

## Create Obstacle Preset

`CreateObstaclePreset` - основной режим authoring для layout.

При включении он создает временный hex grid в сцене и переключает tool camera для preset editing. Grid использует отдельные материалы для:

- доступных клеток;
- закрытых клеток;
- клеток, занятых placed obstacles.

Tool может загрузить доступные battle locations из server data и использует эти locations при сохранении preset.

## Obstacle Palette

Palette window открывается из preset tool.

`ObstaclePaletteWindow` сканирует папку prefabs и показывает obstacle prefabs в виде визуальной сетки. Каждый элемент рисует footprint obstacle по его `BoundsPoints`, поэтому пользователь видит форму до placement.

Возможности palette:

- настраиваемая папка obstacle prefabs;
- searchable prefab list;
- настройка icon size и visual scale;
- click для выбора prefab;
- drag prefab в SceneView для placement на hex grid.

Default prefab folder:

```text
Assets/Core/Prefabs/Game/Battle/Obstacles/ObstacleBlockers
```

## SceneView Placement

В preset mode obstacles можно перетаскивать из palette в SceneView.

Placement flow:

1. Tool получает drag event через `SceneView.duringSceneGui`.
2. Mouse position проецируется на battlefield plane.
3. World position конвертируется в hex grid coordinate.
4. Footprint obstacle проверяется через `BattleObstacle.BoundsPoints`.
5. Prefab создается через `PrefabUtility.InstantiatePrefab`.
6. Object снапится к target hex cell.
7. Occupied cells обновляются и отображаются на grid.
8. Spawned obstacle выбирается в editor.

Уже размещенные obstacles также можно двигать и вращать в SceneView. Tool снапит position к ближайшей hex cell и округляет Y rotation к шагу 60 градусов.

## Preset Loading

`LoadPreset` читает JSON preset и восстанавливает obstacle layout в editor.

Loaded preset содержит:

- active battle locations;
- obstacle prefab Addressables addresses;
- obstacle positions;
- obstacle rotations;
- occupied cells;
- attack-through flags;
- optional view override Addressables addresses.

Для каждого obstacle tool загружает подходящий prefab по Addressables address, ставит его на temporary grid, восстанавливает настройки и обновляет визуализацию занятых cells.

## Preset Saving

`SavePreset` экспортирует текущий obstacle layout в JSON.

Output format - `WSPackageObstaclePreset`.

Каждый obstacle сохраняется как `WSPackageBattleObstacle` с:

- `mAttackIsPossibleThrough`;
- `pAttackIsPossibletThrough`;
- `skillAttackIsPossibleThrough`;
- grid `position`;
- Y-axis `rotation`;
- obstacle `type`;
- `obstacleBundleName`;
- optional `obstacleViewOverride`;
- occupied `cells`.

Preset также хранит выбранные active battle locations в `locationIDs`.

## Runtime Connection

Экспортированный JSON совпадает с форматом, который использует runtime battle obstacle system.

В runtime `BattleObstaclesController` читает obstacle package data, загружает obstacle prefabs через Addressables, размещает их на battle grid и применяет optional view overrides.

Это синхронизирует editor workflow и runtime spawning path:

```text
Editor placement
  -> JSON preset
    -> WSPackageObstaclePreset
      -> Runtime Addressables loading
        -> BattleObstacle instances on grid
```

## Data Model

Obstacle prefabs основаны на `BattleObstacle`.

Важные данные obstacle:

- `BoundsPoints` определяют footprint obstacle на hex grid.
- `MAttackIsPossibleThrough` определяет, могут ли melee attacks проходить через obstacle.
- `PAttackIsPossibleThrough` определяет, могут ли physical/projectile attacks проходить через obstacle.
- `SkillAttackIsPossibleThrough` определяет, могут ли skill attacks проходить через obstacle.
- `BattleObstacleView` может быть подключен как visual override.

Tool использует те же данные, что и runtime gameplay, поэтому authored layout отражает реальное поведение battle obstacles.

## Workflow Summary

1. Создать или подготовить obstacle prefabs.
2. Создать matching visual views при необходимости.
3. Открыть `CreateObstaclePreset`.
4. Открыть obstacle palette.
5. Перетащить obstacles на hex grid.
6. Передвинуть и повернуть placed obstacles в SceneView.
7. Выбрать active locations для preset.
8. Сохранить layout как JSON.
9. Использовать exported preset в battle loading pipeline.
