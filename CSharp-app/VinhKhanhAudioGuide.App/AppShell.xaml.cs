namespace VinhKhanhAudioGuide.App;

public partial class AppShell : Shell
{
    public AppShell()
    {
        System.Diagnostics.Debug.WriteLine("[AppShell] Constructor started");
        InitializeComponent();
        System.Diagnostics.Debug.WriteLine("[AppShell] InitializeComponent completed");

        // Register routes for navigation
        Routing.RegisterRoute("login", typeof(LoginPage));
        Routing.RegisterRoute("account", typeof(SettingsPage));
        Routing.RegisterRoute("poiDetail", typeof(PoiDetailPage));
        Routing.RegisterRoute("audio", typeof(AudioPlayerPage));
        Routing.RegisterRoute("qrScan", typeof(QrScanPage));
        Routing.RegisterRoute("tourDetail", typeof(TourDetailPage));
        
        System.Diagnostics.Debug.WriteLine("[AppShell] All routes registered");
    }

    public async Task SwitchToAccountTabAsync()
    {
        // Navigate to the account tab
        await GoToAsync("accountTab");
    }
}
