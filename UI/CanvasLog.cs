using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text; 
using System.Windows.Forms;

namespace Tenrec.UI
{
    public class CanvasLog : IDisposable
    {
        #region Fields
        private GH_Canvas _canvas;
        private int _width;
        private Timer _timer;
        private StringBuilder _sb;
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
            if (sender.Graphics == null)
                return;
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
