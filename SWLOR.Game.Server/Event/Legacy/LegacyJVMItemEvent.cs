using System;
using System.Reflection;
using SWLOR.Game.Server.Event;

namespace SWLOR.Game.Server.NWN.Events.Legacy
{
    public static class LegacyJVMItemEvent
    {
        public static void Run(string script)
        {
            if (!script.StartsWith("Item."))
            {
                script = "Item." + script;
            }

            Type type = Type.GetType(Assembly.GetExecutingAssembly().GetName().Name + "." + script);

            if (type == null)
            {
                Console.WriteLine("Unable to locate type for LegacyJVMItemEvent: " + script);
                return;
            }

            IRegisteredEvent @event = Activator.CreateInstance(type) as IRegisteredEvent;
            @event?.Run();
        }
    }
}
