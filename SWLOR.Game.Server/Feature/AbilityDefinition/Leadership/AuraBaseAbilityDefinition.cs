using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Leadership
{
    public abstract class AuraBaseAbilityDefinition
    {
        private const int MaxNumberOfAuras = 4;

        private class PlayerAuraDetail
        {
            public StatusEffectType Type { get; set; }
            public bool TargetsSelf { get; set; }
            public bool TargetsParty { get; set; }
            public bool TargetsNPCs { get; set; }

            public PlayerAuraDetail(StatusEffectType type, bool targetsSelf, bool targetsParty, bool targetsNPCs)
            {
                Type = type;
                TargetsSelf = targetsSelf;
                TargetsParty = targetsParty;
                TargetsNPCs = targetsNPCs;
            }
        }

        private class PlayerAura
        {
            public List<PlayerAuraDetail> Auras { get; set; }
            public List<uint> PartyMembersInRange { get; set; }
            public List<uint> CreaturesInRange { get; set; }

            public PlayerAura()
            {
                Auras = new List<PlayerAuraDetail>();
                PartyMembersInRange = new List<uint>();
                CreaturesInRange = new List<uint>();
            }
        }

        private static readonly Dictionary<uint, PlayerAura> _playerAuras = new();

        protected bool OnAuraActivation(uint activator, StatusEffectType type)
        {
            if (!_playerAuras.ContainsKey(activator))
                return true;

            // Aura is active and player wants to deactivate it.
            // Remove it from the list and send a notification message.
            var aura = _playerAuras[activator];
            var existing = aura.Auras.FirstOrDefault(x => x.Type == type);
            if (existing != null)
            {
                var statusEffect = StatusEffect.GetDetail(type);

                SendMessageToPC(activator, ColorToken.Red( $"Aura '{statusEffect.Name}' deactivated."));

                if (existing.TargetsSelf)
                {
                    StatusEffect.Remove(activator, type, false);
                }

                if (existing.TargetsParty)
                {
                    foreach (var member in aura.PartyMembersInRange)
                    {
                        StatusEffect.Remove(member, type, false);
                    }
                }

                if (existing.TargetsNPCs)
                {
                    foreach (var npc in aura.CreaturesInRange)
                    {
                        StatusEffect.Remove(npc, type, false);
                    }
                }

                _playerAuras[activator].Auras.Remove(existing);
                return false;
            }

            return true;
        }

        /// <summary>
        /// When a player enters the server, apply the Aura AOE effect.
        /// </summary>
        [NWNEventHandler("mod_enter")]
        public static void ApplyAuraAOE()
        {
            var player = GetEnteringObject();

            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            RemoveEffectByTag(player, "AURA_EFFECT");

            AssignCommand(player, () =>
            {
                var effect = SupernaturalEffect(EffectAreaOfEffect(AreaOfEffect.CustomAoe, "aura_enter", string.Empty, "aura_exit"));
                effect = TagEffect(effect, "AURA_EFFECT");
                ApplyEffectToObject(DurationType.Permanent, effect, player);
            });
        }

        /// <summary>
        /// Whenever a creature enters the aura, add them to the cache.
        /// </summary>
        [NWNEventHandler("aura_enter")]
        public static void AuraEnter()
        {
            var entering = GetEnteringObject();
            var self = GetAreaOfEffectCreator(OBJECT_SELF);

            if(!_playerAuras.ContainsKey(self))
                _playerAuras.Add(self, new PlayerAura());

            // Party Members
            if (GetIsPC(entering) && !GetIsDM(entering) && !GetIsDMPossessed(entering) && GetFactionEqual(self, entering))
            {
                if (_playerAuras[self].PartyMembersInRange.Contains(entering))
                    return;

                _playerAuras[self].PartyMembersInRange.Add(entering);

                foreach (var detail in _playerAuras[self].Auras)
                {
                    if (detail.TargetsParty)
                    {
                        StatusEffect.Apply(self, entering, detail.Type, 0f, self);
                    }
                }
            }

            // NPCs
            else if (!GetIsPC(entering) && !GetIsDM(entering))
            {
                if (_playerAuras[self].CreaturesInRange.Contains(entering))
                    return;

                _playerAuras[self].CreaturesInRange.Add(entering);
                
                foreach (var detail in _playerAuras[self].Auras)
                {
                    if (detail.TargetsNPCs)
                    {
                        StatusEffect.Apply(self, entering, detail.Type, 0f, self);
                    }
                }
            }
        }

        /// <summary>
        /// Whenever a creature exits the aura, remove it from the cache.
        /// </summary>
        [NWNEventHandler("aura_exit")]
        public static void AuraExit()
        {
            var exiting = GetExitingObject();
            var self = GetAreaOfEffectCreator(OBJECT_SELF);

            if (!_playerAuras.ContainsKey(self))
                _playerAuras.Add(self, new PlayerAura());

            if (GetIsPC(exiting) && !GetIsDM(exiting) && !GetIsDMPossessed(exiting))
            {
                if (!_playerAuras[self].PartyMembersInRange.Contains(exiting))
                    return;

                _playerAuras[self].PartyMembersInRange.Remove(exiting);

                foreach (var detail in _playerAuras[self].Auras)
                {
                    if (detail.TargetsParty)
                    {
                        StatusEffect.Remove(exiting, detail.Type, false);
                    }
                }
            }

            else if (!GetIsPC(exiting) && !GetIsDM(exiting))
            {
                if (!_playerAuras[self].CreaturesInRange.Contains(exiting))
                    return;

                _playerAuras[self].CreaturesInRange.Remove(exiting);

                foreach (var detail in _playerAuras[self].Auras)
                {
                    if (detail.TargetsNPCs)
                    {
                        StatusEffect.Remove(exiting, detail.Type, false);
                    }
                }
            }
        }

        /// <summary>
        /// Whenever a weapon's OnHit event is fired, add a Leadership combat point if an Aura is active.
        /// </summary>
        [NWNEventHandler("item_on_hit")]
        public static void AddLeadershipCombatPoint()
        {
            var player = OBJECT_SELF;
            var target = GetSpellTargetObject();
            if (!GetIsPC(player) || GetIsDM(player) || !GetIsObjectValid(player))
                return;

            if (GetIsPC(target) || GetIsDM(target))
                return;

            if (!_playerAuras.ContainsKey(player))
                return;

            var aura = _playerAuras[player];

            if (aura.Auras.Count <= 0)
                return;

            CombatPoint.AddCombatPoint(player, target, SkillType.Leadership);
        }

        private int GetMaxNumberOfAuras(uint activator)
        {
            var social = GetAbilityScore(activator, AbilityType.Social);
            var count = 1 + (social - 10) / 5;

            if (count > MaxNumberOfAuras)
                count = MaxNumberOfAuras;

            return count;
        }

        protected void ApplyAura(uint activator, StatusEffectType type, bool targetsSelf, bool targetsParty, bool targetsNPCs)
        {
            if(!_playerAuras.ContainsKey(activator))
                _playerAuras.Add(activator, new PlayerAura());

            var aura = _playerAuras[activator];

            // Safety check - ensure the same aura never enters the cache more than once.
            if(aura.Auras.Exists(x => x.Type == type))
                return;

            var maxAuras = GetMaxNumberOfAuras(activator);
            var detail = StatusEffect.GetDetail(type);

            while (aura.Auras.Count >= maxAuras)
            {
                var removeType = aura.Auras[0].Type;
                if (aura.Auras[0].TargetsSelf)
                {
                    StatusEffect.Remove(activator, removeType, false);
                }

                if (aura.Auras[0].TargetsParty)
                {
                    foreach (var member in aura.PartyMembersInRange)
                    {
                        StatusEffect.Remove(member, removeType, false);
                    }
                }

                if (aura.Auras[0].TargetsNPCs)
                {
                    foreach (var npc in aura.CreaturesInRange)
                    {
                        StatusEffect.Remove(npc, removeType, false);
                    }
                }

                aura.Auras.RemoveAt(0);
            }

            aura.Auras.Add(new PlayerAuraDetail(type, targetsSelf, targetsParty, targetsNPCs));

            if (targetsSelf)
            {
                StatusEffect.Apply(activator, activator, type, 0f, activator);
            }

            SendMessageToPC(activator, ColorToken.Green($"Aura '{detail.Name}' activated."));
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Fnf_Sound_Burst), activator);
        }
    }
}
