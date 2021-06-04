using GH_IO.Serialization;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection; 
using System.Windows.Forms;
using Tenrec.Components;

namespace Tenrec
{
    public static class Utils
    {
        #region Runner
        public static UnitTestState EvaluateGroup(GH_Group group, out List<ObjectMessage> log)
        {
            var state = UnitTestState.Untested;
            log = new List<ObjectMessage>();

            foreach(var obj in group.Objects())
            {
                if(obj is IGH_ActiveObject aobj)
                {
                    aobj.CollectData();
                    aobj.ComputeData();
                }
            }

            foreach (var obj in group.Objects())
            {
                if (obj is IGH_ActiveObject aobj)
                {
                    var level = aobj.RuntimeMessageLevel;
                    var messages = aobj.RuntimeMessages(level);
                    if (messages != null && messages.Count > 0)
                    {
                        var name = obj.NickName.Equals(obj.Name, StringComparison.OrdinalIgnoreCase) ? obj.Name : $"{obj.Name}({obj.NickName})";
                        log.Add(new ObjectMessage(name, level, messages, obj));
                        if ((state == UnitTestState.Untested || state == UnitTestState.Valid) &&
                            level == GH_RuntimeMessageLevel.Error)
                            state = UnitTestState.Failure;
                    }
                }
            }
           
            if (state == UnitTestState.Untested)
                state = UnitTestState.Valid;
            return state;
        }
        public static bool RunTenrecGroup(GH_Document doc, Guid tenrecGroupInstance, TestContext context)
        {
            if (!RhinoHeadless.Initialized)
                RhinoHeadless.Initialize();

            var obj = doc.FindObject(tenrecGroupInstance, false);
            if (obj == null)
                throw new Exception($"Group with id: {tenrecGroupInstance} not found");
            if (obj is GH_Group group)
            {
                var state = EvaluateGroup(group, out List<ObjectMessage> log);
                switch (state)
                {
                    case UnitTestState.Valid:
                        foreach (var l in log)
                        {
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
        public static bool RunTenrecGroups(GH_Document doc, TestContext context)
        {
            if (!RhinoHeadless.Initialized)
                RhinoHeadless.Initialize();
            return doc.Objects.OfType<GH_Group>()
                .All(obj => RunTenrecGroup(doc, obj.InstanceGuid, context));
        }
        public static bool RunTenrecGroup(string filePath, Guid tenrecGroupInstance, TestContext context)
        {
            if (!RhinoHeadless.Initialized)
                RhinoHeadless.Initialize();
            return OpenDocument(filePath, out GH_Document doc) && RunTenrecGroup(doc, tenrecGroupInstance, context);
        }
        public static bool RunTenrecGroups(string filePath, TestContext context)
        {
            if (!RhinoHeadless.Initialized)
                RhinoHeadless.Initialize();
            return OpenDocument(filePath, out GH_Document doc) && RunTenrecGroups(doc, context);
        }
        #endregion

        public static bool OpenDocument(string filePath, out GH_Document doc)
        { 
            var io = new GH_DocumentIO();
            if (!io.Open(filePath))
            {
                doc = null;
                throw new Exception($"Failed to open file: {filePath}");
            }
            doc = io.Document;
            doc.Enabled = true;
            return true;
        }

        #region Generator
        public static string CodeableNickname(string nickname)
        {
            return nickname.Replace(" ", "_");
        }
        public static string CreateAutoTestSourceFile(string[] ghTestFolders,
            string outputFolder, string outputName = "TenrecGeneratedTests",
            string language = "cs", string testFramework = "mstest")
        {
            if (ghTestFolders == null && ghTestFolders.Length == 0)
                throw new ArgumentNullException(nameof(outputFolder));
            if (string.IsNullOrEmpty(outputFolder))
                throw new ArgumentNullException(nameof(outputFolder));
            if (!language.Equals("cs"))
                throw new NotImplementedException(nameof(language));
            if (!testFramework.Equals("mstest"))
                throw new NotImplementedException(nameof(testFramework));

            var log = new System.Text.StringBuilder();
            var sb = new System.Text.StringBuilder();
            var exits = false;
            var fileName = string.Empty;
            try
            {
                sb.AppendLine("using Microsoft.VisualStudio.TestTools.UnitTesting;");
                sb.AppendLine();
                sb.AppendLine($"namespace TenrecGeneratedTests");
                sb.AppendLine("{");
                foreach (var folder in ghTestFolders)
                {
                    var files = Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories).Where(s => s.EndsWith(".gh") || s.EndsWith(".ghx"));
                    if (files != null && files.Any())
                    {
                        foreach (var file in files)
                        {
                            if (OpenDocument(file, out GH_Document doc))
                            {
                                var groups = new List<IGH_DocumentObject>();
                                foreach (var obj in doc.Objects)
                                {
                                    if (obj.ComponentGuid == Group_UnitTest.ID)
                                    {
                                        groups.Add(obj);
                                    }
                                }
                                if (groups != null && groups.Any())
                                {
                                    sb.AppendLine("    [TestClass]");
                                    sb.AppendLine($"    public class AutoTest_{CodeableNickname(doc.DisplayName)}");
                                    sb.AppendLine("    {");
                                    sb.AppendLine($"        public string FilePath => @\"{doc.FilePath}\";");
                                    sb.AppendLine("        private TestContext testContextInstance;");
                                    sb.AppendLine("        public TestContext TestContext { get => testContextInstance; set => testContextInstance = value; }");
                                    foreach (var group in groups)
                                    {
                                        sb.AppendLine("        [TestMethod]");
                                        sb.AppendLine($"        public void {CodeableNickname(group.NickName)}()");
                                        sb.AppendLine("        {");
                                        sb.AppendLine($"            Tenrec.Utils.RunTenrecGroup(FilePath, new System.Guid(\"{group.InstanceGuid}\"), TestContext);");
                                        sb.AppendLine("        }");
                                    }
                                    sb.AppendLine("    }");
                                    sb.AppendLine();
                                }
                            }
                            else
                            {
                                log.AppendLine($"File {file} failed to open.");
                            }
                        }
                    }
                }
                sb.AppendLine("}");

                fileName = Path.Combine(outputFolder, outputName + ".cs");
                exits = File.Exists(fileName);
                File.WriteAllText(fileName, sb.ToString());
            }
            catch (Exception e)
            {
                log.AppendLine($"EXCEPTION: {e}.");
            }

            if (exits)
                log.AppendLine($"File successfully overwritten.");
            else
                log.AppendLine($"File successfully created.");

            return log.ToString();
        }
        #endregion

        #region Types
        public static string GetAssemblyDirectory(Assembly assembly = null)
        {
            if (assembly == null)
                assembly = Assembly.GetExecutingAssembly();
            UriBuilder uri = new UriBuilder(assembly.CodeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path);
        }
        public static IEnumerable<Type> GetTypesFromAssembly(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e2)
            {
                return e2.Types.Where(t => t != null);
            }
        }
        public static IEnumerable<Type> GetTypesFromAssemblyAssignableFrom(Assembly assembly, Type type, bool ignoreAbstracts = true, bool ignoreGenerics = false, bool ignorePrivates = true)
        {
            foreach (var t in GetTypesFromAssembly(assembly))
            {
                if (t == type)
                    continue;
                if (ignoreAbstracts && t.IsAbstract)
                    continue;
                if (ignoreGenerics && t.IsGenericType)
                    continue;
                if (ignorePrivates && !t.IsPublic)
                    continue;
                if (!IsType(t, type))
                    continue;
                yield return t;
            }
        }
        public static bool IsType(Type toCheck, Type type)
        {
            if (toCheck == null || type == null)
                return false;
            if (toCheck == type)
                return true;
            if (type.IsInterface)
            {
                if (type.IsGenericType)
                    return toCheck.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == type);
                else
                    return type.IsAssignableFrom(toCheck);
            }
            else if (type.IsGenericType)
            {
                while (toCheck != null)
                {
                    var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                    if (cur == type)
                        return true;
                    toCheck = toCheck.BaseType;
                }
            }
            else
            {
                while (toCheck != null)
                {
                    if (toCheck == type)
                        return true;
                    toCheck = toCheck.BaseType;
                }
            }

            return false;
        }
        public static IEnumerable<Assembly> LoadTenrecAssembliesFrom(string directory = "")
        {
            if (string.IsNullOrEmpty(directory))
                directory = GetAssemblyDirectory();// AppDomain.CurrentDomain.BaseDirectory;
            else
                if (!System.IO.Directory.Exists(directory))
                throw new DirectoryNotFoundException(directory);
            var assName = typeof(Group_UnitTest).Assembly.GetName().Name;
            foreach (var ass in Directory.GetFiles(directory, "*.dll")
                .Concat(Directory.GetFiles(directory, "*.gha")))
            {
                Assembly a = null;
                try
                {
                    var n = AssemblyName.GetAssemblyName(ass);
                    if (n == null)
                        continue;
                    a = Assembly.Load(n);
                    if (!(a.GetName().Name.Equals(assName) ||
                    a.GetReferencedAssemblies().Any(ra => ra.Name.Equals(assName))
                      && !Guid.TryParse(a.GetName().Name, out _)))
                        continue;
                }
                catch (Exception e)
                {

                }
                if (a == null)
                    continue;
                yield return a;
            }

        }
        #endregion
    }

}

namespace Tenrec.UI 
{ 
    public class CanvasLog : IDisposable
    {
        #region Fields
        private GH_Canvas _canvas;
        private int _width;
        private Timer _timer;
        private System.Text.StringBuilder _sb;
        private List<LogItem> _items;
        private List<Tuple<string, int, bool>> _itemsToAdd;
        private StringFormat _sf;
        private Font _font;
        private const float _margin = 6f;
        private static CanvasLog _instance;
        #endregion

        #region Properties
        public static CanvasLog Instance
        {
            get
            {
                return _instance;
            }
            internal set
            {
                _instance = value;
            }
        }
        #endregion

        #region Constructors 
        public CanvasLog(GH_Canvas canvas, int width = 400, StringAlignment align = StringAlignment.Far)
        {
            _canvas = canvas;
            _canvas.CanvasPaintEnd -= _canvas_CanvasPaintEnd;
            _canvas.CanvasPaintEnd += _canvas_CanvasPaintEnd;
            _width = width;
            _timer = new Timer();
            _timer.Interval = 50;
            _timer.Tick += _timer_Tick;
            _timer.Tag = 0;
            _sb = new System.Text.StringBuilder();
            _items = new List<LogItem>();
            _itemsToAdd = new List<Tuple<string, int, bool>>();
            _sf = new StringFormat() { Alignment = align, LineAlignment = StringAlignment.Near };
            _font = GH_FontServer.NewFont(GH_FontServer.FamilyStandard,
              16f / Grasshopper.GUI.GH_GraphicsUtil.UiScale, FontStyle.Regular);
        }
        #endregion

        #region Methods 
        public void Clear()
        {
            _items.Clear();
            _itemsToAdd.Clear();
            _sb.Clear();
        }

        public void WriteLine(string text, int ms = 2000, bool locked = false)
        {
            _timer.Start();
            _sb.AppendLine(text);
            _itemsToAdd.Add(new Tuple<string, int, bool>(text, ms, locked));
        }

        private void _timer_Tick(object sender, EventArgs e)
        {
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                var item = _items[i];
                if (item.Locked)
                    continue;
                item.LifeTime -= _timer.Interval;
                if (item.LifeTime <= 0)
                {
                    _items.Remove(item);
                }
            }
            if (_sf.LineAlignment == StringAlignment.Near)
            {
                for (int i = 0; i < _items.Count; i++)
                {
                    var y0 = i == 0 ? _margin : _items[i - 1].Bounds.Bottom;
                    var y1 = _items[i].Bounds.Top;
                    var dy = y1 - y0;
                    if (dy > _margin)
                    {
                        var r = _items[i].Bounds;
                        r.Offset(0, -Math.Max(5, dy * 0.5f));
                        _items[i].Bounds = r;
                    }
                }
            }

            _canvas.Invalidate();

            if (_items.Count == 0)
            {
                _timer.Stop();
            }
        }

        private void _canvas_CanvasPaintEnd(GH_Canvas sender)
        {
            if (_itemsToAdd.Count > 0)
            {
                foreach (var item in _itemsToAdd)
                {
                    _items.Add(new LogItem(CreateNextBounds(item.Item1), item.Item1, item.Item2, item.Item3));
                }
                _itemsToAdd.Clear();
            }
            var xf = sender.Graphics.Transform;
            sender.Graphics.ResetTransform();
            sender.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            var color = GH_Skin.canvas_edge;
            foreach (var item in _items)
            {
                using (var brh = new SolidBrush(Color.FromArgb(Math.Min(255, item.LifeTime), color)))
                {
                    sender.Graphics.DrawString(item.Text, _font, brh, item.Bounds, _sf);
                }
            }
            sender.Graphics.Transform = xf;
        }

        private RectangleF CreateNextBounds(string text)
        {
            var size = _canvas.Graphics.MeasureString(text, _font, _width, _sf);
            float x = 0f, y = 0f;
            if (_items.Count == 0)
            {
                switch (_sf.LineAlignment)
                {
                    case StringAlignment.Near:
                        y = _margin;
                        break;
                    case StringAlignment.Center:
                        y = _canvas.ClientRectangle.Y + _canvas.ClientRectangle.Height / 2 - size.Height;
                        break;
                    case StringAlignment.Far:
                        y = _canvas.ClientRectangle.Height - size.Height - _margin;
                        break;
                }
            }
            else
            {
                y = _items.Last().Bounds.Bottom + _margin;
            }
            switch (_sf.Alignment)
            {
                case StringAlignment.Near:
                    x = _margin;
                    break;
                case StringAlignment.Center:
                    x = _canvas.ClientRectangle.X + _canvas.ClientRectangle.Width / 2 - size.Width;
                    break;
                case StringAlignment.Far:
                    x = _canvas.ClientRectangle.Width - size.Width - _margin;
                    break;
            }

            return new RectangleF(x, y, size.Width, size.Height);
        }

        public void Dispose()
        {
            _canvas.CanvasPaintEnd -= _canvas_CanvasPaintEnd;
            if (_font != null)
            {
                _font.Dispose();
                _font = null;
            }
            if (_sf != null)
            {
                _sf.Dispose();
                _sf = null;
            }
        }

        public override string ToString()
        {
            return _sb.ToString();
        }
        #endregion

        public class LogItem
        {
            public RectangleF Bounds { get; set; }
            public string Text { get; private set; }
            public int LifeTime { get; set; }
            public bool Locked { get; set; }
            public LogItem(RectangleF bounds, string text, int lifeTime, bool locked = false)
            {
                Bounds = bounds;
                Text = text;
                LifeTime = lifeTime;
                Locked = locked;
            }
        }
    }
}
