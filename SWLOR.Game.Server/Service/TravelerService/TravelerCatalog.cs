using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.TravelerService
{
    /// <summary>
    /// Every traveler (Player City Revamp, "Travelers"): who they are and the pools their weekly stock is drawn from. This
    /// file is the one place the roster's names, stories, rumors, pools and weekly limits live.
    ///
    /// The owner approved this file on 2026-11-02, as written:
    /// - The names, titles, stories, greetings and rumors.
    /// - The pools and weekly limits. The prices are the items' own: a traveler adds no markup of its own, and the
    ///   host city's Sales tax is added on top.
    /// - The looks are existing script-free Veles colonist templates of playable species, as the townsfolk use.
    /// - The fashion merchant carries existing cosmetic outfits only. Every plain outfit in the module except the Traveler's
    ///   Clothes is already sold by an NPC store, so exclusive fashion items (banners, crests and the like) are a content
    ///   follow-up that joins the pool here.
    ///
    /// The young animals and the tier 3 to 5 seeds belong to the farm items slice and are listed as missing until it lands.
    /// </summary>
    public static class TravelerCatalog
    {
        private static readonly Lazy<IReadOnlyDictionary<TravelerType, TravelerDetail>> _all = new(Load);

        public static IReadOnlyDictionary<TravelerType, TravelerDetail> All => _all.Value;

        /// <summary>The traveler's definition, or null for a type that has none.</summary>
        public static TravelerDetail Get(TravelerType type)
        {
            return All.TryGetValue(type, out var detail) ? detail : null;
        }

        /// <summary>The traveler with this key (the name of its <see cref="TravelerType"/> member), or null.</summary>
        public static TravelerDetail GetByKey(string key)
        {
            return Enum.TryParse<TravelerType>(key, false, out var type) ? Get(type) : null;
        }

        private static IReadOnlyDictionary<TravelerType, TravelerDetail> Load()
        {
            var builder = new TravelerBuilder();

            BeastTrader(builder);
            Prospector(builder);
            FashionMerchant(builder);
            InformationBroker(builder);

            return builder.Build();
        }

        // Approved 2026-11-02: name, story and greeting as written. The limits fall with the tier, since a corral raises animals
        // of the tier its owner's Crop Management reaches, and a Thune is the rarest.
        private static void BeastTrader(TravelerBuilder builder)
        {
            builder.Create(TravelerType.BeastTrader)
                .Name("Ssorak Dhal")
                .Title("exotic beast trader")
                .Story("Ssorak Dhal drives a caravan of young beasts from one settlement to the next. He swears every animal he sells was raised by hand, and he never says where.")
                .Greeting("Young stock, hand-raised and healthy. Take a look, and mind the teeth.")
                .Look("colonisttrando2")
                .Group("Young animals", 3)
                .AddItem("livestock_waro", 6)
                .AddItem("livestock_cairn", 4)
                .AddItem("livestock_gimp", 4)
                .AddItem("livestock_klor", 3)
                .AddItem("livestock_arad", 3)
                .AddItem("livestock_womp", 2)
                .AddItem("livestock_thune", 1);
        }

        // Approved 2026-11-02: a few high-tier seeds and a rare find or two each week. The seeds are the 16 of tiers 3 to 5, and the rare
        // materials are the ones harvesting only gives as rare finds.
        private static void Prospector(TravelerBuilder builder)
        {
            builder.Create(TravelerType.Prospector)
                .Name("Tavor Quill")
                .Title("rare-materials prospector")
                .Story("Tavor Quill works claims nobody else will touch and sells what the deep rock gives up. A Kel Dor, he keeps his mask on and his prices honest.")
                .Greeting("Fresh from the claim. Seeds and stone, and not much of either.")
                .Look("colonistkeldor")
                .Group("Seeds", 3)
                .AddItem("seed_acorn", 4)
                .AddItem("seed_pineapple", 4)
                .AddItem("seed_veg_clump", 4)
                .AddItem("seed_seaweed", 4)
                .AddItem("seed_fiber_3", 4)
                .AddItem("seed_ginger", 3)
                .AddItem("seed_melon", 3)
                .AddItem("seed_mushroom", 3)
                .AddItem("seed_walnut", 3)
                .AddItem("seed_cornucopia", 3)
                .AddItem("seed_desert_sage", 3)
                .AddItem("seed_fiber_4", 3)
                .AddItem("seed_turnip", 2)
                .AddItem("seed_tomato", 2)
                .AddItem("seed_oregano", 2)
                .AddItem("seed_fiber_5", 2)
                .Group("Rare materials", 2)
                .AddItem("obsidian", 2)
                .AddItem("crystal", 2)
                .AddItem("zinsiam", 1)
                .AddItem("ore_arda", 4);
        }

        // Approved 2026-11-02: the pool is existing cosmetic outfits (no stats). New exclusive fashion items are a content follow-up.
        private static void FashionMerchant(TravelerBuilder builder)
        {
            builder.Create(TravelerType.FashionMerchant)
                .Name("Lyssa Teven")
                .Title("fashion merchant")
                .Story("Lyssa Teven dresses cantina singers and dockhands alike, and carries the latest cuts from the core worlds in a single trunk.")
                .Greeting("Dressed to be remembered, or not at all. What shall it be?")
                .Look("colonisttwilek")
                .Group("Outfits", 4)
                .AddItem("traveler_m", 5)
                .AddItem("traveler_f", 5)
                .AddItem("noble_gr", 3)
                .AddItem("noble_purple", 3)
                .AddItem("gambler_blue", 3)
                .AddItem("gambler_gr", 3)
                .AddItem("party_male", 3)
                .AddItem("party_male2", 3)
                .AddItem("party_female", 3)
                .AddItem("party_female2", 3)
                .AddItem("dancer_m_1", 3)
                .AddItem("dancer_m_2", 3)
                .AddItem("dancer_f_1", 3)
                .AddItem("dancer_f_2", 3)
                .AddItem("musicianoutfit1", 3);
        }

        // Approved 2026-11-02: the broker sells nothing. The owner took the area maps out of the pool because they made map drops less
        // rare, so the broker offers rumors, the whereabouts of the other travelers and the contract board. The rumors are
        // general lines that name no established place or person.
        private static void InformationBroker(TravelerBuilder builder)
        {
            builder.Create(TravelerType.InformationBroker)
                .Name("Orla Vass")
                .Title("information broker")
                .Story("Orla Vass trades in what other people would rather keep quiet. A Bothan, she never carries anything on paper that she can carry in her head.")
                .Greeting("Everything has a price, and everyone has something to sell. Rumors, news, contracts. Which is it?")
                .Look("colonistbothan")
                .Rumor("They say a shipment went missing between two ports, and the people who lost it are paying well to hear nothing about it.")
                .Rumor("A quiet buyer has been asking after old claim maps. Whoever it is does not want the owners to know.")
                .Rumor("The best work is never posted on a board. It goes to whoever happens to be standing in the right cantina.")
                .Rumor("Something large has been seen moving at night outside the settlements. Nobody agrees on what it is, and nobody goes to look.")
                .Rumor("Credits change hands faster on a high-tax street than on a low-tax one. Draw your own conclusions.");
        }
    }
}
