using SWLOR.NWN.API.Contracts;
using SWLOR.Shared.Events.Events.NWNX;
using SWLOR.Shared.Events.Events.Player;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Component.Character.Feature
{
    public class SaveCharacters
    {
        private const string SaveCharactersVariable = "SAVE_CHARACTERS_TICK";
        private const string IsBarteringVariable = "IS_BARTERING";
        private readonly IEventsPluginService _eventsPlugin;

        public SaveCharacters(
            IEventsPluginService eventsPlugin,
            IEventAggregator eventAggregator)
        {
            _eventsPlugin = eventsPlugin;

            // Subscribe to events
            eventAggregator.Subscribe<OnPlayerHeartbeat>(e => HandleSaveCharacters());
            eventAggregator.Subscribe<OnBartenderStartBefore>(e => SetBarteringFlag());
            eventAggregator.Subscribe<OnBartenderEndBefore>(e => RemoveBarteringFlag());
        }

        /// <summary>
        /// Saves characters every minute unless they're currently preoccupied (barter)
        /// </summary>
        public void HandleSaveCharacters()
        {
            var player = OBJECT_SELF;
            var tick = GetLocalInt(player, SaveCharactersVariable) + 1;

            if(tick >= 10)
            {
                if (!GetLocalBool(player, IsBarteringVariable))
                {
                    ExportSingleCharacter(player);
                    tick = 0;
                }
            }

            SetLocalInt(player, SaveCharactersVariable, tick);
        }

        /// <summary>
        /// Marks players as bartering. This is used to ensure the PCs are not exported during this process.
        /// </summary>
        public void SetBarteringFlag()
        {
            var player1 = OBJECT_SELF;
            var player2 = StringToObject(_eventsPlugin.GetEventData("BARTER_TARGET"));

            SetLocalBool(player1, IsBarteringVariable, true);
            SetLocalBool(player2, IsBarteringVariable, true);
        }

        /// <summary>
        /// Removes the bartering flag from PCs involved in a trade. This will ensure their files are exported on the next save occurrence.
        /// </summary>
        public void RemoveBarteringFlag()
        {
            var player1 = OBJECT_SELF;
            var player2 = StringToObject(_eventsPlugin.GetEventData("BARTER_TARGET"));

            DeleteLocalBool(player1, IsBarteringVariable);
            DeleteLocalBool(player2, IsBarteringVariable);
        }
    }
}
