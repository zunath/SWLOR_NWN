namespace SWLOR.Game.Server.Service.PazaakService
{
    public enum PazaakCardType
    {
        Invalid = 0,

        Plus1 = 1,
        Plus2 = 2,
        Plus3 = 3,
        Plus4 = 4,
        Plus5 = 5,
        Plus6 = 6,

        Minus1 = 11,
        Minus2 = 12,
        Minus3 = 13,
        Minus4 = 14,
        Minus5 = 15,
        Minus6 = 16,

        PlusMinus1 = 21,
        PlusMinus2 = 22,
        PlusMinus3 = 23,
        PlusMinus4 = 24,
        PlusMinus5 = 25,
        PlusMinus6 = 26,

        OneOrMinusTwo = 31,
        Double = 32,
        TieBreaker = 33,
        Flip2And4 = 34,
        Flip3And6 = 35,
    }
}
