namespace SWLOR.Game.Server.Service.BeastMasteryService
{
    /// <summary>
    /// The three kinds of incubation enzyme. Each comes in five colors (<see cref="EnzymeColorType"/>), and the item resref is
    /// the lowercase type, an underscore and the lowercase color: <c>hydrolase_blue</c>, <c>lyase_black</c>.
    /// </summary>
    public enum EnzymeType
    {
        Invalid = 0,
        Hydrolase = 1,
        Isomerase = 2,
        Lyase = 3,
    }
}
