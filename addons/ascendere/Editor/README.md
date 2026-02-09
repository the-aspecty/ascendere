# Editor Settings (Ascendere)

This module registers the `ascendere` section in the Godot Project Settings and exposes helper methods to add or update settings dynamically from editor code.

Usage (from editor-only code):

```csharp
// Ensure defaults are registered (AscenderePlugin already does this on enter tree)
MetaFramework.Editor.EditorSettings.RegisterDefaultSettings();

// Add or update a setting dynamically
MetaFramework.Editor.EditorSettings.AddOrUpdateBool("enable_feature_x", true, "Enable feature X in Ascendere");

// Read a setting
var enabled = MetaFramework.Editor.EditorSettings.GetSetting<bool>("enable_feature_x", false);

// Update a setting value
MetaFramework.Editor.EditorSettings.SetSetting("enable_feature_x", false);
```

The settings appear under the `Project Settings -> ascendere` category in the Editor.
