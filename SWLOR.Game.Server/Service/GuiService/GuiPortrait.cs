using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.GuiService;

public static class GuiPortrait
{
    public static string GetLarge(uint creature)
    {
        return ResolveLarge(GetPortraitResRef(creature), GetGender(creature),
            resref => ResManGetAliasFor(resref, ResType.TGA) != string.Empty ||
                      ResManGetAliasFor(resref, ResType.DDS) != string.Empty);
    }

    public static string ResolveLarge(string portrait, Gender gender, Func<string, bool> resourceExists)
    {
        var resref = portrait + "l";
        if (!string.IsNullOrWhiteSpace(portrait) && resourceExists(resref))
            return resref;

        // Player-local custom portraits may be unavailable to other clients.
        return gender == Gender.Female ? "po_hu_f_99_l" : "po_hu_m_99_l";
    }
}
