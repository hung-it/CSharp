namespace VinhKhanhAudioGuide.App;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute("audio", typeof(AudioPlayerPage));
		Routing.RegisterRoute("poiDetail", typeof(PoiDetailPage));
		Routing.RegisterRoute("qrScan", typeof(QrScanPage));
		Routing.RegisterRoute("tourDetail", typeof(TourDetailPage));
	}
}
