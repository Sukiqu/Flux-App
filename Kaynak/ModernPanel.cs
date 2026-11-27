using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Akis
{
    // The desktop compositor supplies the live colour behind these alpha pixels.
    // No screen capture, wallpaper sampling or background polling is required.
    internal static class ModernPanel
    {
        internal static RectangleF Previous(bool center){return new RectangleF(center?202:173,87,36,36);}
        internal static RectangleF Play(bool center){return new RectangleF(center?252:223,87,36,36);}
        internal static RectangleF Next(bool center){return new RectangleF(center?302:273,87,36,36);}
        internal static readonly RectangleF Seek=new RectangleF(156,126,224,18);
        internal static readonly RectangleF Volume=new RectangleF(76,42,210,26);
        internal static string Hit(string kind,PointF point,bool center)
        {
            if(kind=="media")
            {
                if(Previous(center).Contains(point))return "previous";
                if(Play(center).Contains(point))return "play";
                if(Next(center).Contains(point))return "next";
                if(Seek.Contains(point))return "seek";
            }
            else if(kind=="volume")
            {
                if(new RectangleF(16,16,48,56).Contains(point))return "mute";
                if(Volume.Contains(point))return "volume";
            }
            return null;
        }
        internal static double SeekRatio(float x){return Math.Max(0,Math.Min(1,(x-Seek.Left)/Seek.Width));}
        internal static float VolumeRatio(float x){return Math.Max(0,Math.Min(1,(x-Volume.Left)/Volume.Width));}
        private static string T(Controller app,string tr,string en){return UiText.English(app.Settings)?en:tr;}
        private static void Glow(Graphics g,RectangleF ellipse,Color color)
        {
            using(var path=new GraphicsPath())
            {
                path.AddEllipse(ellipse);
                using(var b=new PathGradientBrush(path)){b.CenterColor=color;b.SurroundColors=new[]{Color.FromArgb(0,color)};g.FillPath(b,path);}
            }
        }
        private static Color Alpha(Color color,int alpha,float opacity){return Color.FromArgb(Math.Max(0,Math.Min(255,(int)(alpha*opacity))),color);}
        internal static void Surface(Graphics g,Preferences p,int w,int h,float opacity)
        {
            bool light=Palette.Light(p);float radius=Math.Min(27,h*.32f);
            var rect=new RectangleF(1.5f,1.5f,w-3,h-3);
            var state=g.Save();
            try {
                g.SmoothingMode=SmoothingMode.AntiAlias;
                using(var path=Draw.Round(rect,radius))
                {
                    g.SetClip(path,CombineMode.Intersect);
                    // Neutral translucent material lets the actual background
                    // colour show through, instead of painting a fixed purple tint.
                    using(var b=new LinearGradientBrush(rect,Alpha(light?Color.FromArgb(253,251,255):Color.FromArgb(43,38,57),light?174:154,opacity),Alpha(light?Color.FromArgb(223,233,249):Color.FromArgb(17,16,28),light?148:130,opacity),75f))g.FillPath(b,path);
                    Glow(g,new RectangleF(-w*.25f,-h*.58f,w*.92f,h*1.24f),Alpha(Color.White,100,opacity));
                    Glow(g,new RectangleF(w*.60f,h*.45f,w*.64f,h*.85f),Alpha(Color.White,47,opacity));
                    Glow(g,new RectangleF(w*.62f,-h*.4f,w*.60f,h*.78f),Alpha(Palette.Accent(p),54,opacity));
                }
            }finally{g.Restore(state);}
            using(var edge=Draw.Round(rect,radius))
            using(var b=new LinearGradientBrush(rect,Alpha(Color.White,205,opacity),Alpha(light?Color.FromArgb(113,127,160):Color.White,68,opacity),68f))
            using(var pen=new Pen(b,1.15f))g.DrawPath(pen,edge);
            using(var edge=Draw.Round(new RectangleF(3,3,w-6,h-6),radius-1.5f))
            using(var pen=new Pen(Alpha(Color.White,25,opacity),1f))g.DrawPath(pen,edge);
        }
        internal static void Paint(Graphics g,Controller app,Preferences p,string kind,bool preview,string name,bool on,int w,int h,float opacity)
        {
            Surface(g,p,w,h,opacity);
            bool light=Palette.Light(p);Color fg=light?Color.FromArgb(31,29,49):Color.FromArgb(250,246,255);
            Color muted=light?Color.FromArgb(86,78,111):Color.FromArgb(192,181,214);
            Color track=Color.FromArgb(65,fg),accent=Palette.Accent(p);
            bool centered=app.Settings.Panel(kind).CenterText;
            StringAlignment align=centered?StringAlignment.Center:StringAlignment.Near;
            if(kind=="media")
            {
                var media=app.Media;bool real=media.Available,enabled=!real||preview;
                var art=new RectangleF(14,14,126,126);
                var saved=g.Save();
                using(var path=Draw.Round(art,16))
                {
                    g.SetClip(path,CombineMode.Intersect);
                    if(real&&media.Artwork!=null)Draw.Artwork(g,media.Artwork,art);
                    else {
                        using(var b=new LinearGradientBrush(art,Color.FromArgb(87,73,123),Color.FromArgb(201,138,154),40f))g.FillRectangle(b,art);
                        Glow(g,new RectangleF(3,12,128,100),Color.FromArgb(145,255,216,197));
                        Draw.Icon(g,"\uE8D6",40,Color.FromArgb(230,255,245,244),art);
                    }
                }
                g.Restore(saved);
                using(var path=Draw.Round(art,16))using(var pen=new Pen(Color.FromArgb(50,Color.White)))g.DrawPath(pen,path);
                string title=real?media.Title:T(app,"Sizin müziğiniz","Your music");
                string artist=real?media.Artist:T(app,"Flux • Medya önizlemesi","Flux • Media preview");
                Draw.Text(g,title,17,fg,new RectangleF(156,17,183,28),false,align);
                Draw.Text(g,artist,14,muted,new RectangleF(156,44,183,24),false,align);
                Draw.Box(g,Color.FromArgb(43,accent),new RectangleF(346,18,35,40),17);
                Draw.Icon(g,"\uE8D6",16,fg,new RectangleF(346,18,35,40));
                Draw.Icon(g,"\uE892",17,enabled||media.CanPrevious?fg:muted,Previous(centered));
                Draw.Icon(g,(real?media.Playing:true)?"\uE769":"\uE768",18,fg,Play(centered));
                Draw.Icon(g,"\uE893",17,enabled||media.CanNext?fg:muted,Next(centered));
                double duration=real?media.End-media.Start:203,position=real?media.CurrentPosition-media.Start:76;
                float ratio=duration>0?(float)Math.Max(0,Math.Min(1,position/duration)):0;
                Draw.Box(g,track,new RectangleF(Seek.Left,133,Seek.Width,5),2.5f);
                if(ratio>0)Draw.Box(g,fg,new RectangleF(Seek.Left,133,Math.Max(5,Seek.Width*ratio),5),2.5f);
                Draw.Text(g,Draw.Time(position),9,muted,new RectangleF(156,143,72,15));
                Draw.Text(g,duration>0?Draw.Time(duration):T(app,"CANLI","LIVE"),9,muted,new RectangleF(308,143,72,15),false,StringAlignment.Far);
            }
            else if(kind=="volume")
            {
                float volume=preview?.64f:app.Audio.Volume;bool mute=!preview&&app.Audio.Muted;
                Draw.Box(g,Color.FromArgb(26,fg),new RectangleF(17,20,44,48),18);
                Draw.Icon(g,mute?"\uE74F":"\uE995",23,fg,new RectangleF(17,20,44,48));
                Draw.Text(g,mute?T(app,"Sessiz","Muted"):T(app,"Ses düzeyi","Volume"),13,muted,new RectangleF(76,16,210,24),false,align);
                Draw.Box(g,track,new RectangleF(Volume.Left,53,Volume.Width,5),2.5f);
                float fill=mute?0:volume;
                if(fill>0)Draw.Box(g,fg,new RectangleF(Volume.Left,53,Math.Max(5,Volume.Width*fill),5),2.5f);
                using(var b=new SolidBrush(fg))g.FillEllipse(b,Volume.Left+Volume.Width*fill-5,50,10,10);
                Draw.Text(g,((int)Math.Round(volume*100)).ToString(),20,fg,new RectangleF(300,18,47,50),false,StringAlignment.Center);
            }
            else
            {
                Draw.Box(g,Color.FromArgb(23,fg),new RectangleF(17,(h-42)/2,42,42),17);
                Draw.Icon(g,on?"\uE785":"\uE72E",22,fg,new RectangleF(17,0,42,h));
                Draw.Text(g,name+(on?T(app," açık"," on"):T(app," kapalı"," off")),14,fg,new RectangleF(71,0,w-85,h),true,align);
            }
        }
    }
}
