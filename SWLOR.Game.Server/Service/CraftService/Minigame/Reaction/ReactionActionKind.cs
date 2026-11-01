namespace SWLOR.Game.Server.Service.CraftService.Minigame.Reaction;

/// <summary>How an action's button reads: a normal action, one that finishes the item, one that can break the piece, or one that cannot be used.</summary>
public enum ReactionActionKind
{
    Normal = 1,
    FinishesItem = 2,
    BreaksItem = 3,
    Unavailable = 4
}
