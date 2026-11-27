using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Akis
{
    internal sealed class SettingsPageView : FlowLayoutPanel
    {
        internal SettingsPageView(){DoubleBuffered=true;ResizeRedraw=true;}
        // Compose child HWNDs into the page buffer instead of exposing their
        // individual erase/paint steps while a complete page becomes visible.
        protected override CreateParams CreateParams
        {get{var cp=base.CreateParams;cp.ExStyle|=0x02000000;return cp;}}
    }
    internal static class SettingsDesign
    {
        internal static string Glyph(string text)
        {
            if(text=="Konum"||text=="Yerleşim"||text=="Placement")return "move";
            if(text=="Ekran")return "\uE7F4";
            if(text.StartsWith("X "))return "\uE8AB";
            if(text.StartsWith("Y "))return "\uE8CB";
            if(text.Contains("boşluk")||text=="Boyut")return "\uE740";
            if(text=="Saydamlık")return "opacity";
            if(text.Contains("kalma"))return "clock";
            if(text.Contains("renk")||text.Contains("reng")||text.Contains("görünüm")||text=="Görünüm")return "\uE790";
            if(text.Contains("Tema")||text.Contains("tema"))return "\uE706";
            if(text=="Dil")return "\uE774";
            if(text.Contains("Lock"))return "\uE72E";
            if(text.Contains("ortala"))return "\uE8E3";
            if(text.Contains("Windows ile"))return "windows";
            if(text.Contains("gizle"))return "eye";
            if(text.Contains("etkin"))return "power";
            if(text=="Boyut ve süre"||text=="Size and duration")return "sliders";
            if(text.Contains("çık"))return "\uE7E8";
            if(text.Contains("Varsayılan"))return "\uE777";
            if(text.Contains("Bağlantı"))return "\uE71B";
            return "\uE713";
        }
        internal static void Icon(Graphics g,string glyph,RectangleF r,Color color)
        {
            UiSymbols.Paint(g,r,"settings/"+glyph+"/"+color.ToArgb(),
                delegate(Graphics target,RectangleF bounds){PaintIcon(target,glyph,bounds,color);});
        }
        private static void PaintIcon(Graphics g,string glyph,RectangleF r,Color color)
        {
            if(glyph.Length==1){Draw.Icon(g,glyph,r.Height*.68f,color,r);return;}
            var state=g.Save();try {
                g.TranslateTransform(r.X,r.Y);g.ScaleTransform(r.Width/24,r.Height/24);g.SmoothingMode=SmoothingMode.AntiAlias;
                using(var p=new Pen(color,1.65f)){p.StartCap=p.EndCap=LineCap.Round;
                    if(glyph=="clock"){g.DrawEllipse(p,3,3,18,18);g.DrawLines(p,new[]{new PointF(12,6),new PointF(12,12),new PointF(16,14)});}
                    else if(glyph=="opacity"){g.DrawEllipse(p,3,3,18,18);using(var b=new SolidBrush(color))g.FillPie(b,3,3,18,18,90,180);}
                    else if(glyph=="eye"){g.DrawBezier(p,2,12,7,3,17,3,22,12);g.DrawBezier(p,2,12,7,21,17,21,22,12);g.DrawEllipse(p,8.5f,8.5f,7,7);}
                    else if(glyph=="power"){g.DrawArc(p,4,4,16,16,-50,280);g.DrawLine(p,12,2,12,11);}
                    else if(glyph=="windows"){using(var b=new SolidBrush(color)){g.FillRectangle(b,3,3,8,8);g.FillRectangle(b,13,3,8,8);g.FillRectangle(b,3,13,8,8);g.FillRectangle(b,13,13,8,8);}}
                    else if(glyph=="move"){g.DrawLine(p,12,2,12,22);g.DrawLine(p,2,12,22,12);g.DrawLines(p,new[]{new PointF(8,6),new PointF(12,2),new PointF(16,6)});g.DrawLines(p,new[]{new PointF(8,18),new PointF(12,22),new PointF(16,18)});g.DrawLines(p,new[]{new PointF(6,8),new PointF(2,12),new PointF(6,16)});g.DrawLines(p,new[]{new PointF(18,8),new PointF(22,12),new PointF(18,16)});}
                    else {for(int i=0;i<3;i++){float y=5+i*7,x=i==1?15:8;g.DrawLine(p,2,y,22,y);using(var b=new SolidBrush(color))g.FillEllipse(b,x-2,y-2,4,4);g.DrawEllipse(p,x-2,y-2,4,4);}}
                }
            }finally{g.Restore(state);}
        }
        internal static void Orb(Graphics g,string glyph,RectangleF r,Preferences prefs)
        {
            Color start=ShellSkin.Strong(prefs,Color.FromArgb(53,117,255)),end=ShellSkin.Strong(prefs,Color.FromArgb(35,49,179));
            UiSymbols.Paint(g,r,"settings-orb/"+glyph+"/"+start.ToArgb()+"/"+end.ToArgb(),delegate(Graphics target,RectangleF bounds){
                using(var b=new LinearGradientBrush(bounds,start,end,60f))target.FillEllipse(b,bounds);
                PaintIcon(target,glyph,new RectangleF(bounds.Width*.23f,bounds.Height*.23f,bounds.Width*.54f,bounds.Height*.54f),Color.White);
            });
        }
        internal static void LayoutRow(Control row)
        {
            int w=row.Width;
            bool range=false;foreach(Control c in row.Controls)if(c is SliderControl)range=true;
            foreach(Control child in row.Controls)
            {
                if(child is ChoiceControl||child is NumberControl)
                {child.Width=Math.Min(298,Math.Max(136,(int)(w*.48)));child.Left=w-child.Width-16;child.Top=(row.Height-child.Height)/2;}
                else if(child is SliderControl){child.Left=Math.Min(230,(int)(w*.32));child.Width=Math.Max(90,w-child.Left-105);}
                else if(child is Label)
                {
                    var label=(Label)child;
                    child.Top=0;child.Height=row.Height;
                    if(label.TextAlign==ContentAlignment.MiddleRight){child.Left=w-89;child.Width=72;}
                    else {label.TextAlign=ContentAlignment.MiddleLeft;child.Left=64;child.Width=range?Math.Min(230,(int)(w*.32))-74:Math.Max(80,(int)(w*.48)-72);}
                }
            }
        }
    }

    internal sealed class SettingsParagraph : Control
    {
        private const TextFormatFlags Flags=TextFormatFlags.WordBreak|TextFormatFlags.NoPrefix|TextFormatFlags.NoPadding;
        internal SettingsParagraph(string text)
        {
            Text=text;AccessibleName=text;Font=new Font("Segoe UI",10);DoubleBuffered=true;ResizeRedraw=true;
            SetStyle(ControlStyles.SupportsTransparentBackColor,true);BackColor=Color.Transparent;
        }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            int height=TextRenderer.MeasureText(Text,Font,new Size(Math.Max(1,Width-12),Int32.MaxValue),Flags).Height+12;
            if(Height!=height)Height=height;
        }
        protected override void OnPaint(PaintEventArgs e)
        {TextRenderer.DrawText(e.Graphics,Text,Font,new Rectangle(6,6,Math.Max(1,Width-12),Math.Max(1,Height-12)),ForeColor,Flags);}
    }

    internal sealed class NumberControl : Control
    {
        private readonly HoverTracker highlight;
        private readonly Preferences prefs;private readonly TextBox input;
        private int value;private bool syncing;
        internal readonly int Minimum,Maximum;
        internal event EventHandler ValueChanged;
        internal int Value {get{return value;}set{int next=Math.Max(Minimum,Math.Min(Maximum,value));if(this.value==next)return;this.value=next;Sync();if(ValueChanged!=null)ValueChanged(this,EventArgs.Empty);}}
        internal NumberControl(Preferences prefs,int min,int max,int initial,string title)
        {
            this.prefs=prefs;Minimum=min;Maximum=max;value=Math.Max(min,Math.Min(max,initial));
            DoubleBuffered=true;ResizeRedraw=true;SetStyle(ControlStyles.SupportsTransparentBackColor,true);BackColor=Color.Transparent;
            AccessibleName=title;AccessibleRole=AccessibleRole.SpinButton;
            input=new TextBox{BorderStyle=BorderStyle.None,Font=new Font("Segoe UI",10),Text=value.ToString(),AccessibleName=title};Controls.Add(input);
            highlight=new HoverTracker(this);
            input.TextChanged+=delegate {int n;if(!syncing&&Int32.TryParse(input.Text,out n)&&n>=Minimum&&n<=Maximum&&value!=n){value=n;if(ValueChanged!=null)ValueChanged(this,EventArgs.Empty);}};
            input.Leave+=delegate{int n;Value=Int32.TryParse(input.Text,out n)?n:value;Sync();};
            input.KeyDown+=delegate(object sender,KeyEventArgs e){if(e.KeyCode==Keys.Up||e.KeyCode==Keys.Down){Value+=e.KeyCode==Keys.Up?1:-1;e.Handled=true;e.SuppressKeyPress=true;}else if(e.KeyCode==Keys.Enter){int n;if(Int32.TryParse(input.Text,out n))Value=n;Sync();e.SuppressKeyPress=true;}};
            Sync();
        }
        private void Sync(){syncing=true;input.Text=value.ToString();syncing=false;Invalidate();}
        internal void RefreshTheme(){input.BackColor=ShellSkin.Bg(prefs);input.ForeColor=Palette.Text(prefs);Invalidate();}
        protected override void OnResize(EventArgs e){base.OnResize(e);if(input!=null)input.SetBounds(12,Math.Max(2,(Height-input.PreferredHeight)/2),Math.Max(20,Width-48),input.PreferredHeight);}
        protected override void OnPaint(PaintEventArgs e)
        {
            var g=e.Graphics;ShellSkin.Surface(g,new RectangleF(1,1,Width-2,Height-2),ShellSkin.Bg(prefs),ShellSkin.Bg(prefs),8,ShellSkin.Line(prefs));
            using(var p=new Pen(ShellSkin.Line(prefs))){g.DrawLine(p,Width-29,5,Width-29,Height-5);g.DrawLine(p,Width-28,Height/2,Width-5,Height/2);}
            UiSymbols.Font(g,"\uE70E",9,ShellSkin.Muted(prefs),new RectangleF(Width-27,1,24,Height/2));UiSymbols.Font(g,"\uE70D",9,ShellSkin.Muted(prefs),new RectangleF(Width-27,Height/2,24,Height/2-1));
            if(highlight.Active)ShellSkin.HoverGlow(g,new RectangleF(1,1,Width-2,Height-2),8,prefs);
        }
        protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);input.Focus();if(e.Button==MouseButtons.Left&&e.X>Width-30)Value+=e.Y<Height/2?1:-1;}
    }

    internal sealed class SettingsGroup : Panel
    {
        private readonly HoverTracker highlight;
        internal readonly Preferences Prefs;
        internal string Glyph,Mode;
        internal bool DeferArrange;
        private bool arranging;
        internal SettingsGroup(Preferences prefs,string title,string mode)
        {
            Prefs=prefs;Text=title;Mode=mode;Glyph=SettingsDesign.Glyph(title);
            DoubleBuffered=true;ResizeRedraw=true;
            SetStyle(ControlStyles.SupportsTransparentBackColor,true);BackColor=Color.Transparent;
            highlight=new HoverTracker(this);
        }
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
            PaintSurface(e.Graphics);
        }
        internal void PaintSurface(Graphics g)
        {
            if(Width<2||Height<2)return;
            var state=g.Save();
            try {
                g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                bool light=Palette.Light(Prefs);
                ShellSkin.Surface(g,new RectangleF(1,1,Width-2,Height-2),light?ShellSkin.Tint(Prefs,Color.FromArgb(243,248,255)):ShellSkin.Tint(Prefs,Color.FromArgb(16,34,73)),light?ShellSkin.Tint(Prefs,Color.FromArgb(232,240,255)):ShellSkin.Tint(Prefs,Color.FromArgb(11,22,47)),18,ShellSkin.Line(Prefs));
                if(Text.Length>0){SettingsDesign.Orb(g,Glyph,new RectangleF(18,12,30,30),Prefs);Draw.Text(g,Text,15,Palette.Text(Prefs),new RectangleF(60,8,Width-80,38),true);}
                bool interactive=false;
                foreach(Control c in Controls)if(c is SettingsCard||c is SwitchRow||c is SettingsAction){interactive=true;break;}
                if(interactive&&highlight.Active)ShellSkin.HoverGlow(g,new RectangleF(1,1,Width-2,Height-2),18,Prefs);
            }finally{g.Restore(state);}
        }
        protected override void OnControlAdded(ControlEventArgs e){base.OnControlAdded(e);Arrange();}
        protected override void OnResize(EventArgs e){base.OnResize(e);Arrange();}
        internal void Arrange()
        {
            if(arranging||DeferArrange)return;arranging=true;
            try {
                int pad=16,gap=8,top=Text.Length>0?52:16,w=Math.Max(100,Width-pad*2),y=top;
                bool split=Width>=820;
                if(Mode=="intro"&&Controls.Count>0)
                {
                    int rightHeight=0;for(int i=1;i<Controls.Count;i++)rightHeight+=Controls[i].Height+gap;
                    int h=Math.Max(214,rightHeight-gap);
                    if(split){int left=(int)(w*.54);Controls[0].SetBounds(pad,top,left,h);int ry=top;for(int i=1;i<Controls.Count;i++){var c=Controls[i];c.SetBounds(pad+left+gap,ry,w-left-gap,c.Height);ry+=c.Height+gap;}y=top+h;}
                    else {Controls[0].SetBounds(pad,y,w,234);y+=244;for(int i=1;i<Controls.Count;i++){var c=Controls[i];c.SetBounds(pad,y,w,c.Height);y+=c.Height+gap;}y-=gap;}
                }
                else if(Mode=="placement"&&split&&Controls.Count>=5)
                {
                    int cw=(w-gap)/2;int leftY=top,rightY=top;
                    for(int i=0;i<5;i++){var c=Controls[i];bool left=i<2;c.SetBounds(left?pad:pad+cw+gap,left?leftY:rightY,cw,54);SettingsDesign.LayoutRow(c);if(left)leftY+=c.Height+gap;else rightY+=c.Height+gap;}
                    if(Controls.Count>6){var action=Controls[6];action.SetBounds(pad,leftY,cw,54);leftY+=54+gap;}
                    y=Math.Max(leftY,rightY);
                    if(Controls.Count>5){var note=Controls[5];note.SetBounds(pad,y,w,note.Height);y+=note.Height;}
                }
                else
                {
                    foreach(Control c in Controls){c.SetBounds(pad,y,w,c.Height);SettingsDesign.LayoutRow(c);y+=c.Height+gap;}y-=gap;
                }
                Height=Math.Max(60,y+pad);
            }finally{arranging=false;}
        }
    }

    internal sealed class SettingsAction : Button
    {
        internal Preferences Prefs;internal string Glyph;private bool hover;
        internal SettingsAction(Preferences prefs,string title)
        {
            Prefs=prefs;Text=title;Glyph=SettingsDesign.Glyph(title);AccessibleName=title;
            DoubleBuffered=true;FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;
            SetStyle(ControlStyles.Opaque|ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.ResizeRedraw,true);
            BackColor=ShellSkin.Card(prefs);Cursor=Cursors.Hand;UseVisualStyleBackColor=false;
        }
        protected override void OnPaintBackground(PaintEventArgs e){}
        protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
        protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
        protected override void OnPaint(PaintEventArgs e)
        {
            var g=e.Graphics;var state=g.Save();try {
                // Repaint the entire buffer, including rounded corners, even when
                // WinForms skips background erasure during hover/focus/scroll.
                using(var fill=new SolidBrush(ShellSkin.Bg(Prefs)))g.FillRectangle(fill,ClientRectangle);
                var group=Parent as SettingsGroup;
                if(group!=null){g.TranslateTransform(-Left,-Top);group.PaintSurface(g);g.TranslateTransform(Left,Top);}
                else
                {
                    // Top-level page actions sit on the shell gradient, not a
                    // flat card. Repaint that exact surface at rounded corners.
                    Point offset=Location;Control node=Parent;
                    while(node!=null&&!(node is ShellPanel)){offset.Offset(node.Left,node.Top);node=node.Parent;}
                    var shell=node as ShellPanel;
                    if(shell!=null){var backgroundState=g.Save();g.TranslateTransform(-offset.X,-offset.Y);shell.PaintSurface(g);g.Restore(backgroundState);}
                }
                g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                ShellSkin.Surface(g,new RectangleF(1,1,Width-2,Height-2),ShellSkin.Card(Prefs),Palette.Light(Prefs)?ShellSkin.Tint(Prefs,Color.FromArgb(230,239,255)):ShellSkin.Tint(Prefs,Color.FromArgb(14,28,59)),12,hover||Focused?ShellSkin.Ink(Prefs):ShellSkin.Line(Prefs));
                SettingsDesign.Icon(g,Glyph,new RectangleF(20,(Height-24)/2,24,24),ShellSkin.Ink(Prefs));
                Draw.Text(g,Text,14,Palette.Text(Prefs),new RectangleF(62,0,Width-103,Height));
                UiSymbols.Font(g,"\uE76C",13,ShellSkin.Muted(Prefs),new RectangleF(Width-36,0,22,Height));
                if(Enabled&&(hover||Focused))ShellSkin.HoverGlow(g,new RectangleF(1,1,Width-2,Height-2),12,Prefs);
            }finally{g.Restore(state);}
        }
    }

    internal sealed class PanelPreview : Control
    {
        private readonly Controller app;private readonly string kind;private Bitmap frame;
        internal PanelPreview(Controller app,string kind)
        {
            this.app=app;this.kind=kind;DoubleBuffered=true;ResizeRedraw=true;
            SetStyle(ControlStyles.SupportsTransparentBackColor,true);BackColor=Color.Transparent;
            AccessibleName=UiText.English(app.Settings)?"Panel preview":"Panel önizlemesi";
            RefreshFrame();
        }
        internal void RefreshFrame()
        {
            var next=(kind=="media"?app.MediaWindow:kind=="volume"?app.VolumeWindow:app.LockWindow).RenderPreviewSnapshot();
            var old=frame;frame=next;if(old!=null)old.Dispose();Invalidate();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g=e.Graphics;var state=g.Save();try {
                g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                var p=app.Settings;bool en=UiText.English(p);
                SettingsDesign.Orb(g,"eye",new RectangleF(2,3,39,39),p);
                string title=kind=="media"?(en?"Media panel preview":"Medya paneli önizlemesi"):kind=="volume"?(en?"Volume panel preview":"Ses paneli önizlemesi"):(en?"Lock panel preview":"Kilit paneli önizlemesi");
                Draw.Text(g,title,17,Palette.Text(p),new RectangleF(55,0,Width-60,29),true);
                Draw.Text(g,en?"Changes appear here instantly.":"Değişiklikleri anında burada önizleyin.",12,ShellSkin.Muted(p),new RectangleF(55,29,Width-60,24));
                var canvas=new RectangleF(1,64,Width-2,Height-66);
                using(var path=Draw.Round(canvas,14)){g.SetClip(path);using(var b=new LinearGradientBrush(canvas,ShellSkin.Tint(p,Color.FromArgb(38,112,250)),ShellSkin.Tint(p,Color.FromArgb(5,15,60)),35))g.FillRectangle(b,canvas);
                    g.SmoothingMode=SmoothingMode.AntiAlias;
                    for(int i=0;i<9;i++)using(var pen=new Pen(ShellSkin.Tint(p,Color.FromArgb(85-i*7,45,155,255)),18+i*5))g.DrawBezier(pen,canvas.Left-90,canvas.Bottom-i*9,canvas.Left+80,canvas.Top-100+i*18,canvas.Right-95,canvas.Bottom+125,canvas.Right+80,canvas.Top-35+i*15);
                    float scale=Math.Min((canvas.Width-42)/frame.Width,(canvas.Height-28)/frame.Height);
                    scale=Math.Min(scale,kind=="media"?.65f:.60f)*Math.Min(1.15f,app.Settings.Panel(kind).Scale/100f);
                    float w=frame.Width*scale,h=frame.Height*scale;g.InterpolationMode=InterpolationMode.HighQualityBicubic;
                    g.DrawImage(frame,new RectangleF(canvas.X+(canvas.Width-w)/2,canvas.Y+(canvas.Height-h)/2,w,h));
                }
            }finally{g.Restore(state);}
        }
        protected override void Dispose(bool disposing){if(disposing&&frame!=null){frame.Dispose();frame=null;}base.Dispose(disposing);}
    }
}
