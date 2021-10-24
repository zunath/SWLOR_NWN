using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ChatCommandService;
using SWLOR.Game.Server.Service.DBService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    public class AdminChatCommand: IChatCommandListDefinition
    {
        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            var builder = new ChatCommandBuilder();

            AddDMCommand(builder);
            RemoveDMCommand(builder);
            GetDMsCommand(builder);

            return builder.Build();
        }

        private static void AddDMCommand(ChatCommandBuilder builder)
        {
            builder.Create("adddm")
                .Description("Adds a new DM. Arguments order: Name CDKey. Example: /adddm Zunath XXXXYYYY")
                .Permissions(AuthorizationLevel.Admin)
                .Validate((user, args) =>
                {
                    if (args.Length < 2)
                    {
                        return "Adding a DM requires a name and a CD key. Example: /adddm Zunath XXXXYYYY";
                    }

                    if (args.Length > 2)
                    {
                        return "Too many arguments specified. Names cannot have spaces.";
                    }

                    var name = args[0];
                    var cdKey = args[1].ToUpper();

                    if (name.Length > 255)
                    {
                        return "Names must be 255 characters or less.";
                    }

                    if (cdKey.Length != 8)
                    {
                        return "CD Keys must be exactly 8 characters.";
                    }

                    return string.Empty;
                })
                .Action((user, target, location, args) =>
                {
                    var name = args[0];
                    var cdKey = args[1].ToUpper();

                    var query = new DBQuery<AuthorizedDM>()
                        .AddFieldSearch(nameof(AuthorizedDM.CDKey), cdKey, false);

                    var existing = DB.Search(query).FirstOrDefault();

                    if (existing == null)
                    {
                        var dm = new AuthorizedDM
                        {
                            Authorization = AuthorizationLevel.DM,
                            CDKey = cdKey,
                            Name = name
                        };

                        DB.Set(cdKey, dm);
                        SendMessageToPC(user, $"DM '{name}' added under CD Key '{cdKey}'.");
                    }
                    else
                    {
                        SendMessageToPC(user, "This CD Key has already been added.");
                    }
                });
        }

        private static void RemoveDMCommand(ChatCommandBuilder builder)
        {
            builder.Create("removedm")
                .Description("Removes an existing DM by CD Key. Use /GetDMs to get the CD Key. Example: /RemoveDM XXXXYYYY. You cannot remove yourself.")
                .Permissions(AuthorizationLevel.Admin)
                .Validate((user, args) =>
                {
                    if (args.Length < 1)
                    {
                        return "Removing a DM requires an ID. Example: /RemoveDM XXXXYYYY";
                    }

                    if (args.Length > 1)
                    {
                        return "Too many arguments specified. Only provide an ID number. Example: /RemoveDM XXXXYYYY";
                    }

                    return string.Empty;
                })
                .Action((user, target, location, args) =>
                {
                    var cdKey = args[0];
                    var query = new DBQuery<AuthorizedDM>()
                        .AddFieldSearch(nameof(AuthorizedDM.CDKey), cdKey, false);
                    var record = DB.Search(query).FirstOrDefault();
                    var userCDKey = GetPCPublicCDKey(user);

                    if (record == null)
                    {
                        SendMessageToPC(user, "Unable to locate an authorized DM with CD Key #" + cdKey + ". Use the /GetDMs command to find a valid CD Key.");
                        return;
                    }

                    if (record.CDKey == userCDKey)
                    {
                        SendMessageToPC(user, "You cannot remove yourself from the authorized DM list.");
                        return;
                    }

                    for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
                    {
                        if (!GetIsDM(player)) continue;

                        if (GetPCPublicCDKey(player) == record.CDKey)
                        {
                            BootPC(player, "Your DM authorization has been revoked.");
                        }
                    }

                    DB.Delete<AuthorizedDM>(cdKey);
                    SendMessageToPC(user, $"DM {record.Name} has been removed.");
                });
        }

        private static void GetDMsCommand(ChatCommandBuilder builder)
        {
            builder.Create("getdms")
                .Description("Gets the list of DMs.")
                .Permissions(AuthorizationLevel.Admin)
                .Action((user, target, location, args) =>
                {
                    var query = new DBQuery<AuthorizedDM>()
                        .AddFieldSearch(nameof(AuthorizedDM.Authorization), $"{(int)AuthorizationLevel.DM}|{(int)AuthorizationLevel.Admin}", false);
                    var dmList = DB.Search(query);
                    var message = string.Empty;

                    foreach (var dm in dmList)
                    {
                        message += $"{dm.CDKey}: {dm.Name} ({dm.Authorization})\n";
                    }

                    SendMessageToPC(user, message);
                });
        }
    }
}
