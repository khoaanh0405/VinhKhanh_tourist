using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Media;
using Plugin.Maui.Audio;
using System.Text.RegularExpressions; // THÊM THƯ VIỆN REGEX

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

        public async Task SpeakAsync(string text, string title)
        {
            if (!_isInitialized || _cachedLocale == null) await InitializeAsync();
            Stop();

            _isUsingTts = true;
            CurrentTrackTitle = title;
            IsSpeaking = true;
            IsPaused = false;

            // 1. Dọn dẹp khoảng trắng thừa và dấu xuống dòng
            string cleanText = text.Replace("\r", " ").Replace("\n", " ");

            // 2. [SỬA Ở ĐÂY] Thêm dấu phẩy (,), chấm phẩy (;), hai chấm (:) vào Regex
            // Lệnh này nghìa là: Cắt câu ở bất kỳ dấu ngắt quãng nào, MIỄN LÀ sau đó có dấu cách (để không cắt nát số 10,000 hay 4.8)
            var rawChunks = Regex.Split(cleanText, @"(?<=[.,?!;:])\s+");

            // 3. Lọc bỏ các đoạn rỗng
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
            try
            {
                while (_currentSentenceIndex < _ttsSentences.Count)
                {
                    if (IsPaused) return;

                    string sentence = _ttsSentences[_currentSentenceIndex];
                    var settings = new SpeechOptions { Locale = _cachedLocale };

                    await TextToSpeech.Default.SpeakAsync(sentence, settings, cancelToken: _cts.Token);

                    if (!IsPaused && !_cts.IsCancellationRequested)
                    {
                        _currentSentenceIndex++;
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