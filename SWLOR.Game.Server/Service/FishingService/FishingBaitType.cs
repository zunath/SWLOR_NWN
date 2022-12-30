using System;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.FishingService
{
    public enum FishingBaitType
    {
        [FishingBait("")]
        Invalid = 0,
        [FishingBait("little_worm", "little_worm99")]
        LittleWorm = 1,
        [FishingBait("insect_ball", "insect_ball99")]
        InsectBall = 2,
        [FishingBait("crayfish_ball", "crayfish_ball99")]
        CrayfishBall = 3,
        [FishingBait("shrimp_lure", "shrimp_lure99")]
        ShrimpLure = 4,
        [FishingBait("lugworm", "lugworm99")]
        Lugworm = 5,
        [FishingBait("sliced_cod", "sliced_cod99")]
        SlicedCod = 6,
        [FishingBait("meatball", "meatball99")]
        Meatball = 7,
        [FishingBait("sinking_minnow", "sinking_minnow99")]
        SinkingMinnow = 8,
        [FishingBait("drill_calamary", "drill_calamary99")]
        DrillCalamary = 9,
        [FishingBait("worm_lure", "worm_lure99")]
        WormLure = 10,
        [FishingBait("lufaise_fly", "lufaise_fly99")]
        LufaiseFly = 11,
        [FishingBait("shell_bug", "shell_bug99")]
        ShellBug = 12,
        [FishingBait("robber_rig", "robber_rig99")]
        RobberRig = 13,
        [FishingBait("peeled_crayfish", "peeled_crayfish99")]
        PeeledCrayfish = 14,
        [FishingBait("trout_ball", "trout_ball99")]
        TroutBall = 15,
        [FishingBait("calla_lure", "calla_lure99")]
        CallaLure = 16,
        [FishingBait("fly_lure", "fly_lure99")]
        FlyLure = 17,
        [FishingBait("rogue_rig", "rogue_rig99")]
        RogueRig = 18,
        [FishingBait("sabiki_rig", "sabiki_rig99")]
        SabikiRig = 19,
        [FishingBait("giantshell_bug", "giantshell_bug99")]
        GiantShellBug = 20,
        [FishingBait("lizard_lure", "lizard_lure99")]
        LizardLure = 21,
        [FishingBait("bluetail", "bluetail99")]
        Bluetail = 22,
        [FishingBait("sardine_ball", "sardine_ball99")]
        SardineBall = 23,
    }

    public class FishingBaitAttribute : Attribute
    {
        public List<string> Resrefs { get; set; }

        public FishingBaitAttribute(string resref, params string[] additionalResrefs)
        {
            Resrefs = new List<string>();
            Resrefs.Add(resref);

            if(additionalResrefs != null)
                Resrefs.AddRange(additionalResrefs);
        }
    }
}
