using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml.Serialization;
namespace Akis {
 internal static class BrandingTests {
  static List<string> log=new List<string>();
  static void Check(bool ok,string message){if(!ok)throw new Exception(message);log.Add("PASS: "+message);}
  static IEnumerable<Control> All(Control c){yield return c;foreach(Control ch in c.Controls)foreach(var x in All(ch))yield return x;}
  static bool OldName(string s){return s!=null&&(s.Contains("Akış")||s.Contains("AKIŞ")||s.Contains("akış"));}
  [STAThread]static int Main(string[] args){Directory.CreateDirectory(args[0]);Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Native.SetProcessDPIAware();try{
   using(var app=new Controller(true))using(var form=new SettingsForm(app)){
    form.ShowInTaskbar=false;form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-3000,-3000);form.ClientSize=new Size(1420,930);form.Show();
    foreach(string language in new[]{"Türkçe","English"})foreach(string page in new[]{"Başlangıç","Medya paneli","Ses paneli","Kilit paneli","Uygulama"}){
     app.Settings.Language=language;app.Settings.Theme="Koyu";app.Settings.Accent="Mavi";form.PreviewPage(page);
     Check(form.Text.StartsWith("Flux —"),"window title "+language+page);
     Check(All(form).All(c=>!OldName(c.Text)&&!OldName(c.AccessibleName)&&!OldName(c.AccessibleDescription)),"visible and accessibility names "+language+page);
     if(page=="Başlangıç"||page=="Uygulama")using(var b=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(b,new Rectangle(Point.Empty,b.Size));b.Save(Path.Combine(args[0],page+"-"+language+".png"));}
    }
    foreach(string language in new[]{"Türkçe","English"}){
     app.Settings.Language=language;using(var menu=app.CreateTrayMenu())Check(menu.Items[0].Text.StartsWith("Flux "),"tray menu "+language);
     Check(UiText.Get(app.Settings,"Flux ayarları")== (language=="English"?"Flux settings":"Flux ayarları"),"settings translation "+language);
    }
    Check(UiText.Get(new Preferences{Language="English"},"Flux 2.1.0 • Veriler bu bilgisayarda saklanır.\nAyarlar: settings.xml").Contains("Data is stored"),"English footer translation");
    Check(Preferences.Folder.EndsWith("\\AkisFlyout"),"existing settings storage retained");
    var serializer=new XmlSerializer(typeof(Preferences));using(var sr=new StringReader("<Preferences><Accent>Özel</Accent><CustomAccent>#20B878</CustomAccent><MediaPanel><Design>Modern</Design><Scale>125</Scale><Accent>Mor</Accent></MediaPanel></Preferences>")){
     var old=(Preferences)serializer.Deserialize(sr);old.EnsurePanels();Check(old.Accent=="Özel"&&old.CustomAccent=="#20B878"&&old.MediaPanel.Design=="Modern"&&old.MediaPanel.Scale==125&&old.MediaPanel.Accent=="Mor","legacy XML preferences preserved");
    }
    const string folder="C:\\Flux test\\";
    Check(Controller.IsStartupCommand("\""+folder+"AkisFlyout.exe\" --tray",folder+"Flux.exe"),"legacy startup works with renamed executable");
    Check(Controller.IsStartupCommand("\""+folder+"Flux.exe\" --tray",folder+"AkisFlyout.exe"),"compatibility launcher recognizes new startup");
    Check(!Controller.IsStartupCommand("\"C:\\Other\\Flux.exe\" --tray",folder+"Flux.exe"),"unrelated startup command excluded");
   }
   File.WriteAllLines(Path.Combine(args[0],"results.txt"),log);return 0;
  }catch(Exception ex){log.Add("FAIL: "+ex);File.WriteAllLines(Path.Combine(args[0],"results.txt"),log);return 1;}}
 }
}
