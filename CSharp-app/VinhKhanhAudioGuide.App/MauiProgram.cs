using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Controls;
using Plugin.Maui.Audio;
using System.IO;
using System.Text;
using ZXing.Net.Maui.Controls;

namespace VinhKhanhAudioGuide.App;

public static class MauiProgram
{
	private static readonly string LogFilePath = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
		"VinKhanhAudioGuide", "app_log.txt");

	public static MauiApp CreateMauiApp()
	{
		// Global exception handlers
		AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
		{
			var ex = args.ExceptionObject as Exception;
			var msg = $"Unhandled Exception:\n{ex?.Message}\n\nStack:\n{ex?.StackTrace}";
			LogError(msg);
			ShowErrorDialog(msg);
		};

		TaskScheduler.UnobservedTaskException += (sender, args) =>
		{
			var msg = $"Unobserved Task Exception:\n{args.Exception?.Message}\n\nStack:\n{args.Exception?.StackTrace}";
			LogError(msg);
			ShowErrorDialog(msg);
			args.SetObserved();
		};

		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.AddAudio()
			.UseBarcodeReader()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}

	private static void LogError(string message)
	{
		try
		{
			var dir = Path.GetDirectoryName(LogFilePath);
			if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
				Directory.CreateDirectory(dir);
			
			var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n";
			File.AppendAllText(LogFilePath, logEntry, System.Text.Encoding.UTF8);
			System.Diagnostics.Debug.WriteLine(logEntry);
		}
		catch { }
	}

	private static void ShowErrorDialog(string message)
	{
		MainThread.BeginInvokeOnMainThread(() =>
		{
			try
			{
				var page = Application.Current?.MainPage;
				if (page != null)
				{
					page.DisplayAlert("LỖI", message, "OK");
				}
			}
			catch
			{
				// Can't show dialog, already in bad state
			}
		});
	}
}
