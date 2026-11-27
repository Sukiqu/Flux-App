using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Akis
{
    // A premultiplied, per-pixel alpha surface. Native window regions have binary
    // edges; this surface keeps fractional coverage all the way to the compositor.
    internal sealed class LayeredSurface : IDisposable
    {
        [StructLayout(LayoutKind.Sequential)] private struct Header
        {
            internal uint Size; internal int Width,Height; internal ushort Planes,Bits;
            internal uint Compression,ImageSize; internal int Xppm,Yppm; internal uint Colors,Important;
        }
        [StructLayout(LayoutKind.Sequential,Pack=1)] private struct Blend
        { internal byte Operation,Flags,Alpha,Format; }
        [DllImport("gdi32.dll",SetLastError=true)] private static extern IntPtr CreateCompatibleDC(IntPtr dc);
        [DllImport("gdi32.dll",SetLastError=true)] private static extern IntPtr CreateDIBSection(IntPtr dc,ref Header header,uint usage,out IntPtr bits,IntPtr section,uint offset);
        [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr dc,IntPtr obj);
        [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr obj);
        [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr dc);
        [DllImport("user32.dll",SetLastError=true)] private static extern bool UpdateLayeredWindow(IntPtr window,IntPtr screen,ref Point position,ref Size size,IntPtr source,ref Point origin,uint key,ref Blend blend,uint flags);
        private IntPtr dc,bitmap,oldBitmap;
        private Bitmap frame,large;
        internal Size Size {get;private set;}
        internal LayeredSurface(Size size)
        {
            Size=size;
            try
            {
                dc=CreateCompatibleDC(IntPtr.Zero);
                if(dc==IntPtr.Zero)throw new Win32Exception();
                var header=new Header{Size=(uint)Marshal.SizeOf(typeof(Header)),Width=size.Width,Height=-size.Height,Planes=1,Bits=32};
                IntPtr bits;bitmap=CreateDIBSection(dc,ref header,0,out bits,IntPtr.Zero,0);
                if(bitmap==IntPtr.Zero)throw new Win32Exception();
                oldBitmap=SelectObject(dc,bitmap);
                if(oldBitmap==IntPtr.Zero||oldBitmap==new IntPtr(-1))throw new Win32Exception();
                frame=new Bitmap(size.Width,size.Height,size.Width*4,PixelFormat.Format32bppPArgb,bits);
                large=new Bitmap(size.Width*2,size.Height*2,PixelFormat.Format32bppPArgb);
            }
            catch {Dispose();throw;}
        }
        internal void Draw(Action<Graphics> paint,float opacity=1f)
        {
            using(var g=Graphics.FromImage(large))
            {
                g.Clear(Color.Transparent);g.SmoothingMode=SmoothingMode.AntiAlias;
                g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                g.ScaleTransform(2,2);paint(g);
            }
            using(var g=Graphics.FromImage(frame))
            using(var attributes=new ImageAttributes())
            {
                g.CompositingMode=CompositingMode.SourceCopy;
                g.InterpolationMode=InterpolationMode.HighQualityBilinear;
                g.PixelOffsetMode=PixelOffsetMode.HighQuality;
                attributes.SetWrapMode(WrapMode.TileFlipXY);
                if(opacity<1f)
                    attributes.SetColorMatrix(new ColorMatrix{Matrix33=Math.Max(0,opacity)});
                g.DrawImage(large,new Rectangle(Point.Empty,Size),0,0,large.Width,large.Height,GraphicsUnit.Pixel,attributes);
            }
        }
        internal Bitmap Snapshot()
        {
            // Clone can share externally owned scan0 storage. Copy into a managed
            // bitmap so snapshots remain valid after the DIB has been released.
            var copy=new Bitmap(Size.Width,Size.Height,PixelFormat.Format32bppPArgb);
            using(var g=Graphics.FromImage(copy))
            {g.CompositingMode=CompositingMode.SourceCopy;g.DrawImageUnscaled(frame,0,0);}
            return copy;
        }
        internal void Present(IntPtr window,Point position,byte alpha)
        {
            var origin=Point.Empty;var size=Size;var blend=new Blend{Alpha=alpha,Format=1};
            if(!UpdateLayeredWindow(window,IntPtr.Zero,ref position,ref size,dc,ref origin,0,ref blend,2))
                throw new Win32Exception(Marshal.GetLastWin32Error(),"Bildirim yüzeyi güncellenemedi.");
        }
        public void Dispose()
        {
            if(frame!=null){frame.Dispose();frame=null;}
            if(large!=null){large.Dispose();large=null;}
            if(dc!=IntPtr.Zero&&oldBitmap!=IntPtr.Zero&&oldBitmap!=new IntPtr(-1)){SelectObject(dc,oldBitmap);oldBitmap=IntPtr.Zero;}
            if(bitmap!=IntPtr.Zero){DeleteObject(bitmap);bitmap=IntPtr.Zero;}
            if(dc!=IntPtr.Zero){DeleteDC(dc);dc=IntPtr.Zero;}
        }
    }
}
