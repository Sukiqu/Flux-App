using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;

namespace Akis
{
    // UI-thread-only cache. Rasterize at the destination's physical resolution,
    // with four samples per axis; do not enlarge a fixed low-resolution icon.
    internal static class UiSymbols
    {
        private const int Samples=4, Padding=2, Limit=384;
        private static readonly Dictionary<string,Bitmap> Cache=new Dictionary<string,Bitmap>();
        private static readonly Queue<string> Order=new Queue<string>();
        internal static void Paint(Graphics g,RectangleF r,string key,Action<Graphics,RectangleF> paint)
        {
            if(r.Width<=0||r.Height<=0)return;
            var vectors=new[]{new PointF(r.Width,0),new PointF(0,r.Height)};
            using(var matrix=g.Transform)matrix.TransformVectors(vectors);
            int w=Math.Max(1,(int)Math.Ceiling(Math.Sqrt(vectors[0].X*vectors[0].X+vectors[0].Y*vectors[0].Y)));
            int h=Math.Max(1,(int)Math.Ceiling(Math.Sqrt(vectors[1].X*vectors[1].X+vectors[1].Y*vectors[1].Y)));
            key+=String.Format(CultureInfo.InvariantCulture,"/{0}/{1}/{2}/{3}",r.Width,r.Height,w,h);
            Bitmap bitmap;
            if(!Cache.TryGetValue(key,out bitmap))
            {
                bitmap=new Bitmap(w+Padding*2,h+Padding*2,PixelFormat.Format32bppPArgb);
                using(var large=new Bitmap(bitmap.Width*Samples,bitmap.Height*Samples,PixelFormat.Format32bppPArgb))
                {
                    using(var hi=Graphics.FromImage(large))
                    {
                        hi.SmoothingMode=SmoothingMode.AntiAlias;
                        hi.TextRenderingHint=TextRenderingHint.AntiAlias;
                        hi.TranslateTransform(Padding*Samples,Padding*Samples);
                        hi.ScaleTransform(w*Samples/r.Width,h*Samples/r.Height);
                        paint(hi,new RectangleF(0,0,r.Width,r.Height));
                    }
                    using(var lo=Graphics.FromImage(bitmap))
                    {
                        lo.CompositingMode=CompositingMode.SourceCopy;
                        lo.InterpolationMode=InterpolationMode.HighQualityBicubic;
                        lo.PixelOffsetMode=PixelOffsetMode.HighQuality;
                        lo.DrawImage(large,new Rectangle(0,0,bitmap.Width,bitmap.Height),0,0,large.Width,large.Height,GraphicsUnit.Pixel);
                    }
                }
                if(Cache.Count>=Limit){string oldest=Order.Dequeue();Cache[oldest].Dispose();Cache.Remove(oldest);}
                Cache.Add(key,bitmap);Order.Enqueue(key);
            }
            var state=g.Save();
            try
            {
                g.CompositingMode=CompositingMode.SourceOver;
                g.InterpolationMode=InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode=PixelOffsetMode.HighQuality;
                float px=r.Width/w,py=r.Height/h;
                g.DrawImage(bitmap,new RectangleF(r.X-Padding*px,r.Y-Padding*py,r.Width+Padding*2*px,r.Height+Padding*2*py),new RectangleF(0,0,bitmap.Width,bitmap.Height),GraphicsUnit.Pixel);
            }
            finally{g.Restore(state);}
        }
        internal static void Font(Graphics g,string glyph,float size,Color color,RectangleF r)
        {
            Paint(g,r,"font/"+glyph+"/"+size.ToString(CultureInfo.InvariantCulture)+"/"+color.ToArgb(),
                delegate(Graphics target,RectangleF bounds){Draw.Icon(target,glyph,size,color,bounds);});
        }
        internal static void Circle(Graphics g,RectangleF r,Color start,Color end)
        {
            Paint(g,r,"circle/"+start.ToArgb()+"/"+end.ToArgb(),delegate(Graphics target,RectangleF bounds){
                using(var brush=new LinearGradientBrush(bounds,start,end,20f))target.FillEllipse(brush,bounds);
            });
        }
    }
}
