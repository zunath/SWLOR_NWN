using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// Who was guarded or deflected recently: the two tables behind <c>HasRecentGuardedHit</c> and <c>HasRecentDeflection</c>.
    /// It takes no services, so damage modifiers, damage taken and the ability definitions all read it plainly.
    /// </summary>
    public interface ICombatDefenseMemoryService : ICombatCreatureStateOwner
    {
        /// <summary>Remembers that the creature just took a guarded hit.</summary>
        void NoteGuardedHit(uint creature);

        /// <summary>Remembers that the creature just deflected an attack from the source.</summary>
        void NoteDeflection(uint creature, DeflectionSource source);

        bool HasRecentGuardedHit(uint creature, float windowSeconds);

        bool HasRecentDeflection(uint creature, DeflectionSource source, float windowSeconds);

        /// <summary>Forgets what the creature guarded or deflected.</summary>
        void ForgetDefenseMemory(uint creature);
    }
}
