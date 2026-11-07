using System.Reflection;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The private tables of the perk instance in the installed <see cref="FacadeContainer"/>. Those were static fields of
/// the old static class that fixtures seeded by reflection; a fixture that seeds them now installs a container and
/// reads them here, so what it seeds is what the facade reads for as long as the container is installed.
/// </summary>
public static class PerkInternals
{
    private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;

    public static Dictionary<PerkType, PerkDetail> AllPerks => Field<Dictionary<PerkType, PerkDetail>>("_allPerks");

    public static Dictionary<PerkType, Dictionary<int, int>> PerkLevelTiers =>
        Field<Dictionary<PerkType, Dictionary<int, int>>>("_perkLevelTiers");

    public static Dictionary<PerkType, List<PerkTriggerPurchasedRefundedAction>> PurchaseTriggers =>
        Field<Dictionary<PerkType, List<PerkTriggerPurchasedRefundedAction>>>("_purchaseTriggers");

    public static Dictionary<PerkType, List<PerkTriggerPurchasedRefundedAction>> RefundTriggers =>
        Field<Dictionary<PerkType, List<PerkTriggerPurchasedRefundedAction>>>("_refundTriggers");

    /// <summary>Runs a private method of the installed instance.</summary>
    public static void Invoke(string method, params object[] arguments) =>
        typeof(PerkService).GetMethod(method, PrivateInstance)!.Invoke(Current, arguments);

    private static T Field<T>(string name) => (T)typeof(PerkService).GetField(name, PrivateInstance)!.GetValue(Current)!;

    private static IPerkService Current => FacadeContainer.Current.Get<IPerkService>();
}
