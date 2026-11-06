using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using Player = SWLOR.Game.Server.Entity.Player;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of the skill catalog, XP awards and rank rules. The logic lives in <see cref="ISkillService"/>;
    /// the event handlers stay here and every member forwards to the container's instance.
    /// </summary>
    public static partial class Skill
    {
        /// <summary>
        /// Handles creating all of the mapping dictionaries used by the skill system on module load.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void LoadMappings()
        {
            Instance.LoadMappings();
        }

        /// <summary>
        /// Retrieves the skill type associated with a base item type.
        /// If no skill is associated with the item, SkillType.Invalid will be returned.
        /// </summary>
        /// <param name="baseItem">The type of base item to look for.</param>
        /// <returns>A skill type associated with the given base item type.</returns>
        public static SkillType GetSkillTypeByBaseItem(BaseItem baseItem)
        {
            return Instance.GetSkillTypeByBaseItem(baseItem);
        }

        /// <summary>
        /// Retrieves the skill an attack made with an item of this base type trains and rolls against.
        /// Arms-slot gloves and bracers are the engine's attack item for bare-handed attacks, so they
        /// resolve to Unarmed here while their stored requirement skill stays unchanged.
        /// </summary>
        public static SkillType GetAttackSkillTypeByBaseItem(BaseItem baseItem)
        {
            return Instance.GetAttackSkillTypeByBaseItem(baseItem);
        }

        /// <summary>Resolves equipped attack identity without rewriting a weapon's stored requirements.</summary>
        public static SkillType GetWeaponSkill(uint creature, uint weapon, SkillType? explicitOverride = null)
        {
            return Instance.GetWeaponSkill(creature, weapon, explicitOverride);
        }
    }
}
