using NRediSearch.QueryBuilder;
using NWN.Core.NWNX;
using SWLOR.Component.Character.Contracts;
using SWLOR.NWN.API.Contracts;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Entities;

namespace SWLOR.Component.Character.Service
{
    internal class StatApplicationService : IStatApplicationService
    {
        private readonly ICreaturePluginService _creaturePlugin;
        private readonly IStatCalculationService _statCalculation;
        private readonly ICharacterResourceService _characterResourceService;
        private readonly IObjectPluginService _objectPlugin;

        public StatApplicationService(
            ICreaturePluginService creaturePlugin,
            IStatCalculationService statCalculation,
            ICharacterResourceService characterResourceService,
            IObjectPluginService objectPlugin)
        {
            _creaturePlugin = creaturePlugin;
            _statCalculation = statCalculation;
            _characterResourceService = characterResourceService;
            _objectPlugin = objectPlugin;
        }

        public void ApplyCharacterMaxHP(uint creature)
        {
            if (GetIsPC(creature))
            {
                ApplyPlayerMaxHP(creature);
            }
            else
            {
                ApplyCreatureMaxHP(creature);
            }
        }

        private void ApplyPlayerMaxHP(uint player)
        {
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var maxHP = _statCalculation.CalculateMaxHP(player);

            // Apply the HP to the NWN creature using level-based distribution
            const int MaxHPPerLevel = 254; // NWN allows max 255 HP per level (255 = 254 + 1 for base)
            var nwnLevelCount = GetLevelByPosition(1, player) +
                                GetLevelByPosition(2, player) +
                                GetLevelByPosition(3, player);

            // Reserve 1 HP for each level to ensure minimum HP requirements
            for (var nwnLevel = 1; nwnLevel <= nwnLevelCount; nwnLevel++)
            {
                maxHP--;
                _creaturePlugin.SetMaxHitPointsByLevel(player, nwnLevel, 1);
            }

            // Distribute remaining HP across levels, respecting the 255 HP per level cap
            if (maxHP > 0)
            {
                for (var nwnLevel = 1; nwnLevel <= nwnLevelCount; nwnLevel++)
                {
                    if (maxHP > MaxHPPerLevel) // Fill level to maximum capacity
                    {
                        _creaturePlugin.SetMaxHitPointsByLevel(player, nwnLevel, 255);
                        maxHP -= 254; // Subtract 254 since we already reserved 1
                    }
                    else // Assign remaining HP to this level
                    {
                        _creaturePlugin.SetMaxHitPointsByLevel(player, nwnLevel, maxHP + 1);
                        break;
                    }
                }
            }

            // If player's current HP is higher than the newly applied max, reduce it
            var currentHP = _characterResourceService.GetCurrentHP(player);
            maxHP = GetMaxHitPoints(player);
            if (currentHP > maxHP)
            {
                SetCurrentHitPoints(player, maxHP);
            }
        }

        private void ApplyCreatureMaxHP(uint creature)
        {
            var maxHP = _statCalculation.CalculateMaxHP(creature);
            _objectPlugin.SetMaxHitPoints(creature, maxHP);
        }
    }
}
