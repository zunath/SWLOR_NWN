using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    /// <summary>The Player Guide topics of the Social category, one method per topic.</summary>
    internal static class PlayerGuideSocialTopics
    {
        internal static PlayerGuideTopic CreateCommunication()
        {
            return new(
                "Communication",
                "Social",
                "Talk ranges, scoped comms, HoloCom calls, disabled Shout, HoloNet broadcasts, names, emotes, languages, notes, and settings.",
                "Chat and tools",
                new[]
                {
                    new ArticleBlock("Talk, Whisper, and Comms",
                        "Normal Talk reaches players within 20 meters. Whisper reaches 4 meters. Party chat acts as Comms: it reaches party members in the same ship, space region, supported event area, or planet, and nearby non-party listeners within 20 meters can overhear it when they share that scope. By default, you receive a warning when party members are out of range; this warning can be disabled in General Settings."),
                    new ArticleBlock("Disabled Shout Channel",
                        "The Shout chat channel is disabled for players. DMs can still use Shout for server-wide messages."),
                    new ArticleBlock("HoloNet Broadcast Window",
                        "The HoloNet broadcast window sends a longer broadcast, costs 2500 credits, and is limited to 600 characters."),
                    new ArticleBlock("HoloCom Calls",
                        "Select a player in the Player List and press Call to call another available online player. Incoming calls are announced in chat; answer or decline them with the Player List buttons or /answer and /decline. HoloCom cannot be used in space, and both participants are immobilized while a connected call is active. Use End Call in the Player List or /endcall to end it; unanswered call attempts time out."),
                    new ArticleBlock("Settings",
                        "Settings controls achievement notifications, subdual mode, reset reminders, Mini-Vitals, chat and emote colors, language colors, character-description access, and identity/privacy options for descriptors and account names."),
                    new ArticleBlock("Notes",
                        "Notes are private player notes. You can keep up to 100 notes, and each note can hold up to 1000 characters. Notes can be searched, sorted into up to 25 categories you create yourself, and filtered by category."),
                    new ArticleBlock("Remembering Other Characters",
                        "Using /name <label> on another player saves a private label only your character can see. It does not rename the other character, and no other player sees the label you entered. The label can be a name your character was told, a nickname, or what your character believes that presented identity is called; it does not have to be the truth. Use /forgetname on the character to remove only your private label."),
                    new ArticleBlock("Your Public Description",
                        "An unrecognized player character appears as a gray public description. Use /name <description> on yourself to set the gray text shown to players who have not saved a label for your current identity. This does not change your real character name. Labels and public descriptions are limited to 64 characters and cannot include color codes."),
                    new ArticleBlock("Introducing Yourself",
                        "Use /introduce <name> to offer a name or alias to players within 20 meters who can see you. They receive a chat notice and can use /introductions to Remember or Dismiss it. Nothing is saved without their approval, and replacing an existing private label requires confirmation. Introductions belong to your current identity, expire after 10 minutes, and become unavailable if you disconnect or change identity. The command has a 2-minute cooldown that persists across reconnects. Repeating a pending name does not send another notice."),
                    new ArticleBlock("Example",
                        "A masked character uses the public description 'Tall Armored Human.' Mira saves the private label 'Red Coat,' so only Mira sees Red Coat. Jax has not saved a label, so Jax still sees Tall Armored Human in gray. Neither player renamed the masked character. If the mask is a disguise, labels saved for the character's normal identity remain separate."),
                    new ArticleBlock("Emotes and Languages",
                        "Speech can include emote text, and /emotestyle toggles between regular and novel formatting. Use /emotes to list emotes or /emotegui to browse them in a window. Use /language help to list language aliases and /language <alias> to switch. Hearing a non-Basic language you do not fully know can grant language XP over time. Wookiees always speak Shyriiwook."),
                    new ArticleBlock("OOC and Speech Restrictions",
                        "Text after // or (( is treated as out-of-character and is not translated through the language system. Dead characters cannot speak, and the Shout channel remains staff-only."),
                    new ArticleBlock("Useful Chat Commands",
                        "Use /introduce <name> to offer a name or alias to nearby players, and /introductions to review names offered to you. /introduce has a 2-minute cooldown. Use /help to browse commands, /dice to open the dice bag, /helm and /cloak to show or hide your helmet and cloak, /bug to report a problem, /resetwindows to restore window positions, and /stuck only as an emergency escape when trapped on a map. /stuck has a 30 minute cooldown.")
                },
                new[]
                {
                    new QuestionAnswer("How far does talk carry?", "Talk reaches 20 meters. Whisper reaches 4 meters."),
                    new QuestionAnswer("What is party chat here?", "Party chat acts as scoped Comms for your party and can be overheard by nearby players in the same scope."),
                    new QuestionAnswer("Why can't I use Shout?", "The player Shout channel is disabled. Use Comms for in-character radio-style communication."),
                    new QuestionAnswer("Can another player rename my character?", "No. They can only save a private label visible to their own character."),
                    new QuestionAnswer("Can two players see different names for the same character?", "Yes. Each character keeps their own private labels."),
                    new QuestionAnswer("Does /name reveal someone's real identity?", "No. It records what your character believes or calls the currently presented identity."),
                    new QuestionAnswer("How do introductions work?", "Use /introduce <name> to offer a name or alias to players within 20 meters who can see you. It has a 2-minute cooldown that persists across reconnects. Recipients use /introductions to choose Remember or Dismiss. Replacing an existing private label requires confirmation; ignoring or dismissing the offer changes nothing. An introduction does not prove the name is true."),
                    new QuestionAnswer("How do I label another player?", "Type /name <label>, then click that player character. Use /forgetname and click them again to remove only your private label. Target yourself with /name to set your gray public description."),
                    new QuestionAnswer("How do I change languages?", "Use /language help, then /language <alias>. Wookiees remain in Shyriiwook."),
                    new QuestionAnswer("How do languages improve?", "Listening to partially understood non-Basic speech can grant language XP over time."),
                    new QuestionAnswer("How do I report a bug?", "Use /bug and include what happened, where it happened, and how to reproduce it.")
                },
                new[] { "Common Questions", "Useful Windows", "Skills", "Quests & Key Items", "Disguises", "Death & Recovery" });
        }

        internal static PlayerGuideTopic CreateDisguises()
        {
            return new(
                "Disguises",
                "Social",
                "Creating identities, public and private fields, activation cooldowns, retirement, restoration, and permanent wiping.",
                "Alternate identities",
                new[]
                {
                    new ArticleBlock("Opening Disguises",
                        "Open Disguises from the Character Sheet, or use /disguise or /disguises. A disguise stores a private slot label only you can see, plus a public description, biography, portrait, sound set, and optional scrambled account identifier."),
                    new ArticleBlock("Separate Presented Identities",
                        "Your normal identity and every disguise are remembered separately. A private label someone saved for your normal identity does not carry over to a disguise, and a label saved for one disguise does not carry over to another. Each observer may label the same disguise differently. Activating or deactivating a disguise automatically restores the labels that observer previously saved for the identity you are presenting."),
                    new ArticleBlock("Identity Slots",
                        "You begin with 1 identity slot. Each False Identities rank adds 1, up to 4 total slots. Every saved identity, including a retired one, occupies a slot. Retiring does not free it; only permanently wiping it does."),
                    new ArticleBlock("Activating",
                        "An active disguise changes your presented identity, portrait, sound set, and examine biography. The base wait between disguise activations is 30 minutes. Cover Story reduces that wait by 40 or 70 percent, with a minimum wait of 5 minutes. Deactivating a disguise is immediate and restores your normal biography, portrait, and sound set."),
                    new ArticleBlock("Biography and Outfit",
                        "Each disguise has its own biography, shown when another player examines you while that disguise is active. Write only what someone could observe about that disguised identity. Edit it in the Disguises window; deactivate the disguise before editing your normal biography. Disguises do not change your equipped clothing or armor, so you remain responsible for changing your outfit."),
                    new ArticleBlock("Retiring and Restoring",
                        "Retiring an identity deactivates it and prevents activation, but keeps its saved setup. You can restore a retired identity from the Disguises window if you want to use it again."),
                    new ArticleBlock("Permanently Wiping",
                        "An Identity Broker can permanently wipe a retired identity for 100,000 credits or 25,000 Available RP XP. Wiping frees the slot and removes other characters' private-label references to that identity. This cannot be undone."),
                    new ArticleBlock("Staff Accountability",
                        "A disguise does not hide your underlying character from staff. Staff tools and server audit logs retain the real character and account identity. Player-created labels are in-character memory aids, not proof of a character's real identity in an out-of-character dispute.")
                },
                new[]
                {
                    new QuestionAnswer("How many disguises can I keep?", "One initially, up to four total with False Identities."),
                    new QuestionAnswer("Why can't I activate another disguise?", "The activation cooldown may still be running. Cover Story reduces it, but it cannot go below 5 minutes."),
                    new QuestionAnswer("Does retiring free the slot?", "No. Restore it for reuse, or permanently wipe it at an Identity Broker to free the slot."),
                    new QuestionAnswer("What does a permanent wipe remove?", "The saved identity and other characters' private-label references to it. The wipe cannot be undone."),
                    new QuestionAnswer("Does a saved name carry between disguises?", "No. Your normal identity and every disguise are remembered separately."),
                    new QuestionAnswer("Does a disguise have its own biography?", "Yes. Activation shows that disguise's biography when examined, and deactivation restores your normal biography."),
                    new QuestionAnswer("Can a disguise hide my identity from staff?", "No. Staff tools and audit logs retain your real character and account identity.")
                },
                new[] { "Communication", "Useful Windows", "Skills", "Perks" });
        }
    }
}
