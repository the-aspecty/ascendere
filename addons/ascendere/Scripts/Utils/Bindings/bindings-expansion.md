# Binding System — Expansion Reference

> Companion to [BindingUtils.md](./BindingUtils.md).  
> Covers: **1)** which modules can adopt the binding pattern, **2)** extended
> capabilities the infrastructure can grow into.

---

## 1. Modules That Can Benefit

Criteria for a good fit: the module has **mutation methods that other systems
need to react to**, or exposes a **query API** where callers want to watch a
value over time rather than poll it.

---

### Currency — `Modules/Currency/` ✅ Implemented

Migrated from the old `ActiveBinding` inner-class pattern to the shared utils.
`CurrencyBindingRegistry` now uses `BindingList<CurrencyEvent>` + `OwnerLifetimeTracker`;
`CurrencyBinding` extends `FluentBuilderBase<CurrencyBinding>`.

---

### SaveLoad — `Modules/SaveLoad/` ✅ Implemented

`SaveLoadBindingRegistry` dispatches `SaveLoadEvent` alongside every EventBus publish
from `SaveLoadManager`. `SaveLoadBinding` extends `FluentBuilderBase<SaveLoadBinding>`.
`SaveLoadManager` uses a `PublishAndDispatch<T>` helper that calls both EventBus and
the registry in one place, keeping both paths in sync automatically.

**Events exposed**: `SaveStarted`, `SaveCompleted`, `LoadStarted`, `LoadCompleted`,
`SlotDeleted`, `SlotCopied`, `AutoSaveTriggered`, `QuickSaveTriggered`, `SaveCorrupted`.

**Filters**: `.ForSlot(slotName)` restricts to a specific save slot.

```csharp
saveLoad.On()
        .SaveCompleted()
        .ForSlot("slot_0")
        .OwnedBy(this)
        .Do(e => ShowSavedToast(e.DurationMs));

saveLoad.On()
        .SaveCorrupted()
        .Once()
        .Do(() => ShowCorruptionWarning());
```

---

### Audio — `Modules/Audio/` ✅ Implemented

`AudioBindingRegistry` dispatches a unified `AudioEvent` struct for all audio subsystems.
Each audio manager (`MusicManager`, `SfxManager`, `VoiceManager`, `AmbientManager`) now
calls `AudioBindingRegistry.DispatchGeneric(evt)` inside its shared `PublishEvent<T>`
helper. `AudioBeatDetector` dispatches directly. `AudioBinding` extends
`FluentBuilderBase<AudioBinding>`.

**Events exposed** (20 kinds): `MusicStarted/Stopped/Paused/Resumed`, `MusicTrackChanged`,
`MusicCrossfadeStarted/Completed`, `PlaylistCompleted`, `SfxPlayed/Stopped`,
`SfxCooldownBlocked`, `SfxConcurrencyBlocked`, `VoiceStarted/Finished/Interrupted`,
`AmbientProfileChanged`, `AmbientLayerAdded/Removed`, `AmbientBlendCompleted`, `Beat`.

**Filters**: `.WithId(id)` restricts by PrimaryId (TrackId, SfxId, VoiceLineId, etc.).

```csharp
// React to any music track change
audio.On().TrackChanged().OwnedBy(this).Do(e => UpdateNowPlaying(e.PrimaryId));

// Beat-synced binding
audio.On().Beat().OwnedBy(this).Do(e => PulseUI(e.IntValue1));
```

---

### Quests — `Modules/Quests/` (planned)

Quest systems are inherently reactive: UI, dialogue, and scripted events all
need to know when objectives complete or quests change state.

**Proposed builder**:

```csharp
quests.When("main_quest_01")
      .ObjectiveCompleted("find_artifact")
      .OwnedBy(this)
      .Do(() => PlayObjectiveCutscene());

quests.When("any")
      .StateChangesTo(QuestState.Completed)
      .OwnedBy(this)
      .Do(q => AwardXP(q.XpReward));
```

**Context type**: `QuestEvent { string QuestId; QuestState State; string ObjectiveId; }`

**Why it matters**: Quest systems accumulate listeners across NPC dialogue nodes,
HUD nodes, achievement nodes, and cutscene triggers. Without ownership tracking
every one of those creates a potential dangling reference.

---

### Leveling / Progression — `Modules/Leveling/` (empty, planned)

XP and level changes are pure reactive events — the XP value changes and every
interested system (stat scaling, UI, achievements, unlocks) needs to know.

**Proposed builder**:

```csharp
leveling.For(playerId)
        .OnLevelUp()
        .OwnedBy(this)
        .Do((oldLevel, newLevel) => PlayLevelUpVFX(newLevel));

leveling.For(playerId)
        .WhenXpChanges()
        .Matches(xp => xp >= 1000)
        .Once()
        .Do(() => CheckAchievement("xp_1000"));
```

**Context type**: `LevelingEvent { string EntityId; int OldLevel; int NewLevel; int Xp; }`

**Synergy with WorldFlags**: XP could be stored as a WorldFlag (`player.xp = 500`),
making `FlagBinding` an immediate stand-in until a dedicated Leveling module ships.

---

### Loot — `Modules/Loot/` ✅ Implemented

`LootBindingRegistry` dispatches `LootEvent` alongside the `LootDroppedEvent` publish in
`LootManager.DepositAndPublish()`. `LootBinding` extends `FluentBuilderBase<LootBinding>`.

**Events exposed**: `Dropped` (loot resolved and deposited into an inventory).

**Filters**: `.FromSource(id)`, `.IntoInventory(id)`, `.ContainingItem(itemId)`.

```csharp
loot.On()
    .Dropped()
    .ContainingItem("legendary_sword")
    .Once()
    .OwnedBy(this)
    .Do(result => AwardAchievement("legendary_drop"));
```

---

### SceneManager — `Modules/SceneManager/` ✅ Implemented

`SceneBindingRegistry` is dispatched from `SceneManager` alongside the `EmitSignal` and
C# delegate invocations. `SceneBinding` extends `FluentBuilderBase<SceneBinding>`.

**Events exposed**: `Changed` (scene active), `Loading` (async progress in [0,1]).

**Filters**: `.To(scenePath)` restricts to a specific scene path (exact match).

```csharp
sceneManager.On()
            .Changed()
            .To("res://Scenes/Town.tscn")
            .Do(() => StartAmbientBirds());

sceneManager.On()
            .Loading()
            .OwnedBy(this)
            .Do(e => UpdateLoadingBar(e.Progress));
```

---

### Crafting — `Modules/Crafting/` (planned)

Crafting involves a pre-condition check, an ingredient consumption, and an output
production — three distinct reactive moments.

**Proposed builder**:

```csharp
crafting.On()
        .RecipeCompleted("iron_sword")
        .OwnedBy(this)
        .Do(result => PlayForgeAnimation(result.OutputItem));

crafting.On()
        .RecipeFailed()
        .Do(ctx => ShowCraftFailFeedback(ctx.Reason));
```

**Context type**: `CraftEvent { string RecipeId; CraftResult Result; string FailReason; }`

---

### AI Blackboard — `Modules/AI/` (planned)

The planned Blackboard (per-agent key-value store for Behavior Trees) maps
perfectly onto the `FlagBinding` pattern but scoped to an agent instance rather
than a global flag store.

**Proposed builder**:

```csharp
// Instanced builder per-agent (not static registry)
agent.Blackboard.When("target_visible")
               .ChangesTo(true)
               .Do(() => TransitionToChaseState());
```

**Implementation note**: Use an *instance* `BindingList<BlackboardEvent>` per
agent (not a static registry) so that when the agent is freed, the list is
simply dropped with it. `OwnerLifetimeTracker` can still manage cross-agent
listeners (e.g. a squad coordinator watching individual agent blackboards).

---

### Networking — `Modules/Networking/` (planned)

Reactive networking state — connection status, peer join/leave, authority changes
— is the classic source of dangling callback bugs because network events can fire
long after a node-level UI has been freed.

**Proposed builder**:

```csharp
network.On()
       .PeerJoined()
       .OwnedBy(this)
       .Do(peer => AddPlayerSlotUI(peer.Id));

network.On()
       .ConnectionLost()
       .Once()
       .Do(ShowDisconnectedScreen);
```

**Context type**: `NetworkEvent { NetworkEventKind Kind; int PeerId; string Reason; }`

**Key uplift**: For networking specifically, the re-entrancy guard in `BindingList`
prevents the dangerous pattern of a `PeerJoined` callback immediately issuing an
RPC that causes another `PeerJoined` to fire synchronously.

---

### Config — `Modules/config_module/` (planned)

Config values change at runtime (player tweaks options, hot-reload from file).
Any system that depends on a config value should react, not poll.

**Proposed builder**:

```csharp
config.When("audio.master_volume")
      .OwnedBy(this)
      .Do((oldVal, newVal) => ApplyVolume(newVal.AsSingle()));
```

**Implementation note**: Config is essentially WorldFlags with a different source
of truth (INI/JSON files vs. in-memory Variant store). A shared
`FluentBuilderBase<ConfigBinding>` with LocalCapture is all that's needed.

---

## 2. Extended Features

Capabilities the current `BindingList` / `OwnerLifetimeTracker` / `FluentBuilderBase`
infrastructure can grow into without breaking existing users.

---

### 2.1 Priority Ordering

Allow bindings to declare an integer priority so higher-priority callbacks always
fire before lower ones, regardless of registration order.

```csharp
flags.When("player.hp")
     .Priority(100)          // fires before default-priority (0) bindings
     .OwnedBy(this)
     .Do(UpdateHealthBar);
```

**Implementation**: Add `int Priority = 0` to `BindingRecord<T>`. `BindingList.Add()`
inserts at the correct sorted position (binary search) rather than `List.Add()`.
Dispatch iterates forwards (highest-priority first) after the sort-order change.

---

### 2.2 Deferred / Next-Frame Callbacks

Let a binding opt into deferred execution so its callback runs at the start of
the next frame rather than inline inside the dispatcher. Solves ASCB005
(re-entrant mutation) without requiring callers to manually use `CallDeferred`.

```csharp
flags.When("world.day_count")
     .Deferred()             // callback posted to the frame queue
     .OwnedBy(this)
     .Do(UpdateDayCycleUI);
```

**Implementation**: Add `bool Deferred` to `BindingRecord<T>`. In `BindingList.Dispatch()`,
instead of invoking the callback immediately, post it via
`Engine.GetMainLoop().CallDeferred(...)` with the captured context. Requires a
deferred-execution helper that runs posted actions at frame start.

---

### 2.3 Throttle and Debounce

Prevent high-frequency events (XP ticking every frame, audio spectrum data) from
overwhelming callbacks.

```csharp
// Fire at most once per 200 ms regardless of how often the event fires
leveling.For(playerId)
        .WhenXpChanges()
        .Throttle(200)       // milliseconds
        .OwnedBy(this)
        .Do(UpdateXpBar);

// Wait for a 500 ms quiet period before firing (e.g. config save-on-change)
config.When("graphics.resolution")
      .Debounce(500)
      .OwnedBy(this)
      .Do(ApplyResolution);
```

**Implementation**: Add `int ThrottleMs` and `int DebounceMs` to `BindingRecord<T>`.
`BindingList.Dispatch()` tracks `ulong _lastFiredMs` per record and skips / delays
invocation accordingly. Debounce requires a `SceneTree.CreateTimer()` per pending
invocation.

---

### 2.4 Binding Groups and Bulk Removal

Tag bindings with a string group, then remove all bindings in that group at once.
Useful for scene-change teardown without per-node `OwnedBy`.

```csharp
flags.When("combat.*")
     .Group("combat_hud")
     .OwnedBy(this)
     .Do(UpdateCombatUI);

// On scene unload:
FlagBindingRegistry.RemoveGroup("combat_hud");
```

**Implementation**: Add `string Group` to `BindingRecord<T>`. Add
`BindingList.RemoveAllByGroup(string group)` iterating with soft-delete.

---

### 2.5 Conditional Chaining (Binding Pipelines)

Allow a binding to trigger another reactive value transformation before the final
callback — a lightweight in-process event pipeline.

```csharp
flags.When("player.raw_damage")
     .Map(v => v.AsInt32() * GetDamageMultiplier())   // transform the context
     .Filter(damage => damage > 0)                    // refined postcondition
     .OwnedBy(this)
     .Do(damage => ApplyScreenShake(damage));
```

**Implementation**: `Map<TOut>(Func<TContext, TOut>)` returns a new
`FluentBuilderBase<TOut>`-derived builder wrapping the upstream filter. No changes
required to `BindingRecord`; the transformation lambda is simply composed into the
`Filter` + `Callback` closures at `Do()` time.

---

### 2.6 Observable Property (`IReadableValue<T>`)

Expose any watched value as a pull-or-push interface so callers can both get the
current value immediately *and* subscribe to future changes.

```csharp
IReadableValue<int> hp = leveling.Observe<int>("player.hp");
GD.Print(hp.Value);                              // pull: current value
hp.OwnedBy(this).OnChanged(UpdateHealthBar);     // push: reactive
```

**Implementation**: `IReadableValue<T>` is a small wrapper that:
1. Caches the last-seen value in the binding callback.
2. Exposes `.Value` for immediate reads.
3. Exposes `.OnChanged(Action<T>)` which internally calls the normal `Register`.

This pattern is common in UI frameworks (React state, Godot's `@export` + setter
combo) and fits naturally on top of `FluentBuilderBase`.

---

### 2.7 Binding Snapshot and Replay (Undo / Debug)

Record all dispatched events for a time window and replay them — useful for
debugging "what happened before this crash" or implementing undo systems.

```csharp
using var snapshot = BindingList.BeginRecording(maxEntries: 100);
// ... time passes ...
snapshot.Replay();    // re-dispatch the last 100 events to all current bindings
```

**Implementation**: Recording wraps the `_items` list dispatch with a
`Queue<TContext>` that stores up to N contexts. `Replay()` iterates the queue and
calls the normal `Dispatch()` path. The `IDisposable` pattern ensures recording
stops automatically.

---

### 2.8 Diagnostic Inspector Integration

Expose live binding counts and owner lists in the Godot editor's bottom panel or
as an overlay node during gameplay — the in-editor equivalent of ASCB001/ASCB004.

```
[Ascendere Bindings Debug]
FlagBindingRegistry      12 active  (3 owned, 9 global)
InventoryBindingRegistry  4 active  (4 owned)
InputBindingRegistry      7 active  (7 owned)
CurrencyBindingRegistry   2 active  (0 owned)  ⚠ ASCB001: 2 unowned bindings
```

**Implementation**: Each registry exposes `Count` (already present) and a new
`IReadOnlyList<BindingDiagnostic> GetDiagnostics()` method returning owner paths
and filter descriptions. An `EditorPlugin` docks a `Control` that calls these
and refreshes every 30 frames.

---

### 2.9 Cross-Registry Binding (Multi-Source Listen)

Let a single callback fire when *any* of several registries dispatch, with a
unified context.

```csharp
// Fire when either XP flags OR leveling module triggers a progression event
AscendereBindings.Any(
    flags.When("player.xp"),
    leveling.For(playerId).WhenLevelUp()
).OwnedBy(this).Do(OnProgressionChanged);
```

**Implementation**: `AscendereBindings.Any(params FluentBuilderBase<?>[] sources)`
creates one internal `BindingRecord` per source registry but routes all of them
to the same user callback. A shared `BindingGroup` ID links them for atomic
`RemoveAllByGroup` teardown.

---

### 2.10 Thread-Safe Dispatch Mode

For modules that may mutate state from a background thread (SaveLoad async I/O,
Networking receive loop), add an opt-in thread-safe dispatch path.

```csharp
// Registry opt-in (per registry, not per binding):
private static readonly BindingList<SaveLoadEvent> _list =
    new(BindingListOptions.ThreadSafeDispatch);
```

**Implementation**: `BindingList` gains a `_lock` object (`object _sync = new()`).
When `ThreadSafeDispatch` is set, `Dispatch()` acquires the lock before iterating
and `Add()`/`RemoveAllByOwner()` also lock. The default (non-locked) path adds no
overhead for the majority of modules that run entirely on the main thread.

> ⚠ **Note**: Godot nodes must only be touched on the main thread. Thread-safe
> dispatch only routes the *notification* safely; any callback that calls a Godot
> API must still use `Callable.From(...).CallDeferred()`.
