using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Akis
{
    // Opaque, code-drawn surfaces. No desktop blur or glass composition.
    internal static class ShellSkin
    {
        internal static Color Accent(Preferences p){return Palette.Accent(p);}
        private static double Luminance(Color c)
        {
            double r=c.R/255.0,g=c.G/255.0,b=c.B/255.0;
            r=r<=.04045?r/12.92:Math.Pow((r+.055)/1.055,2.4);
            g=g<=.04045?g/12.92:Math.Pow((g+.055)/1.055,2.4);
            b=b<=.04045?b/12.92:Math.Pow((b+.055)/1.055,2.4);
            return r*.2126+g*.7152+b*.0722;
        }
        private static Color Readable(Color color,Color background,double minimum)
        {
            double bg=Luminance(background);bool darken=bg>.4;
            for(int i=0;i<64;i++)
            {
                double fg=Luminance(color);
                if((Math.Max(fg,bg)+.05)/(Math.Min(fg,bg)+.05)>=minimum)break;
                int target=darken?0:255;
                color=Color.FromArgb(color.A,(int)(color.R*.9+target*.1),(int)(color.G*.9+target*.1),(int)(color.B*.9+target*.1));
            }
            return color;
        }
        internal static Color Ink(Preferences p){return Readable(Accent(p),Palette.Light(p)?Color.White:Color.FromArgb(19,20,24),3);}
        internal static Color Strong(Preferences p,Color source){return Readable(Tint(p,source),Color.White,4.5);}
        // Keep each surface's light/dark hierarchy while replacing its blue hue
        // with the selected accent. Neutral custom accents produce neutral tints.
        internal static Color Tint(Preferences p,Color source)
        {
            Color accent=Accent(p);float h=accent.GetHue()/60f;
            float s=source.GetSaturation()*accent.GetSaturation(),l=source.GetBrightness();
            float c=(1-Math.Abs(2*l-1))*s,x=c*(1-Math.Abs(h%2-1)),m=l-c/2;
            float r=0,g=0,b=0;
            if(h<1){r=c;g=x;}else if(h<2){r=x;g=c;}else if(h<3){g=c;b=x;}
            else if(h<4){g=x;b=c;}else if(h<5){r=x;b=c;}else{r=c;b=x;}
            return Color.FromArgb(source.A,(int)Math.Round((r+m)*255),(int)Math.Round((g+m)*255),(int)Math.Round((b+m)*255));
        }
        internal static Color Bg(Preferences p){return Palette.Light(p)?ShellSkin.Tint(p,Color.FromArgb(237,242,255)):ShellSkin.Tint(p,Color.FromArgb(9,16,34));}
        internal static Color Card(Preferences p){return Palette.Light(p)?ShellSkin.Tint(p,Color.FromArgb(249,251,255)):ShellSkin.Tint(p,Color.FromArgb(20,31,57));}
        internal static Color Line(Preferences p){return Palette.Light(p)?ShellSkin.Tint(p,Color.FromArgb(198,210,240)):ShellSkin.Tint(p,Color.FromArgb(47,65,107));}
        internal static Color Muted(Preferences p){return Palette.Light(p)?ShellSkin.Tint(p,Color.FromArgb(87,105,140)):ShellSkin.Tint(p,Color.FromArgb(165,183,218));}
        internal static void Surface(Graphics g,RectangleF r,Color a,Color b,float radius,Color edge)
        {
            var oldSmoothing=g.SmoothingMode;var oldPixel=g.PixelOffsetMode;var oldComposite=g.CompositingMode;
            g.SmoothingMode=SmoothingMode.AntiAlias;g.PixelOffsetMode=PixelOffsetMode.HighQuality;g.CompositingMode=CompositingMode.SourceOver;
            using(var path=Draw.Round(r,radius))
            using(var brush=new LinearGradientBrush(r,a,b,65f))g.FillPath(brush,path);
            // Keep the one-pixel outline on half-pixel coordinates. Drawing it
            // on the outer bounds clips corners unevenly on scaled controls.
            var border=new RectangleF(r.X+.5f,r.Y+.5f,Math.Max(0,r.Width-1),Math.Max(0,r.Height-1));
            using(var path=Draw.Round(border,Math.Max(0,radius-.5f)))
            using(var pen=new Pen(edge,1f))g.DrawPath(pen,path);
            g.SmoothingMode=oldSmoothing;g.PixelOffsetMode=oldPixel;g.CompositingMode=oldComposite;
        }
        internal static void Paragraph(Graphics g,string text,float size,Color c,RectangleF r)
        {
            using(var font=new Font("Segoe UI",size,FontStyle.Regular,GraphicsUnit.Pixel))
            using(var brush=new SolidBrush(c))
            using(var format=new StringFormat{Trimming=StringTrimming.EllipsisWord})g.DrawString(text,font,brush,r,format);
        }
        internal static void HoverGlow(Graphics g,RectangleF bounds,float radius,Preferences prefs)
        {
            if(bounds.Width<4||bounds.Height<4)return;
            var state=g.Save();
            try
            {
                g.SmoothingMode=SmoothingMode.AntiAlias;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
                g.CompositingMode=CompositingMode.SourceOver;
                using(var clip=Draw.Round(bounds,radius))g.SetClip(clip,CombineMode.Intersect);
                var edge=new RectangleF(bounds.X+.75f,bounds.Y+.75f,bounds.Width-1.5f,bounds.Height-1.5f);
                using(var path=Draw.Round(edge,Math.Max(1,radius-.75f)))
                {
                    Color accent=Ink(prefs);
                    using(var pen=new Pen(Color.FromArgb(22,accent),8))g.DrawPath(pen,path);
                    using(var pen=new Pen(Color.FromArgb(42,accent),4))g.DrawPath(pen,path);
                    using(var pen=new Pen(Color.FromArgb(225,accent),1.4f))g.DrawPath(pen,path);
                }
            }
            finally{g.Restore(state);}
        }
        internal static void Orb(Graphics g,string glyph,RectangleF r,Preferences prefs)
        {
            Color start=Strong(prefs,Color.FromArgb(75,123,255)),end=Strong(prefs,Color.FromArgb(34,46,154));
            UiSymbols.Paint(g,r,"shell-orb/"+glyph+"/"+start.ToArgb()+"/"+end.ToArgb(),delegate(Graphics target,RectangleF bounds){
                using(var brush=new LinearGradientBrush(bounds,start,end,50f))target.FillEllipse(brush,bounds);
                Draw.Icon(target,glyph,bounds.Height*.48f,Color.White,bounds);
            });
        }
    }

    internal sealed class ShellPanel : Panel
    {
        internal Preferences Prefs; internal bool Sidebar;
        internal ShellPanel(){DoubleBuffered=true;ResizeRedraw=true;}
        protected override void OnPaintBackground(PaintEventArgs e)
        {PaintSurface(e.Graphics);}
        internal void PaintSurface(Graphics g)
        {
            var state=g.Save();
            try {
            g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            Color bg=ShellSkin.Bg(Prefs);using(var fill=new SolidBrush(bg))g.FillRectangle(fill,ClientRectangle);
            if(Width<2||Height<2)return;
            if(Sidebar)ShellSkin.Surface(g,new RectangleF(1,1,Width-2,Height-2),Palette.Light(Prefs)?Color.White:ShellSkin.Tint(Prefs,Color.FromArgb(10,20,43)),ShellSkin.Card(Prefs),18,ShellSkin.Line(Prefs));
            else using(var brush=new LinearGradientBrush(ClientRectangle,Palette.Light(Prefs)?ShellSkin.Tint(Prefs,Color.FromArgb(222,233,255)):ShellSkin.Tint(Prefs,Color.FromArgb(17,37,83)),bg,65f))g.FillRectangle(brush,ClientRectangle);
            }finally{g.Restore(state);}
        }
    }

    internal sealed class BrandMark : Control
    {
        internal Preferences Prefs;
        internal BrandMark(){DoubleBuffered=true;SetStyle(ControlStyles.SupportsTransparentBackColor,true);BackColor=Color.Transparent;}
        protected override void OnPaint(PaintEventArgs e)
        {
            var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            BrandAssets.Paint(g,new RectangleF(3,8,62,55));
            Draw.Text(g,"flux",43,Palette.Text(Prefs),new RectangleF(70,0,139,68),true);
            Draw.Text(g,UiText.English(Prefs)?"Make your panels your own":"Panellerini kendine göre tasarla",12,ShellSkin.Muted(Prefs),new RectangleF(4,71,215,28));
        }
    }

    internal sealed class SettingsCard : Panel
    {
        private readonly Preferences prefs;
        private readonly HoverTracker highlight;
        internal string Glyph;
        internal SettingsCard(Preferences prefs){this.prefs=prefs;DoubleBuffered=true;ResizeRedraw=true;SetStyle(ControlStyles.SupportsTransparentBackColor,true);BackColor=Color.Transparent;highlight=new HoverTracker(this);}
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            base.OnPaintBackground(e);var g=e.Graphics;var state=g.Save();try {
            ShellSkin.Surface(g,new RectangleF(1,1,Width-2,Height-2),ShellSkin.Card(prefs),Palette.Light(prefs)?ShellSkin.Tint(prefs,Color.FromArgb(238,244,255)):ShellSkin.Tint(prefs,Color.FromArgb(17,32,63)),12,ShellSkin.Line(prefs));
            if(!String.IsNullOrEmpty(Glyph))SettingsDesign.Icon(g,Glyph,new RectangleF(20,(Height-24)/2,24,24),Palette.Light(prefs)?ShellSkin.Ink(prefs):ShellSkin.Tint(prefs,Color.FromArgb(208,223,255)));
            if(highlight.Active)ShellSkin.HoverGlow(g,new RectangleF(1,1,Width-2,Height-2),12,prefs);
            }finally{g.Restore(state);}
        }
    }

    internal sealed class NavButton : Button
    {
        internal Preferences Prefs;internal string Glyph;internal bool Selected;private bool hover;
        internal NavButton(){FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;DoubleBuffered=true;UseVisualStyleBackColor=false;SetStyle(ControlStyles.Opaque|ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.ResizeRedraw,true);Cursor=Cursors.Hand;}
        protected override void OnPaintBackground(PaintEventArgs e){}
        protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
        protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
        protected override void OnPaint(PaintEventArgs e)
        {
            var g=e.Graphics;var state=g.Save();
            try {
            var shell=Parent as ShellPanel;
            if(shell!=null){g.TranslateTransform(-Left,-Top);shell.PaintSurface(g);g.TranslateTransform(Left,Top);}
            else using(var clear=new SolidBrush(ShellSkin.Bg(Prefs)))g.FillRectangle(clear,ClientRectangle);
            g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            if(Selected)ShellSkin.Surface(g,new RectangleF(1,1,Width-3,Height-3),ShellSkin.Strong(Prefs,Color.FromArgb(61,108,247)),ShellSkin.Strong(Prefs,Color.FromArgb(22,44,116)),14,ShellSkin.Tint(Prefs,Color.FromArgb(72,110,210)));
            else if(hover||Focused)Draw.Box(g,ShellSkin.Card(Prefs),new RectangleF(1,1,Width-3,Height-3),14);
            Color iconBg=Selected?ShellSkin.Strong(Prefs,Color.FromArgb(70,146,255)):ShellSkin.Card(Prefs);
            UiSymbols.Circle(g,new RectangleF(13,9,39,39),iconBg,iconBg);
            UiSymbols.Font(g,Glyph,19,Selected?Color.White:Palette.Text(Prefs),new RectangleF(13,9,39,39));
            Draw.Text(g,Text,14,Selected?Color.White:Palette.Text(Prefs),new RectangleF(67,0,Width-72,Height),Selected);
            if(Enabled&&(hover||Focused))ShellSkin.HoverGlow(g,new RectangleF(1,1,Width-3,Height-3),14,Prefs);
            }finally{g.Restore(state);}
        }
    }

    internal sealed class DashboardButton : Button
    {
        internal Preferences Prefs;internal string Caption,Detail,Glyph,Design;internal bool Selected;
        private bool hover;
        internal DashboardButton(Preferences prefs,string caption,string detail,string glyph)
        {Prefs=prefs;Caption=caption;Detail=detail;Glyph=glyph;AccessibleName=caption;AccessibleDescription=detail;Text=caption;FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;DoubleBuffered=true;Cursor=Cursors.Hand;UseVisualStyleBackColor=false;SetStyle(ControlStyles.Opaque|ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.ResizeRedraw,true);}
        protected override void OnPaintBackground(PaintEventArgs e){}
        protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
        protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
        protected override void OnPaint(PaintEventArgs e)
        {
            var g=e.Graphics;var paintState=g.Save();
            try {
            var dashboard=Parent as HomeDashboard;
            if(dashboard!=null)dashboard.PaintButtonBackground(g,Bounds);
            else using(var clear=new SolidBrush(ShellSkin.Bg(Prefs)))g.FillRectangle(clear,ClientRectangle);
            g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            float scale=Width/(Design!=null?160f:240f);g.ScaleTransform(scale,scale);float h=Height/scale,w=Width/scale;
            Color card=ShellSkin.Card(Prefs),edge=Selected||hover||Focused?ShellSkin.Ink(Prefs):ShellSkin.Line(Prefs);
            ShellSkin.Surface(g,new RectangleF(1,1,w-3,h-3),Selected?ShellSkin.Strong(Prefs,Color.FromArgb(48,89,191)):card,Selected?ShellSkin.Strong(Prefs,Color.FromArgb(25,44,104)):Palette.Light(Prefs)?ShellSkin.Tint(Prefs,Color.FromArgb(230,237,255)):ShellSkin.Tint(Prefs,Color.FromArgb(17,27,52)),13,edge);
            if(Design!=null)
            {
                Color accent=ShellSkin.Ink(Prefs);
                var preview=new RectangleF(13,13,134,76);
                if(Design=="Modern")
                {
                    ShellSkin.Surface(g,preview,ShellSkin.Tint(Prefs,Color.FromArgb(76,76,123)),Color.FromArgb(126,68,103),10,ShellSkin.Tint(Prefs,Color.FromArgb(90,119,163)));
                    var sampleState=g.Save();g.TranslateTransform(13,13);g.ScaleTransform(134f/400,76f/166);ModernPanel.Surface(g,Prefs,400,166,.85f);g.Restore(sampleState);
                }
                else if(Design=="Plak")
                {
                    var sampleState=g.Save();g.TranslateTransform(13,13);g.ScaleTransform(134f/400,76f/166);VinylPanel.PaintPreview(g,Prefs,400,166,.85f);g.Restore(sampleState);
                }
                else ShellSkin.Surface(g,preview,ShellSkin.Tint(Prefs,Color.FromArgb(33,35,43)),ShellSkin.Tint(Prefs,Color.FromArgb(33,35,43)),8,ShellSkin.Tint(Prefs,Color.FromArgb(78,85,105)));
                if(Design!="Plak")
                {
                    using(var b=new LinearGradientBrush(new Rectangle(23,22,31,31),accent,ShellSkin.Tint(Prefs,Color.FromArgb(193,225,247)),70f))g.FillRectangle(b,23,22,31,31);
                    using(var b=new SolidBrush(ShellSkin.Tint(Prefs,Color.FromArgb(21,44,70))))g.FillPolygon(b,new[]{new PointF(23,53),new PointF(37,31),new PointF(42,43),new PointF(49,37),new PointF(54,53)});
                    Draw.Box(g,ShellSkin.Tint(Prefs,Color.FromArgb(213,226,250)),new RectangleF(62,27,63,3),1.5f);
                    Draw.Box(g,ShellSkin.Tint(Prefs,Color.FromArgb(150,157,185)),new RectangleF(62,36,39,2),1);
                    Draw.Box(g,ShellSkin.Tint(Prefs,Color.FromArgb(100,120,147)),new RectangleF(23,60,114,2),1);
                    Draw.Box(g,Design=="Modern"?Color.White:accent,new RectangleF(23,60,48,2),1);
                    UiSymbols.Font(g,"\uE892",8,Color.White,new RectangleF(47,65,16,18));
                    if(Design=="Modern")UiSymbols.Font(g,"\uE768",9,Color.White,new RectangleF(73,66,17,17));else ShellSkin.Orb(g,"\uE768",new RectangleF(73,66,17,17),Prefs);
                    UiSymbols.Font(g,"\uE893",8,Color.White,new RectangleF(101,65,16,18));
                }
                Draw.Text(g,Caption,12,Selected?Color.White:Palette.Text(Prefs),new RectangleF(4,h-33,w-8,27),false,StringAlignment.Center);
                if(Selected)ShellSkin.Orb(g,"\uE73E",new RectangleF(w-27,8,19,19),Prefs);
            }
            else if(String.IsNullOrEmpty(Detail))
            {Draw.Text(g,Caption,14,Palette.Text(Prefs),new RectangleF(14,0,w-44,h));UiSymbols.Font(g,"\uE72A",12,ShellSkin.Muted(Prefs),new RectangleF(w-34,0,24,h));}
            else
            {
                Draw.Box(g,Palette.Light(Prefs)?ShellSkin.Tint(Prefs,Color.FromArgb(221,225,255)):ShellSkin.Tint(Prefs,Color.FromArgb(40,36,102)),new RectangleF(12,(h-45)/2,45,45),13);
                UiSymbols.Font(g,Glyph,24,Palette.Light(Prefs)?ShellSkin.Ink(Prefs):Color.White,new RectangleF(12,(h-45)/2,45,45));
                Draw.Text(g,Caption,14,Palette.Text(Prefs),new RectangleF(71,h/2-24,w-89,26),true);
                Draw.Text(g,Detail,11,ShellSkin.Muted(Prefs),new RectangleF(71,h/2+2,w-88,25));
                UiSymbols.Font(g,"\uE76C",11,ShellSkin.Muted(Prefs),new RectangleF(w-20,0,17,h));
            }
            if(Enabled&&(hover||Focused))ShellSkin.HoverGlow(g,new RectangleF(1,1,w-3,h-3),13,Prefs);
            }finally{g.Restore(paintState);}
        }
    }

    internal sealed class HomeDashboard : UserControl
    {
        private readonly Controller app;private readonly Action<string> navigate;private readonly Action changed;
        private Bitmap preview,background; private readonly Timer timer;
        private bool backgroundDirty=true;
        private string backgroundKey;
        private string informationHover;
        private readonly DashboardButton[] designs=new DashboardButton[PanelDesigns.Names.Length];
        private int ThemeExtraHeight {get{return Math.Max(0,(designs.Length+5)/6-1)*140;}}
        private Preferences P {get{return app.Settings;}}
        private string L(string tr,string en){return UiText.English(P)?en:tr;}
        internal HomeDashboard(Controller controller,Action<string> navigate,Action changed)
        {
            app=controller;this.navigate=navigate;this.changed=changed;DoubleBuffered=true;SetStyle(ControlStyles.Opaque|ControlStyles.AllPaintingInWmPaint|ControlStyles.ResizeRedraw,true);Size=new Size(1100,815+ThemeExtraHeight);
            AddButton("preview",L("Paneli ekranda göster","Show panel on desktop"),"","",new RectangleF(736,108,318,38),delegate{app.MediaWindow.Reveal(true);});
            AddButton("appearance",L("Tema ve renk","Theme and color"),L("Görünümü kişiselleştir","Make it your own"),"\uE790",new RectangleF(579,257,241,84),delegate{navigate("Uygulama");});
            AddButton("panels",L("Panel ayarları","Panel settings"),L("Konum, boyut ve süre","Position, size, duration"),"\uE9E9",new RectangleF(832,257,241,84),delegate{navigate("Medya paneli");});
            AddButton("background",L("Arka plan","Background"),L("Panel saydamlığı","Panel opacity"),"\uE91B",new RectangleF(579,355,241,84),delegate{navigate("Medya paneli");});
            AddButton("application",L("Uygulama","Application"),L("Dil ve genel tercihler","Language and preferences"),"\uE713",new RectangleF(832,355,241,84),delegate{navigate("Uygulama");});
            for(int i=0;i<designs.Length;i++)
            {
                string design=PanelDesigns.Names[i];
                string caption=UiText.Get(P,design);
                designs[i]=AddButton("design"+design,caption,L("Üç panele uygula","Apply to all three panels"),"",new RectangleF(18+(i%6)*180,556+(i/6)*140,164,126),delegate{ApplyDesign(design);});
                designs[i].Design=design;
            }
            AddButton("themes",L("Medya görünümünü düzenle","Edit media appearance"),"","",new RectangleF(821,493,253,37),delegate{navigate("Medya paneli");});
            AddButton("notes",L("Sürüm notlarını oku","Read release notes"),"","",new RectangleF(321,753,192,36),delegate{navigate("Sürüm Bilgisi");});
            AddButton("faq",L("Yanıtları göster","Show answers"),"","",new RectangleF(881,753,192,36),delegate{navigate("Yardım ve SSS");});
            RefreshPreview();timer=new Timer{Interval=1000};timer.Tick+=delegate{if(Visible)RefreshPreview();};timer.Start();Resize+=delegate{LayoutButtons();};VisibleChanged+=delegate{if(!Visible)SetInformationHover(null);};LayoutButtons();
        }
        private DashboardButton AddButton(string name,string title,string detail,string glyph,RectangleF bounds,Action click)
        {
            var b=new DashboardButton(P,title,detail,glyph){Name=name,Tag=bounds};b.Click+=delegate{click();};
            if(name=="notes"||name=="faq"){
                b.MouseEnter+=delegate{SetInformationHover(name=="notes"?"Sürüm Bilgisi":"Yardım ve SSS");};
                b.MouseLeave+=delegate{SetInformationHover(null);};
            }
            Controls.Add(b);return b;
        }
        private void LayoutButtons()
        {float s=Width/1100f;Height=(int)Math.Ceiling((815+ThemeExtraHeight)*s);foreach(Control c in Controls){RectangleF r=(RectangleF)c.Tag;if(c.Name=="notes"||c.Name=="faq")r.Y+=ThemeExtraHeight;c.Bounds=Rectangle.Round(new RectangleF(r.X*s,r.Y*s,r.Width*s,r.Height*s));}backgroundDirty=true;Invalidate(true);}
        private string InformationTarget(Point point)
        {
            float scale=Width/1100f;
            if(scale<=0)return null;
            var p=new PointF(point.X/scale,point.Y/scale);
            if(new RectangleF(0,712+ThemeExtraHeight,541,100).Contains(p))return "Sürüm Bilgisi";
            if(new RectangleF(556,712+ThemeExtraHeight,543,100).Contains(p))return "Yardım ve SSS";
            return null;
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {string target=InformationTarget(e.Location);Cursor=target==null?Cursors.Default:Cursors.Hand;SetInformationHover(target);base.OnMouseMove(e);}
        protected override void OnMouseLeave(EventArgs e){SetInformationHover(null);base.OnMouseLeave(e);}
        private void SetInformationHover(string target)
        {if(informationHover==target)return;informationHover=target;backgroundDirty=true;Invalidate(true);}
        protected override void OnMouseClick(MouseEventArgs e)
        {
            string target=InformationTarget(e.Location);
            if(e.Button==MouseButtons.Left&&target!=null)navigate(target);
            base.OnMouseClick(e);
        }
        internal void ApplyDesign(string design)
        {
            if(Array.IndexOf(PanelDesigns.Names,design)<0)return;
            foreach(string kind in new[]{"media","volume","lock"})P.Panel(kind).Design=design;
            changed();RefreshPreview();
        }
        internal void RefreshPreview()
        {
            if(IsDisposed)return;
            var next=app.MediaWindow.RenderPreviewSnapshot();var old=preview;preview=next;if(old!=null)old.Dispose();
            foreach(var b in designs)
            {
                b.Selected=P.Panel("media").Design==b.Design&&P.Panel("volume").Design==b.Design&&P.Panel("lock").Design==b.Design;
                b.AccessibleDescription=L("Üç panele uygula","Apply to all three panels")+(b.Selected?L(" • Seçili"," • Selected"):"");b.Invalidate();
            }
            backgroundDirty=true;Invalidate(true);
        }
        private void Card(Graphics g,float x,float y,float w,float h)
        {ShellSkin.Surface(g,new RectangleF(x,y,w,h),ShellSkin.Card(P),Palette.Light(P)?ShellSkin.Tint(P,Color.FromArgb(233,240,255)):ShellSkin.Tint(P,Color.FromArgb(12,24,49)),19,ShellSkin.Line(P));}
        private void Title(Graphics g,string glyph,string title,string subtitle,float x,float y)
        {ShellSkin.Orb(g,glyph,new RectangleF(x,y,46,46),P);Draw.Text(g,title,19,Palette.Text(P),new RectangleF(x+62,y-2,440,30),true);Draw.Text(g,subtitle,13,ShellSkin.Muted(P),new RectangleF(x+62,y+27,435,22));}
        protected override void OnPaintBackground(PaintEventArgs e){}
        private Bitmap BackgroundImageFrame()
        {
            Point offset=Point.Empty;Control node=this;
            while(node!=null&&!(node is ShellPanel)){offset.Offset(node.Left,node.Top);node=node.Parent;}
            var shell=node as ShellPanel;
            string key=Width+":"+Height+":"+offset+":"+(shell==null?Size.Empty:shell.Size)+":"+Palette.Light(P)+":"+UiText.English(P)+":"+ShellSkin.Accent(P).ToArgb();
            if(background!=null&&!backgroundDirty&&key==backgroundKey)return background;
            var next=new Bitmap(Math.Max(1,Width),Math.Max(1,Height),System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            using(var g=Graphics.FromImage(next))
            {
                g.Clear(ShellSkin.Bg(P));
                if(shell!=null){var state=g.Save();g.TranslateTransform(-offset.X,-offset.Y);shell.PaintSurface(g);g.Restore(state);}
                DrawDashboard(g);
            }
            var old=background;background=next;backgroundKey=key;backgroundDirty=false;if(old!=null)old.Dispose();return background;
        }
        internal void PaintButtonBackground(Graphics g,Rectangle bounds)
        {
            // Copy exactly the pixels behind this child. The Button base class
            // must not replay the parent's scaled paint or draw a square skin.
            var state=g.Save();
            try {g.CompositingMode=CompositingMode.SourceCopy;g.DrawImage(BackgroundImageFrame(),new Rectangle(Point.Empty,bounds.Size),bounds,GraphicsUnit.Pixel);}
            finally{g.Restore(state);}
        }
        protected override void OnPaint(PaintEventArgs e)
        {e.Graphics.DrawImageUnscaled(BackgroundImageFrame(),0,0);}
        private void DrawDashboard(Graphics g)
        {
            var paintState=g.Save();
            try {
            g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;g.ScaleTransform(Width/1100f,Width/1100f);
            Color fg=Palette.Text(P),muted=ShellSkin.Muted(P);
            UiSymbols.Font(g,"\uE80F",18,fg,new RectangleF(0,0,34,32));Draw.Text(g,L("Başlangıç","Home"),14,muted,new RectangleF(48,0,200,32));
            string intro=L("Flux’ı Kendine Göre","Make Flux");float introWidth;
            using(var font=new Font("Segoe UI",35,FontStyle.Bold,GraphicsUnit.Pixel))introWidth=g.MeasureString(intro,font).Width;
            Draw.Text(g,intro,35,fg,new RectangleF(8,65,introWidth+10,53),true);
            Draw.Text(g,L("Tasarla!","your own."),35,ShellSkin.Ink(P),new RectangleF(8+introWidth,65,700-introWidth,53),true);
            ShellSkin.Paragraph(g,L("Windows'un medya, ses ve kilit panellerini\nkendi tarzına göre kişiselleştir.","Personalize your Windows media, volume and lock panels.\nYour panels. Your style."),16,muted,new RectangleF(10,125,690,57));
            ShellSkin.Surface(g,new RectangleF(716,37,381,121),ShellSkin.Strong(P,Color.FromArgb(56,104,242)),ShellSkin.Strong(P,Color.FromArgb(17,37,87)),22,ShellSkin.Tint(P,Color.FromArgb(81,120,211)));
            Draw.Text(g,L("Anında Önizleme","Instant preview"),21,Color.White,new RectangleF(738,51,278,34),true);
            Draw.Text(g,L("Değişiklikler otomatik kaydedilir.","Changes are saved automatically."),13,ShellSkin.Tint(P,Color.FromArgb(206,222,255)),new RectangleF(738,83,340,24));
            Card(g,0,191,541,273);Card(g,556,191,543,273);
            Title(g,"\uE890",L("Canlı Önizleme","Live preview"),L("Mevcut medya panelinin görünümü","Preview your current media panel."),17,204);
            Title(g,"\uE945",L("Hızlı İşlemler","Quick actions"),L("En sık kullandığın ayarlara hızlıca ulaş.","A shortcut to your everyday settings."),575,204);
            var stage=new RectangleF(17,267,507,179);
            using(var path=Draw.Round(stage,13))
            {
                var state=g.Save();g.SetClip(path);
                using(var brush=new LinearGradientBrush(stage,ShellSkin.Tint(P,Color.FromArgb(61,147,255)),ShellSkin.Tint(P,Color.FromArgb(6,25,109)),30f))g.FillRectangle(brush,stage);
                for(int i=0;i<11;i++)using(var pen=new Pen(ShellSkin.Tint(P,Color.FromArgb(105-i*6,55+i*13,151+i*7,255)),12))
                    g.DrawBezier(pen,-10,453-i*5,170,211+i*8,388,540-i*14,573,251+i*6);
                if(preview!=null){float s=Math.Min(425f/preview.Width,156f/preview.Height);float w=preview.Width*s,h=preview.Height*s;g.DrawImage(preview,stage.X+(stage.Width-w)/2,stage.Y+(stage.Height-h)/2,w,h);}
                g.Restore(state);
            }
            Card(g,0,478,1099,219+ThemeExtraHeight);
            Title(g,"\uE790",L("Hazır Temalar","Panel themes"),L("Panel tasarımını seç, üç panele birden uygula.","Choose a design for all three panels."),17,493);
            int extra=ThemeExtraHeight;
            Card(g,0,712+extra,541,100);Card(g,556,712+extra,543,100);
            Title(g,"\uE946",L("Sürüm Bilgisi","Version info"),"Flux 2.1.0",17,726+extra);
            Draw.Text(g,L("Windows 10 · Yerel uygulama","Windows 10 · Local application"),12,muted,new RectangleF(80,778+extra,239,22));
            Title(g,"\uE897",L("Yardım ve SSS","Help and FAQ"),L("Kullanım ve sık sorulan sorular.","Getting started and common questions."),574,726+extra);
            if(informationHover!=null)ShellSkin.HoverGlow(g,new RectangleF(informationHover=="Sürüm Bilgisi"?0:556,712+extra,informationHover=="Sürüm Bilgisi"?541:543,100),19,P);
            }finally{g.Restore(paintState);}
        }
        protected override void Dispose(bool disposing)
        {if(disposing){if(timer!=null)timer.Dispose();if(preview!=null)preview.Dispose();if(background!=null)background.Dispose();}base.Dispose(disposing);}
    }
}
