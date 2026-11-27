using System;
using System.Drawing;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace Akis
{
    internal sealed class SwitchRow : Control
    {
        private readonly HoverTracker highlight;
        internal bool Value;
        internal string Description;
        internal Preferences Prefs;
        internal string Glyph;
        internal event Action<bool> Changed;
        public SwitchRow(string title,string description,bool value,Preferences prefs)
        {
            Text=title;Description=description;Value=value;Prefs=prefs;Glyph=SettingsDesign.Glyph(title);
            Height=78;Width=620;DoubleBuffered=true;ResizeRedraw=true;TabStop=true;Cursor=Cursors.Hand;
            SetStyle(ControlStyles.SupportsTransparentBackColor,true);BackColor=Color.Transparent;
            AccessibleName=title;AccessibleRole=AccessibleRole.CheckButton;
            SetStyle(ControlStyles.Selectable,true);
            highlight=new HoverTracker(this);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g=e.Graphics;g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            ShellSkin.Surface(g,new RectangleF(1,1,Width-2,Height-2),ShellSkin.Card(Prefs),Palette.Light(Prefs)?ShellSkin.Tint(Prefs,Color.FromArgb(235,242,255)):ShellSkin.Tint(Prefs,Color.FromArgb(14,29,60)),13,ShellSkin.Line(Prefs));
            SettingsDesign.Orb(g,Glyph,new RectangleF(16,19,39,39),Prefs);
            Draw.Text(g,Text,15,Palette.Text(Prefs),new RectangleF(69,12,Width-149,25),true);
            Draw.Text(g,Description,12,ShellSkin.Muted(Prefs),new RectangleF(69,38,Width-150,26));
            Draw.Box(g,Value?ShellSkin.Ink(Prefs):ShellSkin.Line(Prefs),new RectangleF(Width-68,25,49,28),14);
            UiSymbols.Circle(g,new RectangleF(Width-68+(Value?24:3),28,22,22),Color.White,Color.White);
            if(highlight.Active)ShellSkin.HoverGlow(g,new RectangleF(1,1,Width-2,Height-2),13,Prefs);
        }
        protected override void OnClick(EventArgs e){Value=!Value;if(Changed!=null)Changed(Value);Invalidate();base.OnClick(e);}
        protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Space){OnClick(EventArgs.Empty);e.Handled=true;}base.OnKeyDown(e);}
    }


    internal sealed class ChoiceControl : Control
    {
        private readonly HoverTracker highlight;
        internal string[] Items;
        internal string Value;
        internal Preferences Prefs;
        internal event Action<string> Changed;
        private ContextMenuStrip selectionMenu;
        internal string DisplayValue {get{return UiText.Get(Prefs,Value);}}
        public ChoiceControl(string[] items,string value,Preferences prefs)
        {Items=items;Value=Array.IndexOf(items,value)>=0?value:items[0];Prefs=prefs;DoubleBuffered=true;ResizeRedraw=true;SetStyle(ControlStyles.SupportsTransparentBackColor,true);BackColor=Color.Transparent;TabStop=true;Cursor=Cursors.Hand;AccessibleRole=AccessibleRole.ComboBox;highlight=new HoverTracker(this);}
        protected override void OnPaint(PaintEventArgs e)
        {
            var g=e.Graphics;g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            ShellSkin.Surface(g,new RectangleF(1,1,Width-2,Height-2),ShellSkin.Bg(Prefs),ShellSkin.Bg(Prefs),8,ShellSkin.Line(Prefs));
            Draw.Text(g,DisplayValue,13,Palette.Text(Prefs),new RectangleF(12,0,Width-40,Height));
            UiSymbols.Font(g,"\uE70D",10,ShellSkin.Muted(Prefs),new RectangleF(Width-28,0,22,Height));
            if(highlight.Active)ShellSkin.HoverGlow(g,new RectangleF(1,1,Width-2,Height-2),8,Prefs);
        }
        protected override void OnClick(EventArgs e)
        {
            if(IsDisposed || Disposing) return;
            // A dropdown belongs to its selector, not to the Closed event.
            // WinForms still uses the dropdown after raising Closed.
            if(selectionMenu==null)
            {
                selectionMenu=new ContextMenuStrip{ShowImageMargin=false,Font=Font};
                foreach(string item in Items)
                {
                    string chosen=item;
                    var entry=new ToolStripMenuItem(UiText.Get(Prefs,item)){Width=Width,Tag=item};
                    entry.Click+=delegate {
                        if(IsDisposed || Disposing) return;
                        Value=chosen;Invalidate();
                        if(Changed!=null)Changed(chosen);
                    };
                    selectionMenu.Items.Add(entry);
                }
            }
            selectionMenu.BackColor=ShellSkin.Card(Prefs);
            selectionMenu.ForeColor=Palette.Text(Prefs);
            foreach(ToolStripMenuItem entry in selectionMenu.Items){entry.Text=UiText.Get(Prefs,(string)entry.Tag);entry.Checked=(string)entry.Tag==Value;}
            selectionMenu.Show(this,new Point(0,Height));
            base.OnClick(e);
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if(e.KeyCode==Keys.Space||e.KeyCode==Keys.Enter){OnClick(EventArgs.Empty);e.Handled=true;}
            else if(e.KeyCode==Keys.Up||e.KeyCode==Keys.Down)
            {
                int i=Array.IndexOf(Items,Value)+(e.KeyCode==Keys.Down?1:-1);
                i=Math.Max(0,Math.Min(Items.Length-1,i));Value=Items[i];Invalidate();if(Changed!=null)Changed(Value);e.Handled=true;
            }
            base.OnKeyDown(e);
        }
        protected override void Dispose(bool disposing)
        {
            if(disposing && selectionMenu!=null)
            {
                var menu=selectionMenu;
                selectionMenu=null;
                menu.Close();
                menu.Dispose();
            }
            base.Dispose(disposing);
        }
    }


    internal sealed class SliderControl : Control
    {
        private readonly HoverTracker highlight;
        internal int Minimum,Maximum;
        private int value;
        internal Preferences Prefs;
        internal event EventHandler ValueChanged;
        internal int Value {get{return value;}set{int v=Math.Max(Minimum,Math.Min(Maximum,value));if(this.value==v)return;this.value=v;Invalidate();if(ValueChanged!=null)ValueChanged(this,EventArgs.Empty);}}
        internal SliderControl(int min,int max,int initial,Preferences prefs)
        {Minimum=min;Maximum=max;value=Math.Max(min,Math.Min(max,initial));Prefs=prefs;DoubleBuffered=true;ResizeRedraw=true;SetStyle(ControlStyles.SupportsTransparentBackColor,true);BackColor=Color.Transparent;TabStop=true;Cursor=Cursors.Hand;AccessibleRole=AccessibleRole.Slider;highlight=new HoverTracker(this);}
        protected override void OnPaint(PaintEventArgs e)
        {
            var g=e.Graphics;g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            float x=10+(Width-20)*(value-Minimum)/(float)(Maximum-Minimum),y=Height/2f;
            Draw.Box(g,ShellSkin.Line(Prefs),new RectangleF(10,y-2,Width-20,4),2);
            if(x>11)Draw.Box(g,ShellSkin.Ink(Prefs),new RectangleF(10,y-3,Math.Max(4,x-10),6),3);
            Color halo=Color.FromArgb(50,ShellSkin.Ink(Prefs));
            UiSymbols.Circle(g,new RectangleF(x-12,y-12,24,24),halo,halo);
            UiSymbols.Circle(g,new RectangleF(x-8,y-8,16,16),ShellSkin.Ink(Prefs),ShellSkin.Tint(Prefs,Color.FromArgb(54,193,255)));
            if(highlight.Active)ShellSkin.HoverGlow(g,new RectangleF(1,1,Width-2,Height-2),8,Prefs);
        }
        private void At(int x){Value=Minimum+(int)Math.Round(Math.Max(0,Math.Min(1,(x-10)/(double)(Width-20)))*(Maximum-Minimum));}
        protected override void OnMouseDown(MouseEventArgs e){if(e.Button==MouseButtons.Left){Focus();Capture=true;At(e.X);}base.OnMouseDown(e);}
        protected override void OnMouseMove(MouseEventArgs e){if(Capture)At(e.X);base.OnMouseMove(e);}
        protected override void OnMouseUp(MouseEventArgs e){Capture=false;base.OnMouseUp(e);}
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if(e.KeyCode==Keys.Left||e.KeyCode==Keys.Down)Value--;
            else if(e.KeyCode==Keys.Right||e.KeyCode==Keys.Up)Value++;
            else if(e.KeyCode==Keys.Home)Value=Minimum;
            else if(e.KeyCode==Keys.End)Value=Maximum;
            else {base.OnKeyDown(e);return;}
            e.Handled=true;
        }
    }

    internal sealed class SettingsForm : Form
    {
        private readonly Controller app;
        private readonly Panel sidebar,main;
        private FlowLayoutPanel content;
        private readonly Label heading,subheading,status;
        private string page="Başlangıç";
        private readonly Timer refresh;
        private sealed class CachedPage {internal FlowLayoutPanel View;internal string Subtitle;internal SwitchRow Master;}
        private readonly Dictionary<string,CachedPage> pageCache=new Dictionary<string,CachedPage>();
        private readonly ToolTip tips=new ToolTip();
        private SwitchRow masterRow;
        private SettingsGroup activeGroup;
        private sealed class OriginalText {internal string Text,Description,AccessibleName;}
        private readonly ConditionalWeakTable<Control,OriginalText> originals=new ConditionalWeakTable<Control,OriginalText>();
        private Preferences P { get { return app.Settings; } }
        public SettingsForm(Controller controller)
        {
            app=controller;
            Text="Flux — Flyout ayarları";FormBorderStyle=FormBorderStyle.Sizable;StartPosition=FormStartPosition.CenterScreen;
            var work=Screen.PrimaryScreen.WorkingArea;
            ClientSize=new Size(Math.Min(1420,work.Width-50),Math.Min(930,work.Height-65));MinimumSize=new Size(1040,700);
            Font=new Font("Segoe UI",10);AutoScaleMode=AutoScaleMode.Dpi;
            Icon=BrandAssets.WindowIcon;
            sidebar=new ShellPanel{Prefs=P,Sidebar=true,Dock=DockStyle.Left,Width=236,Padding=new Padding(18)};
            Controls.Add(sidebar);
            var brand=new BrandMark{Prefs=P,Name="brand",Location=new Point(22,25),Size=new Size(211,111),AccessibleName="Flux"};
            sidebar.Controls.Add(brand);
            string[] pages={"Başlangıç","Medya paneli","Ses paneli","Kilit paneli","Uygulama"};
            string[] glyphs={"\uE80F","\uE768","\uE767","\uE72E","\uE713"};
            for(int i=0;i<pages.Length;i++)
            {
                string name=pages[i];
                var button=new NavButton{Prefs=P,Glyph=glyphs[i],Text=Translate(name),Size=new Size(210,58),Tag="nav",Name=name,AccessibleName=Translate(name),Location=new Point(13,157+i*66)};
                button.Click+=delegate{NavigatePage(name);};
                sidebar.Controls.Add(button);
            }
            status=new Label{Name="status",Text="●  Bildirimler etkin",Location=new Point(27,sidebar.Height-172),Size=new Size(194,40),Anchor=AnchorStyles.Left|AnchorStyles.Bottom,Font=new Font("Segoe UI",9)};
            sidebar.Controls.Add(status);
            var version=new Label{Name="version",Text="FLUX 2.1.0\nWindows 10 • Yerel uygulama",Location=new Point(27,sidebar.Height-131),Size=new Size(195,45),Anchor=AnchorStyles.Left|AnchorStyles.Bottom,Font=new Font("Segoe UI",9),Tag="muted"};
            sidebar.Controls.Add(version);
            var quit=new NavButton{Prefs=P,Glyph="\uE7E8",Text="Uygulamadan çık",Name="exit",Tag="exit",Location=new Point(13,sidebar.Height-74),Size=new Size(210,58),Anchor=AnchorStyles.Left|AnchorStyles.Bottom};
            quit.Click+=delegate{app.RequestExit();};sidebar.Controls.Add(quit);
            sidebar.Resize+=delegate{LayoutSidebar();};
            LayoutSidebar();

            main=new ShellPanel{Prefs=P,Dock=DockStyle.Fill,Padding=new Padding(24,0,24,0)};
            Controls.Add(main);main.BringToFront();
            heading=new Label{Text=page,Location=new Point(32,24),Size=new Size(850,58),Font=new Font("Segoe UI",29,FontStyle.Bold)};
            subheading=new Label{Location=new Point(34,82),Size=new Size(790,41),Font=new Font("Segoe UI",10),Tag="muted"};
            main.Controls.Add(heading);main.Controls.Add(subheading);
            content=CreatePageView();
            main.Controls.Add(content);
            main.Resize+=delegate{LayoutPage();};
            FormClosing+=delegate(object sender,FormClosingEventArgs e){if(e.CloseReason==CloseReason.UserClosing){e.Cancel=true;Hide();}};
            refresh=new Timer{Interval=1500};
            refresh.Tick+=delegate {RefreshStatus();};
            refresh.Start();
            BuildPage();
        }
        private FlowLayoutPanel CreatePageView()
        {
            return new SettingsPageView{Visible=false,Location=new Point(24,132),Size=new Size(Math.Max(100,main.Width-48),Math.Max(100,main.Height-152)),Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,BackColor=Color.Transparent};
        }
        private void DiscardInactivePages()
        {
            var keys=new List<string>();
            foreach(var pair in pageCache)if(pair.Value.View!=content){pair.Value.View.Dispose();keys.Add(pair.Key);}
            foreach(string key in keys)pageCache.Remove(key);
        }
        private void NavigatePage(string name)
        {
            if(name==page)return;
            page=name;BuildPage(true);
        }
        private void SelectNavigation()
        {
            foreach(Control c in sidebar.Controls){var nav=c as NavButton;if(nav==null)continue;bool selected=nav.Name==NavigationParent;if(nav.Selected!=selected){nav.Selected=selected;nav.Invalidate();}}
        }
        private string NavigationParent {get{return page=="Sürüm Bilgisi"||page=="Yardım ve SSS"?"Başlangıç":page;}}
        private Button Button(string title,int width=185,int height=36)
        {
            return new SettingsAction(P,title){Size=new Size(width,Math.Max(48,height)),Font=new Font("Segoe UI",10),Tag="button"};
        }
        private string NavLabel(string name)
        {
            return name=="Başlangıç"?L("Başlangıç","Home"):Translate(name);
        }
        private void Add(Control c)
        {
            c.Margin=new Padding(0,0,0,10);
            if(activeGroup!=null){activeGroup.Controls.Add(c);return;}
            if(c is Panel && !(c is SettingsCard) && (string)c.Tag=="card")
            {
                EventHandler shape=delegate {using(var path=Draw.Round(new RectangleF(0,0,c.Width,c.Height),12)){var old=c.Region;c.Region=new Region(path);if(old!=null)old.Dispose();}};
                c.Resize+=shape;shape(c,EventArgs.Empty);
            }
            content.Controls.Add(c);
        }
        private void Hero(string title,string subtitle)
        {
            var card=new Panel{Size=new Size(620,106),Tag="card",Padding=new Padding(18,14,18,10)};
            card.Paint+=delegate(object sender,PaintEventArgs e)
            {
                Color accent=Palette.Accent(P), baseColor=Palette.Card(P);
                using(var brush=new LinearGradientBrush(card.ClientRectangle,Color.FromArgb(95,accent),baseColor,25f))
                    e.Graphics.FillRectangle(brush,card.ClientRectangle);
                using(var pen=new Pen(Color.FromArgb(125,accent),1f))e.Graphics.DrawRectangle(pen,0,0,card.Width-1,card.Height-1);
            };
            card.Controls.Add(new Label{Text=title,Location=new Point(18,14),Size=new Size(560,32),Font=new Font("Segoe UI",16,FontStyle.Bold),Tag="hero"});
            card.Controls.Add(new Label{Text=subtitle,Location=new Point(19,52),Size=new Size(560,38),Font=new Font("Segoe UI",9),Tag="heroMuted"});
            Add(card);
        }
        private void StatusCards()
        {
            var row=new FlowLayoutPanel{Size=new Size(620,78),WrapContents=false,Margin=new Padding(0,0,0,10),BackColor=Color.Transparent};
            string[] names={"MEDYA","SES","KİLİT"}; bool[] values={P.Media,P.Volume,P.Caps||P.Num||P.Scroll};
            for(int i=0;i<3;i++)
            {
                int index=i;var card=new Panel{Size=new Size(194,68),Margin=new Padding(0,0,12,0),Tag="card"};
                card.Paint+=delegate(object sender,PaintEventArgs e){using(var pen=new Pen(Color.FromArgb(85,Palette.Accent(P)),1f))e.Graphics.DrawRectangle(pen,0,0,card.Width-1,card.Height-1);};
                card.Controls.Add(new Label{Text=names[index],Location=new Point(14,10),Size=new Size(150,22),Font=new Font("Segoe UI",8,FontStyle.Bold),Tag="muted"});
                card.Controls.Add(new Label{Text=values[index]?"● Hazır":"○ Kapalı",Location=new Point(14,34),Size=new Size(160,24),Font=new Font("Segoe UI",10,FontStyle.Bold),ForeColor=values[index]?Palette.Accent(P):Palette.Muted(P)});
                row.Controls.Add(card);
            }
            Add(row);
        }
        private string L(string tr,string en){return English?en:tr;}
        private void HomePage()
        {
            var dashboard=new HomeDashboard(app,NavigatePage,Changed);
            Add(dashboard);
        }
        private void InformationSection(string title,string body,string glyph)
        {
            Section(title);activeGroup.Glyph=glyph;
            Add(new SettingsParagraph(body){Width=620});
        }
        private void InformationPage(bool release)
        {
            subheading.Text=release?L("Yüklü sürüm ve Flux'taki yenilikler.","Your installed version and what's new in Flux."):
                L("Flux kullanımı hakkında sık sorulan sorular.","Answers to common questions about using Flux.");
            var back=(SettingsAction)Button(L("Başlangıca dön","Back to Home"),620,48);
            back.Name="backHome";back.Glyph="\uE72B";back.Click+=delegate{NavigatePage("Başlangıç");};Add(back);
            if(release)
            {
                InformationSection("Flux 2.1.0",L("Yüklü sürüm: 2.1.0\nGeliştirici: sukiqu\nWindows 10 • Yerel uygulama","Installed version: 2.1.0\nDeveloper: sukiqu\nWindows 10 • Local application"),"\uE946");
                InformationSection(L("Bu sürümde neler var?","What's new?"),L(
                    "• Uygulamanın yeni adı Flux. Mevcut ayarlar ve eski kısayollar korunur.\n• Medya, ses ve kilit panellerinde Classic ve Modern tasarımlar.\n• Yeni kart düzeni ve değişiklikleri anında gösteren önizlemeler.\n• Arayüzün tamamına uygulanan sistem, hazır ve özel vurgu renkleri.\n• Yumuşatılmış simgeler, yuvarlak kenarlar ve iyileştirilmiş sekme geçişleri.\n• Sürüm bilgisi ve yardım sayfaları artık uygulamanın içinde açılır.",
                    "• The app is now called Flux. Existing settings and shortcuts are preserved.\n• Classic and Modern designs for media, volume and lock panels.\n• A new card layout with previews that reflect changes instantly.\n• System, preset and custom accent colors throughout the interface.\n• Smoother icons, rounded edges and improved tab navigation.\n• Version information and help now open inside the application."),"\uE734");
                InformationSection(L("2.1.0 yenilikleri","New in 2.1.0"),L(
                    "Yeni Flux logosu ve Windows simgeleri. Ayar dosyaları daha güvenli okunur. Tanılama dosyaları şarkı ve sanatçı adlarını içermez. İndirilen yayın paketleri SHA-256 ile doğrulanabilir.",
                    "New Flux logo and Windows icons. Safer settings loading. Diagnostic files no longer include song titles or artist names. Downloaded release packages can be verified with SHA-256."),"\uE946");
                InformationSection(L("Sürüm hakkında","About this version"),L(
                    "Bu bilgiler yüklü sürüme aittir. Çevrimiçi güncelleme denetimi yapılmaz. Geliştirmeler, yeni bir sürüm duyurulana kadar 2.1.0 altında devam eder.",
                    "These notes describe the installed version. No online update check is performed. Improvements remain on 2.1.0 until a new version is announced."),"\uE946");
            }
            else
            {
                InformationSection(L("Ayarlar nasıl kaydedilir?","How are settings saved?"),L("Değişiklikler anında uygulanır ve otomatik kaydedilir. Uygula düğmesine basmanız gerekmez.","Changes apply immediately and save automatically. There is no need to press an Apply button."),"\uE74E");
                InformationSection(L("Classic ve Modern neyi değiştirir?","What do Classic and Modern change?"),L("Başlangıçtaki tema kartları üç panelin tasarımını değiştirir. Konum, boyut ve renk tercihleriniz korunur. Her panelin tasarımını kendi sekmesinden ayrı seçebilirsiniz.","The theme cards on Home change the design of all three panels. Your position, size and color preferences are preserved. You can also select each panel's design independently in its tab."),"\uE790");
                InformationSection(L("Uygulamanın rengini nasıl değiştiririm?","How do I change the app's color?"),L("Uygulama > Görünüm > Uygulama vurgu rengi yolunu kullanın. İstediğiniz renk için Uygulama için özel renk seç düğmesine basın. İlk kurulumda Windows sistem rengi kullanılır.","Go to Application > Appearance > Application accent color. Select Choose a custom application color to pick your own color. A new installation uses the Windows system accent color."),"\uE790");
                InformationSection(L("Saydamlık nasıl çalışır?","How does opacity work?"),L("Düşük değer panel arka planını daha saydam yapar. Yazılar, simgeler ve kapak daha az solar. Classic %100'de opaktır; Modern tasarımı %100'de de yarı saydam kalır.","Lower values make the panel background more transparent. Text, icons and artwork fade less. Classic is opaque at 100%; Modern remains translucent even at 100%."),"opacity");
                InformationSection(L("X düğmesi uygulamayı kapatır mı?","Does the X button quit the app?"),L("X, uygulamayı sistem tepsisine küçültür. Tamamen kapatmak için Uygulamadan çık seçeneğini veya tepsi menüsündeki Çıkış'ı kullanın.","X hides the app to the system tray. To quit completely, use Exit application or Exit in the tray menu."),"power");
                InformationSection(L("Medya paneli neden kendiliğinden açılıyor?","Why does the media panel appear automatically?"),L("Medya paneli sekmesindeki Parça değişince göster ve Medya panelini sabitle seçeneklerini kontrol edin. Paneli elle açmak için Başlangıç sayfasındaki Paneli ekranda göster düğmesini kullanabilirsiniz.","Check Show on track change and Pin media panel in the Media panel tab. To open it manually, select Show panel on desktop on the Home page."),"\uE768");
            }
        }
        private void Section(string title)
        {
            Group(title,title=="Yerleşim"?"placement":"stack");
        }
        private void Group(string title,string mode)
        {
            activeGroup=null;
            var group=new SettingsGroup(P,Translate(title),mode){DeferArrange=true,Width=620,Margin=new Padding(0,0,0,14)};
            Add(group);activeGroup=group;
        }
        private void Toggle(string title,string desc,bool value,Action<bool> action)
        {
            var row=new SwitchRow(title,desc,value,P);
            if(title=="Göstergeleri etkinleştir")masterRow=row;
            row.Changed+=delegate(bool v){action(v);Changed();};Add(row);
        }
        private void Choice(string title,string[] choices,string selected,Action<string> action)
        {
            var panel=new SettingsCard(P){Size=new Size(620,58),Tag="card",Glyph=SettingsDesign.Glyph(title)};
            panel.Controls.Add(new Label{Text=title,Location=new Point(17,18),Size=new Size(252,29)});
            var combo=new ChoiceControl(choices,selected,P){Location=new Point(301,13),Size=new Size(298,35),AccessibleName=title};
            combo.Changed+=delegate(string value){action(value);Changed();};
            panel.Controls.Add(combo);Add(panel);
        }
        private void Range(string title,int value,int min,int max,string unit,Action<int> action)
        {
            if(unit==" sn")unit=L(" sn"," s");
            var panel=new SettingsCard(P){Size=new Size(620,58),Tag="card",Glyph=SettingsDesign.Glyph(title)};
            var text=new Label{Text=title,Location=new Point(17,17),Size=new Size(205,32)};
            var amount=new Label{Text=value+unit,Location=new Point(532,17),Size=new Size(76,30),TextAlign=ContentAlignment.MiddleRight};
            var slider=new SliderControl(min,max,value,P){Location=new Point(230,12),Size=new Size(293,42),AccessibleName=title};
            slider.ValueChanged+=delegate{amount.Text=slider.Value+unit;action(slider.Value);Changed();};
            panel.Controls.Add(text);panel.Controls.Add(slider);panel.Controls.Add(amount);Add(panel);
        }

        private bool English {get{return UiText.English(P);}}
        private string Translate(string text){return UiText.Get(P,text);}
        internal void RefreshStatus()
        {
            if(masterRow!=null&&!masterRow.IsDisposed){masterRow.Value=P.Enabled;masterRow.Invalidate();}
            status.Text=Translate(P.Enabled?"●  Bildirimler etkin":"○  Bildirimler kapalı");
            status.ForeColor=P.Enabled?Palette.Accent(P):Palette.Muted(P);
        }
        private void ApplyLanguage(Control root)
        {
            if(root is HomeCard||root is HomeDashboard||root is BrandMark)return; // Dashboard strings are rebuilt for the selected language.
            if((string)root.Tag=="nav")
            {root.Text=NavLabel(root.Name);root.AccessibleName=Translate(root.Name);}
            else if(root==heading)root.Text=Translate(page);
            else if(root==subheading)root.Text=Translate(root.Text);
            else if(root==status)RefreshStatus();
            else
            {
                var original=originals.GetValue(root,delegate(Control c){var row=c as SwitchRow;return new OriginalText{Text=c.Text,AccessibleName=c.AccessibleName,Description=row==null?null:row.Description};});
                if(root is Label||root is Button||root is SwitchRow)root.Text=Translate(original.Text);
                if(original.AccessibleName!=null)root.AccessibleName=Translate(original.AccessibleName);
                var toggle=root as SwitchRow;
                if(toggle!=null)toggle.Description=Translate(original.Description);
            }
            foreach(Control child in root.Controls)ApplyLanguage(child);
            var selector=root as ChoiceControl;
            if(selector!=null)selector.AccessibleDescription=selector.DisplayValue;
            if(root==this)Text=L("Flux — Flyout ayarları","Flux — Flyout settings");
        }
        private void Number(string title,int value,int min,int max,string unit,Action<int> action)
        {
            var panel=new SettingsCard(P){Size=new Size(620,58),Tag="card",Glyph=SettingsDesign.Glyph(title)};
            panel.Controls.Add(new Label{Text=title,Location=new Point(17,18),Size=new Size(270,29)});
            var input=new NumberControl(P,min,max,value,title){Location=new Point(301,10),Size=new Size(298,36)};
            input.ValueChanged+=delegate{action(input.Value);Changed();};
            panel.Controls.Add(input);Add(panel);
        }

        private void PanelPage(string kind)
        {
            var p=P.Panel(kind);
            subheading.Text="Her panelin görünümü, konumu ve süresi bağımsızdır. Paneller aynı konumda üst üste gelebilir.";
            Group("","intro");
            Add(new PanelPreview(app,kind){Size=new Size(620,240)});
            var preview=Button("Bu paneli önizle",210,37);
            preview.Click+=delegate {
                if(kind=="media")app.MediaWindow.Reveal(true);
                else if(kind=="volume")app.VolumeWindow.Reveal(true);
                else app.LockWindow.RevealLock("Caps Lock",true,true);
            };
            var align=Button(kind=="media"?"Diğer panelleri buraya yerleştir":"Medya ile aynı konuma getir",390,37);
            align.Click+=delegate {
                if(kind=="media"){P.Panel("volume").CopyPlacementFrom(p);P.Panel("lock").CopyPlacementFrom(p);}
                else p.CopyPlacementFrom(P.Panel("media"));
                Changed();BuildPage();
            };
            if(kind=="media")
            {
                Toggle("Medya paneli etkin","Medya panelini etkinleştirir veya kapatır.",P.Media,v=>P.Media=v);
                Toggle("Medya panelini sabitle","Panelin ekrandan otomatik olarak kaybolmasını engeller.",P.Pinned,v=>P.Pinned=v);
                Toggle("Parça değişince göster","Kapalıysa yeni parça paneli açmaz.",P.AutoShowMedia,v=>P.AutoShowMedia=v);
            }
            else if(kind=="volume")
            {
                Toggle("Ses paneli etkin","Ses değişikliklerinde ve ses tuşlarında göster.",P.Volume,v=>P.Volume=v);
                Toggle("Windows'un eski panelini gizle","Klasik Windows ses panelini isteğe bağlı olarak gizler.",P.HideNative,v=>P.HideNative=v);
            }
            else
            {
                Toggle("Caps Lock","Büyük harf kilidi bildirimi.",P.Caps,v=>P.Caps=v);
                Toggle("Num Lock","Sayısal tuş takımı kilidi bildirimi.",P.Num,v=>P.Num=v);
                Toggle("Scroll Lock","Kaydırma kilidi bildirimi.",P.Scroll,v=>P.Scroll=v);
            }
            Section("Yerleşim");
            Choice("Konum",new[]{"Sol üst","Orta üst","Sağ üst","Sol alt","Orta alt","Sağ alt"},p.Position,v=>p.Position=v);
            var screens=new string[Screen.AllScreens.Length];
            for(int i=0;i<screens.Length;i++)screens[i]="Ekran "+(i+1)+(Screen.AllScreens[i].Primary?" (Ana)":"");
            Choice("Ekran",screens,screens[Math.Max(0,Math.Min(screens.Length-1,p.Monitor))],v=>p.Monitor=Array.IndexOf(screens,v));
            Number("Kenardan boşluk (px)",p.Margin,0,500,"",v=>p.Margin=v);
            Number("X kaydırma (px)",p.OffsetX,-10000,10000,"",v=>p.OffsetX=v);
            Number("Y kaydırma (px)",p.OffsetY,-10000,10000,"",v=>p.OffsetY=v);
            Add(new Label{Text="X: sağa (+), sola (-). Y: aşağı (+), yukarı (-). Son açılan panel önde görünür.",
                Size=new Size(620,32),Font=new Font("Segoe UI",9),Tag="muted"});
            Add(align);
            Section("Boyut ve süre");
            Range("Boyut",p.Scale,80,150,"%",v=>p.Scale=v);
            Range("Saydamlık",p.Opacity,45,100,"%",v=>p.Opacity=v);
            Add(new Label{Text=L("Yazı ve ikonlar arka plandan daha az solar.","Text and icons fade less than the background."),
                Size=new Size(620,44),Font=new Font("Segoe UI",9),Tag="muted"});
            Range("Ekranda kalma",p.Duration,1,15," sn",v=>p.Duration=v);
            Section("Bu panelin görünümü");
            Choice("Panel tasarımı",PanelDesigns.Names,p.Design,v=>p.Design=v);
            Choice("Tema",new[]{"Uygulama ile aynı","Sistem","Koyu","Açık"},p.Theme,v=>p.Theme=v);
            Choice("Vurgu rengi",new[]{"Uygulama ile aynı","Sistem","Kırmızı","Mavi","Mor","Özel"},p.Accent,v=>p.Accent=v);
            Toggle("Metinleri ortala","Panel başlığı ve açıklamalarını ortalar.",p.CenterText,v=>p.CenterText=v);
            var color=Button("Bu panel için özel renk seç",620,39);
            color.Click+=delegate {
                using(var dialog=new ColorDialog{Color=Palette.Accent(P.Appearance(kind)),FullOpen=true})
                if(dialog.ShowDialog(this)==DialogResult.OK){p.CustomAccent=ColorTranslator.ToHtml(dialog.Color);p.Accent="Özel";Changed();BuildPage();}
            };Add(color);
            Add(preview);
            if(kind=="media")Toggle("Plağı döndür","Medya oynarken plak dönüşünü gösterir.",p.Spin,v=>p.Spin=v);
        }

        private void BuildPage(bool reuse=false)
        {
            var previous=content;
            CachedPage cached;
            if(reuse&&pageCache.TryGetValue(page,out cached)&&!cached.View.IsDisposed)
            {
                main.SuspendLayout();
                content=cached.View;masterRow=cached.Master;
                heading.Text=Translate(page);subheading.Text=cached.Subtitle;
                LayoutPage();
                RefreshPanelPreviews(content);
                content.Visible=true;content.BringToFront();previous.Visible=false;
                main.ResumeLayout(false);SelectNavigation();RefreshStatus();
                return;
            }
            if(!reuse)DiscardInactivePages();
            P.EnsurePanels();
            SuspendLayout();
            main.SuspendLayout();
            content=CreatePageView();
            content.SuspendLayout();
            main.Controls.Add(content);
            activeGroup=null;
            content.AutoScrollPosition=Point.Empty;
            heading.Text=page;
            masterRow=null;
            if(page=="Başlangıç")
            {
                HomePage();
            }
            else if(page=="Sürüm Bilgisi"||page=="Yardım ve SSS")
            {
                InformationPage(page=="Sürüm Bilgisi");
            }
            else if(page=="Medya paneli" || page=="Ses paneli" || page=="Kilit paneli")
            {
                PanelPage(page=="Medya paneli"?"media":page=="Ses paneli"?"volume":"lock");
            }
            else
            {
                subheading.Text=L("Dil, görünüm, başlangıç ve uygulama yönetimi.","Language, appearance, startup and application settings.");
                Section(L("Başlangıç ve yönetim","Startup and management"));
                Toggle("Göstergeleri etkinleştir","Tüm Flux bildirimlerini tek yerden yönetin.",P.Enabled,v=>P.Enabled=v);
                Toggle("Windows ile başlat","Kullanıcı oturum açtığında Flux'ı tepside çalıştır.",app.StartupEnabled,v=>app.SetStartup(v));
                Toggle("Windows'un eski panelini gizle","Klasik Windows ses panelini isteğe bağlı olarak gizler.",P.HideNative,v=>P.HideNative=v);
                Add(new Label{Text="Medya bilgisi, kullandığınız uygulamanın Windows ile paylaştığı bilgilerle sınırlıdır. Ayarlar yerel olarak saklanır.",Size=new Size(620,58),Padding=new Padding(12,6,12,4),Tag="muted"});
                Section("Görünüm");
                Choice("Dil",new[]{"Sistem","Türkçe","English"},P.Language,v=>{
                    P.Language=v;
                    // Let the dropdown finish its click/close before replacing it.
                    if(IsHandleCreated)BeginInvoke(new Action(delegate{if(!IsDisposed)BuildPage();}));
                    else BuildPage();
                });
                Choice("Uygulama teması",new[]{"Sistem","Koyu","Açık"},P.Theme,v=>P.Theme=v);
                Choice("Uygulama vurgu rengi",new[]{"Sistem","Kırmızı","Mavi","Mor","Özel"},P.Accent,v=>P.Accent=v);
                var color=Button("Uygulama için özel renk seç",620,39);
                color.Click+=delegate {
                    using(var dialog=new ColorDialog{Color=Palette.Accent(P),FullOpen=true})
                    if(dialog.ShowDialog(this)==DialogResult.OK){P.CustomAccent=ColorTranslator.ToHtml(dialog.Color);P.Accent="Özel";Changed();BuildPage();}
                };Add(color);
                Add(new Label{Text="Her panelin bağımsız konumu, süresi, rengi ve saydamlığı kendi sekmesindedir.",
                    Size=new Size(620,44),Padding=new Padding(12,5,12,5),Tag="muted"});
                Group(L("Uygulama yönetimi","Application management"),"stack");
                var info=Button("Bağlantı durumunu göster",620,40);
                info.Click+=delegate {
                    MessageBox.Show(this,ConnectionStatusText(),L("Flux • Durum","Flux • Status"),MessageBoxButtons.OK,MessageBoxIcon.Information);
                };Add(info);
                var reset=Button("Varsayılan ayarlara dön",620,40);
                reset.Click+=delegate {
                    if(MessageBox.Show(this,L("Görünüm ve gösterge ayarları sıfırlansın mı?","Reset appearance and indicator settings?"),"Flux",MessageBoxButtons.YesNo,MessageBoxIcon.Question)==DialogResult.Yes)
                    {app.Settings=new Preferences();app.Settings.EnsurePanels();Changed();BuildPage();}
                };Add(reset);
                var quit=Button("Uygulamadan çık",620,40);quit.Click+=delegate{app.RequestExit();};Add(quit);
                Add(new Label{Text="Flux 2.1.0 • Veriler bu bilgisayarda saklanır.\nAyarlar: %LOCALAPPDATA%\\AkisFlyout\\settings.xml",Size=new Size(620,60),Padding=new Padding(3,10,0,0),Font=new Font("Segoe UI",9),Tag="muted"});
            }
            ApplyTheme(content);ApplyTheme(sidebar);
            heading.BackColor=subheading.BackColor=Color.Transparent;
            heading.ForeColor=Palette.Text(P);subheading.ForeColor=ShellSkin.Muted(P);
            status.Text=P.Enabled?"●  Bildirimler etkin":"○  Bildirimler kapalı";
            ApplyLanguage(content);ApplyLanguage(sidebar);
            heading.Text=Translate(page);subheading.Text=Translate(subheading.Text);
            Text=L("Flux — Flyout ayarları","Flux — Flyout settings");
            foreach(Control c in content.Controls){var group=c as SettingsGroup;if(group!=null)group.DeferArrange=false;}
            LayoutPage();
            content.ResumeLayout(false);
            content.PerformLayout();
            content.Visible=true;
            content.BringToFront();
            previous.Visible=false;
            bool retainPrevious=false;
            if(reuse)foreach(var entry in pageCache.Values)if(entry.View==previous){retainPrevious=true;break;}
            if(!retainPrevious)
            {
                var stale=new List<string>();foreach(var pair in pageCache)if(pair.Value.View==previous)stale.Add(pair.Key);
                foreach(string key in stale)pageCache.Remove(key);
                previous.Dispose();
            }
            pageCache[page]=new CachedPage{View=content,Subtitle=subheading.Text,Master=masterRow};
            main.ResumeLayout(false);
            ResumeLayout(false);
            SelectNavigation();
        }

        private void LayoutPage()
        {
            if(content==null)return;
            bool home=page=="Başlangıç";heading.Visible=subheading.Visible=!home;
            int y=home?25:132;
            int width=main.ClientSize.Width-48;
            int x=24;
            content.SetBounds(x,y,Math.Max(100,width),Math.Max(100,main.ClientSize.Height-y-20));
            if(!home)
            {
                heading.Left=x+8;heading.Width=width-16;subheading.Left=x+10;subheading.Width=width-20;
                foreach(Control row in content.Controls)
                {
                    row.Width=Math.Max(620,width-24);
                    var group=row as SettingsGroup;if(group!=null){group.Arrange();continue;}
                    foreach(Control child in row.Controls)
                    {
                        if(child is ChoiceControl||child is NumericUpDown)child.Left=row.Width-child.Width-21;
                        else if(child is SliderControl)child.Width=row.Width-327;
                        else if(child is Label&&child.Left>500)child.Left=row.Width-88;
                    }
                }
            }
            if(home&&content.Controls.Count>0)content.Controls[0].Width=Math.Max(680,content.ClientSize.Width-22);
        }

        private void LayoutSidebar()
        {
            if(sidebar==null)return;
            Control brand=sidebar.Controls["brand"];if(brand!=null)brand.SetBounds(22,25,211,111);
            string[] names={"Başlangıç","Medya paneli","Ses paneli","Kilit paneli","Uygulama"};
            for(int i=0;i<names.Length;i++){Control nav=sidebar.Controls[names[i]];if(nav!=null)nav.SetBounds(13,157+i*66,210,58);}
            Control state=sidebar.Controls["status"];if(state!=null)state.SetBounds(27,Math.Max(0,sidebar.ClientSize.Height-172),194,40);
            Control version=sidebar.Controls["version"];if(version!=null)version.SetBounds(27,Math.Max(0,sidebar.ClientSize.Height-131),195,45);
            Control exit=sidebar.Controls["exit"];if(exit!=null)exit.SetBounds(13,Math.Max(0,sidebar.ClientSize.Height-74),210,58);
            sidebar.Invalidate(true);
        }

        private void Changed()
        {
            DiscardInactivePages();
            app.Changed();
            ApplyTheme(this);
            RefreshPanelPreviews(content);
        }
        private void RefreshPanelPreviews(Control root)
        {foreach(Control c in root.Controls){var preview=c as PanelPreview;if(preview!=null)preview.RefreshFrame();else RefreshPanelPreviews(c);}}
        public void RefreshTheme(){DiscardInactivePages();ApplyTheme(this);RefreshPanelPreviews(content);}
        internal void PreviewPage(string name){page=name;BuildPage();}
        internal string ConnectionStatusText()
        {
            string media=app.Media.Available?Translate(app.Media.Source)+" — "+app.Media.Title:Translate("Etkin medya oturumu yok.");
            return L("Medya: ","Media: ")+media+L("\nSes aygıtı: ","\nAudio device: ")+Translate(app.Audio.Available?"Bağlı":"Bulunamadı")+
                "\n\n"+Translate(app.Media.Error)+"\n"+Translate(app.Audio.Error);
        }
        private void ApplyTheme(Control control)
        {
            var shell=control as ShellPanel;if(shell!=null){shell.Prefs=P;shell.Invalidate();}
            var brand=control as BrandMark;if(brand!=null){brand.Prefs=P;brand.Invalidate();return;}
            if(control is HomeDashboard){control.Invalidate(true);return;}
            if(control is PanelPreview){control.Invalidate();return;}
            if(control is SettingsParagraph){control.BackColor=Color.Transparent;control.ForeColor=ShellSkin.Muted(P);control.Invalidate();return;}
            if(control is SettingsAction){control.Invalidate();return;}
            var number=control as NumberControl;if(number!=null){number.RefreshTheme();return;}
            var nav=control as NavButton;if(nav!=null){nav.Prefs=P;nav.Selected=nav.Name==NavigationParent;nav.Invalidate();return;}
            if(control is HomeCard){control.BackColor=Palette.Bg(P);control.Invalidate();return;}
            if(control is SwitchRow || control is ChoiceControl || control is SliderControl){control.Invalidate();return;}
            control.BackColor=(string)control.Tag=="card"||(string)control.Tag=="button"?ShellSkin.Card(P):ShellSkin.Bg(P);
            control.ForeColor=(string)control.Tag=="muted"?ShellSkin.Muted(P):Palette.Text(P);
            if(control==content||control is SettingsCard||control is SettingsGroup)control.BackColor=Color.Transparent;
            if(control is Label && control.Parent!=null)control.BackColor=Color.Transparent;
            if((string)control.Tag=="nav")
            {
                bool active=control.Name==page;
                control.BackColor=active?Palette.Accent(P):Color.Transparent;
                control.ForeColor=active?(Palette.Accent(P).GetBrightness()>.6?Color.Black:Color.White):Palette.Text(P);
            }
            if(control is ComboBox||control is TrackBar)control.BackColor=Palette.Card(P);
            foreach(Control c in control.Controls)ApplyTheme(c);
            status.ForeColor=Palette.Accent(P);
        }
        protected override void Dispose(bool disposing)
        {
            if(disposing){refresh.Dispose();tips.Dispose();pageCache.Clear();}
            base.Dispose(disposing);
        }
    }
}
