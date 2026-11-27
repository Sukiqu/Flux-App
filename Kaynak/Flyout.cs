using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Akis
{
    internal sealed class Flyout : Form
    {
        internal readonly Controller OwnerApp;
        internal readonly string Kind;
        private DateTime expires;
        private bool demo;
        private string lockName="Caps Lock";
        private bool locked=true;
        internal string CurrentLock {get{return lockName;}}
        private bool hovering;
        private readonly Timer animation;
        private LayeredSurface surface;
        private bool presenting;
        internal double EffectiveOpacity {get{return Math.Max(.45,Math.Min(1,Options.Opacity/100.0));}}
        internal double ContentOpacity {get{return 1-(1-EffectiveOpacity)*.25;}}
        private bool English { get { return UiText.English(OwnerApp.Settings); } }
        private string T(string tr,string en){return English?en:tr;}
        private float deviceDpiFactor=1f;
        private PanelPreferences Options {get{return OwnerApp.Settings.Panel(Kind);}}
        private float Factor { get { return Options.Scale/100f * DeviceDpiFactor; } }
        private float DeviceDpiFactor { get { return deviceDpiFactor; } }
        private int LockWidth { get { return Math.Max(220,100+lockName.Length*9); } }
        private int LogicalWidth { get { return Kind=="media"?400:Kind=="volume"?360:LockWidth; } }
        private bool Modern {get{return Options.Design=="Modern";}}
        private bool Vinyl {get{return Options.Design=="Plak";}}
        private int LogicalHeight { get { return Kind=="media"?((Modern||Vinyl)?166:194):Kind=="volume"?(Modern?88:80):88; } }

        public Flyout(Controller app,string kind)
        {
            OwnerApp=app;Kind=kind;Icon=BrandAssets.WindowIcon;
            FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;
            StartPosition=FormStartPosition.Manual;DoubleBuffered=true;
            using(var g=CreateGraphics())deviceDpiFactor=Math.Max(1f,g.DpiX/96f);
            BackColor=Palette.Card(app.Settings);Font=new Font("Segoe UI",10);
            AccessibleName=kind=="media"?T("Medya kontrolleri","Media controls"):kind=="volume"?T("Ses düzeyi","Volume"):T("Kilit tuşu bildirimi","Lock key notification");
            // Keep the vinyl rotation fluid without using a busy loop.
            animation=new Timer{Interval=33};
            animation.Tick+=delegate {
                if(Visible&&!hovering&&!(Kind=="media"&&app.Settings.Pinned&&!demo)&&DateTime.UtcNow>expires)Hide();
                if(Visible&&Kind=="media")
                {
                    bool vinyl=Options.Design=="Plak";
                    bool active=OwnerApp.Media.Playing||demo;
                    if(!vinyl||active)RefreshContent();
                }
            };
            animation.Start();
            MouseEnter+=delegate{hovering=true;};
            MouseLeave+=delegate{hovering=false;expires=DateTime.UtcNow.AddSeconds(1.5);};
            var menu=new ContextMenuStrip();
            menu.Items.Add(T("Flux ayarları","Flux settings"),null,delegate{app.ShowSettings();});
            menu.Items.Add(T("Bildirimi kapat","Dismiss notification"),null,delegate{Hide();});
            ContextMenuStrip=menu;
            Apply();
        }
        protected override bool ShowWithoutActivation { get { return true; } }
        protected override CreateParams CreateParams { get { var p=base.CreateParams;p.ExStyle|=0x08000000|0x80|0x80000;return p; } }
        public void Apply()
        {
            if(IsDisposed)return;
            RefreshLanguage();
            float f=Factor;ClientSize=new Size((int)(LogicalWidth*f),(int)(LogicalHeight*f));
            // Do not set Form.Opacity/Region: both bypass the per-pixel alpha path.
            Place();Invalidate();
            if(Visible)PresentSurface();
        }
        private void RefreshLanguage()
        {
            if(ContextMenuStrip!=null)
            {
                ContextMenuStrip.Items[0].Text=T("Flux ayarları","Flux settings");
                ContextMenuStrip.Items[1].Text=T("Bildirimi kapat","Dismiss notification");
            }
            AccessibleName=Kind=="media"?T("Medya kontrolleri","Media controls"):Kind=="volume"?T("Ses düzeyi","Volume"):lockName+(locked?T(" açık"," on"):T(" kapalı"," off"));
        }
        private void Place()
        {
            var p=Options;
            Screen[] all=Screen.AllScreens;var area=all[Math.Max(0,Math.Min(all.Length-1,p.Monitor))].WorkingArea;
            float dpi=DeviceDpiFactor;
            int margin=(int)(p.Margin*dpi);
            int x=p.Position.StartsWith("Sağ")?area.Right-Width-margin:p.Position.StartsWith("Orta")?area.Left+(area.Width-Width)/2:area.Left+margin;
            int y=p.Position.Contains("alt")?area.Bottom-Height-margin:area.Top+margin;
            x+=(int)(p.OffsetX*dpi);y+=(int)(p.OffsetY*dpi);
            Location=new Point(Math.Max(area.Left,Math.Min(area.Right-Width,x)),Math.Max(area.Top,Math.Min(area.Bottom-Height,y)));
        }

        public void Reveal(bool preview=false)
        {
            demo=preview;
            if(!preview&&!OwnerApp.Settings.Enabled)return;
            expires=DateTime.UtcNow.AddSeconds(Options.Duration);
            Apply();if(!Visible)Show();
            Native.SetWindowPos(Handle,new IntPtr(-1),0,0,0,0,0x13);RefreshContent();
        }
        public void RevealLock(string name,bool state,bool preview=false)
        {
            lockName=name;locked=state;Reveal(preview);
            AccessibleName=name+(state?T(" açık"," on"):T(" kapalı"," off"));
        }
        protected override void OnPaintBackground(PaintEventArgs e){}
        protected override void OnPaint(PaintEventArgs e){PresentSurface();}
        internal void RefreshContent()
        {
            // UpdateLayeredWindow owns the displayed pixels. Invalidate alone
            // does not reliably produce WM_PAINT after the first frame.
            if(Visible)PresentSurface();
        }
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if(Visible&&OwnerApp!=null)PresentSurface();
        }
        private void DrawSurface(LayeredSurface target,Preferences preferences,bool preview)
        {
            target.Draw(delegate(Graphics g){
                g.ScaleTransform(target.Size.Width/(float)LogicalWidth,target.Size.Height/(float)LogicalHeight);
                PaintCard(g,preferences,Kind,preview,lockName,locked,0);
            },(float)ContentOpacity);
        }
        internal Bitmap RenderSnapshot()
        {
            using(var target=new LayeredSurface(ClientSize))
            {DrawSurface(target,OwnerApp.Settings.Appearance(Kind),demo);return target.Snapshot();}
        }
        internal Bitmap RenderPreviewSnapshot()
        {
            using(var target=new LayeredSurface(new Size(LogicalWidth*2,LogicalHeight*2)))
            {DrawSurface(target,OwnerApp.Settings.Appearance(Kind),true);return target.Snapshot();}
        }
        private void PresentSurface()
        {
            if(presenting||!Visible||IsDisposed||!IsHandleCreated||OwnerApp==null)return;
            presenting=true;
            try
            {
                if(surface==null||surface.Size!=ClientSize)
                {if(surface!=null)surface.Dispose();surface=new LayeredSurface(ClientSize);}
                DrawSurface(surface,OwnerApp.Settings.Appearance(Kind),demo);
                // Opacity is already baked into the frame, including its preview.
                surface.Present(Handle,Location,255);
            }
            finally{presenting=false;}
        }

        private void PaintSurface(Graphics g,Preferences p,int w,int h,float opacity)
        {
            using(var path=Draw.Round(new RectangleF(0,0,w,h),14))
            {
                using(var brush=new SolidBrush(Color.FromArgb((int)Math.Round(255*opacity),Palette.Card(p))))g.FillPath(brush,path);
            }
        }
        internal void PaintCard(Graphics g,Preferences p,string kind,bool preview,string name,bool state,double remaining)
        {
            if(OwnerApp.Settings.Panel(kind).Design=="Modern")
            {
                ModernPanel.Paint(g,OwnerApp,p,kind,preview,name,state,LogicalWidth,LogicalHeight,(float)(EffectiveOpacity/ContentOpacity));
                return;
            }
            if(OwnerApp.Settings.Panel(kind).Design=="Plak")
            {
                VinylPanel.Paint(g,OwnerApp,p,kind,preview,name,state,LogicalWidth,LogicalHeight,(float)(EffectiveOpacity/ContentOpacity));
                return;
            }
            Color bg=Palette.Card(p),fg=Palette.Text(p),muted=Palette.Muted(p),accent=Palette.Accent(p),line=Palette.Line(p);
            StringAlignment align=OwnerApp.Settings.Panel(kind).CenterText?StringAlignment.Center:StringAlignment.Near;
            int w=kind=="media"?400:kind=="volume"?360:Math.Max(220,100+name.Length*9),h=kind=="media"?194:kind=="volume"?80:88;
            // Reduce the background first, then apply a gentler fade to the
            // whole frame. Solid content ends at ContentOpacity, background at
            // EffectiveOpacity; overlapping controls/artwork fade as one group.
            float backgroundRatio=(float)(EffectiveOpacity/ContentOpacity);
            PaintSurface(g,p,w,h,backgroundRatio);
            using(var path=Draw.Round(new RectangleF(.5f,.5f,w-1,h-1),14))
            using(var pen=new Pen(Color.FromArgb((int)Math.Round(255*backgroundRatio),line)))g.DrawPath(pen,path);
            if(kind=="media")
            {
                var m=OwnerApp.Media;
                bool has=m.Available&&!preview, showReal=m.Available;
                Draw.Box(g,Palette.Bg(p),new RectangleF(20,20,90,90),10);
                if(showReal&&m.Artwork!=null)
                {
                    var saved=g.Save();using(var path=Draw.Round(new RectangleF(20,20,90,90),10))g.SetClip(path);
                    Draw.Artwork(g,m.Artwork,new RectangleF(20,20,90,90));g.Restore(saved);
                }
                else { Draw.Icon(g,"\uE8D6",32,accent,new RectangleF(20,30,90,62)); }
                string title=showReal?m.Title:preview?T("Sizin müziğiniz, sizin alanınız","Your music, your space"):T("Medya bekleniyor","Waiting for media");
                string artist=showReal?m.Artist:preview?T("Flux • Medya önizlemesi","Flux • Media preview"):T("Müzik uygulamasında oynatın","Play music in your player");
                Draw.Text(g,title,15,fg,new RectangleF(128,20,249,27),true,align);
                Draw.Text(g,artist,12,muted,new RectangleF(128,48,249,24),false,align);
                int prevX=align==StringAlignment.Center?188:122,playX=align==StringAlignment.Center?230:164,nextX=align==StringAlignment.Center?276:210;
                Draw.Icon(g,"\uE892",17,has&&!m.CanPrevious?line:fg,new RectangleF(prevX,78,36,35));
                Draw.Box(g,accent,new RectangleF(playX,77,40,36),8);
                Color contrast=accent.GetBrightness()>.6?Color.Black:Color.White;
                Draw.Icon(g,(showReal?m.Playing:true)?"\uE769":"\uE768",17,contrast,new RectangleF(playX,77,40,36));
                Draw.Icon(g,"\uE893",17,has&&!m.CanNext?line:fg,new RectangleF(nextX,78,36,35));
                double duration=showReal?m.End-m.Start:203,position=showReal?m.CurrentPosition-m.Start:76;
                float ratio=duration>0?(float)Math.Max(0,Math.Min(1,position/duration)):0;
                Draw.Box(g,line,new RectangleF(20,138,360,4),2);
                if(ratio>0)Draw.Box(g,accent,new RectangleF(20,138,Math.Max(4,360*ratio),4),2);
                using(var b=new SolidBrush(accent))g.FillEllipse(b,15+360*ratio,135,10,10);
                Draw.Text(g,Draw.Time(position),11,muted,new RectangleF(20,153,80,22));
                Draw.Text(g,duration>0?Draw.Time(duration):T("CANLI","LIVE"),11,muted,new RectangleF(337,153,47,22));
                if(!showReal&&!preview)Draw.Text(g,T("Ayarlar için sağ tıklayın","Right-click for settings"),10,muted,new RectangleF(136,153,175,22),false,align);
            }
            else if(kind=="volume")
            {
                var a=OwnerApp.Audio;float volume=preview?.64f:a.Volume;bool mute=!preview&&a.Muted;
                Draw.Icon(g,mute?"\uE74F":volume>.5?"\uE995":"\uE993",23,fg,new RectangleF(16,23,40,34));
                Draw.Text(g,mute?T("Sessiz","Muted"):T("Ses düzeyi","Volume"),11,muted,new RectangleF(69,11,150,23),false,align);
                Draw.Box(g,line,new RectangleF(72,45,218,5),2.5f);
                if(!mute&&volume>0)Draw.Box(g,accent,new RectangleF(72,45,Math.Max(5,218*volume),5),2.5f);
                using(var b=new SolidBrush(accent))g.FillEllipse(b,67+218*volume,42,11,11);
                Draw.Text(g,((int)Math.Round(volume*100)).ToString(),17,fg,new RectangleF(310,25,47,31),true);
            }
            else
            {
                // Both helpers center vertically within the full panel height.
                bool closedIcon=!state;
                Draw.Icon(g,closedIcon?"\uE72E":"\uE785",22,fg,new RectangleF(16,0,41,h));
                Draw.Text(g,name+(state?T(" açık"," on"):T(" kapalı"," off")),14,fg,new RectangleF(65,0,w-80,h),true,align);
                // Kilit bildirimi anlık durum gösterir; süre çizgisi kullanılmaz.
            }
        }
        protected override async void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if(e.Button!=MouseButtons.Left||demo)return;
            float x=e.X/Factor,y=e.Y/Factor;
            if(Modern||Vinyl)
            {
                string command=Modern?ModernPanel.Hit(Kind,new PointF(x,y),Options.CenterText):VinylPanel.Hit(Kind,new PointF(x,y));
                if(Kind=="media")
                {
                    double ratio=Modern?ModernPanel.SeekRatio(x):VinylPanel.SeekRatio(x);
                    if(command=="seek")await OwnerApp.Media.Command("seek",OwnerApp.Media.Start+(OwnerApp.Media.End-OwnerApp.Media.Start)*ratio);
                    else if(command!=null)await OwnerApp.Media.Command(command);
                }
                else if(Kind=="volume")
                {
                    if(command=="mute")OwnerApp.Audio.ToggleMute();
                    else if(command=="volume")OwnerApp.Audio.Set(ModernPanel.VolumeRatio(x));
                }
                expires=DateTime.UtcNow.AddSeconds(Options.Duration);RefreshContent();return;
            }
            if(Kind=="media")
            {
                if(y>=76&&y<=116)
                {
                    int prevX=Options.CenterText?188:120,playX=Options.CenterText?230:162,nextX=Options.CenterText?276:208;
                    if(x>=prevX&&x<prevX+40)await OwnerApp.Media.Command("previous");
                    else if(x>=playX&&x<=playX+44)await OwnerApp.Media.Command("play");
                    else if(x>=nextX&&x<=nextX+42)await OwnerApp.Media.Command("next");
                }
                else if(y>=127&&y<=152&&x>=20&&x<=380)
                    await OwnerApp.Media.Command("seek",OwnerApp.Media.Start+(OwnerApp.Media.End-OwnerApp.Media.Start)*(x-20)/360);
            }
            else if(Kind=="volume")
            {
                if(x<60)OwnerApp.Audio.ToggleMute();
                else if(x>=67&&x<=299)OwnerApp.Audio.Set((x-72)/218);
            }
            expires=DateTime.UtcNow.AddSeconds(Options.Duration);RefreshContent();
        }
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if(Kind=="volume"&&!demo)OwnerApp.Audio.Set(OwnerApp.Audio.Volume+(e.Delta>0?.02f:-.02f));
            base.OnMouseWheel(e);
        }
        public void SavePreview(string path,Preferences preferences)
        {
            int w=LogicalWidth,h=LogicalHeight;
            using(var target=new LayeredSurface(new Size(w*2,h*2)))
            {
                DrawSurface(target,preferences,true);
                using(var image=target.Snapshot())image.Save(path,System.Drawing.Imaging.ImageFormat.Png);
            }
        }
        protected override void Dispose(bool disposing)
        {
            if(disposing){animation.Dispose();if(surface!=null){surface.Dispose();surface=null;}var menu=ContextMenuStrip;ContextMenuStrip=null;if(menu!=null)menu.Dispose();}
            base.Dispose(disposing);
        }
    }
}
