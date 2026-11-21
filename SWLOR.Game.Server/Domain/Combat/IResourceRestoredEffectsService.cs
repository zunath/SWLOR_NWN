using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The riders restoring FP or Stamina grants (part 8 of the stat split; until then members of
    /// <c>ICombatResourcesService</c>). The pools reach it as Character's <see cref="IResourceRestoredListener"/>.
    /// </summary>
    public interface IResourceRestoredEffectsService : IResourceRestoredListener
    {
    }
}
