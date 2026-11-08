using Content.Server.Administration.Logs;
using Content.Server.Antag;
using Content.Server.CMU14.Systems;
using Content.Server.CMU14.Round.Antags.ColonyBounty;
using Content.Server.GameTicking.Rules;
using Content.Shared.CMU14.Round.Antags.StrikeOrganizer;
using Content.Shared.Database;
using Content.Shared.Dataset;
using Content.Shared.GameTicking.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.CMU14.Round.Antags.StrikeOrganizer;

public sealed partial class StrikeOrganizerRuleSystem : GameRuleSystem<StrikeOrganizerRuleComponent>
{
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private AntagSelectionSystem _antag = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private WantedSystem _wantedSystem = default!;

    private static readonly StrikeStance[] Stances =
    {
        StrikeStance.Aligned,
        StrikeStance.Neutral,
        StrikeStance.Opposed,
    };

    public static readonly ProtoId<DatasetPrototype> GrievanceDataset = "CMUStrikeGrievances";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StrikeOrganizerComponent, ComponentStartup>(OnStrikeOrganizerSpawned);
    }

    protected override void Started(EntityUid uid, StrikeOrganizerRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        // Roll before base.Started: organizer gear and the petition spawn during selection,
        // and everything reads this venture from the rule
        var roll = RollVenture(_random, _proto);
        component.Grievance = roll.Grievance;
        component.ClfStance = roll.Clf;
        component.GovforStance = roll.Govfor;
        component.VentureRolled = true;

        _adminLog.Add(LogType.AntagSelection, LogImpact.Medium,
            $"Strike venture rolled: grievance '{roll.Grievance}', CLF {roll.Clf}, GOVFOR {roll.Govfor}");

        base.Started(uid, component, gameRule, args);
    }

    public static (string Grievance, StrikeStance Clf, StrikeStance Govfor) RollVenture(
        IRobustRandom random, IPrototypeManager proto)
        => (random.Pick(proto.Index(GrievanceDataset).Values), random.Pick(Stances), random.Pick(Stances));

    private void OnStrikeOrganizerSpawned(EntityUid uid, StrikeOrganizerComponent component, ComponentStartup args)
    {
        _wantedSystem.SendPaperToGroup(ColonyCmbFax.MarshalBureauFaxGroup, "AUPaperStrikeOrganizer");

        if (ResolveVenture() is not { } venture)
            return;

        // The venture owns its organizer so petitions, faxes, and succession survive any single paper
        venture.Organizer ??= uid;

        var (grievance, clf, govfor) = StrikeVentureText.Compose(Loc, venture.Grievance, venture.ClfStance, venture.GovforStance);
        _antag.SendBriefing(uid,
            Loc.GetString("cmu-strike-organizer-venture",
                ("grievance", grievance), ("clfStance", clf), ("govforStance", govfor)),
            venture.BriefingColor, venture.BriefingSound);
    }

    private StrikeOrganizerRuleComponent? ResolveVenture()
    {
        StrikeOrganizerRuleComponent? found = null;
        var query = EntityQueryEnumerator<StrikeOrganizerRuleComponent>();
        while (query.MoveNext(out _, out var rule))
        {
            if (rule.VentureRolled)
            {
                found = rule;
                break;
            }
        }

        return found;
    }
}
