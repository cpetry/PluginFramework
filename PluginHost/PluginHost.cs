using System;
using System.IO;
using System.Security.Permissions;
using System.Timers;

namespace PluginFramework {
    internal class PluginHost {
        private static AppDomain _domain;
        private static Runner _runner;
        private static FileSystemWatcher _watcher;

        [STAThread]
        private static void Main() {
            AppDomain root = AppDomain.CurrentDomain;
            var cachePath = Path.Combine(root.SetupInformation.ApplicationBase, "ShadowCopyCache");
            var pluginPath = Path.Combine(root.SetupInformation.ApplicationBase, "Plugins");
            if (!Directory.Exists(cachePath)) {
                Directory.CreateDirectory(cachePath);
            }

            if (!Directory.Exists(pluginPath)) {
                Directory.CreateDirectory(pluginPath);
            }

            // This creates a ShadowCopy of the MEF DLL's 
            // (and any other DLL's in the ShadowCopyDirectories)
            var setup = new AppDomainSetup
            {
                CachePath = cachePath,
                ShadowCopyFiles = "true",
                ShadowCopyDirectories = pluginPath
            };

            // Create a new AppDomain then create a new instance 
            // of this application in the new AppDomain.            
            _domain = AppDomain.CreateDomain("Host_AppDomain", AppDomain.CurrentDomain.Evidence, setup);
            _runner = (Runner)_domain.CreateInstanceAndUnwrap(assemblyName: typeof(Runner).Assembly.FullName, typeName: typeof(Runner).FullName);
            
            Console.WriteLine("The main AppDomain is: {0}", AppDomain.CurrentDomain.FriendlyName);

            // Create domain and export interfaces
            _runner.CreateSeparateDomain();

            bool wantsToQuit = false;
            SetTimer();
            SetFileWatcher(pluginPath);

            Console.WriteLine("Press Enter key to quit...");
            while (!wantsToQuit)
            {
                if (Console.KeyAvailable)
                {
                    wantsToQuit = Console.ReadKey().Key == ConsoleKey.Enter;
                }
            }

            // Clean up.
            AppDomain.Unload(_domain);
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        private static void SetFileWatcher(string pluginPath)
        {
            // Create a new FileSystemWatcher and set its properties.
            _watcher = new FileSystemWatcher(pluginPath, "*.dll");
            // Watch for changes in LastWrite times
            _watcher.NotifyFilter = NotifyFilters.LastWrite
                | NotifyFilters.CreationTime
                | NotifyFilters.FileName;

            _watcher.IncludeSubdirectories = false;

            // Add event handlers.
            _watcher.Changed += OnFileChanged;
            _watcher.Created += OnFileCreated;
            _watcher.Deleted += OnFileDeleted;

            // Begin watching.
            _watcher.EnableRaisingEvents = true;
        }

        // Define the event handlers.
        private static void OnFileChanged(object source, FileSystemEventArgs e)
        {
            _watcher.EnableRaisingEvents = false;
            Console.WriteLine("Updated " + e.Name);
            _runner.RecomposeUpdatedFile(e.Name);
            _watcher.EnableRaisingEvents = true;
        }
        
        private static void OnFileCreated(object source, FileSystemEventArgs e)
        {
            Console.WriteLine("Added " + e.Name);
            _runner.Recompose();
        }

        private static void OnFileDeleted(object source, FileSystemEventArgs e)
        {
            Console.WriteLine("Deleted " + e.Name);
            _runner.Recompose();
        }

        private static void SetTimer()
        {
            // Create a timer with a two second interval.
            var aTimer = new System.Timers.Timer(2000);
            // Hook up the Elapsed event for the timer. 
            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
        }

        private static void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            _runner.DoTest();
        }
    }
}