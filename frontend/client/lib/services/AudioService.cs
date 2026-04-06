// ═══════════════════════════════════════════════════════════════════
//  FILE: AudioService.cs
//  Namespace: client.lib.services
//  Mô tả: Service quản lý toàn bộ vòng đời audio của ứng dụng.
//         Hỗ trợ: Stream URL, TTS đa ngôn ngữ (vi/en/ko), Pause/Resume.
//         Không phụ thuộc GPS.
// ═══════════════════════════════════════════════════════════════════

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Media;
using Plugin.Maui.Audio;
using System.Text.RegularExpressions;

namespace client.lib.services
{
    public partial class AudioService : ObservableObject
    {
        // ── Dependencies ──────────────────────────────────────────────
        private readonly IAudioManager _audioManager;

        // ── Observable properties (bindable từ XAML) ──────────────────

        [ObservableProperty] private bool _isSpeaking;
        [ObservableProperty] private bool _isPaused;
        [ObservableProperty] private string _currentTrackTitle = string.Empty;

        // ── Internal state ────────────────────────────────────────────
        private IAudioPlayer? _audioPlayer;
        private CancellationTokenSource? _cts;

        private bool _isInitialized = false;
        private bool _isUsingTts = false;

        // TTS sentence chunking
        private List<string> _ttsSentences = new();
        private int _currentSentenceIndex = 0;

        // Locale cache – được cập nhật mỗi khi SpeakAsync gọi với langCode mới
        private Locale? _cachedLocale;
        private string _cachedLocaleLanguage = string.Empty;

        // ── Constructor ───────────────────────────────────────────────
        public AudioService(IAudioManager audioManager)
        {
            _audioManager = audioManager;
        }

        // ── Init (gọi 1 lần khi app start) ───────────────────────────

        /// <summary>
        /// Khởi tạo locale mặc định (vi). Idempotent – an toàn khi gọi nhiều lần.
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isInitialized) return;
            try
            {
                var locales = await TextToSpeech.Default.GetLocalesAsync();
                _cachedLocale = locales.FirstOrDefault(l => l.Language.StartsWith("vi")) ?? locales.FirstOrDefault();
                _cachedLocaleLanguage = "vi";
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AudioService] Init error: {ex.Message}");
            }
        }

        // ── Play audio từ URL ─────────────────────────────────────────

        /// <summary>
        /// Stream audio từ URL (MP3/WAV). Dừng bất kỳ audio nào đang phát trước.
        /// </summary>
        public async Task PlayAudioFromUrlAsync(string url, string title)
        {
            try
            {
                Stop();
                _isUsingTts = false;
                CurrentTrackTitle = title;
                IsSpeaking = true;
                IsPaused = false;

                using var httpClient = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(30)
                };

                var audioStream = await httpClient.GetStreamAsync(url);
                _audioPlayer = _audioManager.CreatePlayer(audioStream);

                _audioPlayer.PlaybackEnded += (s, e) =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (!IsPaused) IsSpeaking = false;
                    });
                };

                _audioPlayer.Play();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AudioService] PlayUrl error: {ex.Message}");
                IsSpeaking = false;
            }
        }

        // ── TTS đa ngôn ngữ ───────────────────────────────────────────

        /// <summary>
        /// Đọc văn bản bằng TTS theo ngôn ngữ chỉ định.
        /// Text được tách thành các câu nhỏ để hỗ trợ Pause/Resume mượt mà.
        /// </summary>
        /// <param name="text">Nội dung cần đọc.</param>
        /// <param name="title">Tiêu đề track (hiển thị trên thanh Now Playing).</param>
        /// <param name="languageCode">
        ///   Mã ngôn ngữ BCP-47 (VD: "vi", "en", "ko").
        ///   Lấy từ Preferences["AppLanguage"]. Không hardcode.
        /// </param>
        public async Task SpeakAsync(string text, string title, string languageCode = "vi")
        {
            if (!_isInitialized) await InitializeAsync();

            Stop(); // Dừng audio cũ trước

            // ── Cập nhật locale nếu ngôn ngữ thay đổi ──────────────
            await EnsureLocaleAsync(languageCode);

            _isUsingTts = true;
            CurrentTrackTitle = title;
            IsSpeaking = true;
            IsPaused = false;

            // ── Tách văn bản thành câu ─────────────────────────────
            // Tách tại các dấu . , ? ! ; : theo sau là khoảng trắng
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

            await PlayTtsLoopAsync();
        }

        // ── TTS loop: xử lý từng câu, hỗ trợ Pause ──────────────────

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
                System.Diagnostics.Debug.WriteLine($"[AudioService] TTS error: {ex.Message}");
            }

            // Tự tắt IsSpeaking khi đọc xong toàn bộ
            if (_currentSentenceIndex >= _ttsSentences.Count && !IsPaused)
            {
                MainThread.BeginInvokeOnMainThread(() => IsSpeaking = false);
            }
        }

        // ── Locale helper ─────────────────────────────────────────────

        /// <summary>
        /// Cập nhật locale cache khi ngôn ngữ thay đổi.
        /// Cache lại để tránh gọi GetLocalesAsync mỗi câu.
        /// </summary>
        private async Task EnsureLocaleAsync(string languageCode)
        {
            // Nếu locale đã đúng ngôn ngữ → không cần query lại
            if (string.Equals(_cachedLocaleLanguage, languageCode, StringComparison.OrdinalIgnoreCase))
                return;

            try
            {
                var locales = await TextToSpeech.Default.GetLocalesAsync();

                // Tìm locale khớp chính xác prefix ngôn ngữ (VD: "ko-KR" khớp "ko")
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
            }
        }

        // ── Controls ──────────────────────────────────────────────────

        /// <summary>
        /// Toggle Pause/Resume. Hoạt động cho cả TTS lẫn audio file.
        /// </summary>
        [RelayCommand]
        public void TogglePause()
        {
            IsPaused = !IsPaused;

            if (_isUsingTts)
            {
                if (IsPaused)
                {
                    // Dừng TTS bằng cách cancel token
                    _cts?.Cancel();
                }
                else
                {
                    // Resume: tiếp tục từ câu đang dở
                    _ = PlayTtsLoopAsync();
                }
            }
            else if (_audioPlayer != null)
            {
                if (IsPaused) _audioPlayer.Pause();
                else _audioPlayer.Play();
            }
        }

        /// <summary>
        /// Dừng hoàn toàn và giải phóng tài nguyên.
        /// An toàn khi gọi nhiều lần.
        /// </summary>
        [RelayCommand]
        public void Stop()
        {
            IsPaused = false;
            IsSpeaking = false;

            // Cancel và dispose CancellationTokenSource
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }

            // Đẩy index về cuối để PlayTtsLoopAsync biết đã dừng
            _currentSentenceIndex = _ttsSentences.Count;

            // Dừng và dispose audio player (stream URL)
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