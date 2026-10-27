using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// The traits of the module that is firing right now, so a module's action reads its own item's traits (one action
    /// serves every copy of a module). Set with <see cref="Begin"/> around a module's action.
    /// </summary>
    public static class ShipModuleTraitContext
    {
        private static readonly IReadOnlyCollection<ShipComponentTraitType> None = new ShipComponentTraitType[0];
        private static readonly Stack<IReadOnlyCollection<ShipComponentTraitType>> _stack = new();

        /// <summary>The firing module's traits; empty outside a module's action.</summary>
        public static IReadOnlyCollection<ShipComponentTraitType> Current => _stack.Count > 0 ? _stack.Peek() : None;

        /// <summary>Makes the traits current until the returned scope is disposed (use it in a <c>using</c>).</summary>
        public static ShipModuleTraitScope Begin(IReadOnlyCollection<ShipComponentTraitType> traits)
        {
            _stack.Push(traits ?? None);
            return new ShipModuleTraitScope();
        }

        internal static void End()
        {
            if (_stack.Count > 0)
                _stack.Pop();
        }
    }
}
