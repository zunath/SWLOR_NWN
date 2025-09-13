using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.NWN.API.Core;
using SWLOR.NWN.API.NWNX;
using System;

namespace SWLOR.Game.Server.Core
{
    public class ScriptExecutor
    {
        private const int ScriptHandled = 0;
        private const int ScriptNotHandled = -1;

        public int ProcessRunScript(string scriptName, uint objectSelf)
        {
            var oldObjectSelf = ServerManager.Bootstrapper.ClosureManager.ObjectSelf;
            ServerManager.Bootstrapper.ClosureManager.ObjectSelf = objectSelf;

            try
            {
                var retVal = RunScripts(scriptName);
                return retVal == -1 ? ScriptNotHandled : retVal;
            }
            finally
            {
                ServerManager.Bootstrapper.ClosureManager.ObjectSelf = oldObjectSelf;
            }
        }

        private int RunScripts(string script)
        {
            if (ServerManager.Scripts.HasConditionalScript(script))
            {
                return ExecuteConditionalScripts(script);
            }
            else if (ServerManager.Scripts.HasScript(script))
            {
                return ExecuteActionScripts(script);
            }

            return ScriptNotHandled;
        }

        private int ExecuteConditionalScripts(string script)
        {
            var result = true;

            foreach (var (action, name) in ServerManager.Scripts.GetConditionalScripts(script))
            {
                ProfilerPlugin.PushPerfScope(OBJECT_SELF, name);
                try
                {
                    var actionResult = action.Invoke();
                    if (result)
                        result = actionResult;
                }
                finally
                {
                    ProfilerPlugin.PopPerfScope();
                }
            }

            return result ? 1 : 0;
        }

        private int ExecuteActionScripts(string script)
        {
            foreach (var (action, name) in ServerManager.Scripts.GetActionScripts(script))
            {
                try
                {
                    ProfilerPlugin.PushPerfScope(OBJECT_SELF, name);
                    action();
                }
                catch (Exception ex)
                {
                    Log.Write(LogGroup.Error, $"C# Script '{script}' threw an exception. Details: {Environment.NewLine}{Environment.NewLine}{ex.ToMessageAndCompleteStacktrace()}", true);
                }
                finally
                {
                    ProfilerPlugin.PopPerfScope();
                }
            }

            return ScriptHandled;
        }
    }
}