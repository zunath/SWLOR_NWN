using SWLOR.Game.Server.Domain.Administration;

namespace SWLOR.Game.Server.Domain.Creatures
{
    /// <summary>
    /// The DM spawn preparation the DM blueprint catalog asks for (<see cref="IDMSpawnPreparation"/>, owned by Administration;
    /// P5-15a of Readmes/MaintainabilityRefactorPhase5.md): the call <see cref="ISpawnService.PrepareDMSpawnedCreature"/> makes.
    /// </summary>
    public class DMSpawnPreparation : IDMSpawnPreparation
    {
        private readonly ISpawnService _spawn;

        public DMSpawnPreparation(ISpawnService spawn)
        {
            _spawn = spawn;
        }

        public void PrepareDMSpawnedCreature(uint creature) => _spawn.PrepareDMSpawnedCreature(creature);
    }
}
