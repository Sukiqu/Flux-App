using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;
using System.Xml;
using Microsoft.Win32;
using Windows.Foundation;
using Windows.Media.Control;
using Windows.Storage.Streams;

namespace Akis
{
    internal static class PanelDesigns
    {
        internal static readonly string[] Names={"Classic","Modern","Plak"};
    }
    public class PanelPreferences
    {
        public string Design="Classic";
        public string Theme="Uygulama ile aynı", Accent="Uygulama ile aynı", CustomAccent="#FF4B59";
        public bool CenterText=false;
        public bool Spin=true;
        public string Position="Sol üst";
        public int Monitor=0, Margin=28, OffsetX=0, OffsetY=0, Scale=100, Opacity=100, Duration=5;
        public void Normalize()
        {
            if(Array.IndexOf(PanelDesigns.Names,Design)<0)Design="Classic";
            Scale=Math.Max(80,Math.Min(150,Scale));Opacity=Math.Max(45,Math.Min(100,Opacity));
            Duration=Math.Max(1,Math.Min(15,Duration));Margin=Math.Max(0,Math.Min(500,Margin));
            OffsetX=Math.Max(-10000,Math.Min(10000,OffsetX));OffsetY=Math.Max(-10000,Math.Min(10000,OffsetY));
            if(Array.IndexOf(new[]{"Sol üst","Orta üst","Sağ üst","Sol alt","Orta alt","Sağ alt"},Position)<0)Position="Sol üst";
        }
        public void CopyPlacementFrom(PanelPreferences source)
        {Position=source.Position;Monitor=source.Monitor;Margin=source.Margin;OffsetX=source.OffsetX;OffsetY=source.OffsetY;}
    }


    public class Preferences
    {
        public bool Enabled = true, Media = true, Volume = true, Caps = true, Num = true, Scroll = true;
        public string Language="Sistem";
        public bool Pinned = false, HideNative = false;
        public string Theme = "Sistem", Accent = "Sistem", CustomAccent = "#FF4B59", Position = "Sol üst";
        public int Duration = 5, Scale = 100, Opacity = 100, Margin = 28, Monitor = 0;
        public PanelPreferences MediaPanel, VolumePanel, LockPanel;
        public bool AutoShowMedia=false;
        public void EnsurePanels()
        {
            if(MediaPanel==null)MediaPanel=FromLegacy(0);
            if(VolumePanel==null)VolumePanel=FromLegacy(208);
            if(LockPanel==null)LockPanel=FromLegacy(300);
            MediaPanel.Normalize();VolumePanel.Normalize();LockPanel.Normalize();
        }
        private PanelPreferences FromLegacy(int oldOffset)
        {
            int scale=Math.Max(80,Math.Min(150,Scale));
            return new PanelPreferences{
                Position=Position,Monitor=Monitor,Margin=(int)Math.Round(Margin*scale/100.0),
                OffsetY=(Position!=null&&Position.Contains("alt")?-1:1)*(int)Math.Round(oldOffset*scale/100.0),
                Scale=scale,Opacity=Opacity,Duration=Duration,CustomAccent=CustomAccent
            };
        }
        public PanelPreferences Panel(string kind)
        {EnsurePanels();return kind=="media"?MediaPanel:kind=="volume"?VolumePanel:LockPanel;}
        public Preferences Appearance(string kind)
        {
            PanelPreferences p=Panel(kind);
            bool theme=p.Theme=="Uygulama ile aynı",accent=p.Accent=="Uygulama ile aynı";
            return new Preferences {Theme=theme?Theme:p.Theme,Accent=accent?Accent:p.Accent,CustomAccent=accent?CustomAccent:p.CustomAccent};
        }

        private static readonly XmlSerializer Serializer = new XmlSerializer(typeof(Preferences));
        public static string Folder { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AkisFlyout"); } }
        public static Preferences Load()
        {
            try { using(var f = File.OpenRead(Path.Combine(Folder,"settings.xml"))) return Read(f); }
            catch { return new Preferences(); }
        }
        internal static Preferences Read(Stream input)
        {
            const int limit=1024*1024;
            if(input.CanSeek&&input.Length>limit)throw new InvalidDataException("Settings file is too large.");
            var options=new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,
                MaxCharactersInDocument=limit,MaxCharactersFromEntities=1024,CloseInput=false};
            using(var reader=XmlReader.Create(input,options))
            {
                var result=(Preferences)Serializer.Deserialize(reader);
                result.EnsurePanels();return result;
            }
        }
        public void Save()
        {
            EnsurePanels();
            Directory.CreateDirectory(Folder);
            string file=Path.Combine(Folder,"settings.xml"), temp=file+".tmp";
            using(var f=File.Create(temp)) Serializer.Serialize(f,this);
            if(File.Exists(file)) File.Replace(temp,file,null); else File.Move(temp,file);
        }
    }

    internal static class Palette
    {
        public static bool Light(Preferences p)
        {
            if(p.Theme=="Açık") return true;
            if(p.Theme=="Koyu") return false;
            try { using(var k=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize")) return Convert.ToInt32(k.GetValue("AppsUseLightTheme",0))!=0; }
            catch { return false; }
        }
        public static Color Accent(Preferences p)
        {
            if(p.Accent=="Özel") { try { return ColorTranslator.FromHtml(p.CustomAccent); } catch {} }
            if(p.Accent=="Kırmızı") return Color.FromArgb(255,75,89);
            if(p.Accent=="Mor") return Color.FromArgb(154,116,255);
            if(p.Accent=="Mavi") return Color.FromArgb(54,146,248);
            try { uint c; bool opaque; if(Native.DwmGetColorizationColor(out c,out opaque)==0) return Color.FromArgb((int)((c>>16)&255),(int)((c>>8)&255),(int)(c&255)); } catch {}
            return Color.FromArgb(255,75,89);
        }
        public static Color Bg(Preferences p) { return Light(p)?Color.FromArgb(247,248,251):Color.FromArgb(19,20,24); }
        public static Color Card(Preferences p) { return Light(p)?Color.White:Color.FromArgb(31,32,37); }
        public static Color Text(Preferences p) { return Light(p)?Color.FromArgb(26,29,38):Color.FromArgb(247,247,250); }
        public static Color Muted(Preferences p) { return Light(p)?Color.FromArgb(100,107,123):Color.FromArgb(159,163,177); }
        public static Color Line(Preferences p) { return Light(p)?Color.FromArgb(223,226,234):Color.FromArgb(56,58,67); }
    }

    internal static class Draw
    {
        public static GraphicsPath Round(RectangleF r,float radius)
        {
            float d=radius*2; var p=new GraphicsPath();
            p.AddArc(r.X,r.Y,d,d,180,90); p.AddArc(r.Right-d,r.Y,d,d,270,90);
            p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90); p.AddArc(r.X,r.Bottom-d,d,d,90,90); p.CloseFigure(); return p;
        }
        public static void Box(Graphics g, Color c, RectangleF r, float radius)
        { using(var p=Round(r,radius)) using(var b=new SolidBrush(c)) g.FillPath(b,p); }
        public static void Text(Graphics g,string text,float size,Color color,RectangleF r,bool bold=false)
        { Text(g,text,size,color,r,bold,StringAlignment.Near); }
        public static void Text(Graphics g,string text,float size,Color color,RectangleF r,bool bold,StringAlignment alignment)
        {
            using(var f=new Font("Segoe UI",size,bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel))
            using(var b=new SolidBrush(color))
            using(var sf=new StringFormat { Alignment=alignment, Trimming=StringTrimming.EllipsisCharacter, FormatFlags=StringFormatFlags.NoWrap, LineAlignment=StringAlignment.Center })
                g.DrawString(text??"",f,b,r,sf);
        }
        public static void Lock(Graphics g,bool locked,Color color,RectangleF r)
        {
            using(var pen=new Pen(color,2.2f))
            using(var brush=new SolidBrush(Color.FromArgb(locked?38:18,color)))
            {
                float bodyY=r.Y+r.Height*.38f;
                g.FillRectangle(brush,r.X+5,bodyY,r.Width-10,r.Height*.42f);
                g.DrawRectangle(pen,r.X+5,bodyY,r.Width-10,r.Height*.42f);
                var path=new GraphicsPath();
                if(locked){path.AddArc(r.X+10,r.Y+5,r.Width-20,r.Height*.55f,180,-180);}
                else {path.AddArc(r.X+4,r.Y+5,r.Width-16,r.Height*.55f,205,-145);}
                g.DrawPath(pen,path);path.Dispose();
            }
        }
        public static void Icon(Graphics g,string glyph,float size,Color color,RectangleF r)
        {
            using(var f=new Font("Segoe MDL2 Assets",size,FontStyle.Regular,GraphicsUnit.Pixel))
            using(var b=new SolidBrush(color))
            using(var sf=new StringFormat { Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center })
                g.DrawString(glyph,f,b,r,sf);
        }
        public static void Artwork(Graphics g,Image image,RectangleF target)
        { Artwork(g,image,target,false); }
        public static void Artwork(Graphics g,Image image,RectangleF target,bool trimSourceBadge)
        {
            if(image==null)return;
            float sourceHeight=trimSourceBadge?image.Height*.86f:image.Height;
            float scale=Math.Max(target.Width/image.Width,target.Height/sourceHeight);
            float width=image.Width*scale,height=sourceHeight*scale;
            var destination=new RectangleF(target.X+(target.Width-width)/2,target.Y+(target.Height-height)/2,width,height);
            var state=g.Save();
            try
            {
                g.InterpolationMode=InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode=PixelOffsetMode.HighQuality;
                g.DrawImage(image,destination,new RectangleF(0,0,image.Width,sourceHeight),GraphicsUnit.Pixel);
            }
            finally{g.Restore(state);}
        }
        public static string Time(double seconds)
        {
            if(Double.IsNaN(seconds)||Double.IsInfinity(seconds)||seconds<0) seconds=0;
            TimeSpan t=TimeSpan.FromSeconds(seconds);
            return t.TotalHours>=1 ? t.ToString(@"h\:mm\:ss") : t.ToString(@"mm\:ss");
        }
    }

    internal static class WinAsync
    {
        public static async Task<T> Get<T>(IAsyncOperation<T> operation)
        {
            IAsyncInfo info=(IAsyncInfo)operation;
            DateTime until=DateTime.UtcNow.AddSeconds(6);
            try
            {
                while(info.Status==AsyncStatus.Started)
                {
                    if(DateTime.UtcNow>until) { info.Cancel(); throw new TimeoutException("Medya yanıt süresi aşıldı."); }
                    await Task.Delay(35);
                }
                return operation.GetResults();
            }
            finally { try { info.Close(); } catch {} }
        }
    }

    internal sealed class MediaService : IDisposable
    {
        private GlobalSystemMediaTransportControlsSessionManager manager;
        internal GlobalSystemMediaTransportControlsSession Session;
        public string Title="", Artist="", Source="", Error="";
        public Bitmap Artwork;
        public bool Playing, CanPlay, CanNext, CanPrevious, CanSeek;
        public double Start, End, Position, Rate=1;
        private DateTime sample=DateTime.UtcNow;
        private string key="";
        private bool busy, disposed;
        private readonly MediaCommands commands=new MediaCommands();
        public double CurrentPosition { get { return Math.Max(Start,Math.Min(End,Position+(Playing?(DateTime.UtcNow-sample).TotalSeconds*Rate:0))); } }
        public bool Available { get { return Session!=null; } }
        public event Action<bool> Updated;

        private bool IsCurrent(GlobalSystemMediaTransportControlsSession target)
        {
            return !disposed&&target!=null&&manager!=null&&target.Equals(Session)&&target.Equals(manager.GetCurrentSession());
        }
        private void ReadPlayback(GlobalSystemMediaTransportControlsSession target)
        {
            var playback=target.GetPlaybackInfo();
            double position=CurrentPosition;
            Playing=playback.PlaybackStatus==GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
            Position=position;sample=DateTime.UtcNow;
            CanPlay=playback.Controls.IsPlayPauseToggleEnabled;
            CanNext=playback.Controls.IsNextEnabled;CanPrevious=playback.Controls.IsPreviousEnabled;
            CanSeek=playback.Controls.IsPlaybackPositionEnabled;
            Rate=playback.PlaybackRate.HasValue?playback.PlaybackRate.Value:1;
        }
        public void RefreshPlayback()
        {
            if(disposed)return;
            try
            {
                if(!IsCurrent(Session))return;
                bool before=Playing;
                ReadPlayback(Session);
                // A play/pause change is not a track change and must not open the panel.
                if(before!=Playing&&Updated!=null)Updated(false);
            }
            catch(Exception ex){Error=ex.Message;}
        }

        public async Task Poll()
        {
            if(busy||disposed) return;
            busy=true;
            try
            {
                if(manager==null) manager=await WinAsync.Get(GlobalSystemMediaTransportControlsSessionManager.RequestAsync());
                if(disposed)return;
                var current=manager.GetCurrentSession();
                if(current==null)
                {
                    bool had=Session!=null; Session=null; key=""; Title=""; Artist=""; Source=""; Playing=false;
                    if(Artwork!=null){Artwork.Dispose();Artwork=null;}
                    if(had&&Updated!=null) Updated(false);
                    return;
                }
                Session=current;
                RefreshPlayback();
                var properties=await WinAsync.Get(current.TryGetMediaPropertiesAsync());
                if(!IsCurrent(current))return;
                var timeline=current.GetTimelineProperties();
                string nextKey=current.SourceAppUserModelId+"|"+properties.Title+"|"+properties.Artist+"|"+properties.AlbumTitle;
                bool changed=key!=nextKey;
                Session=current; Title=properties.Title; Artist=properties.Artist;
                Source=NiceName(current.SourceAppUserModelId);
                ReadPlayback(current);
                Start=timeline.StartTime.TotalSeconds; End=timeline.EndTime.TotalSeconds;
                Position=timeline.Position.TotalSeconds; sample=DateTime.UtcNow;
                if(Playing) Position=Math.Min(End, Position+Math.Max(0,(DateTimeOffset.UtcNow-timeline.LastUpdatedTime).TotalSeconds)*Rate);
                if(changed)
                {
                    key=nextKey;
                    Bitmap image=null;
                    if(properties.Thumbnail!=null)
                    {
                        try
                        {
                            using(var stream=await WinAsync.Get(properties.Thumbnail.OpenReadAsync()))
                            {
                                if(stream.Size>0 && stream.Size<8*1024*1024)
                                using(var reader=new DataReader(stream))
                                {
                                    await WinAsync.Get(reader.LoadAsync((uint)stream.Size));
                                    byte[] bytes=new byte[(int)stream.Size]; reader.ReadBytes(bytes);
                                    using(var ms=new MemoryStream(bytes)) using(var original=Image.FromStream(ms))
                                        image=new Bitmap(original);
                                }
                            }
                        }
                        catch { /* Artwork is optional; controls and metadata still work. */ }
                    }
                    if(!IsCurrent(current)) { if(image!=null) image.Dispose(); key=""; return; }
                    if(Artwork!=null) Artwork.Dispose();
                    Artwork=image;
                }
                Error="";
                RefreshPlayback();
                if(Updated!=null) Updated(changed);
            }
            catch(Exception ex)
            {
                Error=ex.Message; manager=null; Session=null; key=""; Playing=false;
                if(!disposed&&Updated!=null) Updated(false);
            }
            finally { busy=false; }
        }
        public async Task<bool> Command(string action,double seconds=0)
        {
            var target=Session;
            if(target==null||disposed)return false;
            bool succeeded=false;
            try
            {
              await commands.Run(async delegate {
                if(!IsCurrent(target))return;
                ReadPlayback(target);
                bool ok=false;
                if(action=="play"&&CanPlay)
                {
                    var result=await MediaCommands.Toggle(()=>IsCurrent(target),
                        ()=>target.GetPlaybackInfo().PlaybackStatus==GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing,
                        ()=>WinAsync.Get(target.TryTogglePlayPauseAsync()),RefreshPlayback);
                    if(result==PlaybackResult.SessionChanged)return;
                    if(result==PlaybackResult.TimedOut){Error="Medya uygulamasından durum onayı zamanında gelmedi.";return;}
                    ok=result==PlaybackResult.Confirmed;
                }
                else if(action=="next"&&CanNext) ok=await WinAsync.Get(target.TrySkipNextAsync());
                else if(action=="previous"&&CanPrevious) ok=await WinAsync.Get(target.TrySkipPreviousAsync());
                else if(action=="seek"&&CanSeek) ok=await WinAsync.Get(target.TryChangePlaybackPositionAsync(TimeSpan.FromSeconds(Math.Max(Start,Math.Min(End,seconds))).Ticks));
                if(!IsCurrent(target))return;
                succeeded=ok;
                Error=ok?"":"Medya uygulaması bu komutu kabul etmedi.";
                RefreshPlayback();
              });
            }
            catch(Exception ex) { Error=ex.Message; }
            await Poll();
            return succeeded;
        }
        public static string NiceName(string source)
        {
            string s=(source??"").ToLowerInvariant();
            if(s.Contains("spotify"))return "Spotify";
            if(s.Contains("chrome"))return "Google Chrome";
            if(s.Contains("msedge"))return "Microsoft Edge";
            if(s.Contains("firefox"))return "Firefox";
            if(s.Contains("music"))return "Medya Oynatıcı";
            try { return Path.GetFileNameWithoutExtension(source); } catch { return "Medya"; }
        }
        public void Dispose(){disposed=true;if(Artwork!=null){Artwork.Dispose();Artwork=null;}}
    }

    [ComImport,Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")] internal class MMEnumerator {}
    [ComImport,Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMMEnumerator
    {
        [PreserveSig] int EnumAudioEndpoints(int flow,uint mask,out IntPtr devices);
        [PreserveSig] int GetDefaultAudioEndpoint(int flow,int role,out IMMDevice device);
    }
    [ComImport,Guid("D666063F-1587-4E43-81F1-B948E807363F"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMMDevice
    {
        [PreserveSig] int Activate(ref Guid iid,uint context,IntPtr param,[MarshalAs(UnmanagedType.IUnknown)]out object obj);
        [PreserveSig] int OpenPropertyStore(int mode,out IntPtr properties);
        [PreserveSig] int GetId([MarshalAs(UnmanagedType.LPWStr)]out string id);
        [PreserveSig] int GetState(out uint state);
    }
    [ComImport,Guid("5CDF2C82-841E-4546-9722-0CF74078229A"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IEndpoint
    {
        [PreserveSig] int RegisterControlChangeNotify(IntPtr callback);
        [PreserveSig] int UnregisterControlChangeNotify(IntPtr callback);
        [PreserveSig] int GetChannelCount(out uint channels);
        [PreserveSig] int SetMasterVolumeLevel(float db,ref Guid context);
        [PreserveSig] int SetMasterVolumeLevelScalar(float value,ref Guid context);
        [PreserveSig] int GetMasterVolumeLevel(out float db);
        [PreserveSig] int GetMasterVolumeLevelScalar(out float value);
        [PreserveSig] int SetChannelVolumeLevel(uint channel,float db,ref Guid context);
        [PreserveSig] int SetChannelVolumeLevelScalar(uint channel,float value,ref Guid context);
        [PreserveSig] int GetChannelVolumeLevel(uint channel,out float db);
        [PreserveSig] int GetChannelVolumeLevelScalar(uint channel,out float value);
        [PreserveSig] int SetMute([MarshalAs(UnmanagedType.Bool)]bool mute,ref Guid context);
        [PreserveSig] int GetMute([MarshalAs(UnmanagedType.Bool)]out bool mute);
    }
    internal sealed class AudioService : IDisposable
    {
        private IMMEnumerator enumerator;
        private IEndpoint endpoint;
        private string deviceId;
        private DateTime lastDevice=DateTime.MinValue;
        public float Volume;
        public bool Muted, Available;
        public string Error="";
        private bool initialized;
        public event Action Changed;
        public void Poll()
        {
            try
            {
                if(endpoint==null && (DateTime.UtcNow-lastDevice).TotalSeconds<2) return;
                if(endpoint==null||(DateTime.UtcNow-lastDevice).TotalSeconds>2)
                {
                    lastDevice=DateTime.UtcNow;
                    if(enumerator==null) enumerator=(IMMEnumerator)new MMEnumerator();
                    IMMDevice device;
                    Marshal.ThrowExceptionForHR(enumerator.GetDefaultAudioEndpoint(0,1,out device));
                    try
                    {
                        string id; Marshal.ThrowExceptionForHR(device.GetId(out id));
                        if(id!=deviceId||endpoint==null)
                        {
                            if(endpoint!=null) Marshal.ReleaseComObject(endpoint);
                            endpoint=null;
                            Guid iid=typeof(IEndpoint).GUID; object obj;
                            Marshal.ThrowExceptionForHR(device.Activate(ref iid,23,IntPtr.Zero,out obj));
                            endpoint=(IEndpoint)obj; deviceId=id; initialized=false;
                        }
                    }
                    finally { Marshal.ReleaseComObject(device); }
                }
                float v; bool m;
                Marshal.ThrowExceptionForHR(endpoint.GetMasterVolumeLevelScalar(out v));
                Marshal.ThrowExceptionForHR(endpoint.GetMute(out m));
                bool change=Math.Abs(v-Volume)>.003||m!=Muted;
                Volume=v; Muted=m; Available=true; Error="";
                if(initialized&&change&&Changed!=null)Changed();
                initialized=true;
            }
            catch(Exception ex)
            {
                Error=ex.Message;Available=false;initialized=false;
                if(endpoint!=null){Marshal.ReleaseComObject(endpoint);endpoint=null;}
                lastDevice=DateTime.UtcNow;
            }
        }
        public void Set(float value)
        {
            if(endpoint==null)return;
            Guid id=Guid.Empty;
            try { Marshal.ThrowExceptionForHR(endpoint.SetMasterVolumeLevelScalar(Math.Max(0,Math.Min(1,value)),ref id)); Poll(); }
            catch(Exception ex){Error=ex.Message;}
        }
        public void ToggleMute()
        {
            if(endpoint==null)return;
            Guid id=Guid.Empty;
            try { Marshal.ThrowExceptionForHR(endpoint.SetMute(!Muted,ref id));Poll(); }
            catch(Exception ex){Error=ex.Message;}
        }
        public void Dispose()
        {
            if(endpoint!=null)Marshal.ReleaseComObject(endpoint);
            if(enumerator!=null)Marshal.ReleaseComObject(enumerator);
        }
    }

    internal static class Native
    {
        [DllImport("user32.dll")]internal static extern short GetKeyState(int key);
        [DllImport("user32.dll")]internal static extern short GetAsyncKeyState(int key);
        [DllImport("dwmapi.dll")]internal static extern int DwmGetColorizationColor(out uint color,out bool opaque);
        [DllImport("user32.dll",CharSet=CharSet.Unicode)]internal static extern IntPtr FindWindowEx(IntPtr parent,IntPtr after,string cls,string title);
        [DllImport("user32.dll")]internal static extern uint GetWindowThreadProcessId(IntPtr window,out uint pid);
        [DllImport("user32.dll")]internal static extern bool IsIconic(IntPtr window);
        [DllImport("user32.dll")]internal static extern bool ShowWindowAsync(IntPtr window,int command);
        [DllImport("user32.dll")]internal static extern bool IsWindow(IntPtr window);
        [DllImport("user32.dll")]internal static extern bool SetWindowPos(IntPtr window,IntPtr after,int x,int y,int width,int height,uint flags);
        [DllImport("user32.dll")]internal static extern bool SetProcessDPIAware();
    }
    // Opt-in compatibility with the original Windows 10 OSD; never touches media processes.
    internal sealed class NativeOsd : IDisposable
    {
        private Dictionary<IntPtr,uint> modified=new Dictionary<IntPtr,uint>();
        public void Tick(bool enabled)
        {
            if(!enabled){Restore();return;}
            IntPtr h=IntPtr.Zero;
            while((h=Native.FindWindowEx(IntPtr.Zero,h,"NativeHWNDHost",null))!=IntPtr.Zero)
            {
                if(Native.FindWindowEx(h,IntPtr.Zero,"DirectUIHWND",null)==IntPtr.Zero)continue;
                uint pid;Native.GetWindowThreadProcessId(h,out pid);
                try
                {
                    using(var p=Process.GetProcessById((int)pid))
                    {
                        if(p.ProcessName!="explorer"&&p.ProcessName!="ShellExperienceHost")continue;
                        if(!Native.IsIconic(h)&&Native.ShowWindowAsync(h,6))modified[h]=pid;
                    }
                }catch{}
            }
            foreach(var h2 in modified.Keys.ToArray())if(!Native.IsWindow(h2))modified.Remove(h2);
        }
        public void Restore()
        {
            foreach(var pair in modified)
            {
                uint pid;Native.GetWindowThreadProcessId(pair.Key,out pid);
                if(pid!=pair.Value||!Native.IsWindow(pair.Key))continue;
                Native.ShowWindowAsync(pair.Key,9);Native.ShowWindowAsync(pair.Key,0);
            }
            modified.Clear();
        }
        public void Dispose(){Restore();}
    }
}
