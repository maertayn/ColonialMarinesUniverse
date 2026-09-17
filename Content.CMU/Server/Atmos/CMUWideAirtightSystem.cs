using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.CMU14.Atmos;

public sealed class CMUWideAirtightSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CMUWideAirtightComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CMUWideAirtightComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnMapInit(Entity<CMUWideAirtightComponent> ent, ref MapInitEvent args)
    {
        var xform = Transform(ent);

        if (!xform.Anchored
            || !TryComp(xform.GridUid, out MapGridComponent? grid))
            return;

        var tile = _transform.GetGridTilePositionOrDefault((ent, xform), grid);

        foreach (var offset in ent.Comp.Offsets)
        {
            var rotated = xform.LocalRotation.RotateVec(offset);
            var offsetTile = new Vector2i((int) MathF.Round(rotated.X), (int) MathF.Round(rotated.Y));
            var seal = Spawn(ent.Comp.Companion, new EntityCoordinates(ent, offsetTile));

            // Anchoring re-parents the seal to the grid, so the door cannot carry it; track instead.
            if (!_transform.AnchorEntity((seal, Transform(seal)), (xform.GridUid.Value, grid), tile + offsetTile))
            {
                QueueDel(seal);
                continue;
            }

            ent.Comp.Seals.Add(seal);
        }
    }

    private void OnShutdown(Entity<CMUWideAirtightComponent> ent, ref ComponentShutdown args)
    {
        foreach (var seal in ent.Comp.Seals)
            QueueDel(seal);
    }
}
