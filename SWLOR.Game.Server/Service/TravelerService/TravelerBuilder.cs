using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.TravelerService
{
    /// <summary>Builds the traveler definitions in <see cref="TravelerCatalog"/>.</summary>
    public class TravelerBuilder
    {
        private readonly Dictionary<TravelerType, TravelerDetail> _travelers = new();
        private TravelerDetail _traveler;
        private TravelerStockGroup _group;

        /// <summary>Starts a traveler. This should be called first.</summary>
        public TravelerBuilder Create(TravelerType type)
        {
            _traveler = new TravelerDetail(type);
            _group = null;
            _travelers[type] = _traveler;
            return this;
        }

        public TravelerBuilder Name(string name)
        {
            _traveler.Name = name;
            return this;
        }

        public TravelerBuilder Title(string title)
        {
            _traveler.Title = title;
            return this;
        }

        public TravelerBuilder Story(string story)
        {
            _traveler.Story = story;
            return this;
        }

        public TravelerBuilder Greeting(string greeting)
        {
            _traveler.Greeting = greeting;
            return this;
        }

        public TravelerBuilder Look(string creatureResref)
        {
            _traveler.LookResref = creatureResref;
            return this;
        }

        public TravelerBuilder Rumor(string rumor)
        {
            _traveler.Rumors.Add(rumor);
            return this;
        }

        /// <summary>Starts a pool. The items that follow are in it.</summary>
        public TravelerBuilder Group(string name, int picks)
        {
            _group = new TravelerStockGroup(name, picks);
            _traveler.Groups.Add(_group);
            return this;
        }

        /// <summary>Adds an item to the pool, with the number the traveler sells of it in a week.</summary>
        public TravelerBuilder AddItem(string resref, int weeklyLimit)
        {
            _group.Items.Add(new TravelerStockItem(resref, weeklyLimit));
            return this;
        }

        public Dictionary<TravelerType, TravelerDetail> Build()
        {
            return _travelers;
        }
    }
}
