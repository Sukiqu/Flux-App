using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
namespace Akis
{
    internal static class UiText
    {
        internal static bool English(Preferences p)
        {
            if(p.Language=="English")return true;
            if(p.Language=="Türkçe")return false;
            return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName!="tr";
        }
        // Preference values remain stable Turkish keys; only displayed text changes.
        private static readonly Dictionary<string,string> Words=new Dictionary<string,string>(StringComparer.Ordinal)
        {
            {"Başlangıç","Home"},
            {"Sürüm Bilgisi","Version info"},
            {"Yardım ve SSS","Help and FAQ"},
            {"Medya paneli","Media panel"},
            {"Ses paneli","Volume panel"},
            {"Kilit paneli","Lock panel"},
            {"Görünüm","Appearance"},
            {"Uygulama","Application"},
            {"Göstergeleri etkinleştir","Enable indicators"},
            {"Göstergeler etkin","●  Indicators enabled"},
            {"●  Göstergeler etkin","●  Indicators enabled"},
            {"Göstergeler kapalı","○  Indicators disabled"},
            {"○  Göstergeler kapalı","○  Indicators disabled"},
            {"●  Bildirimler etkin","●  Notifications enabled"},
            {"○  Bildirimler kapalı","○  Notifications disabled"},
            {"Metinleri ortala","Center text"},
            {"Windows ile başlat","Start with Windows"},
            {"Uygulamadan çık","Exit application"},
            {"Varsayılan ayarlara dön","Reset to defaults"},
            {"Bağlantı durumunu göster","Show connection status"},
            {"Bu paneli önizle","Preview this panel"},
            {"Diğer panelleri buraya yerleştir","Place other panels here"},
            {"Medya ile aynı konuma getir","Match the media panel position"},
            {"Yerleşim","Placement"},
            {"Boyut ve süre","Size and duration"},
            {"Bu panelin görünümü","This panel's appearance"},
            {"Dil","Language"},
            {"Uygulama teması","Application theme"},
            {"Uygulama vurgu rengi","Application accent color"},
            {"Tema","Theme"},
            {"Panel tasarımı","Panel design"},
            {"Plak","Vinyl"},
            {"Vurgu rengi","Accent color"},
            {"Konum","Position"},
            {"Ekran","Monitor"},
            {"Saydamlık","Opacity"},
            {"Ekranda kalma","Display duration"},
            {"Boyut","Size"},
            {"Kenardan boşluk (px)","Edge margin (px)"},
            {"X kaydırma (px)","X offset (px)"},
            {"Y kaydırma (px)","Y offset (px)"},
            {"Medya kartını göster","Show media card"},
            {"Windows'un eski panelini gizle","Hide the Windows volume panel"},
            {"Koyu","Dark"},
            {"Açık","Light"},
            {"Sistem","System"},
            {"Uygulama ile aynı","Same as application"},
            {"Kırmızı","Red"},
            {"Mavi","Blue"},
            {"Mor","Purple"},
            {"Özel","Custom"},
            {"MASAÜSTÜ GÖSTERGELERİ","DESKTOP INDICATORS"},
            {"Tepsiye küçült","Minimize to tray"},
            {"Flux ayarları","Flux settings"},
            {"Bildirimi kapat","Dismiss notification"},
            {"Çıkış","Exit"},
            {"Medya paneli etkin","Enable media panel"},
            {"Medya panelini etkinleştirir veya kapatır.","Enable or disable the media panel."},
            {"Medya tuşlarıyla veya menüden açın.","Open with media keys or from the menu."},
            {"Medya panelini sabitle","Pin media panel"},
            {"Panelin ekrandan otomatik olarak kaybolmasını engeller.","Keep the panel visible while a media session is active."},
            {"Etkin oturum varken sürekli görünür tutun.","Keep visible while a media session is active."},
            {"Parça değişince göster","Show on track change"},
            {"Kapalıysa yeni parça paneli açmaz.","When off, a new track will not open the panel."},
            {"Plağı döndür","Rotate record"},
            {"Medya oynarken plak dönüşünü gösterir.","Show the record rotating while media is playing."},
            {"Ses paneli etkin","Enable volume panel"},
            {"Ses değişikliklerinde ve ses tuşlarında göster.","Show on volume changes and volume key presses."},
            {"Büyük harf kilidi bildirimi.","Caps Lock notification."},
            {"Sayısal tuş takımı kilidi bildirimi.","Num Lock notification."},
            {"Kaydırma kilidi bildirimi.","Scroll Lock notification."},
            {"Panel başlığı ve açıklamalarını ortalar.","Center the panel title and descriptions."},
            {"Bu panelin görünümü, konumu ve süresi bağımsızdır. Aynı konumda üst üste gelebilir.","Appearance, position and duration are independent. Panels can share the same position."},
            {"Her panelin görünümü, konumu ve süresi bağımsızdır. Paneller aynı konumda üst üste gelebilir.","Each panel has its own appearance, position and display duration. Panels can overlap at the same position."},
            {"X: sağa (+), sola (-). Y: aşağı (+), yukarı (-). Son açılan panel önde görünür.","X: right (+), left (-). Y: down (+), up (-). The latest panel appears in front."},
            {"Bu panel için özel renk seç","Choose a custom color for this panel"},
            {"Uygulama için özel renk seç","Choose a custom application color"},
            {"Uygulama temasını ve tüm panellerin temel görünümünü ayarlayın.","Set the application theme and the default appearance of all panels."},
            {"Her panelin bağımsız konumu, süresi, rengi ve saydamlığı kendi sekmesindedir.","Each panel has its own position, duration, color and opacity settings in its tab."},
            {"Dil, başlangıç ve uygulama yönetimi.","Language, startup and application settings."},
            {"Tüm Flux bildirimlerini tek yerden yönetin.","Manage all Flux notifications in one place."},
            {"Kullanıcı oturum açtığında Flux'ı tepside çalıştır.","Start Flux in the system tray when you sign in."},
            {"Klasik Windows ses panelini isteğe bağlı olarak gizler.","Optionally hide the classic Windows volume panel."},
            {"Medya bilgisi, kullandığınız uygulamanın Windows ile paylaştığı bilgilerle sınırlıdır. Ayarlar yerel olarak saklanır.","Media details depend on what your player shares with Windows. Settings are stored locally."},
            {"Sol üst","Top left"},{"Orta üst","Top center"},{"Sağ üst","Top right"},
            {"Sol alt","Bottom left"},{"Orta alt","Bottom center"},{"Sağ alt","Bottom right"},
            {"Türkçe","Turkish"},
            {"Medya Oynatıcı","Media Player"},{"Medya","Media"},
            {"MEDYA","MEDIA"},{"SES","VOLUME"},{"KİLİT","LOCK"},
            {"● Hazır","● Ready"},{"○ Kapalı","○ Off"},
            {"Etkin medya oturumu yok.","No active media session."},
            {"Bağlı","Connected"},{"Bulunamadı","Not found"},
            {"Medya yanıt süresi aşıldı.","The media request timed out."},
            {"Medya uygulamasından durum onayı zamanında gelmedi.","The player did not confirm its playback state in time."},
            {"Medya uygulaması bu komutu kabul etmedi.","The player did not accept this command."},
            {"Bildirim yüzeyi güncellenemedi.","The notification surface could not be updated."}
        };
        internal static string Get(Preferences p,string text)
        {
            if(!English(p)||string.IsNullOrEmpty(text))return text;
            string translated;if(Words.TryGetValue(text,out translated))return translated;
            if(Regex.IsMatch(text,@"^Ekran \d+( \(Ana\))?$"))return text.Replace("Ekran ","Monitor ").Replace(" (Ana)"," (Primary)");
            if(text.StartsWith("FLUX ",StringComparison.Ordinal))return text.Replace("Yerel uygulama","Local application");
            if(text.StartsWith("Flux ",StringComparison.Ordinal)&&text.Contains("Veriler bu bilgisayarda saklanır."))
                return text.Replace("Veriler bu bilgisayarda saklanır.","Data is stored on this computer.").Replace("Ayarlar:","Settings:");
            return text;
        }
    }
}
