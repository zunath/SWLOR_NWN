namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// The ids of the capstone mastery quests that the perk definitions require (<c>RequirementQuest</c>). They live here,
    /// below the quest definitions, because perk content reads them and the quest definitions are content of a higher
    /// domain; each quest definition still names its own constant and takes the value from here.
    /// </summary>
    public static class CapstoneQuestIds
    {
        // BeastMasteryCapstoneQuestDefinition
        public const string AlphaRhythmMasteryQuestId = "alpha_rhythm_mastery";
        public const string ApexBiteMasteryQuestId = "apex_bite_mastery";
        public const string ForceBondedBeastMasteryQuestId = "force_bonded_beast_mastery";
        public const string PrimalOverrunMasteryQuestId = "primal_overrun_mastery";
        public const string UnbreakableBeastMasteryQuestId = "unbreakable_beast_mastery";
        public const string UntouchableInstinctMasteryQuestId = "untouchable_instinct_mastery";

        // BloodFrenzyQuestDefinition
        public const string BloodFrenzyFinalQuestId = "blood_frenzy_mastery";

        // DevicesCapstoneQuestDefinition
        public const string EmergencyBunkerMasteryQuestId = "emergency_bunker_mastery";
        public const string KillzoneBeaconMasteryQuestId = "killzone_beacon_mastery";
        public const string OverloadBarrageMasteryQuestId = "overload_barrage_mastery";
        public const string ThermalDetonatorMasteryQuestId = "thermal_detonator_mastery";

        // FirstAidCapstoneQuestDefinition
        public const string EmergencyCocktailMasteryQuestId = "emergency_cocktail_mastery";

        // ForceCapstoneQuestDefinition
        public const string EclipseOfResolveMasteryQuestId = "eclipse_of_resolve_mastery";
        public const string HungerOfTheDarkMasteryQuestId = "hunger_of_the_dark_mastery";
        public const string LastStandOfTheLightMasteryQuestId = "last_stand_of_the_light_mastery";

        // HeavyVibrobladeCapstoneQuestDefinition
        public const string AbsoluteDefenseMasteryQuestId = "absolute_defense_mastery";
        public const string SoulAscensionMasteryQuestId = "soul_ascension_mastery";

        // KatarCapstoneQuestDefinition
        public const string AdamantineGuardMasteryQuestId = "adamantine_guard_mastery";
        public const string ScrapheapLockdownMasteryQuestId = "scrapheap_lockdown_mastery";

        // LeadershipCapstoneQuestDefinition
        public const string DecisiveCommandMasteryQuestId = "decisive_command_mastery";
        public const string HoldTheLineMasteryQuestId = "hold_the_line_mastery";

        // LightsaberCapstoneQuestDefinition
        public const string GuardianMasterMasteryQuestId = "guardian_master_mastery";
        public const string SaberStormMasteryQuestId = "saber_storm_mastery";

        // PistolCapstoneQuestDefinition
        public const string DeadMansHandMasteryQuestId = "dead_mans_hand_mastery";
        public const string LastWordMasteryQuestId = "last_word_mastery";

        // RifleCapstoneQuestDefinition
        public const string KillBoxMasteryQuestId = "kill_box_mastery";
        public const string OneShotMasteryQuestId = "one_shot_mastery";

        // SaberstaffCapstoneQuestDefinition
        public const string InfiniteConduitMasteryQuestId = "infinite_conduit_mastery";
        public const string SaberCycloneMasteryQuestId = "saber_cyclone_mastery";

        // SpearCapstoneQuestDefinition
        public const string CripplingDefenseMasteryQuestId = "crippling_defense_mastery";
        public const string ForcebaneMasteryQuestId = "forcebane_mastery";

        // StaffCapstoneQuestDefinition
        public const string UnmovingCenterMasteryQuestId = "unmoving_center_mastery";
        public const string WorldbreakerMasteryQuestId = "worldbreaker_mastery";

        // ThrowingCapstoneQuestDefinition
        public const string PerfectFlurryMasteryQuestId = "perfect_flurry_mastery";
        public const string RainOfSteelMasteryQuestId = "rain_of_steel_mastery";

        // TwinBladeCapstoneQuestDefinition
        public const string RedBloomMasteryQuestId = "red_bloom_mastery";
        public const string TempestBloomMasteryQuestId = "tempest_bloom_mastery";

        // TwinPistolCapstoneQuestDefinition
        public const string FinalSalvoMasteryQuestId = "final_salvo_mastery";
        public const string TwinSunsMasteryQuestId = "twin_suns_mastery";

        // UnarmedMasteryQuestDefinition
        public const string HaymakerMasteryQuestId = "haymaker_mastery";
        public const string PerfectFormMasteryQuestId = "perfect_form_mastery";

        // VibrobladeCapstoneQuestDefinition
        public const string InvincibleMasteryQuestId = "invincible_mastery";

        // VibroknifeCapstoneQuestDefinition
        public const string SystemicShutdownMasteryQuestId = "systemic_shutdown_mastery";
        public const string VitalRuptureMasteryQuestId = "vital_rupture_mastery";
    }
}
