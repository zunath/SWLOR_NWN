using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// The traits and Module Bonus of the module that is firing right now, so a module's action reads its own item's
    /// (one action serves every copy of a module). Set with <see cref="Begin"/> around a module's action.
    /// </summary>
    public static class ShipModuleTraitContext
    {
        private static readonly IReadOnlyCollection<ShipComponentTraitType> None = new ShipComponentTraitType[0];
        private static readonly Stack<IReadOnlyCollection<ShipComponentTraitType>> _stack = new();
        private static readonly Stack<int?> _slots = new();
        private static readonly Stack<int> _moduleBonuses = new();

        /// <summary>The firing module's traits; empty outside a module's action.</summary>
        public static IReadOnlyCollection<ShipComponentTraitType> Current => _stack.Count > 0 ? _stack.Peek() : None;

        /// <summary>The hotbar slot of the module whose action is running, or null.</summary>
        public static int? CurrentSlot => _slots.Count > 0 ? _slots.Peek() : null;

        /// <summary>The firing module's Module Bonus after its Yield traits (<see cref="ShipTraitRules.ModuleBonus"/>); 0 outside a module's action.</summary>
        public static int CurrentModuleBonus => _moduleBonuses.Count > 0 ? _moduleBonuses.Peek() : 0;

        /// <summary>Makes the traits and Module Bonus current until the returned scope is disposed (use it in a <c>using</c>).</summary>
        public static ShipModuleTraitScope Begin(IReadOnlyCollection<ShipComponentTraitType> traits, int? slot = null, int moduleBonus = 0)
        {
            _stack.Push(traits ?? None);
            _slots.Push(slot);
            _moduleBonuses.Push(moduleBonus);
            return new ShipModuleTraitScope();
        }

        internal static void End()
        {
            if (_stack.Count > 0)
                _stack.Pop();
            if (_slots.Count > 0)
                _slots.Pop();
            if (_moduleBonuses.Count > 0)
                _moduleBonuses.Pop();
        }
    }
}
