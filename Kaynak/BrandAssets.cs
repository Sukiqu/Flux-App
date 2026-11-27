using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace Akis
{
    // Shared, embedded branding: no dependency on the executable's working directory.
    internal static class BrandAssets
    {
        private static readonly Bitmap symbol=LoadSymbol();
        internal static readonly Icon WindowIcon=LoadIcon(System.Windows.Forms.SystemInformation.IconSize);
        internal static readonly Icon TrayIcon=LoadIcon(System.Windows.Forms.SystemInformation.SmallIconSize);

        private static Stream Open(string name)
        {
            var stream=typeof(BrandAssets).Assembly.GetManifestResourceStream("Flux."+name);
            if(stream==null)throw new InvalidOperationException("Missing embedded Flux asset: "+name);
            return stream;
        }
        private static Bitmap LoadSymbol()
        {
            using(var stream=Open("Symbol.png"))using(var source=new Bitmap(stream))return new Bitmap(source);
        }
        private static Icon LoadIcon(Size size)
        {
            using(var stream=Open("App.ico"))using(var icon=new Icon(stream,size))return (Icon)icon.Clone();
        }
        internal static void Paint(Graphics g,RectangleF bounds)
        {
            // The square master includes transparent padding; fit the actual F mark.
            var source=new RectangleF(90,330,1080,688);
            float scale=Math.Min(bounds.Width/source.Width,bounds.Height/source.Height);
            var target=new RectangleF(bounds.X+(bounds.Width-source.Width*scale)/2,
                bounds.Y+(bounds.Height-source.Height*scale)/2,source.Width*scale,source.Height*scale);
            var state=g.Save();
            try
            {
                g.InterpolationMode=InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode=PixelOffsetMode.HighQuality;
                g.DrawImage(symbol,target,source,GraphicsUnit.Pixel);
            }
            finally{g.Restore(state);}
        }
    }
}
