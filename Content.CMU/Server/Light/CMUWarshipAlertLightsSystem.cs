using Content.Server._RMC14.Light;
using Content.Shared._RMC14.AlertLevel;
using Content.Shared.CMU14.Marines;
using Content.Shared.CMU14.ZLevels.Core.EntitySystems;
using Content.Shared.Light.Components;
using Robust.Server.GameObjects;

namespace Content.Server.CMU14.Light;

/// <summary>
///     Tints warship light fixtures to the RMC alert level's color.
///     Room fixtures keep their own power behavior and only the point light color is touched.
/// </summary>
public sealed partial class CMUWarshipAlertLightsSystem : EntitySystem
{
    // Alert palette. Green alert restores each fixture's original color.
    private static readonly Dictionary<RMCAlertLevels, Color> AlertColors = new()
    {
        [RMCAlertLevels.Blue] = Color.FromHex("#1150FF"),
        [RMCAlertLevels.Red] = Color.FromHex("#EE0500"),
        // [RMCAlertLevels.Yellow] = Color.FromHex("#FFFF00"), // CMU14 TODO
        [RMCAlertLevels.Delta] = Color.FromHex("#000000"), // no lights
    };

    [Dependency] private PointLightSystem _pointLight = default!;
    [Dependency] private RMCAlertLevelSystem _alertLevel = default!;
    [Dependency] private CMUSharedZLevelsSystem _zLevels = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PoweredLightComponent, ComponentStartup>(OnLightStartup);
        SubscribeLocalEvent<RMCBreakLightOnAttackComponent, MapInitEvent>(OnRmcLightMapInit);
        SubscribeLocalEvent<RMCAlertLevelChangedEvent>(OnAlertChanged);
    }

    private void OnLightStartup(Entity<PoweredLightComponent> ent, ref ComponentStartup args)
        => ApplyAlertColor(ent);

    private void OnRmcLightMapInit(Entity<RMCBreakLightOnAttackComponent> ent, ref MapInitEvent args)
        => ApplyAlertColor(ent);

    private void OnAlertChanged(ref RMCAlertLevelChangedEvent args)
    {
        ApplyToWarshipFixtures<PoweredLightComponent>(args.Level, args.Ship);
        ApplyToWarshipFixtures<RMCBreakLightOnAttackComponent>(args.Level, args.Ship);
    }

    private void ApplyToWarshipFixtures<T>(RMCAlertLevels level, EntityUid? ship) where T : IComponent
    {
        var query = EntityQueryEnumerator<T, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (!IsOnWarship(xform))
                continue;

            // A ship filter only matters when more than one warship exists, then match its whole z-network
            if (ship != null && !_zLevels.IsSameZNetwork(xform.MapUid, ship.Value))
                continue;

            ApplyAlertColor(uid, level);
        }
    }

    private void ApplyAlertColor(EntityUid uid)
    {
        var xform = Transform(uid);
        if (!IsOnWarship(xform))
            return;

        ApplyAlertColor(uid, _alertLevel.Get(xform.MapUid));
    }

    private void ApplyAlertColor(EntityUid uid, RMCAlertLevels? level)
    {
        // PointLightComponent is the registered concrete type; TryComp on the abstract base throws in Debug builds
        if (!TryComp(uid, out PointLightComponent? light))
            return;

        if (level is { } lvl && AlertColors.TryGetValue(lvl, out var color))
        {
            var marker = EnsureComp<CMUWarshipAlertLightComponent>(uid);
            marker.Original ??= light.Color;
            _pointLight.SetColor(uid, color, light);
        }
        else if (TryComp<CMUWarshipAlertLightComponent>(uid, out var marker) && marker.Original is { } original)
        {
            _pointLight.SetColor(uid, original, light);
        }
    }

    private bool IsOnWarship(TransformComponent xform)
    {
        // Grid fallback is display-only; alert-level reads resolve through the map entity.
        if (HasComp<WarshipComponent>(xform.MapUid)
            || (xform.GridUid is { } grid && HasComp<WarshipComponent>(grid)))
            return true;

        // Multi-z warships carry the alert level on the primary deck map, other decks resolve through it
        return xform.MapUid is { } map
            && _zLevels.TryGetZNetwork(map, out var network)
            && _zLevels.TryGetMapAtDepth(network.Value, 0, out var primary)
            && HasComp<RMCAlertLevelComponent>(primary);
    }
}
