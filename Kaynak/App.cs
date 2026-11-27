using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly:System.Reflection.AssemblyVersion("2.1.0.0")]
[assembly:System.Reflection.AssemblyFileVersion("2.1.0.0")]
[assembly:System.Reflection.AssemblyTitle("Flux")]
[assembly:System.Reflection.AssemblyProduct("Flux")]
[assembly:System.Reflection.AssemblyDescription("Flux desktop flyout panels")]
[assembly:System.Reflection.AssemblyCompany("sukiqu")]
[assembly:System.Reflection.AssemblyCopyright("Copyright © 2026 sukiqu. All rights reserved.")]

namespace Akis
{
    internal sealed class KeyWatcher : IDisposable
    {
        private delegate IntPtr HookProc(int code,IntPtr message,IntPtr data);
        [DllImport("user32.dll",SetLastError=true)]private static extern IntPtr SetWindowsHookEx(int id,HookProc callback,IntPtr module,uint thread);
        [DllImport("user32.dll")]private static extern bool UnhookWindowsHookEx(IntPtr hook);
        [DllImport("user32.dll")]private static extern IntPtr CallNextHookEx(IntPtr hook,int code,IntPtr message,IntPtr data);
        [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]private static extern IntPtr GetModuleHandle(string name);
        private readonly HookProc callback;
        private IntPtr hook;
        private readonly Queue<int> pending=new Queue<int>();
        private readonly HashSet<int> down=new HashSet<int>();
        internal bool Active {get{return hook!=IntPtr.Zero;}}
        internal KeyWatcher(){callback=OnKey;hook=SetWindowsHookEx(13,callback,GetModuleHandle(null),0);}
        private IntPtr OnKey(int code,IntPtr message,IntPtr data)
        {
            if(code>=0)
            {
                int key=Marshal.ReadInt32(data),msg=message.ToInt32();
                // Only lock/media/volume virtual keys are observed. No key text is retained.
                if(key==20||key==144||key==145||(key>=173&&key<=179))
                {
                    if(msg==0x100||msg==0x104){if(down.Add(key))pending.Enqueue(key);}
                    else if(msg==0x101||msg==0x105)down.Remove(key);
                }
            }
            return CallNextHookEx(hook,code,message,data);
        }
        internal bool TryPop(out int key){if(pending.Count>0){key=pending.Dequeue();return true;}key=0;return false;}
        public void Dispose(){if(hook!=IntPtr.Zero){UnhookWindowsHookEx(hook);hook=IntPtr.Zero;}GC.KeepAlive(callback);}
    }

    internal sealed class Controller : ApplicationContext
    {
        public Preferences Settings;
        internal readonly MediaService Media=new MediaService();
        internal readonly AudioService Audio=new AudioService();
        internal readonly Flyout MediaWindow,VolumeWindow,LockWindow;
        private readonly NativeOsd native=new NativeOsd();
        private readonly System.Windows.Forms.Timer timer;
        private readonly NotifyIcon tray;
        private SettingsForm settingsWindow;
        private readonly EventWaitHandle stop,show;
        private readonly KeyWatcher keyboard;
        private bool closing,testMode,exitRequested,nativeEnabled,savePending;
        private DateTime saveDue;
        private int ticks;
        private bool lastLight;
        private int lastAccent;
        private bool caps,num,scroll;
        private ToolStripMenuItem activeItem;

        internal Controller(bool test=false)
        {
            testMode=test;
            Settings=test?new Preferences():Preferences.Load();
            Settings.Scale=Math.Max(80,Math.Min(150,Settings.Scale));
            Settings.Duration=Math.Max(1,Math.Min(15,Settings.Duration));
            Settings.Opacity=Math.Max(45,Math.Min(100,Settings.Opacity));
            Settings.EnsurePanels();
            nativeEnabled=Settings.Enabled&&Settings.HideNative;
            MediaWindow=new Flyout(this,"media");
            VolumeWindow=new Flyout(this,"volume");
            LockWindow=new Flyout(this,"lock");
            lastLight=Palette.Light(Settings);lastAccent=Palette.Accent(Settings).ToArgb();
            if(test)return;
            stop=new EventWaitHandle(false,EventResetMode.AutoReset,@"Local\AkisFlyout.Stop");
            show=new EventWaitHandle(false,EventResetMode.AutoReset,@"Local\AkisFlyout.Show");
            keyboard=new KeyWatcher();
            caps=Lock(20);num=Lock(144);scroll=Lock(145);
            var menu=CreateTrayMenu();
            tray=new NotifyIcon{Icon=BrandAssets.TrayIcon,Text="Flux • Flyout",ContextMenuStrip=menu,Visible=true};
            tray.DoubleClick+=delegate{ShowSettings();};
            Audio.Changed+=delegate {
                if(Settings.Enabled&&Settings.Volume)VolumeWindow.Reveal();

            };
            Media.Updated+=delegate(bool changed) {
                if(!Media.Available){MediaWindow.Hide();return;}
                if(Settings.Enabled&&Settings.Media&&((changed&&Settings.AutoShowMedia)||(Settings.Pinned&&!MediaWindow.Visible)))MediaWindow.Reveal();
                MediaWindow.RefreshContent();
            };
            timer=new System.Windows.Forms.Timer{Interval=100};
            timer.Tick+=Tick;timer.Start();
        }
        private static bool Lock(int key){return (Native.GetKeyState(key)&1)!=0;}
        internal ContextMenuStrip CreateTrayMenu()
        {
            var menu=new ContextMenuStrip();
            menu.Items.Add(new ToolStripMenuItem("Flux ayarları",null,delegate{ShowSettings();}){Tag="Flux ayarları"});
            menu.Items.Add(new ToolStripMenuItem("Medya kartını göster",null,delegate{MediaWindow.Reveal(true);}){Tag="Medya kartını göster"});
            activeItem=new ToolStripMenuItem("Göstergeler etkin"){Checked=Settings.Enabled,Tag="Göstergeler etkin"};
            activeItem.Click+=delegate{Settings.Enabled=!Settings.Enabled;Changed();};menu.Items.Add(activeItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(new ToolStripMenuItem("Çıkış",null,delegate{RequestExit();}){Tag="Çıkış"});
            menu.Opening+=delegate{TranslateTrayMenu(menu);};
            TranslateTrayMenu(menu);return menu;
        }
        internal void TranslateTrayMenu(ContextMenuStrip menu)
        {
            foreach(ToolStripItem item in menu.Items)if(item.Tag is string)item.Text=UiText.Get(Settings,(string)item.Tag);
        }
        private void Tick(object sender,EventArgs e)
        {
            if(closing)return;
            if(exitRequested || stop.WaitOne(0)){ExitThread();return;}
            if(show.WaitOne(0))ShowSettings();
            int key;
            while(keyboard.TryPop(out key))
            {
                if(key==20){caps=Lock(20);if(Settings.Enabled&&Settings.Caps)LockWindow.RevealLock("Caps Lock",caps);}
                if(key==144){num=Lock(144);if(Settings.Enabled&&Settings.Num)LockWindow.RevealLock("Num Lock",num);}
                if(key==145){scroll=Lock(145);if(Settings.Enabled&&Settings.Scroll)LockWindow.RevealLock("Scroll Lock",scroll);}
                if(key>=173&&key<=175&&Settings.Enabled&&Settings.Volume)VolumeWindow.Reveal();
                if(key>=176&&key<=179&&Settings.Enabled&&Settings.Media&&Media.Available)MediaWindow.Reveal();
            }
            if(!keyboard.Active)
            {
                bool c=Lock(20),n=Lock(144),s=Lock(145);
                if(c!=caps&&Settings.Enabled&&Settings.Caps)LockWindow.RevealLock("Caps Lock",c);
                if(n!=num&&Settings.Enabled&&Settings.Num)LockWindow.RevealLock("Num Lock",n);
                if(s!=scroll&&Settings.Enabled&&Settings.Scroll)LockWindow.RevealLock("Scroll Lock",s);
                caps=c;num=n;scroll=s;
            }
            ticks++;
            if(ticks%2==0){Audio.Poll();Media.RefreshPlayback();}
            if(ticks%10==0)PollMedia();
            if(ticks%10==0&&nativeEnabled)native.Tick(true);
            if(savePending&&DateTime.UtcNow>=saveDue)SaveSettingsNow();
            if(ticks%40==0)
            {
                bool light=Palette.Light(Settings);int accent=Palette.Accent(Settings).ToArgb();
                if(light!=lastLight||accent!=lastAccent)
                {
                    lastLight=light;lastAccent=accent;ApplyVisual();
                    if(settingsWindow!=null&&!settingsWindow.IsDisposed)settingsWindow.RefreshTheme();
                }
            }
        }
        private async void PollMedia(){await Media.Poll();}
        public void ShowSettings()
        {
            if(settingsWindow==null||settingsWindow.IsDisposed)settingsWindow=new SettingsForm(this);
            settingsWindow.Show();settingsWindow.WindowState=FormWindowState.Normal;settingsWindow.Activate();
        }
        internal void ApplyVisual(){MediaWindow.Apply();VolumeWindow.Apply();LockWindow.Apply();}
        internal void Changed()
        {
            if(!testMode){savePending=true;saveDue=DateTime.UtcNow.AddMilliseconds(350);}
            if(tray!=null)TranslateTrayMenu(tray.ContextMenuStrip);
            if(activeItem!=null)activeItem.Checked=Settings.Enabled;
            if(!Settings.Enabled||!Settings.Media)MediaWindow.Hide();
            if(!Settings.Enabled||!Settings.Volume)VolumeWindow.Hide();
            if(!Settings.Enabled || (LockWindow.CurrentLock=="Caps Lock"&&!Settings.Caps) || (LockWindow.CurrentLock=="Num Lock"&&!Settings.Num) || (LockWindow.CurrentLock=="Scroll Lock"&&!Settings.Scroll))LockWindow.Hide();
            bool wantedNative=Settings.Enabled&&Settings.HideNative;
            if(wantedNative!=nativeEnabled){nativeEnabled=wantedNative;native.Tick(nativeEnabled);}
            ApplyVisual();
            if(Settings.Enabled&&Settings.Media&&Settings.Pinned&&Media.Available)MediaWindow.Reveal();
        }
        internal bool StartupEnabled
        {
            get
            {
                using(var k=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
                    return k!=null&&IsStartupCommand(k.GetValue("AkisFlyout") as string,Application.ExecutablePath);
            }
        }
        // Retain the existing registration and data identity so renaming the
        // executable does not reset preferences or create duplicate startup items.
        internal static bool IsStartupCommand(string command,string executable)
        {
            string folder=Path.GetDirectoryName(executable);
            foreach(string path in new[]{executable,Path.Combine(folder,"AkisFlyout.exe"),Path.Combine(folder,"Flux.exe")})
                if(String.Equals(command,"\""+path+"\" --tray",StringComparison.OrdinalIgnoreCase))return true;
            return false;
        }
        internal void SetStartup(bool enabled)
        {
            try
            {
                using(var k=Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
                {
                    if(enabled)k.SetValue("AkisFlyout","\""+Application.ExecutablePath+"\" --tray");
                    else k.DeleteValue("AkisFlyout",false);
                }
            }
            catch(Exception ex){MessageBox.Show((UiText.English(Settings)?"Could not change the startup setting: ":"Başlangıç ayarı değiştirilemedi: ")+UiText.Get(Settings,ex.Message),"Flux");}
        }
        internal void RenderPreviews(string folder)
        {
            Directory.CreateDirectory(folder);
            Settings.Theme="Koyu";Settings.Accent="Kırmızı";
            MediaWindow.SavePreview(Path.Combine(folder,"medya-koyu.png"),Settings);
            LockWindow.SavePreview(Path.Combine(folder,"kilit-koyu.png"),Settings);
            VolumeWindow.SavePreview(Path.Combine(folder,"ses-koyu.png"),Settings);
            Settings.Theme="Açık";Settings.Accent="Mavi";
            MediaWindow.SavePreview(Path.Combine(folder,"medya-acik.png"),Settings);
            Settings.Theme="Koyu";Settings.Accent="Sistem";
            foreach(var panel in new[]{MediaWindow,VolumeWindow,LockWindow})
            {
                Settings.Panel(panel.Kind).Design="Modern";
                panel.SavePreview(Path.Combine(folder,panel.Kind+"-modern.png"),Settings);
                Settings.Panel(panel.Kind).Design="Plak";
                panel.SavePreview(Path.Combine(folder,panel.Kind+"-vinyl.png"),Settings);
                Settings.Panel(panel.Kind).Design="Classic";
            }
            using(var form=new SettingsForm(this))
            {
                form.ShowInTaskbar=false;form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-2000,-2000);
                form.Show();Application.DoEvents();
                using(var bmp=new Bitmap(form.Width,form.Height))
                {form.DrawToBitmap(bmp,new Rectangle(Point.Empty,bmp.Size));bmp.Save(Path.Combine(folder,"ayarlar.png"));}
                string[] pages={"Medya paneli","Ses paneli","Kilit paneli","Sürüm Bilgisi","Yardım ve SSS"};
                string[] files={"medya-ayarlari","ses-ayarlari","kilit-ayarlari","surum-bilgisi","yardim-sss"};
                for(int i=0;i<pages.Length;i++)
                {
                    form.PreviewPage(pages[i]);Application.DoEvents();
                    using(var bmp=new Bitmap(form.Width,form.Height))
                    {form.DrawToBitmap(bmp,new Rectangle(Point.Empty,bmp.Size));bmp.Save(Path.Combine(folder,files[i]+".png"));}
                }
                form.PreviewPage("Uygulama");Application.DoEvents();
                using(var bmp=new Bitmap(form.Width,form.Height))
                {form.DrawToBitmap(bmp,new Rectangle(Point.Empty,bmp.Size));bmp.Save(Path.Combine(folder,"uygulama-koyu.png"));}
                Settings.Theme="Açık";form.PreviewPage("Uygulama");Application.DoEvents();
                using(var bmp=new Bitmap(form.Width,form.Height))
                {form.DrawToBitmap(bmp,new Rectangle(Point.Empty,bmp.Size));bmp.Save(Path.Combine(folder,"uygulama-acik.png"));}
            }
        }
        private void SaveSettingsNow()
        {
            if(!savePending||testMode)return;
            try { Settings.Save(); savePending=false; }
            catch(Exception ex){savePending=false;MessageBox.Show((UiText.English(Settings)?"Could not save settings: ":"Ayarlar kaydedilemedi: ")+UiText.Get(Settings,ex.Message),"Flux");}
        }        internal void RequestExit(){exitRequested=true;}
        protected override void ExitThreadCore(){Dispose();base.ExitThreadCore();}
        protected override void Dispose(bool disposing)
        {
            if(!closing)
            {
                closing=true;
                if(timer!=null)timer.Dispose();
                if(keyboard!=null)keyboard.Dispose();
                native.Dispose();
                if(tray!=null){var menu=tray.ContextMenuStrip;tray.ContextMenuStrip=null;tray.Visible=false;tray.Dispose();if(menu!=null)menu.Dispose();}
                if(settingsWindow!=null)settingsWindow.Dispose();
                Media.Dispose();Audio.Dispose();
                MediaWindow.Dispose();VolumeWindow.Dispose();LockWindow.Dispose();
                if(stop!=null)stop.Dispose();if(show!=null)show.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    internal static class Entry
    {
        [STAThread]
        private static int Main(string[] args)
        {
            Native.SetProcessDPIAware();
            Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
            if(args.Length==1&&args[0]=="--stop"){Signal("Stop");return 0;}
            if(args.Length==2&&args[0]=="--diagnose")
            {
                try{Diagnose(args[1]).GetAwaiter().GetResult();return 0;}
                catch(Exception ex){File.WriteAllText(args[1],SafeError(ex));return 1;}
            }
            if(args.Length==2&&args[0]=="--render-previews")
            {
                using(var c=new Controller(true))c.RenderPreviews(args[1]);
                return 0;
            }
            bool first;
            using(var mutex=new Mutex(true,@"Local\AkisFlyout.Instance",out first))
            {
                if(!first){Signal("Show");return 0;}
                try
                {
                    using(var c=new Controller())
                    {
                        if(args.Length==0||args[0]!="--tray")c.ShowSettings();
                        Application.Run(c);
                    }
                }
                catch(Exception ex)
                {
                    try{Directory.CreateDirectory(Preferences.Folder);File.WriteAllText(Path.Combine(Preferences.Folder,"last-error.txt"),DateTime.UtcNow.ToString("s")+"Z\r\n"+SafeError(ex));}catch{}
                    var preferences=Preferences.Load();
                    MessageBox.Show((UiText.English(preferences)?"Flux encountered an error:\n":"Flux bir hatayla karşılaştı:\n")+UiText.Get(preferences,ex.Message),"Flux",MessageBoxButtons.OK,MessageBoxIcon.Error);return 1;
                }
                finally{mutex.ReleaseMutex();}
            }
            return 0;
        }
        private static void Signal(string kind)
        {try{using(var e=EventWaitHandle.OpenExisting(@"Local\AkisFlyout."+kind))e.Set();}catch(WaitHandleCannotBeOpenedException){}}
        internal static string SafeError(Exception error)
        {return error.GetType().FullName+" (0x"+error.HResult.ToString("X8")+")";}
        private static async Task Diagnose(string file)
        {
            using(var m=new MediaService())using(var a=new AudioService())
            {
                await m.Poll();a.Poll();
                File.WriteAllText(file,"Flux 2.1.0 diagnostics\r\nMedia available: "+m.Available+
                    "\r\nArtwork available: "+(m.Artwork!=null)+"\r\nSeek supported: "+m.CanSeek+
                    "\r\nPlayPause supported: "+m.CanPlay+"\r\nMedia error present: "+(!String.IsNullOrEmpty(m.Error))+
                    "\r\nAudio available: "+a.Available+"\r\nAudio error present: "+(!String.IsNullOrEmpty(a.Error)));
                if(!String.IsNullOrEmpty(m.Error)||!a.Available)throw new InvalidOperationException("API diagnostic failed.");
            }
        }
    }
}
