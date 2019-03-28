using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.NWNX
{
    public class AttackEventData
    {
        public NWObject Target { get; set; }
        public int Bludgeoning { get; set; }
        public int Pierce { get; set; }
        public int Slash { get; set; }
        public int Magical { get; set; }
        public int Acid { get; set; }
        public int Cold { get; set; }
        public int Divine { get; set; }
        public int Electrical { get; set; }
        public int Fire { get; set; }
        public int Negative { get; set; }
        public int Positive { get; set; }
        public int Sonic { get; set; }
        public int Base { get; set; }
        public int AttackNumber { get; set; } // 1-based index of the attack in current combat round
        public int AttackResult { get; set; } // 1=hit, 3=critical hit, 4=miss, 8=concealed
        public int AttackType { get; set; }   // 1=main hand, 2=offhand, 3-5=creature, 6=haste
        public int SneakAttack { get; set; }  // 0=neither, 1=sneak attack, 2=death attack, 3=both
    }
}
