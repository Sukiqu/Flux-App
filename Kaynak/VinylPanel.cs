using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Akis
{
    // A compact record-player inspired panel. Artwork is clipped to the record
    // label so the design remains recognizable without requiring extra assets.
    internal static class VinylPanel
    {
        internal static readonly RectangleF PreviousButton=new RectangleF(187,82,36,36);
        internal static readonly RectangleF PlayButton=new RectangleF(238,78,44,44);
        internal static readonly RectangleF NextButton=new RectangleF(296,82,36,36);
        internal static readonly RectangleF Seek=new RectangleF(174,135,205,16);

        internal static string Hit(string kind,PointF point)
        {
            if(kind!="media")return null;
            if(PreviousButton.Contains(point))return "previous";
            if(PlayButton.Contains(point))return "play";
            if(NextButton.Contains(point))return "next";
            if(Seek.Contains(point))return "seek";
            return null;
        }

        internal static double SeekRatio(float x){return Math.Max(0,Math.Min(1,(x-Seek.Left)/Seek.Width));}

        private static Color Alpha(Color c,int a,float opacity)
        {return Color.FromArgb(Math.Max(0,Math.Min(255,(int)Math.Round(a*opacity))),c);}

        private static void Rounded(Graphics g,RectangleF rect,float radius,Color top,Color bottom,float opacity)
        {
            using(var path=Draw.Round(rect,radius))
            using(var brush=new LinearGradientBrush(rect,Alpha(top,255,opacity),Alpha(bottom,255,opacity),90f))g.FillPath(brush,path);
        }

        private static void Groove(Graphics g,RectangleF disc,Color color,float opacity)
        {
            using(var pen=new Pen(Alpha(Color.Black,78,opacity),1.1f))
            using(var shine=new Pen(Alpha(color,46,opacity),.8f))
            using(var glint=new Pen(Alpha(Color.White,24,opacity),1f))
            {
                for(int i=8;i<58;i+=7)
                {
                    var ring=new RectangleF(disc.X+i,disc.Y+i,disc.Width-2*i,disc.Height-2*i);
                    g.DrawEllipse(pen,ring);
                    g.DrawEllipse(shine,ring);
                    g.DrawArc(glint,ring,205,72);
                }
            }
        }

        private static void DiscLight(Graphics g,RectangleF disc,float opacity)
        {
            var state=g.Save();
            try
            {
                using(var path=new GraphicsPath())
                {
                    path.AddEllipse(disc);g.SetClip(path,CombineMode.Intersect);
                    using(var light=new LinearGradientBrush(disc,Alpha(Color.White,52,opacity),Color.Transparent,135f))g.FillEllipse(light,disc);
                }
            }
            finally{g.Restore(state);}
        }

        private static void DiscShade(Graphics g,RectangleF disc,float opacity)
        {
            using(var shade=new LinearGradientBrush(disc,Color.Transparent,Alpha(Color.Black,105,opacity),135f))g.FillEllipse(shade,disc);
        }

        private static void PlaybackGlyph(Graphics g,RectangleF bounds,bool playing,Color color,float opacity)
        {
            using(var brush=new SolidBrush(Alpha(color,235,opacity)))
            {
                if(playing)
                {
                    float cx=bounds.X+bounds.Width/2f,cy=bounds.Y+bounds.Height/2f;
                    float barWidth=4f,gap=5f,h=bounds.Height*.34f;
                    float x=cx-(barWidth*2+gap)/2f,y=cy-h/2f;
                    g.FillRectangle(brush,x,y,barWidth,h);g.FillRectangle(brush,x+barWidth+gap,y,barWidth,h);
                }
                else
                {
                    float cx=bounds.X+bounds.Width/2f,cy=bounds.Y+bounds.Height/2f;
                    var points=new[]{new PointF(cx-4f,cy-9f),new PointF(cx-4f,cy+9f),new PointF(cx+8f,cy)};
                    g.FillPolygon(brush,points);
                }
            }
        }

        private static void Record(Graphics g,Controller app,Preferences p,bool preview,float opacity)
        {
            var disc=new RectangleF(18,18,128,128);var media=app.Media;
            var spin=g.Save();
            if(app.Settings.Panel("media").Spin&&(preview||media.Playing))
            {
                // 33 1/3 RPM is approximately 200 degrees per second.
                float angle=(float)((DateTime.UtcNow.Ticks/(double)TimeSpan.TicksPerSecond*200.0)%360.0);
                g.TranslateTransform(82,82);g.RotateTransform(angle);g.TranslateTransform(-82,-82);
            }
            using(var brush=new LinearGradientBrush(disc,Alpha(Color.FromArgb(36,37,41),255,opacity),Alpha(Color.FromArgb(2,2,4),255,opacity),45f))g.FillEllipse(brush,disc);
            DiscLight(g,disc,opacity);
            DiscShade(g,disc,opacity);
            Groove(g,disc,Color.White,opacity);
            // Let the artwork run into the grooves so source-image borders do
            // not leave visible gaps around the record label.
            var label=new RectangleF(48,48,68,68);var saved=g.Save();
            using(var path=new GraphicsPath()){path.AddEllipse(label);g.SetClip(path,CombineMode.Intersect);if(!preview&&media.Artwork!=null)Draw.Artwork(g,media.Artwork,label,true);else {using(var b=new LinearGradientBrush(label,Color.FromArgb(225,225,225),Color.FromArgb(110,110,116),45f))g.FillEllipse(b,label);Draw.Icon(g,"\uE8D6",22,Color.FromArgb(80,80,87),label);}}
            g.Restore(saved);
            using(var pen=new Pen(Alpha(Color.White,110,opacity),1f))g.DrawEllipse(pen,label);
            using(var b=new SolidBrush(Alpha(Color.FromArgb(220,220,220),255,opacity)))g.FillEllipse(b,new RectangleF(78,78,8,8));
            using(var pen=new Pen(Alpha(Color.White,75,opacity),1f))g.DrawEllipse(pen,new RectangleF(27,27,110,110));
            g.Restore(spin);
            // The arm follows the reference silhouette down over the record's
            // right edge and finishes with a visible stylus.
            using(var shadow=new Pen(Alpha(Color.Black,185,opacity),6f)){g.DrawLine(shadow,133,24,133,16);g.DrawLine(shadow,133,16,133,76);g.DrawBezier(shadow,133,76,133,91,132,99,123,109);}
            using(var pen=new Pen(Alpha(Color.FromArgb(235,235,232),240,opacity),3.2f)){g.DrawLine(pen,133,24,133,16);g.DrawLine(pen,133,16,133,76);g.DrawBezier(pen,133,76,133,91,132,99,123,109);}
            using(var stylus=new Pen(Alpha(Color.FromArgb(248,248,244),245,opacity),2f)){g.DrawLine(stylus,123,109,118,114);g.DrawLine(stylus,118,114,125,116);}
            using(var joint=new SolidBrush(Alpha(Color.FromArgb(40,40,42),245,opacity)))g.FillEllipse(joint,new RectangleF(124,7,18,18));
            using(var ring=new Pen(Alpha(Color.FromArgb(235,235,232),235,opacity),1.8f))g.DrawEllipse(ring,new RectangleF(124,7,18,18));
            using(var b=new SolidBrush(Alpha(Color.FromArgb(190,190,188),235,opacity)))g.FillEllipse(b,new RectangleF(130,13,6,6));
        }

        internal static void Surface(Graphics g,Preferences p,int w,int h,float opacity)
        {
            var rect=new RectangleF(1,1,w-2,h-2);g.SmoothingMode=SmoothingMode.AntiAlias;
            Rounded(g,rect,17,Color.FromArgb(108,111,114),Color.FromArgb(31,33,35),opacity);
            using(var edge=Draw.Round(rect,17))using(var pen=new Pen(Alpha(Color.FromArgb(232,233,232),210,opacity),1.3f))g.DrawPath(pen,edge);
            using(var edge=Draw.Round(new RectangleF(4,4,w-8,h-8),14))using(var pen=new Pen(Alpha(Color.Black,75,opacity),1f))g.DrawPath(pen,edge);
        }

        internal static void Paint(Graphics g,Controller app,Preferences p,string kind,bool preview,string name,bool on,int w,int h,float opacity)
        {
            Surface(g,p,w,h,opacity);Color fg=Color.FromArgb(245,245,244),muted=Color.FromArgb(188,190,190),accent=Palette.Accent(p);
            if(kind=="media")
            {
                Record(g,app,p,preview,opacity);var media=app.Media;bool real=media.Available,enabled=!real||preview;
                string title=real?media.Title:preview?"Pemutar Musik":T(app,"Medya bekleniyor","Waiting for media");
                string artist=real?media.Artist:preview?"iWidgets":T(app,"Müzik uygulamasında oynatın","Play music in your player");
                StringAlignment align=app.Settings.Panel(kind).CenterText?StringAlignment.Center:StringAlignment.Near;
                Draw.Text(g,title,17,fg,new RectangleF(174,24,202,27),true,align);
                Draw.Text(g,artist,13,muted,new RectangleF(174,51,202,23),false,align);
                Draw.Icon(g,"\uE892",16,enabled||media.CanPrevious?fg:muted,PreviousButton);
                using(var pen=new Pen(Alpha(Color.White,235,opacity),1.2f))g.DrawEllipse(pen,PlayButton);
                PlaybackGlyph(g,PlayButton,real&&media.Playing,fg,opacity);
                Draw.Icon(g,"\uE893",16,enabled||media.CanNext?fg:muted,NextButton);
                double duration=real?media.End-media.Start:203,position=real?media.CurrentPosition-media.Start:76;float ratio=duration>0?(float)Math.Max(0,Math.Min(1,position/duration)):0;
                Draw.Box(g,Color.FromArgb(90,Color.White),new RectangleF(174,137,205,4),2);
                if(ratio>0)Draw.Box(g,Color.White,new RectangleF(174,137,Math.Max(4,205*ratio),4),2);
            }
            else if(kind=="volume")
            {
                float volume=preview?.64f:app.Audio.Volume;bool mute=!preview&&app.Audio.Muted;Draw.Icon(g,mute?"\uE74F":volume>.5?"\uE995":"\uE993",23,fg,new RectangleF(18,20,44,48));Draw.Text(g,mute?T(app,"Sessiz","Muted"):T(app,"Ses düzeyi","Volume"),13,muted,new RectangleF(78,16,210,24));Draw.Box(g,Color.FromArgb(85,Color.White),new RectangleF(78,53,210,5),2.5f);if(!mute&&volume>0)Draw.Box(g,accent,new RectangleF(78,53,Math.Max(5,210*volume),5),2.5f);Draw.Text(g,((int)Math.Round(volume*100)).ToString(),20,fg,new RectangleF(304,18,47,50),false,StringAlignment.Center);
            }
            else
            {
                Draw.Icon(g,on?"\uE785":"\uE72E",22,fg,new RectangleF(18,0,42,h));Draw.Text(g,name+(on?T(app," açık"," on"):T(app," kapalı"," off")),14,fg,new RectangleF(72,0,w-88,h),true,app.Settings.Panel(kind).CenterText?StringAlignment.Center:StringAlignment.Near);
            }
        }

        internal static void PaintPreview(Graphics g,Preferences p,int w,int h,float opacity)
        {
            Surface(g,p,w,h,opacity);var disc=new RectangleF(18,18,128,128);
            using(var b=new LinearGradientBrush(disc,Color.FromArgb(31,32,36),Color.FromArgb(2,2,4),45f))g.FillEllipse(b,disc);
            DiscLight(g,disc,opacity);
            DiscShade(g,disc,opacity);
            Groove(g,disc,Color.White,opacity);
            using(var b=new LinearGradientBrush(new RectangleF(48,48,68,68),Color.FromArgb(226,227,224),Color.FromArgb(132,133,134),45f))g.FillEllipse(b,new RectangleF(48,48,68,68));
            using(var b=new SolidBrush(Color.FromArgb(70,70,75)))g.FillEllipse(b,new RectangleF(78,78,8,8));
            using(var shadow=new Pen(Alpha(Color.Black,185,opacity),6f)){g.DrawLine(shadow,133,24,133,16);g.DrawLine(shadow,133,16,133,76);g.DrawBezier(shadow,133,76,133,91,132,99,123,109);}
            using(var pen=new Pen(Alpha(Color.FromArgb(235,235,232),240,opacity),3.2f)){g.DrawLine(pen,133,24,133,16);g.DrawLine(pen,133,16,133,76);g.DrawBezier(pen,133,76,133,91,132,99,123,109);}
            using(var stylus=new Pen(Alpha(Color.FromArgb(248,248,244),245,opacity),2f)){g.DrawLine(stylus,123,109,118,114);g.DrawLine(stylus,118,114,125,116);}
            using(var joint=new SolidBrush(Alpha(Color.FromArgb(40,40,42),245,opacity)))g.FillEllipse(joint,new RectangleF(124,7,18,18));
            using(var ring=new Pen(Alpha(Color.FromArgb(235,235,232),235,opacity),1.8f))g.DrawEllipse(ring,new RectangleF(124,7,18,18));
            using(var b=new SolidBrush(Alpha(Color.FromArgb(190,190,188),235,opacity)))g.FillEllipse(b,new RectangleF(130,13,6,6));
            Draw.Text(g,"Pemutar Musik",17,Color.FromArgb(245,245,244),new RectangleF(174,24,202,27),true);
            Draw.Text(g,"iWidgets",13,Color.FromArgb(188,190,190),new RectangleF(174,51,202,23));
            Draw.Icon(g,"\uE892",16,Color.White,PreviousButton);using(var pen=new Pen(Color.White,1.2f))g.DrawEllipse(pen,PlayButton);PlaybackGlyph(g,PlayButton,false,Color.White,opacity);Draw.Icon(g,"\uE893",16,Color.White,NextButton);
            Draw.Box(g,Color.FromArgb(90,Color.White),new RectangleF(174,137,205,4),2);Draw.Box(g,Color.White,new RectangleF(174,137,85,4),2);
        }

        private static string T(Controller app,string tr,string en){return UiText.English(app.Settings)?en:tr;}
    }
}
