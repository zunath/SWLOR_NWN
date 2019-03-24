using System;
using System.Linq;
using SWLOR.Game.Server.Conversation.Contracts;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWN.Events.Module;
using SWLOR.Game.Server.ValueObject.Dialog;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Service
{
    public static class DialogService
    {
        public const int NumberOfDialogs = 255;

        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnModuleRest>(message => OnModuleRest());
        }

        private static void StorePlayerDialog(Guid globalID, PlayerDialog dialog)
        {
            if (dialog.DialogNumber <= 0)
            {
                for (int x = 1; x <= NumberOfDialogs; x++)
                {
                    var existingDialog = AppCache.PlayerDialogs.SingleOrDefault(d => d.Value.DialogNumber == x);
                    if (!AppCache.DialogFilesInUse[x] || existingDialog.Value == null)
                    {
                        AppCache.DialogFilesInUse[x] = true;
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

            AppCache.PlayerDialogs[globalID] = dialog;
        }

        public static int NumberOfResponsesPerPage => 12;

        public static bool HasPlayerDialog(Guid globalID)
        {
            return AppCache.PlayerDialogs.ContainsKey(globalID);
        }

        public static PlayerDialog LoadPlayerDialog(Guid globalID)
        {
            if (globalID == null) throw new ArgumentException(nameof(globalID), nameof(globalID) + " cannot be null, empty, or whitespace.");
            if (!AppCache.PlayerDialogs.ContainsKey(globalID)) throw new Exception(nameof(globalID) + " '" + globalID + "' could not be found. Be sure to call " + nameof(LoadConversation) + " first.");

            return AppCache.PlayerDialogs[globalID];
        }

        public static void RemovePlayerDialog(Guid globalID)
        {
            if (globalID == null) throw new ArgumentException(nameof(globalID), nameof(globalID) + " cannot be null, empty, or whitespace.");

            PlayerDialog dialog = AppCache.PlayerDialogs[globalID];
            AppCache.DialogFilesInUse[dialog.DialogNumber] = false;

            AppCache.PlayerDialogs.Remove(globalID);
        }

        public static void LoadConversation(NWPlayer player, NWObject talkTo, string @class, int dialogNumber)
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

        public static void StartConversation(NWPlayer player, NWObject talkTo, string @class)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));
            if (player.Object == null) throw new ArgumentNullException(nameof(player.Object));
            if (talkTo == null) throw new ArgumentNullException(nameof(talkTo));
            if (talkTo.Object == null) throw new ArgumentNullException(nameof(talkTo.Object));
            if (string.IsNullOrWhiteSpace(@class)) throw new ArgumentException(nameof(@class), nameof(@class) + " cannot be null, empty, or whitespace.");

            LoadConversation(player, talkTo, @class, -1);
            PlayerDialog dialog = AppCache.PlayerDialogs[player.GlobalID];

            // NPC conversations
            
            if (_.GetObjectType(talkTo.Object) == _.OBJECT_TYPE_CREATURE &&
                !talkTo.IsPlayer &&
                !talkTo.IsDM)
            {
                _.BeginConversation("dialog" + dialog.DialogNumber, new Object());
            }
            // Everything else
            else
            {
                player.AssignCommand(() => _.ActionStartConversation(talkTo.Object, "dialog" + dialog.DialogNumber, _.TRUE, _.FALSE));
            }
        }

        public static void StartConversation(NWCreature player, NWObject talkTo, string @class)
        {
            StartConversation((NWPlayer) player, talkTo, @class);
        }

        public static void EndConversation(NWPlayer player)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));
            
            PlayerDialog playerDialog = LoadPlayerDialog(player.GlobalID);
            playerDialog.IsEnding = true;
            StorePlayerDialog(player.GlobalID, playerDialog);
        }

        private static void OnModuleRest()
        {
            NWPlayer player = (_.GetLastPCRested());
            int restType = _.GetLastRestEventType();

            if (restType != _.REST_EVENTTYPE_REST_STARTED ||
                !player.IsValid ||
                player.IsDM) return;

            player.AssignCommand(() => _.ClearAllActions());

            StartConversation(player, player, "RestMenu");
        }

        public static void OnActionsTaken(int nodeID)
        {
            NWPlayer player = (_.GetPCSpeaker());
            PlayerDialog dialog = LoadPlayerDialog(player.GlobalID);
            int selectionNumber = nodeID + 1;
            int responseID = nodeID + (NumberOfResponsesPerPage * dialog.PageOffset);

            if (selectionNumber == NumberOfResponsesPerPage + 1) // Next page
            {
                dialog.PageOffset = dialog.PageOffset + 1;
            }
            else if (selectionNumber == NumberOfResponsesPerPage + 2) // Previous page
            {
                dialog.PageOffset = dialog.PageOffset - 1;
            }
            else if (selectionNumber == NumberOfResponsesPerPage + 3) // Back
            {
                string currentPageName = dialog.CurrentPageName;
                var previous = dialog.NavigationStack.Pop();

                // This might be a little confusing but we're passing the active page as the "old page" to the Back() method.
                // This is because we need to run any dialog-specific clean up prior to moving the conversation backwards.
                App.ResolveByInterface<IConversation>("Conversation." + dialog.ActiveDialogName, convo =>
                {
                    convo.Back(player, currentPageName, previous.PageName);
                });

                // Previous page was in a different conversation. Switch to it.
                if (previous.DialogName != dialog.ActiveDialogName)
                {
                    LoadConversation(player, dialog.DialogTarget, previous.DialogName, dialog.DialogNumber);
                    dialog = LoadPlayerDialog(player.GlobalID);
                    dialog.ResetPage();

                    dialog.CurrentPageName = previous.PageName;
                    dialog.PageOffset = 0;

                    App.ResolveByInterface<IConversation>("Conversation." + dialog.ActiveDialogName, convo =>
                    {
                        convo.Initialize();
                        player.SetLocalInt("DIALOG_SYSTEM_INITIALIZE_RAN", 1);
                    });
                }
                // Otherwise it's in the same conversation. Switch to that.
                else
                {
                    dialog.CurrentPageName = previous.PageName;
                    dialog.PageOffset = 0;
                }
            }
            else if (selectionNumber != NumberOfResponsesPerPage + 4) // End
            {
                App.ResolveByInterface<IConversation>("Conversation." + dialog.ActiveDialogName, convo =>
                {
                    convo.DoAction(player, dialog.CurrentPageName, responseID + 1);
                });
            }
        }

        public static bool OnAppearsWhen(int nodeType, int nodeID)
        {
            NWPlayer player = (_.GetPCSpeaker());
            bool hasDialog = HasPlayerDialog(player.GlobalID);
            if (!hasDialog) return false;

            PlayerDialog dialog = LoadPlayerDialog(player.GlobalID);
            DialogPage page = dialog.CurrentPage;
            int currentSelectionNumber = nodeID + 1;
            bool displayNode = false;
            string newNodeText = string.Empty;
            int dialogOffset = (NumberOfResponsesPerPage + 1) * (dialog.DialogNumber - 1);

            if (currentSelectionNumber == NumberOfResponsesPerPage + 1) // Next page
            {
                int displayCount = page.NumberOfResponses - (NumberOfResponsesPerPage * dialog.PageOffset);

                if (displayCount > NumberOfResponsesPerPage)
                {
                    displayNode = true;
                }
            }
            else if (currentSelectionNumber == NumberOfResponsesPerPage + 2) // Previous Page
            {
                if (dialog.PageOffset > 0)
                {
                    displayNode = true;
                }
            }
            else if (currentSelectionNumber == NumberOfResponsesPerPage + 3) // Back
            {
                if (dialog.NavigationStack.Count > 0 && dialog.EnableBackButton)
                {
                    displayNode = true;
                }
            }
            else if (nodeType == 2)
            {
                int responseID = (dialog.PageOffset * NumberOfResponsesPerPage) + nodeID;
                if (responseID + 1 <= page.NumberOfResponses)
                {
                    DialogResponse response = page.Responses[responseID];

                    if (response != null)
                    {
                        newNodeText = response.Text;
                        displayNode = response.IsActive;
                    }
                }
            }
            else if (nodeType == 1)
            {
                return App.ResolveByInterface<IConversation, bool>("Conversation." + dialog.ActiveDialogName, convo =>
                {
                    if (player.GetLocalInt("DIALOG_SYSTEM_INITIALIZE_RAN") != 1)
                    {
                        convo.Initialize();
                        player.SetLocalInt("DIALOG_SYSTEM_INITIALIZE_RAN", 1);
                    }

                    if (dialog.IsEnding)
                    {
                        convo.EndDialog();
                        RemovePlayerDialog(player.GlobalID);
                        player.DeleteLocalInt("DIALOG_SYSTEM_INITIALIZE_RAN");
                        return false;
                    }

                    page = dialog.CurrentPage;
                    newNodeText = page.Header;

                    _.SetCustomToken(90000 + dialogOffset, newNodeText);
                    return true;
                });
            }

            _.SetCustomToken(90001 + nodeID + dialogOffset, newNodeText);
            return displayNode;
        }

        public static void OnDialogStart()
        {
            NWPlayer pc = (_.GetLastUsedBy());
            if (!pc.IsValid) pc = (_.GetPCSpeaker());

            string conversation = _.GetLocalString(Object.OBJECT_SELF, "CONVERSATION");

            if (!string.IsNullOrWhiteSpace(conversation))
            {
                int objectType = _.GetObjectType(Object.OBJECT_SELF);
                if (objectType == _.OBJECT_TYPE_PLACEABLE)
                {
                    NWPlaceable talkTo = (Object.OBJECT_SELF);
                    StartConversation(pc, talkTo, conversation);
                }
                else
                {
                    NWCreature talkTo = (Object.OBJECT_SELF);
                    StartConversation(pc, talkTo, conversation);
                }
            }
            else
            {
                _.ActionStartConversation(pc.Object, "", _.TRUE, _.FALSE);
            }
        }

        public static void OnDialogEnd()
        {
            NWPlayer player = (_.GetPCSpeaker());
            if (!HasPlayerDialog(player.GlobalID)) return;

            PlayerDialog dialog = LoadPlayerDialog(player.GlobalID);

            App.ResolveByInterface<IConversation>("Conversation." + dialog.ActiveDialogName, convo =>
            {
                convo.EndDialog();
                RemovePlayerDialog(player.GlobalID);
                player.DeleteLocalInt("DIALOG_SYSTEM_INITIALIZE_RAN");
            });
        }
    }
}
