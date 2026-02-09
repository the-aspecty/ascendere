// Editor settings helper for the Ascendere plugin
#if TOOLS
using System;
using Godot;
using Godot.Collections;
using System.Reflection;
using Ascendere.Utils;

namespace Ascendere.Editor
{
    /// <summary>
    /// Manages Project Settings entries for the Ascendere plugin.
    /// This class registers default settings under the 'ascendere' category in the
    /// Project Settings window and exposes helper methods to add and retrieve settings.
    /// </summary>
    [Tool]
    public static class EditorSettings
    {
        private const string SectionName = "ascendere";
        public static string Prefix => SectionName + "/";

        /// <summary>
        /// Register the default set of project settings for Ascendere. This will create
        /// a new "ascendere" category in the Project Settings dialog.
        /// </summary>
        public static void RegisterDefaultSettings()
        {
            if (!Engine.IsEditorHint())
                return; // Only register settings when running in the editor

            try
            {
                // If property info is already registered for our key, assume defaults were registered; skip to avoid duplicates
                if (IsPropertyInfoRegistered(Prefix + "enabled"))
                    return;
                // Basic toggles and defaults
                AddOrUpdateBool(
                    "enabled",
                    true,
                    "Enable Ascendere plugin features",
                    setBasic: true
                );
                AddOrUpdateBool(
                    "verbose_logging",
                    false,
                    "Enable verbose log output from Ascendere",
                    setBasic: true
                );
                AddOrUpdateString(
                    "default_namespace",
                    "Aspecty",
                    "Default namespace for generated code",
                    setBasic: true
                );

                // Default paths for core resources (reflects ConfigBase defaults)
                AddOrUpdateString(
                    "paths/components",
                    "/Components",
                    "Default folder path for component scripts/resources",
                    setBasic: true
                );
                AddOrUpdateString(
                    "paths/entities",
                    "/Entities",
                    "Default folder path for entity scenes",
                    setBasic: true
                );
                AddOrUpdateString(
                    "paths/systems",
                    "/Systems",
                    "Default folder path for system scripts",
                    setBasic: true
                );
                AddOrUpdateString(
                    "paths/events",
                    "/Events",
                    "Default folder path for event classes/resources",
                    setBasic: true
                );
                AddOrUpdateString(
                    "paths/modules",
                    "/Modules",
                    "Default folder path for module scripts",
                    setBasic: true
                );
                AddOrUpdateString(
                    "paths/plugins",
                    "/Plugins",
                    "Default folder path for plugin scripts",
                    setBasic: true
                );
                AddOrUpdateString(
                    "paths/gamescenes",
                    "/GameScenes",
                    "Default folder path for game scenes",
                    setBasic: true
                );

                AddOrUpdateInt(
                    "max_cache_items",
                    60,
                    "Max number of cached items used by Ascendere",
                    setBasic: true,
                    hint: PropertyHint.Range,
                    hintString: "1,1024,1"
                );
            }
            catch (Exception ex)
            {
                GD.PrintErr(
                    $"[Ascendere.EditorSettings] Failed to register default settings: {ex}"
                );
            }
        }

        public static bool AddOrUpdateBool(
            string key,
            bool defaultValue,
            string description = "",
            bool setBasic = true,
            PropertyHint hint = PropertyHint.None,
            string hintString = ""
        )
        {
            return AddOrUpdateProperty(
                key,
                Variant.Type.Bool,
                defaultValue,
                description,
                setBasic,
                hint,
                hintString
            );
        }

        public static bool AddOrUpdateInt(
            string key,
            int defaultValue,
            string description = "",
            bool setBasic = true,
            PropertyHint hint = PropertyHint.None,
            string hintString = ""
        )
        {
            return AddOrUpdateProperty(
                key,
                Variant.Type.Int,
                defaultValue,
                description,
                setBasic,
                hint,
                hintString
            );
        }

        public static bool AddOrUpdateFloat(
            string key,
            float defaultValue,
            string description = "",
            bool setBasic = true,
            PropertyHint hint = PropertyHint.None,
            string hintString = ""
        )
        {
            return AddOrUpdateProperty(
                key,
                Variant.Type.Float,
                defaultValue,
                description,
                setBasic,
                hint,
                hintString
            );
        }

        public static bool AddOrUpdateString(
            string key,
            string defaultValue,
            string description = "",
            bool setBasic = true,
            PropertyHint hint = PropertyHint.None,
            string hintString = ""
        )
        {
            return AddOrUpdateProperty(
                key,
                Variant.Type.String,
                defaultValue,
                description,
                setBasic,
                hint,
                hintString
            );
        }

        /// <summary>
        /// Adds or updates a property inside the 'ascendere' Project Settings section.
        /// If the property already exists it will not be added twice; however the default
        /// value will be set if the setting is not already present.
        /// </summary>
        public static bool AddOrUpdateProperty(
            string key,
            Variant.Type type,
            object defaultValue,
            string description = "",
            bool setBasic = true,
            PropertyHint hint = PropertyHint.None,
            string hintString = ""
        )
        {
            if (!Engine.IsEditorHint())
                return false;

            var fullName = Prefix + key;

            var propertyInfo = new Godot.Collections.Dictionary();
            propertyInfo["name"] = fullName;
            propertyInfo["type"] = (int)type;
            propertyInfo["hint"] = (int)hint;
            propertyInfo["hint_string"] = hintString;
            propertyInfo["default"] = VariantUtils.ToVariant(defaultValue);

            try
            {
                // Register the property info in the Project Settings (this is what adds the category and UI)
                // Avoid duplicate registration: check if a property info with the same name is already registered.
                var wasRegistered = IsPropertyInfoRegistered(fullName);
                if (!wasRegistered)
                {
                    try
                    {
                        // If a runtime-only setting exists that is not registered as property info, clear it to avoid duplicate errors
                        if (
                            ProjectSettings.HasSetting(fullName)
                            && !IsPropertyInfoRegistered(fullName)
                        )
                        {
                            ProjectSettings.Clear(fullName);
                        }
                        ProjectSettings.AddPropertyInfo(propertyInfo);
                        GD.Print(
                            $"[Ascendere.EditorSettings] Registered property info: {fullName}"
                        );
                        // Refresh our registration check after attempting to add
                        wasRegistered = IsPropertyInfoRegistered(fullName);
                    }
                    catch (Exception ex)
                    {
                        GD.PrintErr(
                            $"[Ascendere.EditorSettings] AddPropertyInfo failed for '{fullName}': {ex}"
                        );
                        // Don't create a runtime setting without a registered property info; return false
                        return false;
                    }
                }

                // Ensure the setting has an initial value and the correct visibility (basic/advanced) after property info is registered
                if (wasRegistered)
                {
                    // Set initial value to avoid saving defaults unless changed
                    ProjectSettings.SetInitialValue(fullName, VariantUtils.ToVariant(defaultValue));
                    GD.Print(
                        $"[Ascendere.EditorSettings] Set initial value for: {fullName} = {defaultValue}"
                    );
                    // Mark as basic or advanced accordingly
                    ProjectSettings.SetAsBasic(fullName, setBasic);
                    GD.Print($"[Ascendere.EditorSettings] Set as basic={setBasic} for: {fullName}");
                }
                else
                {
                    GD.PrintErr(
                        $"[Ascendere.EditorSettings] Property info for '{fullName}' is not registered; skipping default value set to avoid creating an advanced/General setting."
                    );
                    return false;
                }

                // Optionally set a localized description - project settings doesn't directly expose it
                return true;
            }
            catch (Exception ex)
            {
                GD.PrintErr(
                    $"[Ascendere.EditorSettings] Could not add/update property '{fullName}': {ex}"
                );
                return false;
            }
        }

        public static T GetSetting<T>(string key, T fallback = default)
        {
            var fullName = Prefix + key;
            if (ProjectSettings.HasSetting(fullName))
            {
                try
                {
                    var v = ProjectSettings.GetSetting(fullName);
                    switch (Type.GetTypeCode(typeof(T)))
                    {
                        case TypeCode.Boolean:
                            return VariantUtils.FromVariant<T>(v);
                        case TypeCode.Int32:
                            return VariantUtils.FromVariant<T>(v);
                        case TypeCode.Single:
                        case TypeCode.Double:
                            return VariantUtils.FromVariant<T>(v);
                        case TypeCode.String:
                            return VariantUtils.FromVariant<T>(v);
                        default:
                            // Try a direct cast via string fallback
                            var asString = v.AsString();
                            return (T)Convert.ChangeType(asString, typeof(T));
                    }
                }
                catch (Exception ex)
                {
                    GD.PrintErr(
                        $"[Ascendere.EditorSettings] Failed to get setting '{fullName}': {ex}"
                    );
                    return fallback;
                }
            }

            return fallback;
        }

        public static void SetSetting<T>(string key, T value)
        {
            var fullName = Prefix + key;
            try
            {
                ProjectSettings.SetSetting(fullName, VariantUtils.ToVariant(value));
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[Ascendere.EditorSettings] Failed to set setting '{fullName}': {ex}");
            }
        }

        public static bool RemoveProperty(string key)
        {
            var fullName = Prefix + key;
            try
            {
                // Try to clear actual runtime setting
                if (ProjectSettings.HasSetting(fullName))
                    ProjectSettings.SetSetting(fullName, new Variant());
                // NOTE: Removing property info from the Editor inspector at runtime is
                // not guaranteed to be supported. We'll clear the runtime value and
                // leave the property info in place for now.
                return true;
            }
            catch (Exception ex)
            {
                GD.PrintErr(
                    $"[Ascendere.EditorSettings] Failed to remove property '{fullName}': {ex}"
                );
                return false;
            }
        }

        private static bool IsPropertyInfoRegistered(string fullName)
        {
            try
            {
                // Try to call ProjectSettings.GetSettingInfoList via reflection if available (older/newer Godot differences).
                var type = typeof(ProjectSettings);
                // ProjectSettings API can vary across Godot versions. Try a few common method names
                // and possible signatures (no-arg, one bool, one int, or one string) and attempt to
                // call them to retrieve property info lists.
                var candidateNames = new[]
                {
                    "GetSettingInfoList",
                    "GetPropertyInfoList",
                    "GetSettingList",
                    "GetPropertyList",
                };
                var methods = type.GetMethods(
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy
                );
                foreach (var name in candidateNames)
                {
                    foreach (var method in methods)
                    {
                        if (method.Name != name)
                            continue;

                        var parameters = method.GetParameters();
                        var argsCandidates = new System.Collections.Generic.List<object[]>()
                        {
                            null,
                        };
                        if (parameters.Length == 1)
                        {
                            var p = parameters[0];
                            if (p.ParameterType == typeof(bool))
                                argsCandidates.Add(new object[] { true });
                            else if (p.ParameterType == typeof(int))
                                argsCandidates.Add(new object[] { 0 });
                            else if (p.ParameterType == typeof(string))
                                argsCandidates.Add(new object[] { string.Empty });
                        }

                        foreach (var args in argsCandidates)
                        {
                            try
                            {
                                var res = method.Invoke(null, args);
                                if (res == null)
                                    continue;

                                if (res is Godot.Collections.Array arr)
                                {
                                    foreach (var item in arr)
                                    {
                                        Godot.Collections.Dictionary dict = null;
                                        if (item is Variant vv)
                                        {
                                            var obj = vv.Obj;
                                            if (obj is Godot.Collections.Dictionary d2)
                                                dict = d2;
                                        }
                                        else
                                        {
                                            // If not a Variant (rare), try to interpret it as a Dictionary
                                            var boxed = (object)item;
                                            if (boxed is Godot.Collections.Dictionary d3)
                                                dict = d3;
                                        }

                                        if (
                                            dict != null
                                            && dict.ContainsKey("name")
                                            && dict["name"].ToString() == fullName
                                        )
                                        {
                                            return true;
                                        }
                                    }
                                }
                            }
                            catch
                            {
                                // ignore and try other overloads or methods
                            }
                        }
                    }
                }

                // Fallback to the presence of a saved setting with the same name.
                return ProjectSettings.HasSetting(fullName);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[Ascendere.EditorSettings] Failed to query property info list: {ex}");
                return ProjectSettings.HasSetting(fullName);
            }
        }
    }
}
#endif
