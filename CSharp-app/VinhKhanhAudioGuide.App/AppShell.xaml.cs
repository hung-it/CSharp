namespace VinhKhanhAudioGuide.App;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Register routes for navigation
        Routing.RegisterRoute("login", typeof(LoginPage));
        Routing.RegisterRoute("account", typeof(SettingsPage));
        Routing.RegisterRoute("poiDetail", typeof(PoiDetailPage));
        Routing.RegisterRoute("audio", typeof(AudioPlayerPage));
        Routing.RegisterRoute("qrScan", typeof(QrScanPage));
        Routing.RegisterRoute("tourDetail", typeof(TourDetailPage));
    }
}
