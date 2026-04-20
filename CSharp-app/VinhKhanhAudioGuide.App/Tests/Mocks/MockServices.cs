using Microsoft.Maui.Essentials;

namespace VinhKhanhAudioGuide.App.Tests.Mocks
{
    public class MockConnectivity : IConnectivity
    {
        private NetworkAccess _networkAccess = NetworkAccess.Internet;
        private List<ConnectionProfile> _connectionProfiles = new() { ConnectionProfile.WiFi };

        public NetworkAccess NetworkAccess => _networkAccess;
        public IEnumerable<ConnectionProfile> ConnectionProfiles => _connectionProfiles;

        public event EventHandler<ConnectivityChangedEventArgs> ConnectivityChanged;

        public void SetNetworkAccess(NetworkAccess access)
        {
            var oldAccess = _networkAccess;
            _networkAccess = access;
            ConnectivityChanged?.Invoke(this, new ConnectivityChangedEventArgs(access, _connectionProfiles));
        }

        public void SetConnectionProfiles(IEnumerable<ConnectionProfile> profiles)
        {
            _connectionProfiles = profiles.ToList();
        }
    }

    public class MockGeolocation : IGeolocation
    {
        private Location _currentLocation = new(10.123456, 106.654321);

        public async Task<Location> GetLastKnownLocationAsync()
        {
            await Task.Delay(100);
            return _currentLocation;
        }

        public async Task<Location> GetLocationAsync(GeolocationRequest request)
        {
            await Task.Delay(500);
            return _currentLocation;
        }

        public async Task<Location> GetLocationAsync(GeolocationRequest request, CancellationToken cancellationToken)
        {
            await Task.Delay(500, cancellationToken);
            return _currentLocation;
        }

        public void SetLocation(double latitude, double longitude)
        {
            _currentLocation = new Location(latitude, longitude);
        }
    }

    public class MockMediaManager : IMediaManager
    {
        private bool _isPlaying;
        private TimeSpan _duration = TimeSpan.FromMinutes(3);
        private TimeSpan _position = TimeSpan.Zero;
        private string _currentUrl;

        public bool IsPlaying => _isPlaying;
        public TimeSpan Duration => _duration;
        public TimeSpan Position => _position;
        public string CurrentUrl => _currentUrl;

        public event EventHandler<MediaStateChangedEventArgs> StateChanged;
        public event EventHandler<MediaPositionChangedEventArgs> PositionChanged;

        public async Task LoadAsync(string url)
        {
            await Task.Delay(200);
            _currentUrl = url;
            StateChanged?.Invoke(this, new MediaStateChangedEventArgs(MediaState.Loaded));
        }

        public async Task PlayAsync()
        {
            await Task.Delay(100);
            _isPlaying = true;
            StateChanged?.Invoke(this, new MediaStateChangedEventArgs(MediaState.Playing));
        }

        public async Task PauseAsync()
        {
            await Task.Delay(100);
            _isPlaying = false;
            StateChanged?.Invoke(this, new MediaStateChangedEventArgs(MediaState.Paused));
        }

        public async Task StopAsync()
        {
            await Task.Delay(100);
            _isPlaying = false;
            _position = TimeSpan.Zero;
            StateChanged?.Invoke(this, new MediaStateChangedEventArgs(MediaState.Stopped));
        }

        public async Task SeekToAsync(TimeSpan position)
        {
            await Task.Delay(50);
            _position = position;
            PositionChanged?.Invoke(this, new MediaPositionChangedEventArgs(position));
        }

        public void SetVolume(float volume)
        {
            // Mock volume setting
        }

        public void SetDuration(TimeSpan duration)
        {
            _duration = duration;
        }
    }

    public class MockFileSystem : IFileSystem
    {
        private readonly Dictionary<string, byte[]> _files = new();

        public string CacheDirectory => "/mock/cache";
        public string AppDataDirectory => "/mock/appdata";

        public async Task<Stream> OpenAppPackageFileAsync(string filename)
        {
            await Task.Delay(50);
            if (_files.ContainsKey(filename))
            {
                return new MemoryStream(_files[filename]);
            }
            throw new FileNotFoundException($"File {filename} not found");
        }

        public async Task<bool> AppPackageFileExistsAsync(string filename)
        {
            await Task.Delay(10);
            return _files.ContainsKey(filename);
        }

        public void AddMockFile(string filename, byte[] content)
        {
            _files[filename] = content;
        }

        public void RemoveMockFile(string filename)
        {
            _files.Remove(filename);
        }
    }

    public class MockPreferences : IPreferences
    {
        private readonly Dictionary<string, object> _preferences = new();

        public void Clear(string sharedName = null)
        {
            _preferences.Clear();
        }

        public bool ContainsKey(string key, string sharedName = null)
        {
            return _preferences.ContainsKey(key);
        }

        public T Get<T>(string key, T defaultValue, string sharedName = null)
        {
            if (_preferences.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }

        public void Remove(string key, string sharedName = null)
        {
            _preferences.Remove(key);
        }

        public void Set<T>(string key, T value, string sharedName = null)
        {
            _preferences[key] = value;
        }
    }

    public class MockSecureStorage : ISecureStorage
    {
        private readonly Dictionary<string, string> _storage = new();

        public async Task<string> GetAsync(string key)
        {
            await Task.Delay(50);
            return _storage.TryGetValue(key, out var value) ? value : null;
        }

        public async Task SetAsync(string key, string value)
        {
            await Task.Delay(50);
            _storage[key] = value;
        }

        public bool Remove(string key)
        {
            return _storage.Remove(key);
        }

        public void RemoveAll()
        {
            _storage.Clear();
        }
    }
}