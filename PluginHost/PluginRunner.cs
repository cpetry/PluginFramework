using PluginFramework.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Registration;
using System.IO;
using System.Linq;

namespace PluginFramework {

    public class Runner : MarshalByRefObject {
        private CompositionContainer _container;
        private DirectoryCatalog _directoryCatalog;
        private List<ILogger> _logger;
        private AppDomainArgs _appDomainArgs;
        private static readonly string _pluginPath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "Plugins");
        private static readonly string _tempPath = Path.Combine(_pluginPath, "Temp"); // needed for forced updated von changed files

        public class AppDomainArgs : MarshalByRefObject
        {
            public string StringArg { get; set; }
        }

        public void CreateSeparateDomain() {

            // Use RegistrationBuilder to set up our MEF parts.
            var regBuilder = new RegistrationBuilder();
            regBuilder.ForTypesDerivedFrom<Interfaces.ILogger>().Export<Interfaces.ILogger>();
            
            var catalog = new AggregateCatalog();
            catalog.Catalogs.Add(new AssemblyCatalog(typeof(Runner).Assembly, regBuilder));
            _directoryCatalog = new DirectoryCatalog(_pluginPath, regBuilder);
            catalog.Catalogs.Add(_directoryCatalog);

            _container = new CompositionContainer(catalog, isThreadSafe: true);
            _container.ComposeExportedValue(_container);

            // Get our exports available to the rest of Program.
            _logger = _container.GetExports<Interfaces.ILogger>().Select(x => x.Value).ToList();
        }
        
        public void Recompose()
        {
            https://stackoverflow.com/questions/14808762/facing-error-during-catalog-refresh-the-new-dll-is-not-used/14842417#14842417
            // Gimme 3 steps...
            // move file, refresh, move file back, refresh
            _directoryCatalog.Refresh();
            _container.ComposeParts(_directoryCatalog.Parts);
            _logger = _container.GetExports<Interfaces.ILogger>().Select(x => x.Value).ToList();
            Console.WriteLine("{0} exports in AppDomain {1}", _logger.Count(), AppDomain.CurrentDomain.FriendlyName);
        }

        public void RecomposeUpdatedFile(string fileName)
        {
            var pluginFilePath = Path.Combine(_pluginPath,fileName);
            var tempPath = Path.Combine(_tempPath,fileName);
            Directory.CreateDirectory(_tempPath);
            File.Move(pluginFilePath, tempPath);
            Recompose();
            File.Move(tempPath, pluginFilePath);
            Directory.Delete(_tempPath);
            Recompose();
        }

        public void DoTest() {
            // Tell our MEF parts to do something.
            _logger.ForEach(e => e.Log("Test"));
        }  
    }
}