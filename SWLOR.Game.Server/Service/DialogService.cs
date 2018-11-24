using System;
using System.Linq;
using SWLOR.Game.Server.Conversation.Contracts;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject.Dialog;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Service
{
    public class DialogService: IDialogService
    {
        private readonly INWScript _;
        private const int NumberOfDialogs = 255;
        private readonly AppCache _cache;

        public DialogService(INWScript script, AppCache cache)
        {
            _ = script;
            _cache = cache;
        }

        private void StorePlayerDialog(Guid globalID, PlayerDialog dialog)
        {
            // Reuse the existing player dialog if it exists.
            if (HasPlayerDialog(globalID))
            {
                _cache.PlayerDialogs[globalID] = dialog;
                return;
            }

            if (dialog.DialogNumber <= 0)
            {
                for (int x = 1; x <= NumberOfDialogs; x++)
                {
                    var existingDialog = _cache.PlayerDialogs.SingleOrDefault(d => d.Value.DialogNumber == x);
                    if (!_cache.DialogFilesInUse[x] || existingDialog.Value == null)
                    {
                        _cache.DialogFilesInUse[x] = true;
                        dialog.DialogNumber = x;
                        break;
                    }
                }
            }

            // Couldn't find an open dialog file. Throw error.
            if (dialog.DialogNumber <= 0)
            {
                Console.WriteLine("ERROR: Unable to locate a free dialog. Add more dialog files, update their custom tokens, and update DialogService.cs");
                return;
            }

            _cache.PlayerDialogs[globalID] = dialog;
        }

        public int NumberOfResponsesPerPage => 12;

        public bool HasPlayerDialog(Guid globalID)
        {
            return _cache.PlayerDialogs.ContainsKey(globalID);
        }

        public PlayerDialog LoadPlayerDialog(Guid globalID)
        {
            if (globalID == null) throw new ArgumentException(nameof(globalID), nameof(globalID) + " cannot be null, empty, or whitespace.");
            if (!_cache.PlayerDialogs.ContainsKey(globalID)) throw new Exception(nameof(globalID) + " '" + globalID + "' could not be found. Be sure to call " + nameof(LoadConversation) + " first.");

            return _cache.PlayerDialogs[globalID];
        }

        public void RemovePlayerDialog(Guid globalID)
        {
            if (globalID == null) throw new ArgumentException(nameof(globalID), nameof(globalID) + " cannot be null, empty, or whitespace.");

            PlayerDialog dialog = _cache.PlayerDialogs[globalID];
            _cache.DialogFilesInUse[dialog.DialogNumber] = false;

            _cache.PlayerDialogs.Remove(globalID);
        }

        public void LoadConversation(NWPlayer player, NWObject talkTo, string @class, int dialogNumber)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));
            if (player.Object == null) throw new ArgumentNullException(nameof(player.Object));
            if (talkTo == null) throw new ArgumentNullException(nameof(talkTo));
            if (talkTo.Object == null) throw new ArgumentNullException(nameof(talkTo.Object));
            if (string.IsNullOrWhiteSpace(@class)) throw new ArgumentException(nameof(@class), nameof(@class) + " cannot be null, empty, or whitespace.");
            if (dialogNumber != -1 && (dialogNumber < 1 || dialogNumber > NumberOfDialogs)) throw new ArgumentOutOfRangeException(nameof(dialogNumber), nameof(dialogNumber) + " must be between 1 and " + NumberOfDialogs);

            App.ResolveByInterface<IConversation>("Conversation." + @class, (convo) =>
            {
                PlayerDialog dialog = convo.SetUp(player);

                if (dialog == null)
                {
                    throw new NullReferenceException(nameof(dialog) + " cannot be null.");
                }

                if (dialogNumber > 0)
                    dialog.DialogNumber = dialogNumber;

                dialog.ActiveDialogName = @class;
                dialog.DialogTarget = talkTo;
                StorePlayerDialog(player.GlobalID, dialog);
            });
        }

        public void StartConversation(NWPlayer player, NWObject talkTo, string @class)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));
            if (player.Object == null) throw new ArgumentNullException(nameof(player.Object));
            if (talkTo == null) throw new ArgumentNullException(nameof(talkTo));
            if (talkTo.Object == null) throw new ArgumentNullException(nameof(talkTo.Object));
            if (string.IsNullOrWhiteSpace(@class)) throw new ArgumentException(nameof(@class), nameof(@class) + " cannot be null, empty, or whitespace.");

            LoadConversation(player, talkTo, @class, -1);
            PlayerDialog dialog = _cache.PlayerDialogs[player.GlobalID];

            // NPC conversations
            
            if (_.GetObjectType(talkTo.Object) == NWScript.OBJECT_TYPE_CREATURE &&
                !talkTo.IsPlayer &&
                !talkTo.IsDM)
            {
                _.BeginConversation("dialog" + dialog.DialogNumber, new Object());
            }
            // Everything else
            else
            {
                player.AssignCommand(() => _.ActionStartConversation(talkTo.Object, "dialog" + dialog.DialogNumber, NWScript.TRUE, NWScript.FALSE));
            }
        }

        public void StartConversation(NWCreature player, NWObject talkTo, string @class)
        {
            StartConversation((NWPlayer) player, talkTo, @class);
        }

        public void EndConversation(NWPlayer player)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));
            
            PlayerDialog playerDialog = LoadPlayerDialog(player.GlobalID);
            playerDialog.IsEnding = true;
            StorePlayerDialog(player.GlobalID, playerDialog);
        }

    }
}
