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
    /// Spawning commands: items, gold and beast eggs.
    /// </summary>
    public class DMSpawnChatCommand: IChatCommandListDefinition
    {
        private readonly ChatCommandBuilder _builder = new ChatCommandBuilder();
        private static string[] _spawnBeastEggHelpMessages = Array.Empty<string>();

        private const int SpawnEggTypesPerHelpMessage = 20;


        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            CopyTargetItem();
            SpawnGold();
            SpawnItem();
            SpawnBeastEgg();

            return _builder.Build();
        }

        [BootPhase(BootReload.Rerun)]
        [NWNEventHandler(ScriptName.OnModuleCacheAfter)]
        public static void CacheSpawnBeastEggHelp()
        {
            var beastTypes = BeastMastery.GetAllBeastTypes()
                .OrderBy(type => type.ToString())
                .ToArray();
            var messages = new List<string>
            {
                "Usage: /spawnegg <beast type>",
                "Available beast types:"
            };

            for (var index = 0; index < beastTypes.Length; index += SpawnEggTypesPerHelpMessage)
            {
                messages.Add(string.Join(", ", beastTypes.Skip(index).Take(SpawnEggTypesPerHelpMessage)));
            }

            _spawnBeastEggHelpMessages = messages.ToArray();
        }

        private void CopyTargetItem()
        {
            _builder.Create("copyitem")
                .Description("Copies the targeted item.")
                .RequiresTarget()
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .AvailableToAllOnTestEnvironment()
                .Action((user, target, location, args) =>
                {
                    if (GetObjectType(target) != ObjectType.Item)
                    {
                        SendMessageToPC(user, "You can only copy items with this command.");
                        return;
                    }

                    CopyItem(target, user, true);
                    SendMessageToPC(user, "Item copied successfully.");
                });
        }

        private void SpawnGold()
        {
            _builder.Create("spawngold")
                .Description("Spawns gold of a specific quantity on your character. Example: /spawngold 33")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .AvailableToAllOnTestEnvironment()
                .Validate((user, args) =>
                {
                    if (args.Length <= 0)
                    {
                        return ColorToken.Red("Please specify a quantity. Example: /spawngold 34");
                    }
                    return string.Empty;
                })
                .Action((user, target, location, args) =>
                {
                    var quantity = 1;

                    if (args.Length >= 1)
                    {
                        if (!int.TryParse(args[0], out quantity))
                        {
                            return;
                        }
                    }

                    GiveGoldToCreature(user, quantity);
                });
        }

        private void SpawnItem()
        {
            _builder.Create("spawnitem")
                .Description("Spawns an item of a specific quantity on your character. Example: /spawnitem my_item 3")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .AvailableToAllOnTestEnvironment()
                .Validate((user, args) =>
                {
                    if (args.Length <= 0)
                    {
                        return ColorToken.Red("Please specify a resref and optionally a quantity. Example: /spawnitem my_resref 20");
                    }

                    return string.Empty;
                })
                .Action((user, target, location, args) =>
                {
                    var resref = args[0];
                    var quantity = 1;

                    if (args.Length > 1)
                    {
                        if (!int.TryParse(args[1], out quantity))
                        {
                            return;
                        }
                    }

                    var item = CreateItemOnObject(resref, user, quantity);

                    if (!GetIsObjectValid(item))
                    {
                        SendMessageToPC(user, ColorToken.Red("Item not found! Did you enter the correct ResRef?"));
                        return;
                    }

                    SetIdentified(item, true);
                });
        }

        private void SpawnBeastEgg()
        {
            _builder.Create("spawnegg")
                .Description("Spawns a beast egg by beast type. Use /spawnegg help to list available types.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .AvailableToAllOnTestEnvironment()
                .Validate((user, args) =>
                {
                    if (args.Length <= 0)
                    {
                        return ColorToken.Red("Please specify a beast type. Use /spawnegg help to list available types.");
                    }

                    if (args[0].Equals("help", StringComparison.OrdinalIgnoreCase))
                        return string.Empty;

                    if (!Enum.TryParse(args[0], true, out BeastType beastType) ||
                        !BeastMastery.GetAllBeastTypes().Contains(beastType))
                    {
                        return ColorToken.Red($"Unknown beast type '{args[0]}'. Use /spawnegg help to list available types.");
                    }

                    return string.Empty;
                })
                .Action((user, target, location, args) =>
                {
                    if (args[0].Equals("help", StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (var message in _spawnBeastEggHelpMessages)
                        {
                            SendMessageToPC(user, message);
                        }

                        return;
                    }

                    Enum.TryParse(args[0], true, out BeastType beastType);
                    var egg = BeastIncubation.CreateBeastEgg(beastType, user);
                    var beastDetail = BeastMastery.GetBeastDetail(beastType);

                    SendMessageToPC(user, $"Spawned '{GetName(egg)}' for beast type {beastType} ({beastDetail.Name}).");
                });
        }
    }
}
