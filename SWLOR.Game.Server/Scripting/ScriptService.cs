using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CSScriptLibrary;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.Scripting.Contracts;
using SWLOR.Game.Server.Service;
using Exception = System.Exception;

namespace SWLOR.Game.Server.Scripting
{
    public static class ScriptService
    {
        /// <summary>
        /// Notifies whenever a plugin is added, removed, or changed in the executing directory.
        /// </summary>
        private static FileSystemWatcher _watcher;

        /// <summary>
        /// Keeps compiled scripts in memory.
        /// </summary>
        private static readonly ConcurrentDictionary<string, IScript> _scriptCache = new ConcurrentDictionary<string, IScript>();

        /// <summary>
        /// Points a namespace to a compiled script in the cache.
        /// </summary>
        private static readonly ConcurrentDictionary<string, string> _namespacePointers = new ConcurrentDictionary<string, string>();

        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnModuleLoad>(OnModuleLoad);
        }

        /// <summary>
        /// Compiles all scripts in the Scripts folder and stores them into the cache.
        /// </summary>
        /// <param name="event">The event raised.</param>
        private static void OnModuleLoad(OnModuleLoad @event)
        {
            CSScript.EvaluatorConfig.Engine = EvaluatorEngine.Roslyn;
            string scriptsDirectory = Environment.GetEnvironmentVariable("NWNX_MONO_BASE_DIRECTORY") + "/Scripts/";
            string[] files = Directory.GetFiles(scriptsDirectory, "*.cs", SearchOption.AllDirectories);

            Console.WriteLine("Compiling script files...");
            Parallel.ForEach(files, (file) =>
            {
                DoLoadScript(file);
            });
            Console.WriteLine("Scripts finished compiling!");

            StartFileWatcher(scriptsDirectory);
        }

        /// <summary>
        /// Executes the Main() function in a compiled script by its namespace.
        /// </summary>
        /// <param name="namespace">The fully qualified namespace of the script you wish to run.</param>
        public static void RunScriptByNamespace(string @namespace)
        {
            if (!_namespacePointers.ContainsKey(@namespace))
            {
                throw new KeyNotFoundException("Script with namespace '" + @namespace + "' not found. Does the script exist in the Scripts folder?");
            }

            string fileKey = _namespacePointers[@namespace];
            IScript script = _scriptCache[fileKey];
            script.Main();
        }

        /// <summary>
        /// Returns whether a script with the given namespace is registered in the script cache.
        /// </summary>
        /// <param name="namespace">The namespace of the script you wish to run.</param>
        /// <returns>true if the script is registered, false otherwise.</returns>
        public static bool IsScriptRegisteredByNamespace(string @namespace)
        {
            return _namespacePointers.ContainsKey(@namespace);
        }

        /// <summary>
        /// Loads a script from disk and compiles it using the Mono compiler.
        /// </summary>
        /// <param name="file">The path to the script file.</param>
        /// <returns>true if load is successful, false otherwise</returns>
        private static bool LoadScript(string file)
        {
            try
            {
                var script = CSScript.MonoEvaluator.LoadFile<IScript>(file);
                _scriptCache[file] = script;
                string @namespace = script.GetType().FullName;
                _namespacePointers[@namespace] = file;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine( "Failed to compile script: " + file + ". Exception: " + ex);

                if (_scriptCache.ContainsKey(file))
                {
                    // It's safe to assume this will always succeed.
                    // The reason being is that we only do parallelism
                    // during the boot-up sequence. Future accesses to the
                    // dictionary are on a single thread.
                    _scriptCache.TryRemove(file, out _);
                }

                return false;
            }
        }

        /// <summary>
        /// Runs the SubscribeEvents method on a particular script.
        /// </summary>
        /// <param name="file">The path to the script file.</param>
        /// <returns>true if events subscribed successfully, false otherwise</returns>
        private static bool SubscribeScriptEvents(string file)
        {
            try
            {
                _scriptCache[file].SubscribeEvents();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to subscribe events in script: " + file + ". Exception: " + ex);
                return false;
            }
        }

        /// <summary>
        /// Runs the UnsubscribeEvents method on a particular script.
        /// </summary>
        /// <param name="file">The path to the script file.</param>
        /// <returns>true if events unsubscribed successfully, false otherwise</returns>
        private static bool UnsubscribeScriptEvents(string file)
        {
            try
            {
                IScript script = _scriptCache[file];
                script.UnsubscribeEvents();
                return true;
            }
            catch(Exception ex)
            {
                Console.WriteLine("Failed to unsubscribe events in script: " + file + ". Exception: " + ex);
                return false;
            }
        }

        /// <summary>
        /// Unloads a script from the cache.
        /// </summary>
        /// <param name="file">The path to the script file.</param>
        /// <returns>true if script unloaded successfully, false otherwise</returns>
        private static bool UnloadScript(string file)
        {
            try
            {
                var namespacePointer = _namespacePointers.Values.Single(x => x == file);
                _namespacePointers.TryRemove(namespacePointer, out _);
                _scriptCache.TryRemove(file, out _);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine( "Failed to unload script: " + file + ". Exception: " + ex);
                return false;
            }
        }

        /// <summary>
        /// Loads a script, subscribes its events, and publishes a message on success.
        /// </summary>
        /// <param name="file">The path to the script file.</param>
        /// <returns>true if successful, false otherwise</returns>
        private static bool DoLoadScript(string file)
        {
            bool loadedSuccessfully = LoadScript(file);
            bool subscribedSuccessfully = SubscribeScriptEvents(file);

            if (loadedSuccessfully && subscribedSuccessfully)
            {
                Console.WriteLine(file + " compiled and subscribed successfully.");
                MessageHub.Instance.Publish(new OnScriptLoaded(_scriptCache[file]));
                return true;
            }

            return false;
        }

        /// <summary>
        /// Unloads a script, unsubscribes its events, and publishes a message on success.
        /// </summary>
        /// <param name="file">The path to the script file.</param>
        /// <returns>true if successful, false otherwise</returns>
        private static bool DoUnloadScript(string file)
        {
            // If a script fails to compile, it won't be in the cache. Simply return false.
            if (!_scriptCache.ContainsKey(file)) return false;

            bool unsubscribedSuccessfully = UnsubscribeScriptEvents(file);
            bool unloadedSuccessfully = UnloadScript(file);

            if (unsubscribedSuccessfully && unloadedSuccessfully)
            {
                Console.WriteLine(file + " unsubscribed and unloaded successfully.");
                MessageHub.Instance.Publish(new OnScriptUnloaded(file));
                return true;
            }

            return false;
        }

        /// <summary>
        /// Refreshes the same script from disk and publishes events if successful.
        /// </summary>
        /// <param name="file"></param>
        private static void DoReloadScript(string file)
        {
            bool unloadedSuccessfully = DoUnloadScript(file);

            if (unloadedSuccessfully)
            {
                DoLoadScript(file);
            }
        }


        /// <summary>
        /// Handles watching the executing directory for any plugins that are added, changed, or deleted.
        /// </summary>
        /// <param name="directory"></param>
        private static void StartFileWatcher(string directory)
        {
            // Per this post: https://stackoverflow.com/questions/31235034/filesystemwatcher-not-responding-to-file-events-in-folder-shared-by-virtual-mach
            // We have to use a polling configuration or else the file watcher won't pick up file changes.
            Environment.SetEnvironmentVariable("MONO_MANAGED_WATCHER", "1");

            Console.WriteLine("Watching directory: " + directory + " for script changes.");
            _watcher = new FileSystemWatcher
            {
                Path = directory,
                Filter = "*.cs",
                NotifyFilter = NotifyFilters.Attributes |
                               NotifyFilters.CreationTime |
                               NotifyFilters.FileName |
                               NotifyFilters.LastAccess |
                               NotifyFilters.LastWrite |
                               NotifyFilters.Size |
                               NotifyFilters.Security, 
                IncludeSubdirectories = true
            };

            _watcher.Changed += (sender, args) =>
            {
                DoReloadScript(args.FullPath);
            };
            _watcher.Created += (sender, args) =>
            {
                DoLoadScript(args.FullPath);
            };
            _watcher.Deleted += (sender, args) =>
            {
                DoUnloadScript(args.FullPath);
            };
            _watcher.Renamed += (sender, args) =>
            {
                DoReloadScript(args.FullPath);
            };
            _watcher.Error += (sender, args) =>
            {
                Console.WriteLine("File Watcher error: " + args.GetException());
            };

            _watcher.EnableRaisingEvents = true;
        }
    }
}