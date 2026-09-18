using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    /// How many generations ordinary tile fires creep outward from where they were lit.
    /// Each generation spawns one adjacent fire that gets one less, so reach is depth - 1
    /// tiles. 0 disables creeping for fires that do not carry their own depth in YAML.
    /// </summary>
    public static readonly CVarDef<int> CMUFireSpreadDepth =
        CVarDef.Create("cmu.fire_spread_depth", 2, CVar.SERVERONLY);

    /// <summary>
    /// Client preference: show temperatures in Fahrenheit instead of Celsius.
    /// Display only, simulation always runs in Kelvin.
    /// </summary>
    public static readonly CVarDef<bool> CMUTemperatureFahrenheit =
        CVarDef.Create("cmu.temperature.fahrenheit", false, CVar.CLIENTONLY | CVar.ARCHIVE);
}
