using System;
using System.Reflection;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Event.Legacy
{
    public static class LegacyJVMItemEvent
    {
        public static void Run(string script)
        {
            if (!script.StartsWith("Item."))
            {
                script = "Item." + script;
            }

            using (new Profiler(nameof(LegacyJVMEvent) + "." + script))
            {
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
}
