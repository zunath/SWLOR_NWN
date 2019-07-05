using System;
using System.Reflection;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Event.Legacy
{
    public static class ScriptItemEvent
    {
        public static void Run(string script)
        {
            if (!script.StartsWith("Item."))
            {
                script = "Item." + script;
            }

            using (new Profiler(nameof(ScriptEvent) + "." + script))
            {
                Type type = Type.GetType(Assembly.GetExecutingAssembly().GetName().Name + "." + script);

                if (type == null)
                {
                    Console.WriteLine("Unable to locate type for ScriptItemEvent: " + script);
                    return;
                }

                IRegisteredEvent @event = Activator.CreateInstance(type) as IRegisteredEvent;
                @event?.Run();
            }
        }
    }
}
