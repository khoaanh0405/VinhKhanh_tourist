using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Text.RegularExpressions;

namespace client.lib.services
{
    public partial class AudioService : ObservableObject
    {
        // 🔥 ĐÃ XÓA: IAudioManager, IAudioPlayer, HttpClient logic

        [ObservableProperty] private bool _isSpeaking;
        [ObservableProperty] private bool _isPaused;
        [ObservableProperty] private string _currentTrackTitle = string.Empty;

        private CancellationTokenSource? _cts;
        private bool _isInitialized = false;
        private Guid _currentSpeakSessionId;

        private List<string> _ttsSentences = new();
        private int _currentSentenceIndex = 0;

        private Locale? _cachedLocale;
        private string _cachedLocaleLanguage = string.Empty;

        // Constructor trống, không cần inject thư viện bên ngoài nữa
        public AudioService() { }

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            try
            {
                var locales = await TextToSpeech.Default.GetLocalesAsync();
                _cachedLocale = locales.FirstOrDefault(l => l.Language.StartsWith("vi")) ?? locales.FirstOrDefault();
                _cachedLocaleLanguage = "vi";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AudioService] Init error: {ex.Message}");
            }
        }

        public async Task SpeakAsync(string text, string title, string languageCode = "vi")
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            Guid sessionId = Guid.NewGuid();
            _currentSpeakSessionId = sessionId;

            if (!_isInitialized) await InitializeAsync();

            await EnsureLocaleAsync(languageCode);

            if (_currentSpeakSessionId != sessionId)
            {
                return;
            }

            Stop();

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

            _cts = new CancellationTokenSource();
            await PlayTtsLoopAsync(_cts);
        }

        // Đổi chữ ký hàm để nhận token từ bên ngoài
        private async Task PlayTtsLoopAsync(CancellationTokenSource localCts)
        {
            try
            {
                while (_currentSentenceIndex < _ttsSentences.Count)
                {
                    if (IsPaused || localCts.IsCancellationRequested) return;

                    string sentence = _ttsSentences[_currentSentenceIndex];

                    var settings = new SpeechOptions { Locale = _cachedLocale };

                    // Dùng token đã được truyền vào
                    await TextToSpeech.Default.SpeakAsync(sentence, settings, cancelToken: localCts.Token);

                    // Kiểm tra token lại một lần nữa trước khi qua câu tiếp theo
                    if (!IsPaused && !localCts.IsCancellationRequested)
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

            // Nếu đọc xong câu cuối mà không bị hủy
            if (_currentSentenceIndex >= _ttsSentences.Count && !IsPaused && !localCts.IsCancellationRequested)
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
            }
            catch (Exception)
            {
                _cachedLocaleLanguage = languageCode;
            }
        }

        [RelayCommand]
        public void TogglePause()
        {
            IsPaused = !IsPaused;

            if (IsPaused)
            {
                _cts?.Cancel();
            }
            else
            {
                // Khi phát tiếp, tạo Token mới và truyền vào
                _cts = new CancellationTokenSource();
                _ = PlayTtsLoopAsync(_cts);
            }
        }

        [RelayCommand]
        public void Stop()
        {
            IsPaused = false;
            IsSpeaking = false;

            // Lưu tham chiếu cục bộ để tránh bị thread khác cướp quyền (Race Condition)
            var currentCts = _cts;
            _cts = null; // Gán null lập tức để khóa cổng

            if (currentCts != null)
            {
                try
                {
                    currentCts.Cancel();
                    currentCts.Dispose();
                }
                catch { /* Bỏ qua lỗi rác nếu có */ }
            }

            _currentSentenceIndex = _ttsSentences.Count;
        }
    }
}