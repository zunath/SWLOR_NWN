using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Legacy.Conversation.Contracts;
using SWLOR.Game.Server.Legacy.Event.Module;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Messaging;
using SWLOR.Game.Server.Legacy.ValueObject.Dialog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using Profiler = SWLOR.Game.Server.Legacy.ValueObject.Profiler;

namespace SWLOR.Game.Server.Legacy.Service
{
    public static class DialogService
    {
        private static readonly Dictionary<string, IConversation> _conversations;
        public const int NumberOfDialogs = 255;

        static DialogService()
        {
            _conversations = new Dictionary<string, IConversation>();
        }

        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnModuleRest>(message => OnModuleRest());
            MessageHub.Instance.Subscribe<OnModuleLoad>(message => LoadModule());
        }

        private static void LoadModule()
        {
            RegisterConversations();
        }

        private static void RegisterConversations()
        {
            // Use reflection to get all of the conversation implementations.
            var classes = Assembly.GetCallingAssembly().GetTypes()
                .Where(p => typeof(IConversation).IsAssignableFrom(p) && p.IsClass && !p.IsAbstract).ToArray();
            foreach (var type in classes)
            {
                var instance = Activator.CreateInstance(type) as IConversation;
                if (instance == null)
                {
                    throw new NullReferenceException("Unable to activate instance of type: " + type);
                }
                _conversations.Add(type.Name, instance);
            }
        }

        public static IConversation GetConversation(string key)
        {
            if (!_conversations.ContainsKey(key))
            {
                throw new KeyNotFoundException("Conversation '" + key + "' is not registered. Did you create a class for it?");
            }

            return _conversations[key];
        }

        private static void StorePlayerDialog(Guid globalID, PlayerDialog dialog)
        {
            if (dialog.DialogNumber <= 0)
            {
                for (var x = 1; x <= NumberOfDialogs; x++)
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

            var dialog = AppCache.PlayerDialogs[globalID];
            AppCache.DialogFilesInUse[dialog.DialogNumber] = false;

            AppCache.PlayerDialogs.Remove(globalID);
        }

        public static void LoadConversation(NWPlayer player, NWObject talkTo, string @class, int dialogNumber)
        {
            if (string.IsNullOrWhiteSpace(@class)) throw new ArgumentException(nameof(@class), nameof(@class) + " cannot be null, empty, or whitespace.");
            if (dialogNumber != -1 && (dialogNumber < 1 || dialogNumber > NumberOfDialogs)) throw new ArgumentOutOfRangeException(nameof(dialogNumber), nameof(dialogNumber) + " must be between 1 and " + NumberOfDialogs);

            var convo = GetConversation(@class);
            var dialog = convo.SetUp(player);

            if (dialog == null)
            {
                throw new NullReferenceException(nameof(dialog) + " cannot be null.");
            }

            if (dialogNumber > 0)
                dialog.DialogNumber = dialogNumber;

            dialog.ActiveDialogName = @class;
            dialog.DialogTarget = talkTo;
            StorePlayerDialog(player.GlobalID, dialog);
        }

        public static void StartConversation(NWPlayer player, NWObject talkTo, string @class)
        {
            if (string.IsNullOrWhiteSpace(@class)) throw new ArgumentException(nameof(@class), nameof(@class) + " cannot be null, empty, or whitespace.");

            LoadConversation(player, talkTo, @class, -1);
            var dialog = AppCache.PlayerDialogs[player.GlobalID];

            // NPC conversations
            if (GetObjectType(talkTo) == ObjectType.Creature &&
                !talkTo.IsPlayer &&
                !talkTo.IsDM)
            {
                talkTo.AssignCommand(() => ActionStartConversation(player, "dialog" + dialog.DialogNumber, true, false));
            }
            // Everything else
            else
            {
                player.AssignCommand(() => ActionStartConversation(talkTo.Object, "dialog" + dialog.DialogNumber, true, false));
            }
        }

        public static void StartConversation(NWCreature player, NWObject talkTo, string @class)
        {
            StartConversation((NWPlayer) player, talkTo, @class);
        }

        public static void EndConversation(NWPlayer player)
        {
            var playerDialog = LoadPlayerDialog(player.GlobalID);
            playerDialog.IsEnding = true;
            StorePlayerDialog(player.GlobalID, playerDialog);
        }

        private static void OnModuleRest()
        {
            NWPlayer player = (GetLastPCRested());
            var restType = GetLastRestEventType();

            if (restType != RestEventType.Started ||
                !player.IsValid ||
                player.IsDM) return;

            player.AssignCommand(() => ClearAllActions());

            StartConversation(player, player, "RestMenu");
        }

        public static void ActionsTaken(int nodeID)
        {
            NWPlayer player = (GetPCSpeaker());
            var dialog = LoadPlayerDialog(player.GlobalID);

            using (new Profiler(nameof(DialogService) + "." + nameof(ActionsTaken) + "." + dialog.ActiveDialogName))
            {
                var convo = GetConversation(dialog.ActiveDialogName);
                var selectionNumber = nodeID + 1;
                var responseID = nodeID + (NumberOfResponsesPerPage * dialog.PageOffset);

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
                    var currentPageName = dialog.CurrentPageName;
                    var previous = dialog.NavigationStack.Pop();

                    // This might be a little confusing but we're passing the active page as the "old page" to the Back() method.
                    // This is because we need to run any dialog-specific clean up prior to moving the conversation backwards.
                    convo.Back(player, currentPageName, previous.PageName);

                    // Previous page was in a different conversation. Switch to it.
                    if (previous.DialogName != dialog.ActiveDialogName)
                    {
                        LoadConversation(player, dialog.DialogTarget, previous.DialogName, dialog.DialogNumber);
                        dialog = LoadPlayerDialog(player.GlobalID);
                        dialog.ResetPage();

                        dialog.CurrentPageName = previous.PageName;
                        dialog.PageOffset = 0;
                        // ActiveDialogName will have changed by this point. Get the new conversation.
                        convo = GetConversation(dialog.ActiveDialogName);
                        convo.Initialize();
                        player.SetLocalInt("DIALOG_SYSTEM_INITIALIZE_RAN", 1);
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
                    convo.DoAction(player, dialog.CurrentPageName, responseID + 1);
                }
            }
        }

        public static bool AppearsWhen(int nodeType, int nodeID)
        {
            NWPlayer player = (GetPCSpeaker());
            var hasDialog = HasPlayerDialog(player.GlobalID);
            if (!hasDialog) return false;
            var dialog = LoadPlayerDialog(player.GlobalID);

            using (new Profiler(nameof(DialogService) + "." + nameof(AppearsWhen) + "." + dialog.ActiveDialogName))
            {
                var page = dialog.CurrentPage;
                var convo = GetConversation(dialog.ActiveDialogName);
                var currentSelectionNumber = nodeID + 1;
                var displayNode = false;
                var newNodeText = string.Empty;
                var dialogOffset = (NumberOfResponsesPerPage + 1) * (dialog.DialogNumber - 1);

                if (currentSelectionNumber == NumberOfResponsesPerPage + 1) // Next page
                {
                    var displayCount = page.NumberOfResponses - (NumberOfResponsesPerPage * dialog.PageOffset);

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
                    var responseID = (dialog.PageOffset * NumberOfResponsesPerPage) + nodeID;
                    if (responseID + 1 <= page.NumberOfResponses)
                    {
                        var response = page.Responses[responseID];

                        if (response != null)
                        {
                            newNodeText = response.Text;
                            displayNode = response.IsActive;
                        }
                    }
                }
                else if (nodeType == 1)
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

                    SetCustomToken(90000 + dialogOffset, newNodeText);
                    return true;
                }

                SetCustomToken(90001 + nodeID + dialogOffset, newNodeText);
                return displayNode;
            }
        }

        public static void OnDialogStart()
        {
            NWPlayer pc = (GetLastUsedBy());
            if (!pc.IsValid) pc = (GetPCSpeaker());

            var conversation = GetLocalString(OBJECT_SELF, "CONVERSATION");

            using (new Profiler(nameof(DialogService) + "." + nameof(OnDialogStart) + "." + conversation))
            {

                if (!string.IsNullOrWhiteSpace(conversation))
                {
                    var objectType = GetObjectType(OBJECT_SELF);
                    if (objectType == ObjectType.Placeable)
                    {
                        NWPlaceable talkTo = (OBJECT_SELF);
                        StartConversation(pc, talkTo, conversation);
                    }
                    else
                    {
                        NWCreature talkTo = (OBJECT_SELF);
                        StartConversation(pc, talkTo, conversation);
                    }
                }
                else
                {
                    ActionStartConversation(pc.Object, "", true, false);
                }
            }
        }

        public static void OnDialogEnd()
        {
            NWPlayer player = (GetPCSpeaker());
            if (!HasPlayerDialog(player.GlobalID)) return;

            var dialog = LoadPlayerDialog(player.GlobalID);
            using (new Profiler(nameof(DialogService) + "." + nameof(OnDialogEnd) + "." + dialog.ActiveDialogName))
            {
                var convo = GetConversation(dialog.ActiveDialogName);
                convo.EndDialog();
                RemovePlayerDialog(player.GlobalID);
                player.DeleteLocalInt("DIALOG_SYSTEM_INITIALIZE_RAN");
            }
        }


        public static void NodeAction0()
        {
            ActionsTaken(0);
        }

        public static void NodeAction1()
        {
            ActionsTaken(1);
        }

        public static void NodeAction2()
        {
            ActionsTaken(2);
        }

        public static void NodeAction3()
        {
            ActionsTaken(3);
        }

        public static void NodeAction4()
        {
            ActionsTaken(4);
        }

        public static void NodeAction5()
        {
            ActionsTaken(5);
        }

        public static void NodeAction6()
        {
            ActionsTaken(6);
        }

        public static void NodeAction7()
        {
            ActionsTaken(7);
        }

        public static void NodeAction8()
        {
            ActionsTaken(8);
        }

        public static void NodeAction9()
        {
            ActionsTaken(9);
        }

        public static void NodeAction10()
        {
            ActionsTaken(10);
        }

        public static void NodeAction11()
        {
            ActionsTaken(11);
        }

        public static bool NodeAppears0()
        {
            return AppearsWhen(2, 0);
        }

        public static bool NodeAppears1()
        {
            return AppearsWhen(2, 1);
        }

        public static bool NodeAppears2()
        {
            return AppearsWhen(2, 2);
        }

        public static bool NodeAppears3()
        {
            return AppearsWhen(2, 3);
        }

        public static bool NodeAppears4()
        {
            return AppearsWhen(2, 4);
        }

        public static bool NodeAppears5()
        {
            return AppearsWhen(2, 5);
        }

        public static bool NodeAppears6()
        {
            return AppearsWhen(2, 6);
        }

        public static bool NodeAppears7()
        {
            return AppearsWhen(2, 7);
        }

        public static bool NodeAppears8()
        {
            return AppearsWhen(2, 8);
        }

        public static bool NodeAppears9()
        {
            return AppearsWhen(2, 9);
        }

        public static bool NodeAppears10()
        {
            return AppearsWhen(2, 10);
        }

        public static bool NodeAppears11()
        {
            return AppearsWhen(2, 11);
        }

        public static bool HeaderAppearsWhen()
        {
            return AppearsWhen(1, 0);
        }

        public static bool NextAppearsWhen()
        {
            return AppearsWhen(3, 12);
        }

        public static void NextAction()
        {
            ActionsTaken(12);
        }

        public static bool PreviousAppearsWhen()
        {
            return AppearsWhen(4, 13);
        }

        public static void PreviousAction()
        {
            ActionsTaken(13);
        }

        public static bool BackAppearsWhen()
        {
            return AppearsWhen(5, 14);
        }

        public static void BackAction()
        {
            ActionsTaken(14);
        }

        public static void EndAction()
        {
            OnDialogEnd();
        }

        public static void StartDialog()
        {
            OnDialogStart();
        }

    }
}
