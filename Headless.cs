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
        public static void Initialize(TestContext context = null)
        {
            if (!Initialized)
            {
                //System.Windows.Forms.MessageBox.Show("1");
                AppDomain.CurrentDomain.AssemblyResolve += ResolveAssemblies;
                _rhinoCore = new Rhino.Runtime.InProcess.RhinoCore();
                Initialized = true;
                //System.Windows.Forms.MessageBox.Show("Initialize");
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

        private static Assembly ResolveAssemblies(object sender, ResolveEventArgs args)
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

    //public static class TenrecHeadlessOBSOLETE
    //{
    //    private static bool _initialized;
    //    private static Rhino.Runtime.InProcess.RhinoCore _rhinoCore;
    //    private static Grasshopper.Plugin.GH_RhinoScriptInterface _grasshopper;
    //    private static readonly Guid _grasshopperGuid = new Guid(0xB45A29B1, 0x4343, 0x4035, 0x98, 0x9E, 0x04, 0x4E, 0x85, 0x80, 0xD9, 0xCF);

    //    public static bool Initialized => _initialized;

    //    static TenrecHeadlessOBSOLETE()
    //    { 
    //        if (!Environment.Is64BitProcess)
    //            throw new Exception("Only 64 bit applications can be used");

    //        RhinoInside.Resolver.Initialize();  
    //    }

    //    [AssemblyInitialize]
    //    public static void Initialize(TestContext context)
    //    { 
    //        if (!_initialized)
    //        {
    //            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssemblies;
    //            StartRhino(); 
    //            _initialized = true;
    //            System.Windows.Forms.MessageBox.Show("INITIALIZED"); 
    //        } 
    //    } 

    //    [STAThread]
    //    static void StartRhino()
    //    {
    //        if(_rhinoCore == null)
    //        {
    //            _rhinoCore = new Rhino.Runtime.InProcess.RhinoCore(); 
    //            //StartGrasshopper();
    //        }
           
    //    }
    //    static void StartGrasshopper()
    //    {
    //        if (!Rhino.PlugIns.PlugIn.LoadPlugIn(_grasshopperGuid))
    //            throw new Exception("Grasshopper could not be loaded");
    //        _grasshopper = new Grasshopper.Plugin.GH_RhinoScriptInterface();
    //        _grasshopper.RunHeadless();

    //    }
    //    private static Assembly ResolveAssemblies(object sender, ResolveEventArgs args)
    //    { 
    //        var name = new AssemblyName(args.Name).Name;
    //        if (name.EndsWith(".resources"))
    //            return null;

    //        var folder = string.Empty;
    //        if (name.Equals("Grasshopper") || name.Equals("GH_IO"))
    //        {
    //            folder = Path.Combine(Directory.GetParent(RhinoInside.Resolver.RhinoSystemDirectory).FullName, "Plug-ins", "Grasshopper");
    //        }
    //        else
    //        {
    //            folder = RhinoInside.Resolver.RhinoSystemDirectory;
    //        }
    //        var path = Path.Combine(folder, name + ".dll");
    //        if (File.Exists(path))
    //        {
    //            return Assembly.LoadFrom(path);
    //        }
    //        if (name.Contains("VisualStudio"))
    //        {
    //            folder = Path.GetDirectoryName(typeof(TenrecHeadlessOBSOLETE).Assembly.Location);
    //            path = Path.Combine(folder, name + ".dll");
    //            if (File.Exists(path))
    //            {
    //                return Assembly.LoadFrom(path);
    //            }
    //        }
          
    //        return null;
    //    }

    //    [AssemblyCleanup]
    //    public static void Dispose()
    //    {
    //        if (_rhinoCore != null)
    //        {
    //            _rhinoCore.Dispose();
    //            _rhinoCore = null;
    //            _grasshopper = null;
    //            _initialized = false;
    //        }
    //        System.Windows.Forms.MessageBox.Show("DISPOSED");
    //    }
    //}

}
