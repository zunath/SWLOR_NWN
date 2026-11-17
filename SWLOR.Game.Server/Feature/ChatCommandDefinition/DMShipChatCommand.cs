using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Async;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Domain.Gameplay;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Quests;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.World;
using ChatChannel = SWLOR.Game.Server.Core.NWNX.Enum.ChatChannel;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Composition;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    /// <summary>
    /// Ship commands: reading a ship's stats and repairing it.
    /// </summary>
    public class DMShipChatCommand: IChatCommandListDefinition
    {
        private readonly ChatCommandBuilder _builder = new ChatCommandBuilder();

        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            ShipStats();
            RepairShip();

            return _builder.Build();
        }

        private void ShipStats()
        {
            _builder.Create("shipstats")
                .Description("Provides a readout of a given ship's stats.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .AvailableToAllOnTestEnvironment()
                .RequiresTarget()
                .Action((user, target, location, args) =>
                {
                    if (GetIsDM(target) != true && GetIsDMPossessed(target) != true)
                    {
                        if (Space.GetShipStatus(target) != null)
                        {
                            var npcStats = Stat.GetNPCStats(target);
                            var level = npcStats.Level;
                            if (GetIsPC(target) == true)
                            {
                                var playerId = GetObjectUUID(target);
                                var dbPlayer = DB.Get<Player>(playerId);
                                level = dbPlayer.Skills[SWLOR.Game.Server.Domain.Foundation.SkillType.Piloting].Rank;
                            }
                            var targetName = GetName(target);
                            var targetStatus = Space.GetShipStatus(target);
                            SendMessageToPC(user, $"{targetName} stats: \n" +
                                $"Armor: {targetStatus.Hull} Max: {targetStatus.MaxHull} \n" +
                                $"Shields: {targetStatus.Shield} Max: {targetStatus.MaxShield} \n" +
                                $"Capacitor: {targetStatus.Capacitor} Max: {targetStatus.MaxCapacitor} \n" +
                                $"Shield Regen: {targetStatus.ShieldRechargeRate} \n" +
                                $"EM Damage Bonus: {targetStatus.EMDamage} \n" +
                                $"Thermal Damage Bonus: {targetStatus.ThermalDamage} \n" +
                                $"Explosive Damage Bonus: {targetStatus.ExplosiveDamage} \n" +
                                $"Accuracy: {targetStatus.Accuracy} \n" +
                                $"Evasion: {targetStatus.Evasion} \n" +
                                $"Thermal Defense: {targetStatus.ThermalDefense} \n" +
                                $"EM Defense: {targetStatus.EMDefense} \n" +
                                $"Explosive Defense: {targetStatus.ExplosiveDefense} \n" +
                                $"Skill Level: {level}");
                        }
                    }
                });
        }

        private void RepairShip()
        {
            _builder.Create("repairship")
                .Description("Restores a ship's armor, shield and capacitor by the indicated amount.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .AvailableToAllOnTestEnvironment()
                .RequiresTarget()
                .Action((user, target, location, args) =>
                {
                    if (Space.GetShipStatus(target) != null && !GetIsDM(target) && !GetIsDMPossessed(target))
                    {
                        var targetStatus = Space.GetShipStatus(target);

                        if (args.Length <= 0)
                        {
                            targetStatus.Capacitor = targetStatus.MaxCapacitor;
                            targetStatus.Hull = targetStatus.MaxHull;
                            targetStatus.Shield = targetStatus.MaxShield;
                        }
                        else if (int.TryParse(args[0], out var amount))
                        {
                            Space.RestoreHull(target, Space.GetShipStatus(target), amount);
                            Space.RestoreShield(target, Space.GetShipStatus(target), amount);
                            Space.RestoreCapacitor(target, Space.GetShipStatus(target), amount);
                        }

                        if (GetIsPC(target))
                        {
                            var targetPlayerId = GetObjectUUID(target);
                            var dbTargetPlayer = DB.Get<Player>(targetPlayerId);
                            var dbPlayerShip = DB.Get<PlayerShip>(dbTargetPlayer.ActiveShipId);

                            dbPlayerShip.Status.Shield = targetStatus.Shield;
                            dbPlayerShip.Status.Hull = targetStatus.Hull;

                            DB.Set(dbPlayerShip);

                            // Trigger UI refresh events after database update
                            ExecuteScript(ScriptName.OnPlayerShieldAdjusted, target);
                            ExecuteScript(ScriptName.OnPlayerHullAdjusted, target);
                            ExecuteScript(ScriptName.OnPlayerCapAdjusted, target);
                        }
                    }
                });
        }
    }
}
