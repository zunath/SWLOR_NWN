using SWLOR.Game.Server.Legacy.GameObject;

namespace SWLOR.Game.Server.Legacy.Quest.Contracts
{
    public interface IQuestReward
    {
        /// <summary>
        /// If true, this reward will become available for the player to select.
        /// If false, this reward will be given regardless if other rewards are selectable.
        /// Note that if the quest doesn't allow reward selection, this is given to them every time no matter what.
        /// </summary>
        bool IsSelectable { get; }

        /// <summary>
        /// The name of the reward as it shows in the 'Select a Reward' menu.
        /// If the quest doesn't allow reward selection, this does nothing.
        /// </summary>
        string MenuName { get; }

        /// <summary>
        /// The actions to take when this reward is given to a player.
        /// </summary>
        /// <param name="player">The player receiving the reward.</param>
        void GiveReward(NWPlayer player);
    }
}
