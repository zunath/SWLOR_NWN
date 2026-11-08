using SWLOR.Game.Server.Domain.Economy;

namespace SWLOR.Game.Server.Composition
{
    public static partial class GameServices
    {
        /// <summary>The global bank (the facade <c>Service.Bank</c> until P2-ZK).</summary>
        public static IBankService Bank => FacadeAccessor.Resolve<IBankService>();

        /// <summary>The casino slot machines (the facade <c>Service.CasinoSlot</c> until P2-ZK).</summary>
        public static ICasinoSlotService CasinoSlot => FacadeAccessor.Resolve<ICasinoSlotService>();

        /// <summary>The currencies a player holds (the facade <c>Service.Currency</c> until P2-ZK).</summary>
        public static ICurrencyService Currency => FacadeAccessor.Resolve<ICurrencyService>();

        /// <summary>The guilds, guild points and guild tasks (the facade <c>Service.Guild</c> until P2-ZK).</summary>
        public static IGuildService Guild => FacadeAccessor.Resolve<IGuildService>();

        /// <summary>The skill XP a crafting guild hand-in gives (the facade <c>Service.GuildTaskXp</c> until P2-ZK).</summary>
        public static IGuildTaskXpService GuildTaskXp => FacadeAccessor.Resolve<IGuildTaskXpService>();

        /// <summary>The player market (the facade <c>Service.PlayerMarket</c> until P2-ZK).</summary>
        public static IPlayerMarketService PlayerMarket => FacadeAccessor.Resolve<IPlayerMarketService>();
    }
}
