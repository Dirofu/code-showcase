# ViewUICustomer

`ViewUICustomer` is a generic Unity UI state preset system. It lets you save visual configurations for nested UI elements in the Unity Inspector and switch them at runtime with a single method call.

The tool is designed for reusable UI components such as inventory slots, skill slots, character cards, quest buttons, market items, battle action buttons, selectors, and other elements that need several visual states.

## Core Idea

Instead of hardcoding colors, materials, positions, sizes, and active states in gameplay code, `ViewUICustomer` stores them as serialized presets.

Runtime code only asks the view to switch state:

```csharp
_view.SetState(ViewUIStateType.Highlight, itemType);
```

The view then applies the saved settings to all registered child elements.

## State Structure

`ViewUICustomer` uses two enum levels:

- `TType` - the global view variant, such as item rarity, team type, slot type, or button type.
- `TSlotElementType` - the internal UI parts, such as background, icon, frame, label, or overlay.

The serialized structure is:

```text
TType
  -> ViewUIStateType
      -> TSlotElementType
          -> ViewUIElement
```

Example:

```text
InventorySlotViewType.Rare
  -> Idle
      -> Background
      -> Icon
      -> Border

InventorySlotViewType.Rare
  -> Highlight
      -> Background
      -> Icon
      -> Border
```

## Supported UI States

`ViewUIStateType` includes the common states used by interactive UI:

- `Idle`
- `Highlight`
- `Pressed`
- `Active`
- `Disactive`

`None = -1` is used as a filter/helper value in editor tooling.

## What Gets Saved

Each `ViewUIElement` stores a snapshot of a child GameObject:

- active state
- `RectTransform.localPosition`
- `RectTransform.sizeDelta`
- `Image.color`
- `Image.material`
- `TMP_Text.color`

When a state is applied, these values are restored on the target object.

## Editor Workflow

1. Create a concrete view class:

```csharp
public class InventorySlotView
	: ViewUICustomer<InventorySlotViewType, InventorySlotElementType>
{
}
```

2. Add typed helper components to child UI objects:

```csharp
public class InventorySlotGetHelper
	: ViewUIGetHelper<InventorySlotElementType>
{
}
```

3. Configure the desired visual appearance in the Unity scene or prefab.

4. In the inspector, select the debug `TType` and `ViewUIStateType`.

5. Use `SaveCurrentState` to capture the current visual setup.

6. Switch states from code:

```csharp
_view.SetState(ViewUIStateType.Idle, InventorySlotViewType.Empty);
_view.SetState(ViewUIStateType.Highlight, InventorySlotViewType.Rare);
_view.SetState(ViewUIStateType.Active, InventorySlotViewType.Legendary);
```

## Button Support

`ViewUICustomer` can optionally subscribe to a `ViewUIButton`.

When button support is enabled, pointer events automatically switch states:

| Pointer Event | Applied State |
|---|---|
| Pointer Enter | `Highlight` |
| Pointer Exit | `Idle` |
| Pointer Down | `Pressed` |
| Pointer Up | `Idle` |
| Click | `Active` |

`ViewUIButton` also plays UI hover and click sounds through `SoundsController`.

## Why This Is Useful

`ViewUICustomer` keeps UI visual logic out of gameplay code.

Instead of writing code like:

```csharp
background.color = rareColor;
border.gameObject.SetActive(true);
label.color = activeTextColor;
```

the caller only switches a named state:

```csharp
_view.SetState(ViewUIStateType.Active, InventorySlotViewType.Rare);
```

This makes UI components easier to reuse, easier to tune in prefabs, and safer for designers to adjust without touching code.

## Performance Notes

The current implementation is suitable for typical Unity UI usage: buttons, slots, cards, selectors, and moderate-size lists.

Most setup cost happens during initialization:

- `Awake()` calls `UpdateStates()`.
- Serialized lists are converted into dictionaries.
- Groups and elements rebuild their lookup dictionaries.

Runtime state switching is lightweight for small components, but it is not a zero-cost operation. `SetState()` iterates over all saved elements in the selected state and applies their settings.

Potential runtime costs:

- repeated `SetState()` calls apply the same values again even if the state did not change;
- `ViewUIElement.SetSettings()` calls `GetRectTransform()` and `TryGetComponent()` each time;
- missing dictionary keys can throw exceptions if a preset was not configured;
- large UI grids with many interactive elements can produce noticeable work during frequent hover or refresh events.

## Current Tradeoff

`ViewUICustomer` is optimized for practical UI production workflow rather than maximum low-level performance.

It favors:

- inspector-driven visual setup;
- enum-based type safety;
- reusable view classes;
- simple runtime calls;
- fast iteration for UI designers and gameplay programmers.

---

# ViewUICustomer RU

`ViewUICustomer` - это универсальная система визуальных пресетов для Unity UI. Она позволяет сохранять настройки дочерних UI-элементов прямо в Unity Inspector и переключать их в runtime одним вызовом метода.

Тулза предназначена для переиспользуемых UI-компонентов: inventory slots, skill slots, карточек персонажей, quest buttons, market items, battle action buttons, селекторов и других элементов, которым нужны разные визуальные состояния.

## Основная идея

Вместо того чтобы хардкодить цвета, материалы, позиции, размеры и активность объектов в gameplay-коде, `ViewUICustomer` хранит эти настройки как сериализованные пресеты.

Runtime-код только просит view переключить состояние:

```csharp
_view.SetState(ViewUIStateType.Highlight, itemType);
```

После этого view применяет сохраненные настройки ко всем зарегистрированным дочерним элементам.

## Структура состояний

`ViewUICustomer` использует два уровня enum-типов:

- `TType` - общий вариант view, например редкость предмета, команда, тип слота или тип кнопки.
- `TSlotElementType` - внутренние части UI, например фон, иконка, рамка, текст или overlay.

Сериализованная структура выглядит так:

```text
TType
  -> ViewUIStateType
      -> TSlotElementType
          -> ViewUIElement
```

Пример:

```text
InventorySlotViewType.Rare
  -> Idle
      -> Background
      -> Icon
      -> Border

InventorySlotViewType.Rare
  -> Highlight
      -> Background
      -> Icon
      -> Border
```

## Поддерживаемые UI-состояния

`ViewUIStateType` содержит основные состояния интерактивного UI:

- `Idle`
- `Highlight`
- `Pressed`
- `Active`
- `Disactive`

`None = -1` используется как вспомогательное значение для фильтров и editor-инструментов.

## Что сохраняется

Каждый `ViewUIElement` хранит снимок настроек дочернего GameObject:

- active state
- `RectTransform.localPosition`
- `RectTransform.sizeDelta`
- `Image.color`
- `Image.material`
- `TMP_Text.color`

Когда состояние применяется, эти значения восстанавливаются на целевом объекте.

## Workflow в Editor

1. Создать конкретный view-класс:

```csharp
public class InventorySlotView
	: ViewUICustomer<InventorySlotViewType, InventorySlotElementType>
{
}
```

2. Добавить typed helper-компоненты на дочерние UI-объекты:

```csharp
public class InventorySlotGetHelper
	: ViewUIGetHelper<InventorySlotElementType>
{
}
```

3. Настроить нужный внешний вид в сцене или prefab.

4. В inspector выбрать debug `TType` и `ViewUIStateType`.

5. Нажать `SaveCurrentState`, чтобы сохранить текущую визуальную конфигурацию.

6. Переключать состояния из кода:

```csharp
_view.SetState(ViewUIStateType.Idle, InventorySlotViewType.Empty);
_view.SetState(ViewUIStateType.Highlight, InventorySlotViewType.Rare);
_view.SetState(ViewUIStateType.Active, InventorySlotViewType.Legendary);
```

## Поддержка кнопок

`ViewUICustomer` может опционально подписываться на `ViewUIButton`.

Если button support включен, pointer events автоматически переключают состояния:

| Pointer Event | Applied State |
|---|---|
| Pointer Enter | `Highlight` |
| Pointer Exit | `Idle` |
| Pointer Down | `Pressed` |
| Pointer Up | `Idle` |
| Click | `Active` |

`ViewUIButton` также проигрывает hover/click UI-звуки через `SoundsController`.

## Зачем это нужно

`ViewUICustomer` убирает визуальную UI-логику из gameplay-кода.

Вместо такого кода:

```csharp
background.color = rareColor;
border.gameObject.SetActive(true);
label.color = activeTextColor;
```

вызывающий код просто переключает именованное состояние:

```csharp
_view.SetState(ViewUIStateType.Active, InventorySlotViewType.Rare);
```

Так UI-компоненты проще переиспользовать, настраивать в prefab и менять без правок gameplay-кода.

## Производительность

Текущая реализация подходит для обычного Unity UI: кнопок, слотов, карточек, селекторов и списков умеренного размера.

Основная стоимость настройки возникает при инициализации:

- `Awake()` вызывает `UpdateStates()`.
- Сериализованные списки преобразуются в словари.
- Groups и elements пересобирают lookup-словари.

Runtime-переключение состояния достаточно легкое для маленьких компонентов, но это не бесплатная операция. `SetState()` проходит по всем сохраненным элементам выбранного состояния и применяет их настройки.

Потенциальные runtime-затраты:

- повторные вызовы `SetState()` применяют те же значения заново, даже если состояние не изменилось;
- `ViewUIElement.SetSettings()` каждый раз вызывает `GetRectTransform()` и `TryGetComponent()`;
- прямой доступ к словарям может выбросить exception, если пресет не настроен;
- большие UI-grid/list с большим количеством интерактивных элементов могут создавать заметную нагрузку при частом hover или refresh.

## Текущий компромисс

`ViewUICustomer` оптимизирован в первую очередь под удобный production workflow для UI, а не под максимальную низкоуровневую производительность.

Он делает ставку на:

- настройку внешнего вида через inspector;
- type safety через enum;
- переиспользуемые view-классы;
- простые runtime-вызовы;
- быструю итерацию для UI-дизайнеров и gameplay-программистов.