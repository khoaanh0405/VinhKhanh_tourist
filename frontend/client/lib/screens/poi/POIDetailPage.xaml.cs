using client.lib.core;
using client.lib.model;
using client.lib.services;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace client.lib.screens.poi;

public partial class POIDetailPage : ContentPage
{
    private POI? _poi;
    private readonly int _poiId;
    private readonly bool _autoPlayAudio;
    private bool _audioStarted = false;

    private readonly ApiService _apiService;
    private readonly AudioService _audioService;
    private readonly LocalDbService _localDbService;
    private readonly AppViewModel? _appViewModel;
    private int _selectedRating = 0;

    public POIDetailPage(int poiId, bool autoPlayAudio = false)
    {
        InitializeComponent();

        _poiId = poiId;
        _autoPlayAudio = autoPlayAudio;

        var services = Application.Current!.Handler!.MauiContext!.Services;
        _apiService = services.GetRequiredService<ApiService>();
        _audioService = services.GetRequiredService<AudioService>();
        _localDbService = services.GetRequiredService<LocalDbService>();

        // Lấy AppViewModel
        _appViewModel = services.GetService<AppViewModel>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadPoiAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (_audioService.IsSpeaking)
            _audioService.Stop();
    }

    private async Task LoadPoiAsync()
    {
        string langCode = GetCurrentLanguageCode();
        SetLoadingState(true);

        try
        {
            // =========================================================
            // 1. ƯU TIÊN GỌI SERVER TRƯỚC (Để lấy đúng ngôn ngữ hiện tại)
            // =========================================================
            if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
            {
                try
                {
                    _poi = await _apiService.FetchPOIByIdAsync(_poiId, langCode);
                    System.Diagnostics.Debug.WriteLine("[POIDetail] Đã load dữ liệu MỚI từ API Server.");
                }
                catch (Exception apiEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[POIDetail] Lỗi API, chuyển sang Offline: {apiEx.Message}");
                }
            }

            // =========================================================
            // 2. FALLBACK: NẾU MẤT MẠNG HOẶC API LỖI -> DÙNG SQLITE CŨ
            // =========================================================
            if (_poi == null)
            {
                var poiLocal = await _localDbService.GetPoiByIdAsync(_poiId);

                if (poiLocal != null)
                {
                    _poi = new POI
                    {
                        PoiId = poiLocal.PoiId,
                        Name = poiLocal.Name,
                        Description = poiLocal.Description,
                        Latitude = poiLocal.Latitude,
                        Longitude = poiLocal.Longitude,
                        AverageRating = poiLocal.AverageRating,
                        ReviewCount = poiLocal.ReviewCount,
                        ImageUrls = string.IsNullOrEmpty(poiLocal.ImageUrlsJoined)
                                    ? new List<string>()
                                    : poiLocal.ImageUrlsJoined.Split(',').ToList()
                    };

                    // Lấy danh sách quán ăn
                    var restaurantsLocal = await _localDbService.GetRestaurantsByPoiIdAsync(_poiId);
                    _poi.Restaurants = new List<Restaurant>();

                    foreach (var r in restaurantsLocal)
                    {
                        var newRest = new Restaurant
                        {
                            RestaurantId = r.RestaurantId,
                            Name = r.Name,
                            Address = r.Address
                        };

                        // Lấy món ăn cho từng quán
                        var foodsLocal = await _localDbService.GetFoodsByRestaurantIdAsync(r.RestaurantId);
                        newRest.Foods = foodsLocal.Select(f => new Food
                        {
                            FoodId = f.FoodId,
                            Name = f.Name,
                            Description = f.Description,
                            Price = f.Price
                        }).ToList();

                        _poi.Restaurants.Add(newRest);
                    }

                    // Lấy Narrations
                    var narrationsLocal = await _localDbService.GetNarrationsByPoiIdAsync(_poiId);
                    _poi.Narrations = narrationsLocal.Select(n => new Narration
                    {
                        NarrationId = n.NarrationId,
                        LanguageCode = n.LanguageCode,
                        Text = n.Text,
                        UseAudioFile = n.UseAudioFile,
                        AudioUrl = n.AudioUrl
                    }).ToList();

                    System.Diagnostics.Debug.WriteLine("[POIDetail] Đã load dữ liệu dự phòng từ SQLite (Offline).");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[POIDetail] Load error: {ex.Message}");
        }

        SetLoadingState(false);

        // 3. Xử lý UI nếu tịt đường (không có cả mạng lẫn offline)
        if (_poi is null)
        {
            await DisplayAlert("Lỗi", "Không thể tải thông tin địa điểm này. Vui lòng kiểm tra lại kết nối mạng.", "OK");
            await Navigation.PopAsync();
            return;
        }

        // 4. Render UI
        RenderPoiData(_poi);
        UpdateAudioButtonState();

        LoadReviews();

        if (_autoPlayAudio && !_audioStarted)
        {
            _audioStarted = true;
            OnMainAudioButtonClicked(this, EventArgs.Empty);
        }
    }

    private async Task PlayNarrationAsync(POI poi, string langCode)
    {
        if (poi.Narrations is null || !poi.Narrations.Any())
        {
            await _audioService.SpeakAsync(text: poi.Name, title: poi.Name, languageCode: langCode);
            return;
        }

        var narration = poi.Narrations
            .FirstOrDefault(n => string.Equals(n.LanguageCode, langCode, StringComparison.OrdinalIgnoreCase))
            ?? poi.Narrations.First();

        if (narration.UseAudioFile && !string.IsNullOrWhiteSpace(narration.AudioUrl))
        {
            await _audioService.PlayAudioFromUrlAsync(narration.AudioUrl, poi.Name);
        }
        else if (!string.IsNullOrWhiteSpace(narration.Text))
        {
            await _audioService.SpeakAsync(text: narration.Text, title: poi.Name, languageCode: langCode);
        }
        else
        {
            await _audioService.SpeakAsync(text: poi.Name, title: poi.Name, languageCode: langCode);
        }
    }

    private static string GetCurrentLanguageCode() => Preferences.Get("AppLanguage", "vi");

    private void SetLoadingState(bool isLoading)
    {
        LoadingIndicator.IsVisible = isLoading;
        LoadingIndicator.IsRunning = isLoading;
        ContentScrollView.IsVisible = !isLoading;
    }

    // ══════════════════════════════════════════════════════════════
    // 🔥 NÂNG CẤP: Render đầy đủ thông tin (carousel, rating, foods)
    // ══════════════════════════════════════════════════════════════
    private void RenderPoiData(POI poi)
    {
        // Tên
        LblName.Text = poi.Name;

        // Mô tả
        LblDescription.Text = !string.IsNullOrWhiteSpace(poi.Description)
            ? poi.Description
            : LocalizationResourceManager.Instance["DetailDefaultDesc"];

        // Rating
        LblRating.Text = poi.AverageRating.ToString("F1");
        LblReviewCount.Text = poi.ReviewCount.ToString();

        // Carousel hình ảnh (thay vì 1 ảnh tĩnh)
        if (poi.ImageUrls != null && poi.ImageUrls.Any())
        {
            ImgCarousel.ItemsSource = poi.ImageUrls;
        }
        else
        {
            ImgCarousel.ItemsSource = new List<string> { "placeholder_img.webp" };
        }
        // 🔥 MỚI: Render danh sách món ăn đầy đủ
        RenderFoodsList(poi);
    }

    /// <summary>
    /// Render danh sách món ăn từ tất cả quán thuộc POI này
    /// </summary>
    private void RenderFoodsList(POI poi)
    {
        FoodsContainer.Children.Clear();

        if (poi.Restaurants == null || !poi.Restaurants.Any())
        {
            FoodsContainer.Children.Add(new Label
            {
                Text = "Chưa có thông tin thực đơn",
                TextColor = Colors.Gray,
                FontAttributes = FontAttributes.Italic,
                FontSize = 14
            });
            return;
        }

        var allFoods = poi.Restaurants
            .Where(r => r.Foods != null)
            .SelectMany(r => r.Foods)
            .ToList();

        if (!allFoods.Any())
        {
            FoodsContainer.Children.Add(new Label
            {
                Text = "Thực đơn đang được cập nhật...",
                TextColor = Colors.Gray,
                FontAttributes = FontAttributes.Italic,
                FontSize = 14
            });
            return;
        }

        foreach (var food in allFoods)
        {
            FoodsContainer.Children.Add(CreateFoodCard(food));
        }
    }

    /// <summary>
    /// Tạo card món ăn với thiết kế đồng bộ theme Teal Explorer
    /// </summary>
    private static View CreateFoodCard(Food food)
    {
        var border = new Border
        {
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 },
            StrokeThickness = 0,
            BackgroundColor = Colors.White,
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 0),
            Shadow = new Shadow
            {
                Brush = Brush.Black,
                Opacity = 0.05f,
                Radius = 8,
                Offset = new Point(0, 2)
            }
        };

        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto }
            }
        };

        // Tên món
        var lblName = new Label
        {
            Text = food.Name,
            FontAttributes = FontAttributes.Bold,
            FontSize = 15,
            TextColor = Color.FromArgb("#1A1A2E")
        };

        // Giá
        var priceBorder = new Border
        {
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
            StrokeThickness = 0,
            BackgroundColor = Color.FromArgb("#FFF0F0"),
            Padding = new Thickness(10, 4),
            VerticalOptions = LayoutOptions.Center
        };
        priceBorder.Content = new Label
        {
            Text = $"{food.Price:N0} đ",
            FontAttributes = FontAttributes.Bold,
            FontSize = 13,
            TextColor = Color.FromArgb("#E74C3C")
        };

        // Mô tả món
        var lblDesc = new Label
        {
            Text = food.Description,
            FontSize = 13,
            TextColor = Color.FromArgb("#636E72"),
            Margin = new Thickness(0, 6, 0, 0),
            LineHeight = 1.4
        };

        grid.Add(lblName, 0, 0);
        grid.Add(priceBorder, 1, 0);

        if (!string.IsNullOrWhiteSpace(food.Description))
        {
            grid.Add(lblDesc, 0, 1);
            Grid.SetColumnSpan(lblDesc, 2);
        }

        border.Content = grid;
        return border;
    }

    private void UpdateAudioButtonState()
    {
        if (_audioService.IsSpeaking)
        {
            if (_audioService.IsPaused)
            {
                LblPlayIcon.Text = "▶";
                LblAudioStatus.Text = LocalizationResourceManager.Instance["DetailAudioPaused"];
            }
            else
            {
                LblPlayIcon.Text = "⏸";
                LblAudioStatus.Text = LocalizationResourceManager.Instance["DetailAudioPlaying"];
            }
        }
        else
        {
            LblPlayIcon.Text = "▶";
            LblAudioStatus.Text = LocalizationResourceManager.Instance["DetailAudioReady"];
        }
    }

    private async void OnMainAudioButtonClicked(object sender, EventArgs e)
    {
        if (_poi is null) return;

        if (_audioService.IsSpeaking)
        {
            _audioService.TogglePause();
            UpdateAudioButtonState();
        }
        else
        {
            string langCode = GetCurrentLanguageCode();

            LblPlayIcon.Text = "⏸";
            LblAudioStatus.Text = LocalizationResourceManager.Instance["DetailAudioPreparing"];

            _ = Task.Run(async () =>
            {
                await PlayNarrationAsync(_poi, langCode);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    UpdateAudioButtonState();
                });
            });

            await Task.Delay(300);
            UpdateAudioButtonState();
        }
    }

    private void UpdateFavoriteButtonState()
    {
        if (_appViewModel != null && _poi != null && _appViewModel.IsPOIFavorite(_poi.PoiId))
        {
            BtnFavorite.Text = "💔 Bỏ yêu thích";
            BtnFavorite.BackgroundColor = Color.FromArgb("#636E72");
        }
        else
        {
            BtnFavorite.Text = "❤️ Thêm vào yêu thích";
            BtnFavorite.BackgroundColor = Color.FromArgb("#FF4757");
        }
    }

    private async void OnFavoriteClicked(object sender, EventArgs e)
    {
        bool isLoggedIn = Preferences.Get("IsLoggedIn", false);
        if (!isLoggedIn)
        {
            bool wantToLogin = await DisplayAlert("Yêu cầu đăng nhập", "Bạn cần đăng nhập để lưu địa điểm yêu thích.", "Đồng ý", "Để sau");
            if (wantToLogin) await Shell.Current.GoToAsync("LoginScreen");
            return;
        }

        if (_appViewModel == null || _poi == null) return;

        _appViewModel.TogglePOIFavorite(_poi);
        UpdateFavoriteButtonState();

        string message = _appViewModel.IsPOIFavorite(_poi.PoiId) ? $"Đã thêm {_poi.Name} vào yêu thích" : $"Đã bỏ {_poi.Name} khỏi yêu thích";
        await Toast.Make(message, ToastDuration.Short, 14).Show();
    }

    private void LoadReviews()
    {
        ListReviewsContainer.Children.Clear();
        // Dữ liệu giả lập (Sau này bạn gọi API để lấy data thật vào đây)
        var mockReviews = new List<Review>
        {
            new Review { UserName = "Tuấn Anh", Rating = 5, Comment = "Rất tuyệt vời, trải nghiệm rất tốt!", CreatedAt = DateTime.Now.AddDays(-1) },
            new Review { UserName = "Mai Phương", Rating = 4, Comment = "Đáng để thử khi đến Vĩnh Khánh.", CreatedAt = DateTime.Now.AddDays(-2) }
        };

        foreach (var review in mockReviews)
        {
            ListReviewsContainer.Children.Add(CreateReviewItemView(review));
        }
    }

    private View CreateReviewItemView(Review review)
    {
        // Thay Frame bằng Border để đồng bộ giao diện và tối ưu hiệu suất
        var border = new Border { StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 }, Padding = 12, BackgroundColor = Colors.White, StrokeThickness = 1, Stroke = Color.FromArgb("#E0E0E0"), Margin = new Thickness(0, 0, 0, 8) };
        var verticalStack = new VerticalStackLayout { Spacing = 4 };
        var headerGrid = new Grid { ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Star }, new ColumnDefinition { Width = GridLength.Auto } } };

        headerGrid.Add(new Label { Text = review.UserName, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#2D3436") }, 0, 0);
        headerGrid.Add(new Label { Text = new string('⭐', review.Rating), FontSize = 12 }, 1, 0);

        verticalStack.Children.Add(headerGrid);
        verticalStack.Children.Add(new Label { Text = review.CreatedAt.ToString("dd/MM/yyyy"), FontSize = 11, TextColor = Colors.Gray });

        if (!string.IsNullOrEmpty(review.Comment))
        {
            verticalStack.Children.Add(new Label { Text = review.Comment, TextColor = Color.FromArgb("#636E72"), Margin = new Thickness(0, 4, 0, 0) });
        }

        border.Content = verticalStack;
        return border;
    }

    private async void OnShowReviewInputClicked(object sender, EventArgs e)
    {
        bool isLoggedIn = Preferences.Get("IsLoggedIn", false);
        if (!isLoggedIn)
        {
            bool wantToLogin = await DisplayAlert("Yêu cầu đăng nhập", "Bạn cần đăng nhập để viết đánh giá.", "Đồng ý", "Để sau");
            if (wantToLogin) await Shell.Current.GoToAsync("LoginScreen");
            return;
        }

        ReviewInputFrame.IsVisible = true;
        BtnShowReviewInput.IsVisible = false;
    }

    private void OnCancelReviewClicked(object sender, EventArgs e) => ResetReviewForm();

    private void OnStarTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is string param && int.TryParse(param, out int rating))
        {
            _selectedRating = rating;
            UpdateStarVisibility(rating);
        }
    }

    private void UpdateStarVisibility(int selectedRating)
    {
        Label[] stars = { Star1, Star2, Star3, Star4, Star5 };
        for (int i = 0; i < stars.Length; i++)
        {
            stars[i].Text = i < selectedRating ? "⭐" : "☆";
            stars[i].TextColor = i < selectedRating ? Color.FromArgb("#FFD700") : Colors.Gray;
        }
    }

    private async void OnSubmitReviewClicked(object sender, EventArgs e)
    {
        if (_selectedRating == 0)
        {
            await DisplayAlert("Chú ý", "Vui lòng chọn số sao đánh giá.", "OK");
            return;
        }

        // 1. Lấy thông tin user thật từ Preferences (được lưu lúc đăng nhập thành công)
        int currentUserId = Preferences.Get("UserId", 0);
        string currentDisplayName = Preferences.Get("DisplayName", "Người dùng");

        // 2. Tạo đối tượng Review với ID thật
        var newReview = new Review
        {
            UserId = currentUserId, // Gắn ID thật vào đây để gửi lên API
            UserName = currentDisplayName, // Gắn tên để hiển thị ngay lập tức lên màn hình
            Rating = _selectedRating,
            Comment = CommentEditor.Text,
            CreatedAt = DateTime.Now
        };

        ListReviewsContainer.Children.Insert(0, CreateReviewItemView(newReview));
        ResetReviewForm();
        await DisplayAlert("Cảm ơn", "Đánh giá của bạn đã được gửi thành công!", "OK");
    }

    private void ResetReviewForm()
    {
        _selectedRating = 0;
        UpdateStarVisibility(0);
        CommentEditor.Text = string.Empty;
        ReviewInputFrame.IsVisible = false;
        BtnShowReviewInput.IsVisible = true;
    }

    private async void OnBackTapped(object sender, TappedEventArgs e)
    {
        // Lệnh PopAsync sẽ đóng trang hiện tại và quay về trang trước đó
        await Navigation.PopAsync();
    }
}