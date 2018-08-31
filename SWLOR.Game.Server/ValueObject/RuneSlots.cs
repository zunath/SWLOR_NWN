namespace SWLOR.Game.Server.ValueObject
{
    public class RuneSlots
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

        public bool CanRedRuneBeAdded => RedSlots + PrismaticSlots - FilledRedSlots > 0;
        public bool CanBlueRuneBeAdded => BlueSlots + PrismaticSlots - FilledBlueSlots > 0;
        public bool CanGreenRuneBeAdded => GreenSlots + PrismaticSlots - FilledGreenSlots > 0;
        public bool CanYellowRuneBeAdded => YellowSlots + PrismaticSlots - FilledYellowSlots > 0;

    }
}
