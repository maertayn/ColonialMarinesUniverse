using Content.Shared._RMC14.Dialog;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.CMU14.Round.Antags.StrikeOrganizer;

/// <summary>
/// Raised on the signer when they confirm the strike petition dialog.
/// </summary>
[Serializable, NetSerializable]
public sealed class StrikeSignAcceptEvent : EntityEventArgs { }

/// <summary>
/// DoAfter event for the petition signing process.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class StrikeSignDoAfterEvent : SimpleDoAfterEvent { }

/// <summary>
/// Raised when the organizer uses their redraft action to print a fresh petition.
/// Not marked NetSerializable: action events inherit BaseActionEvent's raw EntityUid
/// fields, which must never enter the serializer graph.
/// </summary>
public sealed partial class StrikeRedraftActionEvent : InstantActionEvent { }

/// <summary>
/// Raised when the pack's transmit action opens the communique input.
/// Not marked NetSerializable: action events inherit BaseActionEvent's raw EntityUid
/// fields, which must never enter the serializer graph.
/// </summary>
public sealed partial class StrikeFaxTransmitActionEvent : InstantActionEvent { }

/// <summary>
/// Raised on the transmitting player when they submit communique text from the pack's input.
/// </summary>
[Serializable, NetSerializable]
public sealed record StrikeFaxSendEvent(string Message = "") : DialogInputEvent(Message);

/// <summary>
/// Temporary component placed on a colonist who was offered a petition to sign.
/// Stores the petition for the accept handler. Always re-offerable: the dialog can
/// outlive the offer invisibly, so a pending offer must never hard-block.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StrikeSignPendingComponent : Component
{
    public EntityUid Petition;
}
