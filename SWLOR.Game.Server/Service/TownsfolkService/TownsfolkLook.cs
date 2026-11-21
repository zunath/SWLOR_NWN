namespace SWLOR.Game.Server.Service.TownsfolkService
{
    /// <summary>
    /// How one piece of street life looks: the existing creature template it is made from, the name it shows, and an
    /// optional appearance row that replaces the template's model.
    /// </summary>
    /// <param name="Resref">The creature template (a UTC in the module).</param>
    /// <param name="Name">The name the creature shows.</param>
    /// <param name="Appearance">An appearance.2da row to show instead of the template's, or null to keep it.</param>
    public sealed record TownsfolkLook(string Resref, string Name, int? Appearance = null);
}
