using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;

namespace VinhKhanhAudioGuide.App;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
		
		// Register route for login page
		Routing.RegisterRoute("login", typeof(LoginPage));
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
}