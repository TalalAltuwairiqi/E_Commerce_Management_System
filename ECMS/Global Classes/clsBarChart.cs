using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ECMS
{
    // A small horizontal bar chart drawn with GDI+ (no extra packages needed).
    public class clsBarChart : Control
    {
        private readonly List<KeyValuePair<string, decimal>> _items = new List<KeyValuePair<string, decimal>>();

        public string ChartTitle { get; private set; } = string.Empty;
        public string ValueFormat { get; private set; } = "N0";
        public Color BarColor { get; set; } = Color.FromArgb(46, 117, 182);

        public clsBarChart()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Color.White;
        }

        public void SetData(IEnumerable<KeyValuePair<string, decimal>> items, string title, string valueFormat)
        {
            _items.Clear();
            _items.AddRange(items);
            ChartTitle = title;
            ValueFormat = valueFormat;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.Clear(BackColor);

            const int margin = 12;
            float top = margin;

            if (ChartTitle.Length > 0)
            {
                using (Font titleFont = new Font(Font, FontStyle.Bold))
                using (Brush titleBrush = new SolidBrush(Color.FromArgb(31, 58, 95)))
                {
                    g.DrawString(ChartTitle, titleFont, titleBrush, margin, top);
                }
                top += 28;
            }

            if (_items.Count == 0)
            {
                using (Brush gray = new SolidBrush(Color.Gray))
                {
                    g.DrawString("No data to show.", Font, gray, margin, top);
                }
                return;
            }

            // Width of the label column and of the value text
            float labelWidth = 0;
            float valueWidth = 0;
            foreach (KeyValuePair<string, decimal> item in _items)
            {
                labelWidth = Math.Max(labelWidth, g.MeasureString(item.Key, Font).Width);
                valueWidth = Math.Max(valueWidth, g.MeasureString(item.Value.ToString(ValueFormat), Font).Width);
            }
            labelWidth = Math.Min(labelWidth, Width * 0.35f);

            float barLeft = margin + labelWidth + 10;
            float barMax = Width - margin - valueWidth - 12 - barLeft;
            if (barMax < 20)
                return;   // the control is too small to draw

            float rowHeight = Math.Min(30f, (Height - top - margin) / _items.Count);
            if (rowHeight < 10f)
                rowHeight = 10f;

            decimal max = _items.Max(i => i.Value);
            if (max <= 0)
                max = 1;

            using (Brush barBrush = new SolidBrush(BarColor))
            using (Brush textBrush = new SolidBrush(Color.FromArgb(60, 60, 60)))
            using (StringFormat labelFormat = new StringFormat
            {
                Alignment = StringAlignment.Far,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            })
            using (StringFormat valueFormat = new StringFormat
            {
                Alignment = StringAlignment.Near,
                LineAlignment = StringAlignment.Center,
                FormatFlags = StringFormatFlags.NoWrap
            })
            {
                float y = top;

                foreach (KeyValuePair<string, decimal> item in _items)
                {
                    g.DrawString(item.Key, Font, textBrush, new RectangleF(margin, y, labelWidth, rowHeight), labelFormat);

                    float barLength = item.Value <= 0 ? 0f : (float)(item.Value / max) * barMax;
                    if (item.Value > 0 && barLength < 2f)
                        barLength = 2f;

                    float barHeight = Math.Max(4f, rowHeight - 8f);
                    g.FillRectangle(barBrush, barLeft, y + (rowHeight - barHeight) / 2f, barLength, barHeight);

                    g.DrawString(item.Value.ToString(ValueFormat), Font, textBrush,
                        new RectangleF(barLeft + barLength + 6, y, valueWidth + 10, rowHeight), valueFormat);

                    y += rowHeight;
                }
            }
        }
    }
}