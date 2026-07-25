using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MetalFatiguePatcher
{
    /// <summary>
    /// A combot-part / superweapon toggle. The icon fills the whole button (no inner frame);
    /// when selected it gets a thick amber border that gently pulses (driven by the form's pulse
    /// timer calling <see cref="Advance"/>). Subclasses CheckBox so Checked / CheckedChanged / Tag
    /// keep working with the existing gather + restore code.
    /// </summary>
    internal sealed class IconToggle : CheckBox
    {
        public Image Icon;
        static double _phase;   // shared so every selected toggle pulses in sync

        public IconToggle()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                     | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Color.FromArgb(24, 24, 28);
            Cursor = Cursors.Hand;
        }

        /// <summary>Advance the shared pulse phase one tick (call ~18x/s, then Invalidate the checked ones).</summary>
        public static void Advance() { _phase += 0.10; }

        protected override void OnCheckedChanged(EventArgs e)
        {
            base.OnCheckedChanged(e);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            var r = ClientRectangle;
            g.Clear(BackColor);

            if (Icon != null)
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(Icon, r);
            }
            else if (!string.IsNullOrEmpty(Text))
            {
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    g.DrawString(Text, Font, Brushes.Gainsboro, r, sf);
            }

            if (Checked)
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                double t = (Math.Sin(_phase) + 1) / 2;          // 0..1, continuous
                float bw = 2.0f + (float)(t * 1.4);             // 2.0..3.4 px (thin, smooth)
                int a = 150 + (int)(t * 105);                   // 150..255 alpha
                using (var pen = new Pen(Color.FromArgb(a, 250, 176, 48), bw))
                    g.DrawRectangle(pen, bw / 2f, bw / 2f, r.Width - bw, r.Height - bw);
            }
            else
            {
                using (var pen = new Pen(Color.FromArgb(70, 72, 82)))
                    g.DrawRectangle(pen, 0, 0, r.Width - 1, r.Height - 1);
            }
        }
    }
}
