using System;
using System.IO;
using System.Reflection; 
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tenrec
{
    [TestClass]
    public static class RhinoHeadless
    { 
        private static Rhino.Runtime.InProcess.RhinoCore _rhinoCore;
        public static bool Initialized { get; private set; }

        static RhinoHeadless()
        {
            RhinoInside.Resolver.Initialize();
        }

        [AssemblyInitialize]
        //[STAThread]
        public static void Initialize(TestContext context = null)
        {
            if (!Initialized)
            { 
                //AppDomain.CurrentDomain.AssemblyResolve += ResolveAssemblies;
                _rhinoCore = new Rhino.Runtime.InProcess.RhinoCore();
                Initialized = true;
                System.Windows.Forms.MessageBox.Show("Initialize");
            }
        }

        [AssemblyCleanup]
        public static void Dispose()
        {
            if (_rhinoCore != null)
            {
                _rhinoCore.Dispose();
                _rhinoCore = null; 
                Initialized = false;
                //System.Windows.Forms.MessageBox.Show("Dispose");
            }
        }

        public static Assembly ResolveAssemblies(object sender, ResolveEventArgs args)
        {
            var name = new AssemblyName(args.Name).Name;
            if (name.EndsWith(".resources"))
                return null; 
            var folder = string.Empty;
            if (name.Equals("Grasshopper") || name.Equals("GH_IO"))
            {
                folder = Path.Combine(Directory.GetParent(RhinoInside.Resolver.RhinoSystemDirectory).FullName, "Plug-ins", "Grasshopper");
            } 
            else
            {
                folder = RhinoInside.Resolver.RhinoSystemDirectory;
            }
            var path = Path.Combine(folder, name + ".dll");
            if (File.Exists(path))
            {
                return Assembly.LoadFrom(path);
            }
          
            return null;
        }
    }

}
