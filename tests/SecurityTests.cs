using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Diagnostics;
namespace Akis {
 internal static class SecurityTests {
  static List<string> log=new List<string>();
  static void Check(bool value,string name){if(!value)throw new Exception(name);log.Add("PASS: "+name);}
  static Preferences Read(string text){using(var s=new MemoryStream(Encoding.UTF8.GetBytes(text)))return Preferences.Read(s);}
  static void Reject(string text,string name){bool rejected=false;try{Read(text);}catch{rejected=true;}Check(rejected,name);}
  static int Main(string[] args){Directory.CreateDirectory(args[0]);try{
   var legacy=Read("<Preferences><Accent>Özel</Accent><CustomAccent>#20B878</CustomAccent><MediaPanel><Design>Modern</Design><Scale>125</Scale></MediaPanel></Preferences>");
   Check(legacy.Accent=="Özel"&&legacy.CustomAccent=="#20B878"&&legacy.MediaPanel.Design=="Modern"&&legacy.MediaPanel.Scale==125,"legacy preferences retained");
   Check(legacy.VolumePanel!=null&&legacy.LockPanel!=null,"missing panels initialized");
   Reject("<!DOCTYPE Preferences [<!ENTITY a 'test'>]><Preferences><Language>&a;</Language></Preferences>","internal DTD rejected");
   Reject("<!DOCTYPE Preferences SYSTEM 'file:///untrusted.xml'><Preferences/>","external DTD rejected");
   Reject("<!DOCTYPE Preferences [<!ENTITY a SYSTEM 'file:///untrusted.xml'>]><Preferences><Language>&a;</Language></Preferences>","external entity rejected");
   Reject("<Preferences><Language>"+new string('x',1024*1024)+"</Language></Preferences>","oversized input rejected");
   Reject("<Preferences>","malformed XML rejected");
   var normalized=Read("<Preferences><MediaPanel><Scale>99999</Scale><Opacity>-10</Opacity><Duration>500</Duration></MediaPanel></Preferences>");
   Check(normalized.MediaPanel.Scale==150&&normalized.MediaPanel.Opacity==45&&normalized.MediaPanel.Duration==15,"untrusted numeric settings normalized");
   string sensitive=String.Join("/",new[]{"private","profile","song"});
   string sanitized=Entry.SafeError(new IOException(sensitive));
   Check(!sanitized.Contains(sensitive)&&sanitized.Contains("System.IO.IOException")&&sanitized.Contains("0x"),"error details omitted, type/code retained");
   var exe=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"..","..","Flux.exe");
   var info=FileVersionInfo.GetVersionInfo(exe);
   Check(info.FileVersion=="2.1.0.0"&&info.CompanyName=="sukiqu","release version and developer metadata");
   byte[] data=File.ReadAllBytes(exe);int pe=BitConverter.ToInt32(data,0x3c),optional=pe+24;
   int flags=BitConverter.ToUInt16(data,optional+70);
   Check((flags&0x40)!=0&&(flags&0x100)!=0,"PE ASLR and DEP flags enabled");
   string binary=Encoding.UTF8.GetString(data);
   Check(binary.Contains("asInvoker")&&binary.Contains("uiAccess=\"false\""),"normal-user manifest embedded");
   Check(BrandAssets.WindowIcon!=null&&BrandAssets.TrayIcon!=null,"embedded Windows icons load");
   File.WriteAllLines(Path.Combine(args[0],"results.txt"),log);return 0;
  }catch(Exception ex){log.Add("FAIL: "+ex.GetType().Name+" "+ex.Message);File.WriteAllLines(Path.Combine(args[0],"results.txt"),log);return 1;}}
 }
}
