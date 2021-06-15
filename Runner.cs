using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Tenrec
{ 
    [TestClass]
    public static class Runner
    {
        private static Rhino.Runtime.InProcess.RhinoCore _rhinoCore;

        public static List<Assembly> MissingLibraries { get; private set; }

        static Runner()
        {
            MissingLibraries = new List<Assembly>();
            RhinoInside.Resolver.Initialize();
            //System.Windows.Forms.MessageBox.Show("Runner Resolver");
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }
       
        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name).Name; 
            string path = System.IO.Path.Combine(RhinoInside.Resolver.RhinoSystemDirectory, assemblyName + ".dll");
            if (System.IO.File.Exists(path))
                return Assembly.LoadFrom(path);

            if (assemblyName.Equals("Grasshopper") || assemblyName.Contains("GH_IO"))
            {
                path = System.IO.Path.Combine(System.IO.Directory.GetParent(RhinoInside.Resolver.RhinoSystemDirectory).FullName, "Plug-ins", "Grasshopper", assemblyName + ".dll");
                if (System.IO.File.Exists(path))
                    return Assembly.LoadFrom(path);
            }

            if (assemblyName.Contains(".resources"))
                return null;

            MissingLibraries.Add(args.RequestingAssembly);
         
            return null;
        }

        [STAThread] 
        public static void Initialize(TestContext context = null)
        {
            if(_rhinoCore == null)
            {
                //System.Windows.Forms.MessageBox.Show("Runner INITIALIZE");
                _rhinoCore = new Rhino.Runtime.InProcess.RhinoCore();
                //LoadGrasshopper(context);
            } 
        }
        private static void LoadGrasshopper(TestContext context)
        {
            //foreach(var error in Grasshopper.Instances.ComponentServer.LoadingExceptions)
            //{
            //    context.WriteLine($"{error.Name}: {error.Type}, {error.Message}");
            //}
            //context.WriteLine("");

            //foreach(var ef in Grasshopper.Kernel.GH_ComponentServer.ExternalFiles(true,true))
            //{
            //    context.WriteLine($"ex... {ef.FileName} | {ef.FilePath}");
            //}
            //context.WriteLine("");

            //var server = new Grasshopper.Kernel.GH_ComponentServer();
            //server.GHAFileLoaded += (s, e) => {
            //    context.WriteLine($"loading... {e.Name} | {e.FileName}");
            //};
            //server.LoadExternalFiles(false);
            //context.WriteLine("");

            //foreach (var lib in Grasshopper.Instances.ComponentServer.Libraries.OrderBy(l => l.Name))
            //{
            //    context.WriteLine($"{lib.Name} | {lib.Location}");
            //}
            //context.WriteLine("");
        }
         
        [AssemblyCleanup]
        public static void Dispose()
        {
            //System.Windows.Forms.MessageBox.Show("Runner DISPOSE");
            if(_rhinoCore != null)
            {
                _rhinoCore.Dispose();
                _rhinoCore = null;
            } 
        }

        /// <summary>
        /// Compute a <see cref="Group_UnitTest"/> and finds all errors or messages for each of the objects it contains. 
        /// </summary>
        /// <param name="group">The <see cref="Group_UnitTest"/> containing the objects to evaluate.</param>
        /// <param name="log">The output messages.</param>
        /// <returns>Resulting unit test state.</returns>
        private static UnitTestState EvaluateGroup(Grasshopper.Kernel.Special.GH_Group group, TestContext context, out List<ObjectMessage> log)
        {
            var state = UnitTestState.Untested;
            log = new List<ObjectMessage>();
             
            if (context != null)
            {
                foreach (var obj in group.Objects())
                {
                    var type = obj.GetType().ToString();
                    if (type == "Grasshopper.Kernel.Components.GH_PlaceholderParameter" || type == "Grasshopper.Kernel.Components.GH_PlaceholderComponent")
                    {
                        context.WriteLine($"Placeholder found! {obj.Name} could not be loaded into the document.");
                    }
                }
            }

            foreach (var obj in group.Objects())
            {
                if (obj is Grasshopper.Kernel.IGH_ActiveObject aobj)
                {
                    aobj.CollectData();
                    aobj.ComputeData();
                }
            }

            foreach (var obj in group.Objects())
            {
                if (obj is Grasshopper.Kernel.IGH_ActiveObject aobj)
                {
                    var level = aobj.RuntimeMessageLevel;
                    var messages = aobj.RuntimeMessages(level);
                    if (messages != null && messages.Count > 0)
                    {
                        var name = obj.NickName.Equals(obj.Name, StringComparison.OrdinalIgnoreCase) ? obj.Name : $"{obj.Name}({obj.NickName})";
                        log.Add(new ObjectMessage(name, level, messages, obj));
                        if ((state == UnitTestState.Untested || state == UnitTestState.Valid) &&
                            level == Grasshopper.Kernel.GH_RuntimeMessageLevel.Error)
                            state = UnitTestState.Failure;
                    }
                }
            }

            if (state == UnitTestState.Untested)
                state = UnitTestState.Valid;
            return state;
        }
        /// <summary>
        /// Evaluate a specific <see cref="Group_UnitTest"/> in a Grasshopper document. 
        /// </summary>
        /// <param name="doc">The Grasshopper document.</param>
        /// <param name="tenrecGroupInstance">The <see cref="Group_UnitTest"/> instance ID.</param>
        /// <param name="context">The context to send messages to the unit test output.</param>
        /// <returns>True if the test was executed, false otherwise.</returns>
        public static bool RunTenrecGroup(Grasshopper.Kernel.GH_Document doc, Guid tenrecGroupInstance, TestContext context)
        {
            var obj = doc.FindObject(tenrecGroupInstance, false);
            if (obj == null)
                throw new Exception($"Group with id: {tenrecGroupInstance} not found");
            if (obj is Grasshopper.Kernel.Special.GH_Group group)
            {
                var state = EvaluateGroup(group, context, out List<ObjectMessage> log);
                switch (state)
                {
                    case UnitTestState.Valid:
                        foreach (var l in log)
                        {
                            if (context != null)
                                context.WriteLine(l.ToString());
                        }
                        break;
                    case UnitTestState.Failure:
                        foreach (var l in log)
                        {
                            throw new AssertFailedException(l.ToString());
                        }
                        break;
                    case UnitTestState.Untested:
                        throw new Exception($"Group {group.NickName} could not be tested");
                }

                return true;
            }
            return false;

        }
        /// <summary>
        /// Evaluate all <see cref="Group_UnitTest"/> that contains a Grasshopper document. 
        /// </summary>
        /// <param name="doc">The Grasshopper document.</param>
        /// <param name="context">The context to send messages to the unit test output.</param>
        /// <returns>True if the test was executed, false otherwise.</returns>
        public static bool RunTenrecGroups(Grasshopper.Kernel.GH_Document doc, TestContext context)
        {
            return doc.Objects.OfType<Grasshopper.Kernel.Special.GH_Group>()
                .All(obj => RunTenrecGroup(doc, obj.InstanceGuid, context));
        }
        /// <summary>
        /// Evaluate a specific <see cref="Group_UnitTest"/> in a Grasshopper file. 
        /// </summary>
        /// <param name="filePath">The Grasshopper file path.</param>
        /// <param name="tenrecGroupInstance">The <see cref="Group_UnitTest"/> instance ID.</param>
        /// <param name="context">The context to send messages to the unit test output.</param>
        /// <returns>True if the test was executed, false otherwise.</returns>
        public static bool RunTenrecGroup(string filePath, Guid tenrecGroupInstance, TestContext context)
        {
            return OpenDocument(filePath, context, out Grasshopper.Kernel.GH_Document doc) && RunTenrecGroup(doc, tenrecGroupInstance, context);
        }
        /// <summary>
        /// Evaluate all <see cref="Group_UnitTest"/> that contains a Grasshopper file. 
        /// </summary>
        /// <param name="filePath">The Grasshopper file path.</param>
        /// <param name="context">The context to send messages to the unit test output.</param>
        /// <returns>True if the test was executed, false otherwise.</returns>
        public static bool RunTenrecGroups(string filePath, TestContext context)
        {
            return OpenDocument(filePath, context, out Grasshopper.Kernel.GH_Document doc) && RunTenrecGroups(doc, context);
        }
        /// <summary>
        /// Get a Grasshopper document from a Grasshopper file path.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="doc">The resulting Grasshopper document.</param>
        /// <returns>True if the file could be opened, false otherwise.</returns>
        private static bool OpenDocument(string filePath, TestContext context, out Grasshopper.Kernel.GH_Document doc)
        { 
            //var io = new GH_DocumentIO();
            //if (!io.Open(filePath))
            //{
            //    doc = null;
            //    throw new Exception($"Failed to open file: {filePath}");
            //} 
            //doc = io.Document;
            //doc.Enabled = true;

            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));

            if (!System.IO.File.Exists(filePath))
                throw new Exception($"File not found: {filePath}");

            doc = new Grasshopper.Kernel.GH_Document();
            var arc = new GH_IO.Serialization.GH_Archive();
            if (!arc.ReadFromFile(filePath))
            {
                OutputMessages(context, arc, true);
                return false;
            }
            OutputMessages(context, arc, false);
            arc.ClearMessages();

            var extracted = arc.ExtractObject(doc, "Definition");
            OutputMessages(context, arc, false);
             
            doc.DestroyProxySources();
            doc.Enabled = true;

            return extracted;

            void OutputMessages(TestContext c, GH_IO.Serialization.GH_Archive a, bool includeInfo)
            {
                if (c != null && a.MessageCount(includeInfo, true, true) > 0)
                    foreach (var m in a.Messages)
                        c.WriteLine($"{m.Type}: {m.Message}");
            }
        }
    }

    ///// <summary>
    ///// Evaluator to test if Grasshopper files contains errors or runtime messages.
    ///// </summary>
    //public static class RunnerOBSOLETE
    //{
    //    /// <summary>
    //    /// Compute a <see cref="Group_UnitTest"/> and finds all errors or messages for each of the objects it contains. 
    //    /// </summary>
    //    /// <param name="group">The <see cref="Group_UnitTest"/> containing the objects to evaluate.</param>
    //    /// <param name="log">The output messages.</param>
    //    /// <returns>Resulting unit test state.</returns>
    //    public static UnitTestState EvaluateGroup(GH_Group group, out List<ObjectMessage> log)
    //    {
    //        var state = UnitTestState.Untested;
    //        log = new List<ObjectMessage>();

    //        foreach (var obj in group.Objects())
    //        {
    //            if (obj is IGH_ActiveObject aobj)
    //            {
    //                aobj.CollectData();
    //                aobj.ComputeData();
    //            }
    //        }

    //        foreach (var obj in group.Objects())
    //        {
    //            if (obj is IGH_ActiveObject aobj)
    //            {
    //                var level = aobj.RuntimeMessageLevel;
    //                var messages = aobj.RuntimeMessages(level);
    //                if (messages != null && messages.Count > 0)
    //                {
    //                    var name = obj.NickName.Equals(obj.Name, StringComparison.OrdinalIgnoreCase) ? obj.Name : $"{obj.Name}({obj.NickName})";
    //                    log.Add(new ObjectMessage(name, level, messages, obj));
    //                    if ((state == UnitTestState.Untested || state == UnitTestState.Valid) &&
    //                        level == GH_RuntimeMessageLevel.Error)
    //                        state = UnitTestState.Failure;
    //                }
    //            }
    //        }

    //        if (state == UnitTestState.Untested)
    //            state = UnitTestState.Valid;
    //        return state;
    //    }
    //    /// <summary>
    //    /// Evaluate a specific <see cref="Group_UnitTest"/> in a Grasshopper document. 
    //    /// </summary>
    //    /// <param name="doc">The Grasshopper document.</param>
    //    /// <param name="tenrecGroupInstance">The <see cref="Group_UnitTest"/> instance ID.</param>
    //    /// <param name="context">The context to send messages to the unit test output.</param>
    //    /// <returns>True if the test was executed, false otherwise.</returns>
    //    public static bool RunTenrecGroup(GH_Document doc, Guid tenrecGroupInstance, TestContext context)
    //    {
    //        RhinoHeadless.Initialize();

    //        var obj = doc.FindObject(tenrecGroupInstance, false);
    //        if (obj == null)
    //            throw new Exception($"Group with id: {tenrecGroupInstance} not found");
    //        if (obj is GH_Group group)
    //        {
    //            var state = EvaluateGroup(group, out List<ObjectMessage> log);
    //            switch (state)
    //            {
    //                case UnitTestState.Valid:
    //                    foreach (var l in log)
    //                    {
    //                        context.WriteLine(l.ToString());
    //                    }
    //                    break;
    //                case UnitTestState.Failure:
    //                    foreach (var l in log)
    //                    {
    //                        throw new AssertFailedException(l.ToString());
    //                    }
    //                    break;
    //                case UnitTestState.Untested:
    //                    throw new Exception($"Group {group.NickName} could not be tested");
    //            }

    //            return true;
    //        }
    //        return false;

    //    }
    //    /// <summary>
    //    /// Evaluate all <see cref="Group_UnitTest"/> that contains a Grasshopper document. 
    //    /// </summary>
    //    /// <param name="doc">The Grasshopper document.</param>
    //    /// <param name="context">The context to send messages to the unit test output.</param>
    //    /// <returns>True if the test was executed, false otherwise.</returns>
    //    public static bool RunTenrecGroups(GH_Document doc, TestContext context)
    //    {
    //        RhinoHeadless.Initialize();

    //        return doc.Objects.OfType<GH_Group>()
    //            .All(obj => RunTenrecGroup(doc, obj.InstanceGuid, context));
    //    }
    //    /// <summary>
    //    /// Evaluate a specific <see cref="Group_UnitTest"/> in a Grasshopper file. 
    //    /// </summary>
    //    /// <param name="filePath">The Grasshopper file path.</param>
    //    /// <param name="tenrecGroupInstance">The <see cref="Group_UnitTest"/> instance ID.</param>
    //    /// <param name="context">The context to send messages to the unit test output.</param>
    //    /// <returns>True if the test was executed, false otherwise.</returns>
    //    public static bool RunTenrecGroup(string filePath, Guid tenrecGroupInstance, TestContext context)
    //    {
    //        RhinoHeadless.Initialize();

    //        return OpenDocument(filePath, context, out GH_Document doc) && RunTenrecGroup(doc, tenrecGroupInstance, context);
    //    }
    //    /// <summary>
    //    /// Evaluate all <see cref="Group_UnitTest"/> that contains a Grasshopper file. 
    //    /// </summary>
    //    /// <param name="filePath">The Grasshopper file path.</param>
    //    /// <param name="context">The context to send messages to the unit test output.</param>
    //    /// <returns>True if the test was executed, false otherwise.</returns>
    //    public static bool RunTenrecGroups(string filePath, TestContext context)
    //    {
    //        RhinoHeadless.Initialize();

    //        return OpenDocument(filePath, context, out GH_Document doc) && RunTenrecGroups(doc, context);
    //    }
    //    /// <summary>
    //    /// Get a Grasshopper document from a Grasshopper file path.
    //    /// </summary>
    //    /// <param name="filePath">The file path.</param>
    //    /// <param name="doc">The resulting Grasshopper document.</param>
    //    /// <returns>True if the file could be opened, false otherwise.</returns>
    //    public static bool OpenDocument(string filePath, TestContext context, out GH_Document doc)
    //    {
    //        //var io = new GH_DocumentIO();
    //        //if (!io.Open(filePath))
    //        //{
    //        //    doc = null;
    //        //    throw new Exception($"Failed to open file: {filePath}");
    //        //} 
    //        //doc = io.Document;
    //        //doc.Enabled = true;

    //        if (string.IsNullOrEmpty(filePath))
    //            throw new ArgumentNullException(nameof(filePath));

    //        if (!System.IO.File.Exists(filePath))
    //            throw new Exception($"File not found: {filePath}");

    //        doc = new GH_Document();
    //        var arc = new GH_IO.Serialization.GH_Archive();
    //        if (!arc.ReadFromFile(filePath))
    //        {
    //            OutputMessages(context, arc, true);
    //            return false;
    //        }
    //        OutputMessages(context, arc, false);
    //        arc.ClearMessages();

    //        var extracted = arc.ExtractObject(doc, "Definition");
    //        OutputMessages(context, arc, false);

    //        if(context != null)
    //        { 
    //            foreach (var obj in doc.Objects)
    //            {
    //                var type = obj.GetType().ToString();
    //                if (type == "Grasshopper.Kernel.Components.GH_PlaceholderParameter" || type == "Grasshopper.Kernel.Components.GH_PlaceholderComponent")
    //                {
    //                    context.WriteLine($"Placeholder found! {obj.Name} could not be loaded into the document.");
    //                }
    //            } 
    //        }
        
    //        doc.DestroyProxySources();
    //        doc.Enabled = true;

    //        return extracted;

    //        void OutputMessages(TestContext c, GH_IO.Serialization.GH_Archive a, bool includeInfo)
    //        {
    //            if (c != null && a.MessageCount(includeInfo, true, true) > 0)
    //                foreach (var m in a.Messages)
    //                    c.WriteLine($"{m.Type}: {m.Message}");
    //        }
    //    }
    //}

}
