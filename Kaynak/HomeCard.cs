using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Akis
{
    internal sealed class HomeCard : Button
    {
        private readonly Preferences prefs;
        private readonly string category, title, body, action, glyph;
        private bool hovering;
        internal HomeCard(Preferences preferences,string category,string title,string body,string action,string glyph)
        {
            prefs=preferences;this.category=category;this.title=title;this.body=body;this.action=action;this.glyph=glyph;
            Size=new Size(302,238);FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;
            DoubleBuffered=true;Cursor=Cursors.Hand;TabStop=true;
            AccessibleName=title+". "+body+". "+action;
        }
        protected override void OnMouseEnter(EventArgs e){hovering=true;Invalidate();base.OnMouseEnter(e);}
        protected override void OnMouseLeave(EventArgs e){hovering=false;Invalidate();base.OnMouseLeave(e);}
        protected override void OnPaint(PaintEventArgs e)
        {
            var g=e.Graphics;g.Clear(BackColor);g.SmoothingMode=SmoothingMode.AntiAlias;
            g.ScaleTransform(Width/302f,Height/238f);
            Color accent=Palette.Accent(prefs),fg=Palette.Text(prefs),muted=Palette.Muted(prefs);
            Draw.Box(g,Palette.Card(prefs),new RectangleF(0,0,301,237),16);
            using(var path=Draw.Round(new RectangleF(.5f,.5f,300,236),16))
            using(var pen=new Pen(hovering||Focused?accent:Palette.Line(prefs),1))g.DrawPath(pen,path);
            Draw.Box(g,Color.FromArgb(28,accent),new RectangleF(20,18,34,34),10);
            UiSymbols.Font(g,glyph,17,accent,new RectangleF(20,18,34,34));
            Draw.Text(g,category,10,muted,new RectangleF(66,18,215,34),true);
            Draw.Text(g,title,20,fg,new RectangleF(20,64,262,30),true);
            using(var font=new Font("Segoe UI",13,FontStyle.Regular,GraphicsUnit.Pixel))
            using(var brush=new SolidBrush(muted))
            using(var format=new StringFormat{Trimming=StringTrimming.EllipsisWord})
                g.DrawString(body,font,brush,new RectangleF(20,104,262,85),format);
            using(var pen=new Pen(Palette.Line(prefs)))g.DrawLine(pen,20,195,282,195);
            Draw.Text(g,action,12,accent,new RectangleF(20,201,236,28),true);
            UiSymbols.Font(g,"\uE76C",12,accent,new RectangleF(258,201,24,28));
        }
    }
}
