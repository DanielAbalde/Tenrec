using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel; 
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using Tenrec.UI;

namespace Tenrec.Components
{
    // TODO MAKE MENU OPT STATIC TO ALL GROUPS
    // double click create panel

    public enum UnitTestState { Untested = -1, Failure = 0, Valid = 1 }

    public class ObjectMessage
    {
        public RectangleF Bounds { get; set; }
        public string Name { get; private set; }
        public GH_RuntimeMessageLevel Level { get; set; }
        public IList<string> Messages { get; set; }
        public IGH_DocumentObject Object { get; set; }

        public ObjectMessage(string name, GH_RuntimeMessageLevel level, IList<string> messages, IGH_DocumentObject obj)
        {
            Name = name;
            Level = level;
            Messages = messages;
            Object = obj;
        }

        public override string ToString()
        {
            var txt = $"{Level} on {Name}! {string.Join(", ", Messages)}";
            if (!char.IsPunctuation(txt[txt.Length - 1]))
                txt = string.Concat(txt, ".");
            return txt;
        }
    }

    public class Group_UnitTest : Grasshopper.Kernel.Special.GH_Group
    {
        #region Fields
        private UnitTestState _state; 
        private List<ObjectMessage> _log = new List<ObjectMessage>();
        private bool _solving;
        #endregion

        #region Properties  
        protected override Bitmap Icon => base.Icon;
        public static Guid ID => new Guid("bd91ba09-773f-4950-b440-2d8e39a77f68");
        public override Guid ComponentGuid => ID;
        public override GH_Exposure Exposure => GH_Exposure.tertiary;
        public UnitTestState State => _state;
        public override string InstanceDescription => string.Join(Environment.NewLine, Log);
    
        public IEnumerable<ObjectMessage> Log
        {
            get
            {
                if (_log.Count == 0)
                    return _log;
                return _log.OrderBy(l => l.Level);
            }
        }

        public bool IgnoreWarnings { get; set; }
        public bool IgnoreRemarks { get; set; }
        public bool DisableCanvasMessages { get; set; }
        #endregion

        #region Constructors
        public Group_UnitTest() : base()
        {
            Name = "Tenrec";
            NickName = "Tenrec";
            Description = "Integration Test runner";
            Category = "Params";
            SubCategory = "Util";
            Reset();
        }
        #endregion

        #region Methods 
        public void Reset()
        {
            _state = UnitTestState.Untested;
            _log.Clear();
            CanvasLog.Instance?.Clear();
        } 
        public void Compute()
        {
            var doc = OnPingDocument();
            if (doc == null)
                return;
            _solving = true;
            doc.ScheduleSolution(10, d => {
                d.SolutionEnd += Doc_SolutionEnd;
                foreach (var obj in Objects())
                    obj.ExpireSolution(false);
            });
            
        }
        public void Update()
        {
            try
            {
                _solving = false;
                Reset();

                _state = Utils.EvaluateGroup(this, out _log);

                if (!DisableCanvasMessages)
                {
                    foreach (var log in Log)
                    {
                        CanvasLog.Instance.WriteLine($"[{NickName}] {log}", 3000);
                    }
                }
              
                Attributes.ExpireLayout();
            }
            catch(Exception e)
            {
                _log.Add(new ObjectMessage(Name, GH_RuntimeMessageLevel.Error, new string[] { e.ToString() }, this));
            } 
        }

        private void Doc_SolutionEnd(object sender, GH_SolutionEventArgs e)
        {
            e.Document.SolutionEnd -= Doc_SolutionEnd;

            Update(); 
        }

        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);
            if (document.SelectedCount == 0)
            {
                if(ObjectIDs.Count == 0)
                {
                    document.RemoveObject(this.Attributes, false);
                    CanvasLog.Instance.WriteLine("Select components before dropping a Tenrec group", 3000);
                    return;
                }
                ExpireCaches();
            }
            else
            {
           
                if (NickName == "Tenrec")
                {
                    var c = document.Objects.OfType<Group_UnitTest>().Count();
                    NickName = $"TestGroup{c}";
                }
                Menu_AddToGroupHandler(null, null);
            }
          
        }

        public override void CreateAttributes()
        {
            m_attributes = new GroupAtt_Tenrec(this);
        }
 
        private void OnObjectAdded(IGH_DocumentObject obj)
        {
            obj.SolutionExpired -= Obj_SolutionExpired;
            obj.SolutionExpired += Obj_SolutionExpired;
        }

        private void OnObjectRemoved(IGH_DocumentObject obj)
        {
            obj.SolutionExpired -= Obj_SolutionExpired;
        }

        private void Obj_SolutionExpired(IGH_DocumentObject sender, GH_SolutionExpiredEventArgs e)
        {
            if (!_solving)
            {
                Reset();
                Update();
            }
                
        }

        #region Menu
        public override bool AppendMenuItems(ToolStripDropDown menu)
        {
            Menu_AppendObjectName(menu);
            Menu_AppendSeparator(menu);
            Menu_AppendItem(menu, "Select all", Menu_SelectAllHandler);
            Menu_AppendSeparator(menu);
            Menu_AppendItem(menu, "Ungroup", Menu_UngroupHandler);
            Menu_AppendItem(menu, "Add to group", Menu_AddToGroupHandler)
                .ToolTipText = "Add all selected objects to this Unit Test";
            Menu_AppendItem(menu, "Remove from group", Menu_RemoveFromGroupHandler)
                .ToolTipText = "Remove all selected objects from this Unit Test";
            return true;
        }
        private void Menu_SelectAllHandler(object sender, EventArgs e)
        {
            List<IGH_DocumentObject> list = Objects();
            if (list != null)
            {
                foreach (IGH_DocumentObject item in list)
                {
                    item.Attributes.Selected = true;
                }
                Instances.RedrawAll();
            }
        }
        private void Menu_UngroupHandler(object sender, EventArgs e)
        {
            var doc = OnPingDocument();
            if (doc != null)
            { 
                doc.UndoUtil.RecordRemoveObjectEvent("Ungroup", this);
                foreach (var obj in Objects())
                    OnObjectRemoved(obj);
                doc.RemoveObject(this, false); 
                Instances.InvalidateCanvas();
            }
        }
        private void Menu_AddToGroupHandler(object sender, EventArgs e)
        {
            var doc = OnPingDocument();
            if (doc != null)
            {
                var objs = doc.SelectedObjects()
                    .Where(o => InstanceGuid != o.InstanceGuid &&
                    !ObjectIDs.Contains(o.InstanceGuid)); 
                if (objs != null && objs.Any())
                {
                    RecordUndoEvent("Add to group");
                    foreach (var obj in objs)
                    {
                        AddObject(obj.InstanceGuid);
                        OnObjectAdded(obj);
                    }
                     
                    Reset();
                    Instances.InvalidateCanvas();
                } 
            }
        }
        private void Menu_RemoveFromGroupHandler(object sender, EventArgs e)
        {
            var doc = OnPingDocument();
            if (doc == null) 
                return;

            var objs = doc.SelectedObjects()
                    .Where(o => InstanceGuid != o.InstanceGuid &&
                    ObjectIDs.Contains(o.InstanceGuid)); 
            if (objs != null && objs.Any())
            {
                 if(objs.Count() == ObjectIDs.Count)
                {
                    doc.UndoUtil.RecordRemoveObjectEvent("Ungroup", this);
                    foreach (var obj in Objects())
                        OnObjectRemoved(obj);
                    doc.RemoveObject(this, false);
                }
                else
                {
                    RecordUndoEvent("Remove from group");
                    foreach (var obj in objs)
                    {
                        OnObjectRemoved(obj);
                        RemoveObject(obj.InstanceGuid);
                    }
                       
                    Reset();
                } 
                Instances.InvalidateCanvas();
            }
        }
        #endregion

        #region Serialization
        public override bool Write(GH_IWriter writer)
        {
            writer.SetInt32(nameof(State), (int)State); 
            writer.SetBoolean(nameof(IgnoreWarnings), IgnoreWarnings);
            writer.SetBoolean(nameof(IgnoreRemarks), IgnoreRemarks);
            writer.SetBoolean(nameof(DisableCanvasMessages), DisableCanvasMessages);
            return base.Write(writer);

            //writer.SetInt32(nameof(State), (int)State);
            //writer.SetInt32("ID_Count", ObjectIDs.Count);
            //for (int i = 0; i < ObjectIDs.Count; i++)
            //    writer.SetGuid("ID", i, ObjectIDs[i]);
            //writer.SetBoolean(nameof(IgnoreWarnings), IgnoreWarnings);
            //writer.SetBoolean(nameof(IgnoreRemarks), IgnoreRemarks);
            //writer.SetBoolean(nameof(DisableCanvasMessages), DisableCanvasMessages);

            //writer.SetString(nameof(Name), Name);
            //writer.SetString(nameof(NickName), NickName);
            //writer.SetString(nameof(Description), Description);
            //writer.SetGuid(nameof(InstanceGuid), InstanceGuid);
            //return Attributes.Write(writer.CreateChunk(nameof(Attributes)));
        }
        public override bool Read(GH_IReader reader)
        {
            _state = (UnitTestState)reader.GetInt32(nameof(State));
            IgnoreWarnings = reader.GetBoolean(nameof(IgnoreWarnings));
            IgnoreRemarks = reader.GetBoolean(nameof(IgnoreRemarks));
            DisableCanvasMessages = reader.GetBoolean(nameof(DisableCanvasMessages));
            return base.Read(reader);

            //ExpireCaches();
            //_state = (UnitTestState)reader.GetInt32(nameof(State));
            //ObjectIDs.Clear();
            //int cnt = reader.GetInt32("ID_Count");
            //for (int i = 0; i < cnt; i++)
            //    ObjectIDs.Add(reader.GetGuid("ID", i));
            //IgnoreWarnings = reader.GetBoolean(nameof(IgnoreWarnings));
            //IgnoreRemarks = reader.GetBoolean(nameof(IgnoreRemarks));
            //DisableCanvasMessages = reader.GetBoolean(nameof(DisableCanvasMessages));

            //Name = reader.GetString(nameof(Name));
            //NickName = reader.GetString(nameof(NickName));
            //Description = reader.GetString(nameof(Description));
            //if (reader.ItemExists(nameof(InstanceGuid)))
            //{
            //    NewInstanceGuid(reader.GetGuid(nameof(InstanceGuid)));
            //}
            //else
            //{
            //    NewInstanceGuid(Guid.NewGuid());
            //}
            //if (m_attributes != null)
            //{
            //    var reader2 = reader.FindChunk(nameof(Attributes));
            //    if (reader2 != null)
            //    {
            //        return Attributes.Read(reader2);
            //    }
            //    reader.AddMessage("Attributes chunk is missing. Could be a hint something's wrong.", GH_Message_Type.info);
            //}

            //return true;
        }
        #endregion
        #endregion

        public class GroupAtt_Tenrec : GH_Attributes<Group_UnitTest>
        {
            #region Fields
            private readonly Group_UnitTest _owner;
            private List<RectangleF> _cacheBoxes; 
            private GraphicsPath _runPath;
            private RectangleF _controlBox;
            private RectangleF _titleBox;
            private RectangleF _contentBox;
            private RectangleF _runBox;
            private RectangleF _optBox;
            private RectangleF _logBox;
            private Font _logFont;
            private const float _titleHeight = 20f;
            private const float _buttonHeight = 20f;
            #endregion

            #region Properties
            public override bool AllowMessageBalloon => false;
            public override bool HasInputGrip => false;
            public override bool HasOutputGrip => false;
            public override string PathName
            {
                get
                {
                    if (_owner.NickName.Equals("UnitTest", StringComparison.OrdinalIgnoreCase))
                    {
                        return "UnitTest";
                    }
                    return $"UnitTest ({_owner.NickName})";
                }
            }
            public override bool TooltipEnabled => true;
            #endregion

            #region Constructors 
            public GroupAtt_Tenrec(Group_UnitTest owner) : base(owner)
            {
                _owner = owner; 
            }
            ~GroupAtt_Tenrec()
            {
                Dispose();
            }
            #endregion

            #region Methods
            #region Region
            public override void AppendToAttributeTree(List<IGH_Attributes> attributes)
            {
                var idx = 0;
                //if (attributes.Count > 0)
                //    while (attributes[idx] is GroupAtt_Tenrec att && !Owner.ObjectIDs.Contains(att.Owner.InstanceGuid))
                //        idx++;
                attributes.Insert(idx, this);
            }
            public override bool InvalidateCanvas(GH_Canvas canvas, GH_CanvasMouseEvent e)
            { 
                return _hoverRunButton || _hoverTitle || _hoverLog > -1;
            }
            public override void SetupTooltip(PointF point, GH_TooltipDisplayEventArgs e)
            {
                base.SetupTooltip(point, e);
            }
            public override bool IsPickRegion(PointF point)
            {
                foreach (var box in _owner.ContentBoxes())
                    if (box.Contains(point))
                        return false;
                return base.IsPickRegion(point);
            }
            public override bool IsMenuRegion(PointF point)
            {
                return base.IsPickRegion(point);
            }
            public override bool IsTooltipRegion(PointF point)
            {
                return base.IsPickRegion(point);
            }
            #endregion

            #region Expiration
            private bool IsExpired()
            {
                if (_cacheBoxes == null)
                    return true;
                var boxes = _owner.ContentBoxes();
                if (boxes == null || boxes.Count != _cacheBoxes.Count)
                    return true;
                for (int i = 0; i < _cacheBoxes.Count; i++)
                {
                    if (boxes[i] != _cacheBoxes[i])
                        return true;
                }
                return false;
            }
            public override void ExpireLayout()
            {
                Dispose();
                base.ExpireLayout();
            }
            private void Dispose()
            {
                if (_runPath != null)
                {
                    _runPath.Dispose();
                    _runPath = null;
                }
                if (_cacheBoxes != null)
                {
                    _cacheBoxes.Clear();
                    _cacheBoxes = null;
                }
                if (_logFont != null)
                {
                    _logFont.Dispose();
                    _logFont = null;
                }
            }
            #endregion

            #region Render
            protected override void Layout()
            { 
                var objs = _owner.Objects();
                var ids = _owner.ObjectIDs;
              
                var boxes = _owner.ContentBoxes();
                if (boxes == null || boxes.Count == 0)
                {
                    Pivot = GH_Convert.ToPoint(Pivot);
                    _contentBox = new RectangleF(Pivot.X, Pivot.Y+ _titleHeight, 80f, 60f);
                    _controlBox = new RectangleF(Pivot.X, Pivot.Y, _contentBox.Width, _titleHeight);
                }
                else
                {
                 
                    _contentBox = boxes[0];
                    for (int i = 1; i < boxes.Count; i++) 
                        _contentBox = RectangleF.Union(_contentBox, boxes[i]); 
                    _contentBox.Inflate(20f, 20f);
                    _logBox = RectangleF.Empty;
                    var logs = Owner.Log; 
                    if(logs != null && logs.Any())
                    {
                        if (_logFont == null)
                            _logFont = GH_FontServer.NewFont(GH_FontServer.FamilyStandard, 6 / GH_GraphicsUtil.UiScale);
                        var messWidth = _contentBox.Width - 8;
                        var messY = _contentBox.Y;
                        foreach (var log in logs)
                        { 
                            var logSize = GH_FontServer.MeasureString(log.ToString(), _logFont, messWidth);
                            logSize.Width += 2;
                            var logLoc = new PointF(_contentBox.Right - logSize.Width - 4, messY - logSize.Height);
                            log.Bounds = new RectangleF(logLoc, logSize);
                            if (_logBox.IsEmpty)
                                _logBox = log.Bounds;
                            else
                                _logBox = RectangleF.Union(_logBox, log.Bounds);
                            messY = logLoc.Y;
                        }
                        _contentBox = GH_Convert.ToRectangle(RectangleF.Union(_contentBox, _logBox));
                    }

                    //_logBox = new RectangleF(_contentBox.X+4, _contentBox.Y - msgHgt, _contentBox.Width-8, msgHgt);
                    
                    _contentBox.Y -= 4;
                    _contentBox.Height += 4;
                    var tltHgt = Math.Max(_titleHeight, GH_FontServer.MeasureString(Owner.NickName,
                      GH_FontServer.StandardAdjusted, _contentBox.Width).Height);
                    _controlBox = new RectangleF(_contentBox.X, _contentBox.Y - tltHgt, _contentBox.Width, tltHgt);
                    _titleBox = new RectangleF(_controlBox.X+4, _controlBox.Y, _controlBox.Width - _buttonHeight * 2-12, tltHgt);
                    _runBox = new RectangleF(_controlBox.Right - _buttonHeight, _controlBox.Y, _buttonHeight, _buttonHeight);
                    _runBox.Inflate(-4f, -4f);
                    _optBox = _runBox;
                    _optBox.X -= _buttonHeight;
                }
                Bounds = RectangleF.Union(_contentBox, _controlBox);
                Pivot = Bounds.Location;
            }
          
            protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
            {
                if (channel == GH_CanvasChannel.First)
                {
                    if (IsExpired())
                    {
                        ExpireLayout();
                        PerformLayout();
                    }
                    var renderText = GH_Canvas.ZoomFadeLow > 0;
                    if(renderText)
                        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                    var fillColor1 = GetFillColor();
                    var edgeColor1 = GH_GraphicsUtil.ScaleColour(fillColor1, 0.25);
                    edgeColor1 = Color.FromArgb((int)120, edgeColor1);
                    var textColor1 = GH_Skin.palette_hidden_standard.Text;
                    var style1 = new GH_PaletteStyle(fillColor1, edgeColor1, textColor1);

                    var fillColor2 = GetStateColor();
                    var edgeColor2 = GH_GraphicsUtil.ScaleColour(fillColor2, 0.25);
                    edgeColor2 = Color.FromArgb((int)120, edgeColor2);
                    var textColor2 = GH_Skin.palette_hidden_standard.Text;
                    var style2 = new GH_PaletteStyle(fillColor2, edgeColor2, textColor2);

                    using (var brhContent = style1.CreateBrush(_contentBox, canvas.Viewport.Zoom))
                    using (var penContent = new Pen(style1.Edge))
                    {
                        DrawNiceRectangle(graphics, brhContent, penContent, _contentBox);
                        if (renderText)
                        {
                            if (Owner.ObjectIDs.Count == 0)
                            {
                                graphics.DrawString("Empty", GH_FontServer.StandardAdjusted, brhContent, _contentBox);
                            }
                            else
                            {
                                using (var sf2 = new StringFormat() { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.LineLimit })
                                {
                                    var i = 0;
                                    foreach (var l in Owner.Log)
                                    {
                                        using (var brhLog = new SolidBrush(GetLogColor(l)))
                                        {
                                            if(_hoverLog == i)
                                            {
                                                using(var logFont = GH_FontServer.NewFont(_logFont, FontStyle.Underline))
                                                    graphics.DrawString(l.ToString(), logFont, brhLog, l.Bounds, sf2);
                                            }
                                            else
                                            {
                                                graphics.DrawString(l.ToString(), _logFont, brhLog, l.Bounds, sf2); 
                                            }
                                        }
                                    
                                        i++;
                                    }
                                }
                            }
                        } 
                    }
                         
                    using (var brhControl = style2.CreateBrush(_controlBox, canvas.Viewport.Zoom))
                    using (var penControl = new Pen(style2.Edge)) 
                    {
                        DrawNiceRectangle(graphics, brhControl, penControl, _controlBox);
                        if (renderText)
                        {
                            if (!string.IsNullOrEmpty(Owner.NickName))
                            {
                                using (var brhTitle = new SolidBrush(style2.Text))
                                using (var sf = new StringFormat() { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.LineLimit })
                                    graphics.DrawString(Owner.NickName, GH_FontServer.StandardAdjusted, brhTitle, _titleBox, sf);
                            }

                            DrawSeparator(graphics, _controlBox.Right - _titleHeight * 2f - 4f, _controlBox.Y + 3, _controlBox.Bottom - 3, edgeColor1);
                            DrawRunButton(graphics, edgeColor1);
                            DrawOptionsButton(graphics);
                        }
                    } 
                } 
            }
            public Color GetStateColor()
            {
                var color = Color.Empty;
                switch (Owner.State)
                {
                    default:
                    case UnitTestState.Untested:
                        color = Selected ? GH_Skin.palette_warning_selected.Fill : GH_Skin.palette_warning_standard.Fill;
                        break;
                    case UnitTestState.Failure:
                        color = Selected ? GH_Skin.palette_error_selected.Fill : GH_Skin.palette_error_standard.Fill;
                        break;
                    case UnitTestState.Valid:
                        color = Selected ? GH_Skin.palette_hidden_selected.Fill : GH_Skin.palette_hidden_standard.Fill;
                        break;
                }
                return Color.FromArgb(120, color);
            }
            public static Color GetLogColor(ObjectMessage objMes)
            {
                var color = Color.Empty;
                switch (objMes.Level)
                {
                    case GH_RuntimeMessageLevel.Error:
                        color = GH_Skin.palette_error_standard.Fill;
                        break;
                    case GH_RuntimeMessageLevel.Warning:
                        color = GH_Skin.palette_warning_standard.Fill;
                        break;
                    case GH_RuntimeMessageLevel.Remark:
                        color = GH_Skin.palette_white_standard.Fill;
                        break;
                    default:
                        color = GH_Skin.palette_normal_standard.Text;
                        break;
                }
                return GH_GraphicsUtil.BlendColour(color, GH_Skin.palette_normal_standard.Text, 0.5);
            }
              
            private Color GetFillColor()
            {
                if (Selected)
                    return Color.FromArgb(100, Color.YellowGreen);
                else
                    return Color.FromArgb(100, GH_Skin.palette_normal_standard.Fill);
            }
            private static void DrawNiceRectangle(Graphics graphics, Brush brush, Pen pen, RectangleF rec)
            {
                using (var path = new GraphicsPath())
                {
                    path.AddRectangle(rec);
                    graphics.FillPath(brush, path);
                    graphics.DrawPath(pen, path);
                }
                var factor = Convert.ToInt32(0.75f * (float)GH_Canvas.ZoomFadeMedium);
                if (factor > 0)
                {
                    rec.Inflate(-1f, -1f);
                    using (var path2 = new GraphicsPath())
                    {
                        path2.AddRectangle(rec);
                        using (var brh2 = new LinearGradientBrush(rec,
                            Color.FromArgb(factor, Color.White),
                            Color.FromArgb(0, Color.White),
                            LinearGradientMode.Vertical)
                        { WrapMode = WrapMode.TileFlipXY })
                        using (Pen pen2 = new Pen(brh2))
                        {
                            graphics.DrawPath(pen2, path2);
                        }
                    }
                }
            }
            private void DrawRunButton(Graphics graphics, Color edgeColor)
            {
                if (_runBox.IsEmpty)
                    return;
                var runColor = Owner.ObjectIDs.Count > 0 ? 
                    (_hoverRunButton ? GH_GraphicsUtil.ScaleColour(Color.YellowGreen, 1.2) : Color.YellowGreen) :
                    (Color.DarkGray);

                if (_runPath == null)
                {
                    _runPath = new GraphicsPath();
                    _runPath.AddLines(new PointF[] {
                                    new PointF(_runBox.X, _runBox.Y),
                                    new PointF(_runBox.Right-1f, _runBox.Y+_runBox.Height/2f),
                                    new PointF(_runBox.X, _runBox.Y+_runBox.Height),
                                    new PointF(_runBox.X, _runBox.Y),
                                });
                    _runPath.CloseAllFigures();
                }
                using (var path3 = new GraphicsPath())
                using (var brh3 = new LinearGradientBrush(_runBox,
                   GH_GraphicsUtil.ScaleColour(runColor, 1.25), runColor,
                    LinearGradientMode.Vertical))
                using (var pen3 = new Pen(edgeColor) { LineJoin = LineJoin.Round })
                {
                    graphics.FillPath(brh3, _runPath);
                    graphics.DrawPath(pen3, _runPath);
                }
            }
            private void DrawOptionsButton(Graphics graphics)
            {
                using (var pen3 = new Pen(Color.Black) { LineJoin = LineJoin.Round })
                { 
                    graphics.DrawRectangle(pen3, _optBox.X, _optBox.Y, _optBox.Width, _optBox.Height);
                }
            }
            private void DrawSeparator(Graphics graphics, float x, float y0, float y1, Color edgeColor)
            {
                var p0 = new PointF(x, y0);
                var p1 = new PointF(x, y1);
                using (var brh = new LinearGradientBrush(p0, p1, Color.Transparent, edgeColor)
                { Blend = new Blend() { 
                    Positions = new float[] { 0f, 0.2f, 0.8f, 1f }, 
                    Factors = new float[] { 0.1f, 1f, 1f, 0.1f }
                } })
                using (var pen = new Pen(brh))
                {
                    graphics.DrawLine(pen, p0, p1);
                }
            }
            #endregion

            #region Handlers
            private void OnIgnoreWarnings(object sender, EventArgs args)
            {
                Owner.IgnoreWarnings = !Owner.IgnoreWarnings;
                Instances.RedrawCanvas();
            }
            private void OnIgnoreRemarks(object sender, EventArgs args)
            {
                Owner.IgnoreRemarks = !Owner.IgnoreRemarks;
                Instances.RedrawCanvas();
            }
            private void OnDisableCanvasMessages(object sender, EventArgs args)
            {
                Owner.DisableCanvasMessages = !Owner.DisableCanvasMessages;
                CanvasLog.Instance.Clear();
            }
            private void OnSourceCodeGenerator(object sender, EventArgs e)
            {
                _form = new UnitTestsSourceCodeGeneratorForm();
                _form.FormClosed += (s, a) => _form = null;
                _form.Show(Grasshopper.Instances.DocumentEditor);
            }
            public static void SetCanvasView(Grasshopper.GUI.Canvas.GH_Canvas canvas, RectangleF region, int length = 100)
            { 
                new Grasshopper.GUI.Canvas.GH_NamedView(canvas.ClientRectangle, region)
                .SetToViewport(canvas, length);
            } 
            #endregion

            #region Interaction
            private bool _hoverTitle;
            private bool _hoverRunButton;
            private int _hoverLog = -1;
            private UI.UnitTestsSourceCodeGeneratorForm _form;
            public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
            { 
                if(e.Button == MouseButtons.Left)
                {
                    if (_hoverTitle)
                    {
                        if (_hoverRunButton)
                        {
                            Owner.Compute();
                            return GH_ObjectResponse.Handled;
                        }
                        if (_optBox.Contains(e.CanvasLocation))
                        {
                            var menu = new ContextMenu();
                            menu.MenuItems.Add(Owner.IgnoreWarnings ? "Include warnings" : "Ignore warnings", OnIgnoreWarnings);
                            menu.MenuItems.Add(Owner.IgnoreRemarks ? "Include remarks" : "Ignore remarks", OnIgnoreRemarks);
                            menu.MenuItems.Add(Owner.DisableCanvasMessages ? "Enable canvas messages" : "Disable canvas messages", OnDisableCanvasMessages);
                            menu.MenuItems.Add("Source Code Generator", OnSourceCodeGenerator);
                            menu.Show(sender, e.ControlLocation);
                            return GH_ObjectResponse.Handled;
                        }
                    }
                    else
                    {
                        if (GH_Canvas.ZoomFadeLow > 0 && _logBox.Contains(e.CanvasLocation))
                        {
                            foreach (var log in Owner.Log)
                            {
                                if (log.Bounds.Contains(e.CanvasLocation))
                                {
                                    sender.Document.DeselectAll();
                                    log.Object.Attributes.Selected = true; 
                                    SetCanvasView(sender, log.Object.Attributes.Bounds, 200);
                                    sender.Invalidate();
                                    return GH_ObjectResponse.Handled;
                                }
                            }
                        }
                    }
                }  
                return base.RespondToMouseDown(sender, e); 
            }

            public override GH_ObjectResponse RespondToMouseMove(GH_Canvas sender, GH_CanvasMouseEvent e)
            {
                _hoverTitle = e.CanvasY - Bounds.Y < _controlBox.Height;
                if(_hoverLog > -1)
                {
                    _hoverLog = -1;
                    sender.Invalidate();
                }
             
                if (_hoverTitle)
                { 
                    _hoverRunButton = Owner.ObjectIDs.Count > 0 && _runPath != null && _runPath.IsVisible(e.CanvasLocation);
                    sender.Invalidate();
                }
               
                if (GH_Canvas.ZoomFadeLow > 0  && _logBox.Contains(e.CanvasLocation))
                {
                    var i = 0;
                    foreach (var log in Owner.Log)
                    {
                        if (log.Bounds.Contains(e.CanvasLocation))
                        {
                            _hoverLog = i;
                            sender.Invalidate();
                            break;
                        }
                        i++;
                    }
                }
                return base.RespondToMouseMove(sender, e);
            }
            public override GH_ObjectResponse RespondToMouseUp(GH_Canvas sender, GH_CanvasMouseEvent e)
            {
                return base.RespondToMouseUp(sender, e);
            }
            public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
            {
                if(e.Button == MouseButtons.Left)
                {
                    if (_titleBox.Contains(e.CanvasLocation))
                    {
                        if (ShowCanvasTitleTextBox(sender))
                            return GH_ObjectResponse.Handled;
                    }
                    //else if( sender.IsDocument && IsPickRegion(e.CanvasLocation) )
                    //{
                    //    CreatePanel(sender.Document);
                    //}
                  
                }
                return base.RespondToMouseDoubleClick(sender, e);
            }
 
            private bool ShowCanvasTitleTextBox(GH_Canvas canvas)
            {
                var rec = RectangleF.Intersect(canvas.Viewport.VisibleRegion, _controlBox);
                if (rec.IsEmpty)
                    return false;
                var a = GH_Convert.ToRectangle(canvas.Viewport.ProjectRectangle(rec));
                a = Rectangle.Intersect(a, canvas.ClientRectangle);
                if (a.Width < 60 || a.Height < _buttonHeight)
                    return false;
                var tb = new TextBox();
                tb.SetBounds(a.X, a.Y, a.Width+1, a.Height+1);
                tb.BorderStyle = BorderStyle.FixedSingle;
                tb.Multiline = true;
                tb.BackColor = Color.FromArgb(255, GetFillColor());
                tb.ForeColor = Color.FromArgb(255, GH_Skin.palette_hidden_standard.Text);
                tb.TextAlign = HorizontalAlignment.Left;
                tb.WordWrap = true;
                tb.ScrollBars = ScrollBars.Vertical;

                var size = Math.Max(8f, GH_FontServer.StandardAdjusted.Size * canvas.Viewport.Zoom);
                tb.Font = GH_FontServer.NewFont(GH_FontServer.StandardAdjusted, size);
                tb.Text = base.Owner.NickName;
                tb.SelectAll();
                tb.KeyDown += CanvasTextBoxKeyDown;
                tb.LostFocus += CanvasTextBoxLostFocus;
                canvas.Controls.Add(tb);
                tb.Focus();
                return true;
            }
            private void CanvasTextBoxKeyDown(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.Cancel)
                {
                    if (sender is TextBox tb)
                    {
                        tb.KeyDown -= CanvasTextBoxKeyDown;
                        tb.LostFocus -= CanvasTextBoxLostFocus;
                        tb.Parent.Controls.Remove(tb);
                    }

                }
                if (e.KeyCode == Keys.Return && (Control.ModifierKeys == Keys.Control || Control.ModifierKeys == Keys.Shift))
                {
                    if (sender is Control ctrl)
                    {
                        ctrl.Parent.Select();
                        e.Handled = true;
                    }
                }
            }
            private void CanvasTextBoxLostFocus(object sender, EventArgs e)
            {
                if (sender is TextBox tb)
                {
                    string text = tb.Text;
                    tb.KeyDown -= CanvasTextBoxKeyDown;
                    tb.LostFocus -= CanvasTextBoxLostFocus;
                    tb.Parent.Controls.Remove(tb);
                    if (!text.Equals(base.Owner.NickName, StringComparison.Ordinal))
                    {
                        base.Owner.RecordUndoEvent("Edit nickname");
                        base.Owner.TriggerAutoSave();
                        base.Owner.NickName = text;
                    }
                }
            }
            #endregion
            #endregion

        }

    }
     
    public class Comp_Assert : GH_Component 
    {
        #region Properties
        protected override Bitmap Icon => null;
        public override Guid ComponentGuid => new Guid("0a2b53e4-3d49-49b3-a8e4-92b7d2da382a");
        public override GH_Exposure Exposure => GH_Exposure.tertiary; 
        #endregion

        #region Constructors 
        public Comp_Assert() : base("Assert", "Assert",
            "Ensure that a condition is met or throw an error.", "Params", "Util") { }
        #endregion

        #region Methods
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Assert", "A", "True if the condition has been met", GH_ParamAccess.item);
            pManager.AddTextParameter("Message", "M", "Assert messsage", GH_ParamAccess.item);
            Params.Input[1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager){ }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool valid = false;
            string message = string.Empty;
            if (!DA.GetData(0, ref valid))
                return;

            if (!DA.GetData(1, ref message) && !valid)
                message = $"{NickName} failed.";
             
            if(!valid || !string.IsNullOrEmpty(message))
            {
                var level = valid ? GH_RuntimeMessageLevel.Remark : GH_RuntimeMessageLevel.Error;
                AddRuntimeMessage(level, message);
            }
        }
         
        #endregion

    }

}
