namespace Content.Server.CMU14.Round.Antags.StrikeOrganizer;

/// <summary>
/// A colonist who signed a strike petition and joined the venture as an antagonist.
/// Back-references keep the petition scoped to its own strike.
/// </summary>
[RegisterComponent]
public sealed partial class StrikeParticipantComponent : Component
{
    public EntityUid Petition;
    public EntityUid? Organizer;
}
