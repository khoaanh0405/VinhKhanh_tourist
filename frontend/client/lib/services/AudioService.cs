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

        [ObservableProperty] private bool _isSpeaking;
        [ObservableProperty] private bool _isPaused;
        [ObservableProperty] private string _currentTrackTitle = string.Empty;

        private IAudioPlayer? _audioPlayer;
        private CancellationTokenSource? _cts;

        private bool _isInitialized = false;
        private bool _isUsingTts = false;

        private List<string> _ttsSentences = new();
        private int _currentSentenceIndex = 0;

        private Locale? _cachedLocale;
        private string _cachedLocaleLanguage = string.Empty;

        public AudioService(IAudioManager audioManager)
        {
            _audioManager = audioManager;
        }
        public async Task InitializeAsync()
        {
            if (_isInitialized) return;
            _isInitialized = true; // 🔥 FIX: Set TRƯỚC để tránh loop

            try
            {
                var locales = await TextToSpeech.Default.GetLocalesAsync();
                _cachedLocale = locales.FirstOrDefault(l => l.Language.StartsWith("vi")) ?? locales.FirstOrDefault();
                _cachedLocaleLanguage = "vi";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AudioService] Init error (sẽ dùng default locale): {ex.Message}");
                // _cachedLocale = null → TTS sẽ dùng locale mặc định của thiết bị
            }
        }

        public async Task<bool> PlayAudioFromUrlAsync(string url, string title)
        {
            try
            {
                Stop();
                _isUsingTts = false;
                CurrentTrackTitle = title;
                IsSpeaking = true;
                IsPaused = false;

                Stream audioStream;

                if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    // ═══════════════════════════════════════════════════════════════
                    // 🔥 FIX #12: GIẢM TIMEOUT HTTP XUỐNG 3 GIÂY
                    // ═══════════════════════════════════════════════════════════════
                    // NGUYÊN NHÂN LỖI CŨ:
                    //   - Timeout = 5s, nhưng khi server OFF mà internet ON:
                    //     DNS resolve thành công → TCP connect HANG → đợi đủ 5s mới timeout
                    //   - User phải đợi 5s mới nghe được TTS fallback
                    //
                    // FIX MỚI:
                    //   - Giảm xuống 3s để phản hồi nhanh hơn
                    //   - Kết hợp với kiểm tra mạng ở PlayNarrationAsync → HTTP call chỉ xảy ra khi có mạng
                    // ═══════════════════════════════════════════════════════════════
                    using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
                    audioStream = await Task.Run(() => httpClient.GetStreamAsync(url));
                }
                else
                {
                    if (!System.IO.File.Exists(url))
                    {
                        System.Diagnostics.Debug.WriteLine($"[AudioService] File không tồn tại: {url}");
                        MainThread.BeginInvokeOnMainThread(() => IsSpeaking = false);
                        return false;
                    }
                    audioStream = File.OpenRead(url);
                }

                _audioPlayer = _audioManager.CreatePlayer(audioStream);

                _audioPlayer.PlaybackEnded += (s, e) =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (!IsPaused) IsSpeaking = false;
                    });
                };

                _audioPlayer.Play();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AudioService] ❌ PlayUrl error: {ex.Message}");
                MainThread.BeginInvokeOnMainThread(() => IsSpeaking = false);
                return false;
            }
        }

        public async Task SpeakAsync(string text, string title, string languageCode = "vi")
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            if (!_isInitialized) await InitializeAsync();

            Stop();

            await EnsureLocaleAsync(languageCode);

            _isUsingTts = true;
            CurrentTrackTitle = title;
            IsSpeaking = true;
            IsPaused = false;

            string cleanText = text
                .Replace("\r\n", " ")
                .Replace("\r", " ")
                .Replace("\n", " ");

            var rawChunks = Regex.Split(cleanText, @"(?<=[.,?!;:])\s+");

            _ttsSentences = rawChunks
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .ToList();

            _currentSentenceIndex = 0;

            System.Diagnostics.Debug.WriteLine(
                $"[AudioService] 🔊 TTS bắt đầu: title='{title}' | lang='{languageCode}' | {_ttsSentences.Count} câu");

            await PlayTtsLoopAsync();
        }

        private async Task PlayTtsLoopAsync()
        {
            _cts = new CancellationTokenSource();
            var localCts = _cts;

            try
            {
                while (_currentSentenceIndex < _ttsSentences.Count)
                {
                    if (IsPaused || localCts.IsCancellationRequested) return;

                    string sentence = _ttsSentences[_currentSentenceIndex];

                    // ═══════════════════════════════════════════════════════════════
                    // 🔥 FIX #13: TTS SPEECHOPTIONS NULL-SAFE
                    // ═══════════════════════════════════════════════════════════════
                    // Nếu _cachedLocale = null (do InitializeAsync fail)
                    // → SpeechOptions với Locale = null → TTS dùng locale mặc định của thiết bị
                    // → Vẫn phát được tiếng (có thể không đúng ngôn ngữ nhưng KHÔNG crash)
                    // ═══════════════════════════════════════════════════════════════
                    var settings = new SpeechOptions { Locale = _cachedLocale };

                    await TextToSpeech.Default.SpeakAsync(sentence, settings, cancelToken: localCts.Token);

                    if (!IsPaused && _cts != null && !_cts.IsCancellationRequested)
                        _currentSentenceIndex++;
                    else
                        break;
                }
            }
            catch (TaskCanceledException) { /* Bình thường khi Stop/Pause */ }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AudioService] ❌ TTS error: {ex.Message}");
            }

            if (_currentSentenceIndex >= _ttsSentences.Count && !IsPaused)
            {
                MainThread.BeginInvokeOnMainThread(() => IsSpeaking = false);
            }
        }

        private async Task EnsureLocaleAsync(string languageCode)
        {
            if (string.Equals(_cachedLocaleLanguage, languageCode, StringComparison.OrdinalIgnoreCase))
                return;

            try
            {
                var locales = await TextToSpeech.Default.GetLocalesAsync();

                _cachedLocale = locales
                    .FirstOrDefault(l => l.Language.StartsWith(languageCode, StringComparison.OrdinalIgnoreCase))
                    ?? locales.FirstOrDefault();

                _cachedLocaleLanguage = languageCode;

                System.Diagnostics.Debug.WriteLine(
                    $"[AudioService] Locale set → {_cachedLocale?.Language ?? "default"} for langCode={languageCode}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AudioService] EnsureLocale error: {ex.Message}");
                // 🔥 FIX: Vẫn cập nhật _cachedLocaleLanguage để tránh gọi lại mỗi câu
                _cachedLocaleLanguage = languageCode;
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
                try
                {
                    if (_audioPlayer.IsPlaying) _audioPlayer.Stop();
                    _audioPlayer.Dispose();
                }
                catch { /* Bỏ qua lỗi dispose */ }
                finally
                {
                    _audioPlayer = null;
                }
            }
        }
    }
}