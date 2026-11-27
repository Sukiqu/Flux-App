using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Collections.Generic;
using System.Windows.Forms;
namespace Akis {
 internal static class NavigationCacheTests {
  static List<string> log=new List<string>();
  static IEnumerable<Control> All(Control c){yield return c;foreach(Control child in c.Controls)foreach(var x in All(child))yield return x;}
  static void Check(bool v,string s){if(!v)throw new Exception(s);log.Add("PASS: "+s);}
  static SettingsPageView View(Form f){return All(f).OfType<SettingsPageView>().Single(c=>c.Visible);}
  static void Nav(Form f,string name){All(f).OfType<NavButton>().Single(c=>c.Name==name).PerformClick();Application.DoEvents();}
  static void Call(Control c,string method,object arg){c.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c,new[]{arg});}
  [STAThread]static int Main(string[] args){Directory.CreateDirectory(args[0]);Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Native.SetProcessDPIAware();try{
   using(var app=new Controller(true))using(var f=new SettingsForm(app)){
    app.Settings.Language="Türkçe";app.Settings.Theme="Koyu";f.StartPosition=FormStartPosition.Manual;f.Location=new Point(-3000,-3000);f.ShowInTaskbar=false;f.Show();
    string[] pages={"Başlangıç","Medya paneli","Ses paneli","Kilit paneli","Uygulama"};var views=new Dictionary<string,SettingsPageView>();
    foreach(string name in pages){Nav(f,name);views[name]=View(f);}
    int delayedMoves=0;foreach(var v in views.Values)v.LocationChanged+=delegate{delayedMoves++;};
    for(int i=0;i<100;i++){string name=pages[i%5];Nav(f,name);Check(Object.ReferenceEquals(View(f),views[name]),"reuse page "+name);Check(All(f).OfType<SettingsPageView>().Count()==5,"cache bounded at five pages");}
    var until=DateTime.UtcNow.AddMilliseconds(250);while(DateTime.UtcNow<until){Application.DoEvents();Thread.Sleep(5);}Check(delayedMoves==0,"no delayed animation moves");
    var active=View(f);Nav(f,"Uygulama");Check(Object.ReferenceEquals(active,View(f)),"same tab click is a no-op");
    Nav(f,"Ses paneli");var volume=View(f);volume.ScrollControlIntoView(volume.Controls[volume.Controls.Count-1]);var position=volume.AutoScrollPosition;Nav(f,"Kilit paneli");Nav(f,"Ses paneli");Check(volume.AutoScrollPosition==position,"scroll position survives navigation");
    var toggle=All(volume).OfType<SwitchRow>().Single(c=>c.Text=="Windows'un eski panelini gizle");bool old=app.Settings.HideNative;Call(toggle,"OnClick",EventArgs.Empty);
    Check(app.Settings.HideNative!=old,"toggle writes setting");Check(All(f).OfType<SettingsPageView>().Count()==1,"change invalidates hidden pages");Nav(f,"Uygulama");Check(All(View(f)).OfType<SwitchRow>().Single(c=>c.Text=="Windows'un eski panelini gizle").Value==app.Settings.HideNative,"shared setting is fresh in rebuilt page");
    var theme=All(View(f)).OfType<ChoiceControl>().Single(c=>c.AccessibleName=="Uygulama teması");Call(theme,"OnKeyDown",new KeyEventArgs(Keys.Down));Check(app.Settings.Theme=="Açık","theme change still works");
    var language=All(View(f)).OfType<ChoiceControl>().Single(c=>c.Items.Contains("English"));Call(language,"OnKeyDown",new KeyEventArgs(Keys.Down));Application.DoEvents();Check(app.Settings.Language=="English","language change still works");
    foreach(var size in new[]{new Size(1040,700),new Size(1420,930)}){f.ClientSize=size;foreach(string name in pages){Nav(f,name);Check(All(f).OfType<NavButton>().Single(c=>c.Selected).Name==name,"selected navigation matches visible page");Check(!View(f).HorizontalScroll.Visible,"no horizontal scroll after cached resize");}}
    Nav(f,"Ses paneli");using(var b=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(b,new Rectangle(Point.Empty,b.Size));b.Save(Path.Combine(args[0],"cached-resized-light.png"));}
    app.Settings.Enabled=false;Nav(f,"Uygulama");Check(!All(View(f)).OfType<SwitchRow>().Single(c=>c.Text=="Enable indicators").Value,"cached master switch refreshes external status");
    f.PreviewPage("Medya paneli");Nav(f,"Uygulama");Check(!View(f).IsDisposed,"forced rebuild does not leave disposed cache entry");
    f.Close();Check(!f.Visible&&!f.IsDisposed,"X hides to tray");f.Dispose();Check(views.Values.All(c=>c.IsDisposed),"cached page resources disposed with form");
   }File.WriteAllLines(Path.Combine(args[0],"results.txt"),log);return 0;
  }catch(Exception ex){log.Add("FAIL: "+ex);File.WriteAllLines(Path.Combine(args[0],"results.txt"),log);return 1;}}
 }
}
