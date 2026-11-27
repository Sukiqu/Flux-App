using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Reflection;
using System.Globalization;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;
namespace Akis {
internal static class Language210 {
static List<string> log=new List<string>();
static void Check(bool ok,string s){if(!ok)throw new Exception(s);log.Add("PASS: "+s);}
static IEnumerable<Control> All(Control root){yield return root;foreach(Control c in root.Controls)foreach(var child in All(c))yield return child;}
static void EnglishText(string text){
if(string.IsNullOrEmpty(text))return;
text=text.Replace("Flux","").Replace("FLUX","").Replace("flux","");
if(Regex.IsMatch(text,@"[çğıöşüÇĞİÖŞÜ]|\b(Konum|Boyut|Yerel|etkin|paneli|sabitle|renk|sn|Sol|Orta|Uygulama|Sistem|Ekran)\b"))throw new Exception("Untranslated display text: "+text);
}
static void Inspect(Form form){foreach(var c in All(form)){
EnglishText(c.Text);EnglishText(c.AccessibleName);EnglishText(c.AccessibleDescription);
var toggle=c as SwitchRow;if(toggle!=null)EnglishText(toggle.Description);
var choice=c as ChoiceControl;if(choice!=null){EnglishText(choice.DisplayValue);foreach(var item in choice.Items)EnglishText(UiText.Get(choice.Prefs,item));}
}log.Add("PASS: visible strings and selector options are English");}
static void Shot(Form f,string path){Application.DoEvents();using(var b=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(b,new Rectangle(Point.Empty,b.Size));b.Save(path);}}
static void SelectLanguage(SettingsForm form,string raw){
form.PreviewPage("Uygulama");var picker=All(form).OfType<ChoiceControl>().Single(c=>c.Items.Contains("English"));
typeof(ChoiceControl).GetMethod("OnClick",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(picker,new object[]{EventArgs.Empty});
var menu=(ContextMenuStrip)typeof(ChoiceControl).GetField("selectionMenu",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(picker);
var entry=menu.Items.OfType<ToolStripMenuItem>().Single(c=>(string)c.Tag==raw);entry.PerformClick();menu.Close();Application.DoEvents();
}
[STAThread]static int Main(string[] args){
Directory.CreateDirectory(args[0]);Native.SetProcessDPIAware();Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
var culture=Thread.CurrentThread.CurrentUICulture;
try{
using(var app=new Controller(true)){
app.Settings.Language="Türkçe";app.Settings.Theme="Koyu";
using(var form=new SettingsForm(app))using(var tray=app.CreateTrayMenu()){
form.ShowInTaskbar=false;form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-2500,-2500);form.Show();Application.DoEvents();
SelectLanguage(form,"English");Check(app.Settings.Language=="English","language chosen from actual dropdown");
string[] pages={"Başlangıç","Medya paneli","Ses paneli","Kilit paneli","Uygulama","Sürüm Bilgisi","Yardım ve SSS"};
foreach(var page in pages){form.PreviewPage(page);form.RefreshStatus();Inspect(form);Shot(form,Path.Combine(args[0],"en-"+Array.IndexOf(pages,page)+".png"));
var navPage=(page=="Sürüm Bilgisi"||page=="Yardım ve SSS")?"Başlangıç":page;var active=All(form).Single(c=>c.Name==navPage);Check(((NavButton)active).Selected,"active navigation remains selected: "+page);}
form.ClientSize=new Size(1040,700);
foreach(var name in new[]{"Medya paneli","Ses paneli","Kilit paneli","Uygulama","Yardım ve SSS"}){
 form.PreviewPage(name);Inspect(form);
 Check(All(form).OfType<SettingsPageView>().All(v=>!v.Visible||!v.HorizontalScroll.Visible),"English page fits narrow window: "+name);
 if(name=="Medya paneli"||name=="Uygulama"||name=="Yardım ve SSS")Shot(form,Path.Combine(args[0],"narrow-"+Array.IndexOf(pages,name)+".png"));
}
form.ClientSize=new Size(1420,930);
var refresh=(System.Windows.Forms.Timer)typeof(SettingsForm).GetField("refresh",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(form);
typeof(System.Windows.Forms.Timer).GetMethod("OnTick",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(refresh,new object[]{EventArgs.Empty});Inspect(form);
app.Settings.Enabled=false;form.RefreshStatus();Check(All(form).Any(c=>c.Text=="○  Notifications disabled"),"timer/status updates stay English");app.Settings.Enabled=true;
form.PreviewPage("Uygulama");var theme=All(form).OfType<ChoiceControl>().Single(c=>c.AccessibleName=="Application theme");
typeof(ChoiceControl).GetMethod("OnKeyDown",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(theme,new object[]{new KeyEventArgs(Keys.Down)});
Check(app.Settings.Theme=="Açık"&&theme.DisplayValue=="Light","display translation does not corrupt saved theme value");
form.PreviewPage("Medya paneli");var position=All(form).OfType<ChoiceControl>().Single(c=>c.AccessibleName=="Position");
typeof(ChoiceControl).GetMethod("OnClick",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(position,new object[]{EventArgs.Empty});
var menu=(ContextMenuStrip)typeof(ChoiceControl).GetField("selectionMenu",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(position);
foreach(ToolStripMenuItem item in menu.Items)EnglishText(item.Text);
menu.Items.OfType<ToolStripMenuItem>().Single(c=>(string)c.Tag=="Sağ alt").PerformClick();menu.Close();
Check(app.Settings.MediaPanel.Position=="Sağ alt"&&position.DisplayValue=="Bottom right","translated menu stores stable position key");
app.TranslateTrayMenu(tray);foreach(ToolStripItem item in tray.Items)EnglishText(item.Text);Check(tray.Items[4].Text=="Exit","tray menu uses current language");
foreach(var panel in new[]{app.MediaWindow,app.VolumeWindow,app.LockWindow}){panel.Apply();foreach(ToolStripItem item in panel.ContextMenuStrip.Items)EnglishText(item.Text);EnglishText(panel.AccessibleName);Check(panel.ContextMenuStrip.Items[0].Text=="Flux settings","existing panel menu switches to English");}
app.Media.Error="Medya uygulaması bu komutu kabul etmedi.";EnglishText(form.ConnectionStatusText());Check(form.ConnectionStatusText().Contains("The player did not accept this command."),"connection status translates app-owned errors");
app.MediaWindow.SavePreview(Path.Combine(args[0],"media-en.png"),app.Settings.Appearance("media"));app.VolumeWindow.SavePreview(Path.Combine(args[0],"volume-en.png"),app.Settings.Appearance("volume"));app.LockWindow.SavePreview(Path.Combine(args[0],"lock-en.png"),app.Settings.Appearance("lock"));
SelectLanguage(form,"Türkçe");Check(app.Settings.Language=="Türkçe","switch back to Turkish without restart");
Check(All(form).Any(c=>c.Text=="Uygulamadan çık"),"persistent sidebar switches back to Turkish");
Check(All(form).Any(c=>c.Name=="Medya paneli"&&c.Text.Contains("Medya paneli")),"navigation switches back to Turkish");
app.TranslateTrayMenu(tray);Check(tray.Items[4].Text=="Çıkış","tray switches back to Turkish");
Check(app.LockWindow.ContextMenuStrip.Items[0].Text=="Flux ayarları","existing panel menu switches back to Turkish");
SelectLanguage(form,"English");form.RefreshStatus();Inspect(form);
app.Settings.Language="Sistem";
foreach(string name in new[]{"en-US","tr-TR","de-DE"}){
Thread.CurrentThread.CurrentUICulture=new CultureInfo(name);form.PreviewPage("Uygulama");app.Changed();Check(UiText.English(app.Settings)==(name!="tr-TR"),"system language follows "+name);if(name!="tr-TR")Inspect(form);
}
var serializer=new XmlSerializer(typeof(Preferences));using(var writer=new StringWriter()){serializer.Serialize(writer,app.Settings);using(var reader=new StringReader(writer.ToString())){var saved=(Preferences)serializer.Deserialize(reader);Check(saved.MediaPanel.Position=="Sağ alt"&&saved.Theme=="Açık"&&saved.Language=="Sistem","localized options survive XML round trip with stable keys");}}
}
}
File.WriteAllLines(Path.Combine(args[0],"results.txt"),log);return 0;
}catch(Exception ex){log.Add("FAIL: "+ex);File.WriteAllLines(Path.Combine(args[0],"results.txt"),log);return 1;}
finally{Thread.CurrentThread.CurrentUICulture=culture;}
}
}
}
