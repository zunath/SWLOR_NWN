namespace SWLOR.Game.Server.Service.CityAffairsService
{
    /// <summary>What the end of a recall election does next.</summary>
    public enum RecallStep
    {
        /// <summary>Nothing: the election was not an empty recall, or it was an ordinary one.</summary>
        None = 0,

        /// <summary>The first recall was empty: the city is marked Abandoned and a second recall opens at once.</summary>
        OpenSecondRecall = 1,

        /// <summary>The second recall was empty and fewer than 5 citizens logged in: the city is removed.</summary>
        RemoveCity = 2,

        /// <summary>The second recall was empty but citizens use the city: it stays, with its Edicts paused for neglect.</summary>
        KeepWithNeglect = 3
    }
}
