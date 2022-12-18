using System;

namespace SWLOR.Game.Server.Service.FishingService
{
    public enum FishingBaitType
    {
        [FishingBait("")]
        Invalid = 0,
        [FishingBait("little_worm")]
        LittleWorm = 1,
        [FishingBait("insect_ball")]
        InsectBall = 2,
        [FishingBait("crayfish_ball")]
        CrayfishBall = 3,
        [FishingBait("shrimp_lure")]
        ShrimpLure = 4,
        [FishingBait("lugworm")]
        Lugworm = 5,
        [FishingBait("sliced_cod")]
        SlicedCod = 6,
        [FishingBait("meatball")]
        Meatball = 7,
        [FishingBait("sinking_minnow")]
        SinkingMinnow = 8,
        [FishingBait("drill_calamary")]
        DrillCalamary = 9,
        [FishingBait("worm_lure")]
        WormLure = 10,
        [FishingBait("lufaise_fly")]
        LufaiseFly = 11,
        [FishingBait("shell_bug")]
        ShellBug = 12,
        [FishingBait("robber_rig")]
        RobberRig = 13,
        [FishingBait("peeled_crayfish")]
        PeeledCrayfish = 14,
        [FishingBait("trout_ball")]
        TroutBall = 15,
        [FishingBait("calla_lure")]
        CallaLure = 16,
        [FishingBait("fly_lure")]
        FlyLure = 17,
        [FishingBait("rogue_rig")]
        RogueRig = 18,
        [FishingBait("sabiki_rig")]
        SabikiRig = 19,
        [FishingBait("giantshell_bug")]
        GiantShellBug = 20,
        [FishingBait("lizard_lure")]
        LizardLure = 21,
        [FishingBait("bluetail")]
        Bluetail = 22,
        [FishingBait("sardine_ball")]
        SardineBall = 23,
    }

    public class FishingBaitAttribute : Attribute
    {
        public string Resref { get; set; }

        public FishingBaitAttribute(string resref)
        {
            Resref = resref;
        }
    }
}
