using Godot;

public class ConfigBase
{
    public string ComponentsPath { get; set; } = "/Components";
    public string EntitiesPath { get; set; } = "/Entities";
    public string SystemsPath { get; set; } = "/Systems";
    public string EventsPath { get; set; } = "/Events";
    public string ModulesPath { get; set; } = "/Modules";
    public string PluginsPath { get; set; } = "/Plugins";
    public string GameScenesPath { get; set; } = "/GameScenes";

    /// <summary>
    /// Load a ConfigBase instance from Project Settings if available (falls back to defaults).
    /// </summary>
    public static ConfigBase FromProjectSettings()
    {
        var cfg = new ConfigBase();

        try
        {
            var prefix = "ascendere/paths/";
            cfg.ComponentsPath = ProjectSettings.HasSetting(prefix + "components")
                ? ProjectSettings.GetSetting(prefix + "components").AsString()
                : cfg.ComponentsPath;
            cfg.EntitiesPath = ProjectSettings.HasSetting(prefix + "entities")
                ? ProjectSettings.GetSetting(prefix + "entities").AsString()
                : cfg.EntitiesPath;
            cfg.SystemsPath = ProjectSettings.HasSetting(prefix + "systems")
                ? ProjectSettings.GetSetting(prefix + "systems").AsString()
                : cfg.SystemsPath;
            cfg.EventsPath = ProjectSettings.HasSetting(prefix + "events")
                ? ProjectSettings.GetSetting(prefix + "events").AsString()
                : cfg.EventsPath;
            cfg.ModulesPath = ProjectSettings.HasSetting(prefix + "modules")
                ? ProjectSettings.GetSetting(prefix + "modules").AsString()
                : cfg.ModulesPath;
            cfg.PluginsPath = ProjectSettings.HasSetting(prefix + "plugins")
                ? ProjectSettings.GetSetting(prefix + "plugins").AsString()
                : cfg.PluginsPath;
            cfg.GameScenesPath = ProjectSettings.HasSetting(prefix + "gamescenes")
                ? ProjectSettings.GetSetting(prefix + "gamescenes").AsString()
                : cfg.GameScenesPath;
        }
        catch
        {
            // Ignore and fallback to defaults
        }

        return cfg;
    }
}
