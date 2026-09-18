using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    /// Client preference: show temperatures in Fahrenheit instead of Celsius.
    /// Display only, simulation always runs in Kelvin.
    /// </summary>
    public static readonly CVarDef<bool> CMUTemperatureFahrenheit =
        CVarDef.Create("cmu.temperature.fahrenheit", false, CVar.CLIENTONLY | CVar.ARCHIVE);
}
