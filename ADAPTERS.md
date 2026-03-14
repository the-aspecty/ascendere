# Adapters

Adapters are lightweight bridge files that wire two modules together **without coupling them to each other**. They live in `addons/ascendere/Adapters/` and are the only place in the codebase that is allowed to depend on multiple domain modules simultaneously.

---

## Why Adapters?

Ascendere modules are deliberately isolated — `Inventory` knows nothing about `SaveLoad`, and `Currency` knows nothing about `WorldFlags`. This keeps modules independently installable and testable.

When cross-module behaviour *is* needed (e.g. saving inventory slots), an **Adapter** holds both sides:

```
Modules/Inventory  ←──┐
                       │ Adapters/Inventory_SaveLoad
Modules/SaveLoad   ←──┘
```

You install only the adapters that match your installed modules. If you don't use `SaveLoad`, simply don't include the `Inventory_SaveLoad` adapter.

---

## Folder Convention

```
addons/ascendere/Adapters/
    <Provider>_<Consumer>/
        <Provider>SaveContributor.cs   (or other bridge class)
```

| Segment      | Meaning                          |
|-------------|----------------------------------|
| `Provider`   | Module that owns the data        |
| `Consumer`   | Module that uses/stores the data |

**Examples:**

| Folder                    | Wires together                        |
|--------------------------|---------------------------------------|
| `Inventory_SaveLoad/`    | Inventory data ↔ SaveLoad slots       |
| `Currency_SaveLoad/`     | Currency balances ↔ SaveLoad slots    |
| `WorldFlags_SaveLoad/`   | World flags ↔ SaveLoad slots          |

---

## module.json Fields

Module manifests use two adapter-related fields:

```json
{
    "optionalDeps": ["saveload_module"],
    "hasAdapters": true
}
```

| Field           | Type       | Description                                                              |
|----------------|------------|--------------------------------------------------------------------------|
| `optionalDeps`  | `string[]` | Module IDs that adapters in this module depend on (install if you want integration) |
| `hasAdapters`   | `bool`     | `true` when the module ships adapters in `Adapters/<ModuleName>_*/`     |

---

## Built-in Adapters

| Adapter                            | Provider    | Consumer  | Auto-registration |
|-----------------------------------|-------------|-----------|-------------------|
| `Inventory_SaveLoad/`             | Inventory   | SaveLoad  | `[Saveable(Order = 20)]` |
| `Currency_SaveLoad/`              | Currency    | SaveLoad  | `[Saveable(Order = 30)]` |
| `WorldFlags_SaveLoad/`            | WorldFlags  | SaveLoad  | `[Saveable(Order = 10)]` |

> **Planned:** `Loot_Inventory`, `Loot_Currency`, `Quest_Inventory`

---

## How SaveLoad Adapters Work

Every SaveLoad adapter implements `ISaveContributor` and is tagged `[Saveable]`. The `SaveLoadModule` reflects all assemblies during `Initialize()` and auto-registers any class carrying that attribute — no bootstrapping code required.

```csharp
[Saveable(Order = 20)]          // auto-registered, lower order = saved first
internal sealed class InventorySaveContributor : ISaveContributor
{
    public string ContributorId => "Inventory";  // unique key in the save file
    public int SaveVersion => 1;

    public Dictionary OnSave()   { /* snapshot state */  }
    public void OnLoad(Dictionary data) { /* restore state */ }
}
```

---

## Creating Your Own Adapter

Follow these five steps to integrate two modules without coupling them.

### Step 1 — Identify the contract

Decide which module **owns the data** (Provider) and which **consumes it** (Consumer). You'll import both interfaces.

### Step 2 — Create the folder

```
addons/ascendere/Adapters/<Provider>_<Consumer>/
```

### Step 3 — Write the bridge class

For SaveLoad integration, implement `ISaveContributor`:

```csharp
using Ascendere.SaveLoad;
using Ascendere.YourModule.Interfaces;   // Provider interface
using Godot;
using Godot.Collections;

namespace Ascendere.Adapters.YourModule_SaveLoad;

[Saveable(Order = 40)]   // choose an order unique among your contributors
internal sealed class YourModuleSaveContributor : ISaveContributor
{
    public string ContributorId => "YourModule";
    public int SaveVersion => 1;

    public Dictionary OnSave()
    {
        var svc = ServiceLocator.GetOrDefault<IYourModuleService>();
        if (svc == null)
            return new Dictionary();

        // Snapshot your data into a Godot Dictionary.
        var root = new Dictionary();
        // ... populate root ...
        return root;
    }

    public void OnLoad(Dictionary data)
    {
        var svc = ServiceLocator.GetOrDefault<IYourModuleService>();
        if (svc == null)
        {
            GD.PrintErr("[YourModule] Service not available during load.");
            return;
        }

        // Restore data from the Dictionary.
    }
}
```

### Step 4 — Update the provider's module.json

Add the consumer to `optionalDeps` and set `hasAdapters: true`:

```json
{
    "optionalDeps": ["saveload_module"],
    "hasAdapters": true
}
```

### Step 5 — Build and verify

The `[Saveable]` attribute is all you need. Rebuild the project; `SaveLoadModule.Initialize()` will discover and register your contributor automatically.

---

## Manual Registration (No `[Saveable]`)

If you prefer explicit control, skip the attribute and register manually in your module's `Initialize()`:

```csharp
public override void Initialize()
{
    var saveLoad = ServiceLocator.GetOrDefault<ISaveLoadService>();
    saveLoad?.RegisterContributor(new YourModuleSaveContributor());
}

public override void Shutdown()
{
    var saveLoad = ServiceLocator.GetOrDefault<ISaveLoadService>();
    saveLoad?.UnregisterContributor("YourModule");
}
```

---

## Rules

| ✅ Do                                                                       | ❌ Don't                                               |
|----------------------------------------------------------------------------|--------------------------------------------------------|
| Keep adapters in `Adapters/<Provider>_<Consumer>/`                         | Put cross-module code inside a domain module           |
| Import only the **interfaces** of both modules                              | Depend on concrete implementations                     |
| Use `ServiceLocator.GetOrDefault<>()` and null-check before using          | Assume services are always available                   |
| Handle `OnLoad` failures gracefully (log + skip, don't throw)              | Call `GD.PushError` and crash on missing data          |
| Keep `SaveVersion` at `1` until a breaking format change requires migration | Bump the version for purely additive changes           |
