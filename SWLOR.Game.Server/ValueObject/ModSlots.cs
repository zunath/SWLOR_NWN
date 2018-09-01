namespace SWLOR.Game.Server.ValueObject
{
    public class ModSlots
    {
        public int RedSlots { get; set; }
        public int BlueSlots { get; set; }
        public int GreenSlots { get; set; }
        public int YellowSlots { get; set; }
        public int PrismaticSlots { get; set; }

        public int FilledRedSlots { get; set; }
        public int FilledBlueSlots { get; set; }
        public int FilledGreenSlots { get; set; }
        public int FilledYellowSlots { get; set; }
        public int FilledPrismaticSlots { get; set; }

        public bool CanRedModBeAdded => RedSlots + PrismaticSlots - FilledRedSlots > 0;
        public bool CanBlueModBeAdded => BlueSlots + PrismaticSlots - FilledBlueSlots > 0;
        public bool CanGreenModBeAdded => GreenSlots + PrismaticSlots - FilledGreenSlots > 0;
        public bool CanYellowModBeAdded => YellowSlots + PrismaticSlots - FilledYellowSlots > 0;

    }
}
