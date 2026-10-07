using System;
using System.Numerics;
using Content.Client.Stylesheets;
using Content.Shared.CCVar;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.Client.CMU14.Interface;

/// <summary>
///     Scanlines drawn straight over whatever is beneath, with no render target involved - the half of
///     <see cref="CrtScreenControl"/> that works on live scrolling text, which the capturing pass does
///     not: it fails there by drawing a stale copy rather than by throwing. No roll bar, since an
///     overlay can only add pixels, never move them.
/// </summary>
public sealed class CrtScanlineOverlay : Control
{
    /// <summary>
    ///     Peak darkening at full intensity. Matches the coefficient the shader's scanline term uses,
    ///     so a surface wearing this and a surface wearing the full pass read as the same tube.
    /// </summary>
    private const float Darkening = 0.85f;

    [Dependency] private readonly IConfigurationManager _cfg = default!;

    /// <summary>Scanline depth; null falls through to the cvar.</summary>
    public float? Intensity { get; set; }

    public CrtScanlineOverlay()
    {
        IoCManager.InjectDependencies(this);

        MouseFilter = MouseFilterMode.Ignore;
        CanKeyboardFocus = false;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        // Checked here every frame rather than trusted to callers, for the same reason
        // CrtScreenControl checks it: this is a CRT-theme effect and must never reach the base UI.
        if (!StyleNano.CrtUiEnabled)
            return;

        if (_cfg.GetCVar(CCVars.CMUCrtEffectIntensity) <= 0f)
            return;

        var intensity = Intensity ?? _cfg.GetCVar(CCVars.CMUCrtEffectIntensity);

        var size = PixelSize;
        if (size.X <= 0 || size.Y <= 0)
            return;

        // Below 2 the line and the gap stop resolving separately and the whole thing reads as a flat
        // darkening - the same floor the shader's `pitch` carries.
        var pitch = MathF.Max(_cfg.GetCVar(CCVars.CMUCrtEffectPitch), 2f);
        var color = Color.Black.WithAlpha(Math.Clamp(Darkening * intensity, 0f, 1f));

        for (var y = 0f; y < size.Y; y += pitch)
        {
            handle.DrawRect(
                new UIBox2(0, y, size.X, y + 1),
                color);
        }
    }
}
