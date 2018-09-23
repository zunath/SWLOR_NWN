using System;
using System.Linq;
using Object = NWN.Object;

namespace SWLOR.Game.Server.GameObject
{
    public static class NWObjectFactory
    {
        private static readonly AppState _state;
        
        static NWObjectFactory()
        {
            _state = App.ResolveUnmanaged<AppState>();
        }


        public static T Build<T>(Object obj)
            where T: NWObject
        {
            var existing = _state.GameObjects.SingleOrDefault(x => x.Object.m_ObjId == obj.m_ObjId);
            if (existing != null)
            {
                if (typeof(T) == existing.GetType()) return (T)existing;

                existing.Dispose();
                _state.GameObjects.Remove(existing);
            }

            var unmanaged = App.ResolveUnmanaged<T>();
            unmanaged.Object = obj;
            _state.GameObjects.Add(unmanaged);
            return unmanaged;
        }

        public static void Clean()
        {
            for (int index = _state.GameObjects.Count - 1; index >= 0; index--)
            {
                var obj = _state.GameObjects.ElementAt(index);

                if (!obj.IsValid)
                {
                    _state.GameObjects.Remove(obj);
                }
            }
        }
    }
}
