using System.Windows.Input;

namespace VinhKhanhAudioGuide.App.Controls;

public class AudioPlayer : Microsoft.Maui.Controls.View
{
    public static readonly BindableProperty SourceProperty =
        BindableProperty.Create(nameof(Source), typeof(string), typeof(AudioPlayer), null);

    public static readonly BindableProperty IsPlayingProperty =
        BindableProperty.Create(nameof(IsPlaying), typeof(bool), typeof(AudioPlayer), false);

    public static readonly BindableProperty DurationProperty =
        BindableProperty.Create(nameof(Duration), typeof(TimeSpan), typeof(AudioPlayer), TimeSpan.Zero);

    public static readonly BindableProperty PositionProperty =
        BindableProperty.Create(nameof(Position), typeof(TimeSpan), typeof(AudioPlayer), TimeSpan.Zero);

    public static readonly BindableProperty CurrentStateProperty =
        BindableProperty.Create(nameof(CurrentState), typeof(MediaPlaybackState), typeof(AudioPlayer), MediaPlaybackState.None);

    public string Source
    {
        get => (string)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public bool IsPlaying
    {
        get => (bool)GetValue(IsPlayingProperty);
        set => SetValue(IsPlayingProperty, value);
    }

    public TimeSpan Duration
    {
        get => (TimeSpan)GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }

    public TimeSpan Position
    {
        get => (TimeSpan)GetValue(PositionProperty);
        set => SetValue(PositionProperty, value);
    }

    public MediaPlaybackState CurrentState
    {
        get => (MediaPlaybackState)GetValue(CurrentStateProperty);
        set => SetValue(CurrentStateProperty, value);
    }

    public event EventHandler? MediaEnded;

    public ICommand? PlayCommand { get; set; }
    public ICommand? PauseCommand { get; set; }
    public ICommand? StopCommand { get; set; }

    public AudioPlayer()
    {
        PlayCommand = new Command(() => Play());
        PauseCommand = new Command(() => Pause());
        StopCommand = new Command(() => Stop());
    }

    public void Play()
    {
        if (PlayCommand?.CanExecute(null) == true)
            PlayCommand.Execute(null);
    }

    public void Pause()
    {
        if (PauseCommand?.CanExecute(null) == true)
            PauseCommand.Execute(null);
    }

    public void Stop()
    {
        if (StopCommand?.CanExecute(null) == true)
            StopCommand.Execute(null);
    }

    public void RaiseMediaEnded()
    {
        MediaEnded?.Invoke(this, EventArgs.Empty);
    }
}

public enum MediaPlaybackState
{
    None,
    Playing,
    Paused,
    Stopped
}
