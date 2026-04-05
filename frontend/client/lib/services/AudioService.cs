using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Media;
using Plugin.Maui.Audio;
using System.Text.RegularExpressions; 

namespace client.lib.services
{
    public partial class AudioService : ObservableObject
    {
        private readonly IAudioManager _audioManager;
        private IAudioPlayer? _audioPlayer;

        [ObservableProperty]
        private bool _isSpeaking;

        [ObservableProperty]
        private bool _isPaused;

        [ObservableProperty]
        private string _currentTrackTitle = string.Empty;

        private CancellationTokenSource? _cts;
        private Locale? _cachedLocale;
        private bool _isInitialized = false;

        private List<string> _ttsSentences = new();
        private int _currentSentenceIndex = 0;
        private bool _isUsingTts = false;

        public AudioService(IAudioManager audioManager)
        {
            _audioManager = audioManager;
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;
            try
            {
                var locales = await TextToSpeech.Default.GetLocalesAsync();
                _cachedLocale = locales.FirstOrDefault(l => l.Language == "vi") ?? locales.FirstOrDefault();
                _isInitialized = true;
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Audio Init Error: {ex.Message}"); }
        }

        public async Task PlayAudioFromUrlAsync(string url, string title)
        {
            try
            {
                Stop();
                _isUsingTts = false;
                CurrentTrackTitle = title;
                IsSpeaking = true;
                IsPaused = false;

                using var httpClient = new HttpClient();
                var audioStream = await httpClient.GetStreamAsync(url);

                _audioPlayer = _audioManager.CreatePlayer(audioStream);
                _audioPlayer.PlaybackEnded += (s, e) => { if (!IsPaused) IsSpeaking = false; };
                _audioPlayer.Play();
            }
            catch (Exception) { IsSpeaking = false; }
        }

        // Thêm tham số languageCode (VD: "vi", "en", "ko")
        public async Task SpeakAsync(string text, string title, string languageCode = "vi")
        {
            if (!_isInitialized) await InitializeAsync();
            Stop();

            // Cập nhật lại Locale theo ngôn ngữ được truyền vào
            var locales = await TextToSpeech.Default.GetLocalesAsync();
            _cachedLocale = locales.FirstOrDefault(l => l.Language.StartsWith(languageCode, StringComparison.OrdinalIgnoreCase))
                            ?? locales.FirstOrDefault(); // Fallback về mặc định nếu không tìm thấy

            _isUsingTts = true;
            CurrentTrackTitle = title;
            IsSpeaking = true;
            IsPaused = false;

            string cleanText = text.Replace("\r", " ").Replace("\n", " ");
            var rawChunks = Regex.Split(cleanText, @"(?<=[.,?!;:])\s+");

            _ttsSentences = rawChunks
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .ToList();

            _currentSentenceIndex = 0;

            await PlayTtsLoopAsync();
        }

        private async Task PlayTtsLoopAsync()
        {
            _cts = new CancellationTokenSource();

            // Tạo một biến tạm để giữ reference, tránh bị crash nếu _cts bị set thành null từ luồng khác
            var localCts = _cts;

            try
            {
                while (_currentSentenceIndex < _ttsSentences.Count)
                {
                    if (IsPaused || localCts.IsCancellationRequested) return;

                    string sentence = _ttsSentences[_currentSentenceIndex];
                    var settings = new SpeechOptions { Locale = _cachedLocale };

                    await TextToSpeech.Default.SpeakAsync(sentence, settings, cancelToken: localCts.Token);

                    // [ĐÃ SỬA] Kiểm tra _cts != null trước khi truy cập thuộc tính của nó
                    if (!IsPaused && _cts != null && !_cts.IsCancellationRequested)
                    {
                        _currentSentenceIndex++;
                    }
                    else
                    {
                        // Nếu bị ấn Stop (_cts bị null) hoặc bị Pause thì thoát vòng lặp
                        break;
                    }
                }
            }
            catch (TaskCanceledException) { }
            catch (Exception) { }

            // Tự động tắt thanh UI khi đọc xong bài
            if (_currentSentenceIndex >= _ttsSentences.Count && !IsPaused)
            {
                MainThread.BeginInvokeOnMainThread(() => IsSpeaking = false);
            }
        }

        [RelayCommand]
        public void TogglePause()
        {
            IsPaused = !IsPaused;

            if (_isUsingTts)
            {
                if (IsPaused)
                {
                    _cts?.Cancel();
                }
                else
                {
                    _ = PlayTtsLoopAsync();
                }
            }
            else if (_audioPlayer != null)
            {
                if (IsPaused) _audioPlayer.Pause();
                else _audioPlayer.Play();
            }
        }

        [RelayCommand]
        public void Stop()
        {
            IsPaused = false;
            IsSpeaking = false;

            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
            _currentSentenceIndex = _ttsSentences.Count;

            if (_audioPlayer != null)
            {
                if (_audioPlayer.IsPlaying) _audioPlayer.Stop();
                _audioPlayer.Dispose();
                _audioPlayer = null;
            }
        }
    }
}