using client.lib.model;
using client.lib.services;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Controls.Shapes;

namespace client.lib.screens.home;

public partial class DetailScreen : ContentPage
{
    private POI _poi;
    private readonly AppViewModel _appViewModel;

    public DetailScreen(POI poi)
    {
        InitializeComponent();
        _poi = poi;
        BindingContext = _poi;

        // Kéo AppViewModel (Singleton) từ hệ thống ra để dùng ké dữ liệu Yêu thích
        _appViewModel = Application.Current.Handler.MauiContext.Services.GetService<AppViewModel>();

        LoadData();
        UpdateFavoriteButtonState(); // Cập nhật nút bấm lúc vừa mở trang
    }

    private void LoadData()
    {
        if (_poi == null) return;

        LblName.Text = _poi.Name;

        // Lấy text từ Narration đầu tiên
        var defaultNarration = _poi.Narrations?.FirstOrDefault();
        LblDescription.Text = defaultNarration?.Text ?? "Chạm để khám phá thêm về địa điểm này.";

        // -----------------------------------------------------
        // 1. SỬA LỖI HÌNH ẢNH: Ép CarouselView nhận dữ liệu
        // -----------------------------------------------------
        if (_poi.ImageUrls != null && _poi.ImageUrls.Any())
        {
            ImgCarousel.ItemsSource = _poi.ImageUrls;
        }
        else
        {
            // Hình mặc định nếu quán chưa có ảnh
            ImgCarousel.ItemsSource = new List<string> { "placeholder_img.webp" };
        }

        // -----------------------------------------------------
        // 2. XỬ LÝ THỰC ĐƠN
        // -----------------------------------------------------
        ListFoodsContainer.Children.Clear();

        if (_poi.Restaurants != null && _poi.Restaurants.Any())
        {
            var allFoods = _poi.Restaurants.SelectMany(r => r.Foods ?? new List<Food>()).ToList();

            if (allFoods.Any())
            {
                foreach (var food in allFoods)
                {
                    ListFoodsContainer.Children.Add(CreateFoodItemView(food));
                }
            }
            else
            {
                ListFoodsContainer.Children.Add(new Label { Text = "Đang cập nhật thực đơn...", TextColor = Colors.Gray, FontAttributes = FontAttributes.Italic });
            }
        }
        else
        {
            ListFoodsContainer.Children.Add(new Label { Text = "Chưa có thông tin thực đơn.", TextColor = Colors.Gray, FontAttributes = FontAttributes.Italic });
        }
        LoadReviews();
    }

    private View CreateFoodItemView(Food food)
    {
        var frame = new Frame
        {
            CornerRadius = 12,
            Padding = 15,
            BackgroundColor = Color.FromArgb("#F8F9FA"),
            HasShadow = false,
            BorderColor = Color.FromArgb("#E0E0E0"),
            Margin = new Thickness(0, 0, 0, 10)
        };

        var grid = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Star }, new ColumnDefinition { Width = GridLength.Auto } },
            RowDefinitions = { new RowDefinition { Height = GridLength.Auto }, new RowDefinition { Height = GridLength.Auto } }
        };

        var lblName = new Label { Text = food.Name, FontAttributes = FontAttributes.Bold, FontSize = 16, TextColor = Color.FromArgb("#333333") };
        var lblPrice = new Label { Text = $"{food.Price:N0} đ", FontAttributes = FontAttributes.Bold, FontSize = 16, TextColor = Color.FromArgb("#E74C3C") };
        var lblDesc = new Label { Text = food.Description, FontSize = 13, TextColor = Color.FromArgb("#666666"), Margin = new Thickness(0, 5, 0, 0) };

        grid.Add(lblName, 0, 0);
        grid.Add(lblPrice, 1, 0);
        grid.Add(lblDesc, 0, 1);
        Grid.SetColumnSpan(lblDesc, 2);

        frame.Content = grid;
        return frame;
    }

    private void UpdateFavoriteButtonState()
    {
        if (_appViewModel != null && _appViewModel.IsPOIFavorite(_poi.PoiId))
        {
            BtnFavorite.Text = "💔 Bỏ yêu thích";
            BtnFavorite.BackgroundColor = Color.FromArgb("#636E72"); // Đổi màu xám
        }
        else
        {
            BtnFavorite.Text = "❤️ Thêm vào yêu thích";
            BtnFavorite.BackgroundColor = Color.FromArgb("#FF4757"); // Màu đỏ
        }
    }

    private async void OnFavoriteClicked(object sender, EventArgs e)
    {
        if (_appViewModel == null) return;

        // Lưu hoặc Xóa khỏi danh sách
        _appViewModel.TogglePOIFavorite(_poi);
        UpdateFavoriteButtonState();

        // Tạo thông báo Toast mượt mà
        string message = _appViewModel.IsPOIFavorite(_poi.PoiId)
            ? $"Đã lưu {_poi.Name} vào yêu thích"
            : $"Đã xóa {_poi.Name} khỏi yêu thích";

        var toast = Toast.Make(message, ToastDuration.Short, 14);
        await toast.Show();
    }

    // ==========================================
    // PHẦN XỬ LÝ ĐÁNH GIÁ VÀ BÌNH LUẬN (SỬA MỚI)
    // ==========================================

    private int _selectedRating = 0; // Biến lưu số sao người dùng chọn

    private void LoadReviews()
    {
        ListReviewsContainer.Children.Clear();

        // [MOCK DATA] Tạo dữ liệu giả để demo hiển thị
        var mockReviews = new List<Review>
        {
            new Review { UserName = "Tuấn Anh", Rating = 5, Comment = "Ốc ở đây cực kỳ tươi ngon, nước chấm đậm đà!", CreatedAt = DateTime.Now.AddDays(-1) },
            new Review { UserName = "Mai Phương", Rating = 4, Comment = "Quán hơi đông, phục vụ hơi chậm nhưng đồ ăn bù lại.", CreatedAt = DateTime.Now.AddDays(-2) }
        };

        foreach (var review in mockReviews)
        {
            ListReviewsContainer.Children.Add(CreateReviewItemView(review));
        }
    }

    private View CreateReviewItemView(Review review)
    {
        // ... [Giữ nguyên hàm vẽ giao diện bình luận cũ của bạn] ...
        var frame = new Frame { CornerRadius = 10, Padding = 12, HasShadow = false, BackgroundColor = Colors.White, BorderColor = Color.FromArgb("#E0E0E0"), Margin = new Thickness(0, 0, 0, 5) };
        var verticalStack = new VerticalStackLayout { Spacing = 4 };
        var headerGrid = new Grid { ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Star }, new ColumnDefinition { Width = GridLength.Auto } } };
        headerGrid.Add(new Label { Text = review.UserName, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#2D3436") }, 0, 0);
        string stars = new string('⭐', review.Rating);
        headerGrid.Add(new Label { Text = stars, FontSize = 12 }, 1, 0);
        verticalStack.Children.Add(headerGrid);
        verticalStack.Children.Add(new Label { Text = review.CreatedAt.ToString("dd/MM/yyyy"), FontSize = 11, TextColor = Colors.Gray });
        if (!string.IsNullOrEmpty(review.Comment)) { verticalStack.Children.Add(new Label { Text = review.Comment, TextColor = Color.FromArgb("#636E72"), Margin = new Thickness(0, 4, 0, 0) }); }
        frame.Content = verticalStack;
        return frame;
    }

    // Hàm để hiện khung nhập liệu
    private void OnShowReviewInputClicked(object sender, EventArgs e)
    {
        ReviewInputFrame.IsVisible = true;
        BtnShowReviewInput.IsVisible = false; // Ẩn nút bấm đi
    }

    // Hàm để ẩn khung nhập liệu và reset
    private void OnCancelReviewClicked(object sender, EventArgs e)
    {
        ResetReviewForm();
    }

    // Xử lý khi người dùng click vào ngôi sao
    private void OnStarTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is string param && int.TryParse(param, out int rating))
        {
            _selectedRating = rating;
            UpdateStarVisibility(rating);
        }
    }

    // Cập nhật giao diện sao (tô màu vàng hoặc xám)
    private void UpdateStarVisibility(int selectedRating)
    {
        Label[] stars = { Star1, Star2, Star3, Star4, Star5 };
        for (int i = 0; i < stars.Length; i++)
        {
            if (i < selectedRating)
            {
                stars[i].Text = "⭐"; // Sao đầy (vàng)
                stars[i].TextColor = Color.FromArgb("#FFD700");
            }
            else
            {
                stars[i].Text = "☆"; // Sao rỗng (xám)
                stars[i].TextColor = Colors.Gray;
            }
        }
    }

    // Xử lý khi bấm nút "Gửi đánh giá"
    private async void OnSubmitReviewClicked(object sender, EventArgs e)
    {
        // 1. Kiểm tra hợp lệ (Phải vote sao mới cho gửi)
        if (_selectedRating == 0)
        {
            await DisplayAlert("Thông báo", "Vui lòng chọn số sao đánh giá (từ 1 đến 5).", "OK");
            return;
        }

        string comment = CommentEditor.Text;

        // 2. Tạo đối tượng Review mới (Demo)
        var newReview = new Review
        {
            UserName = "Bạn (Demo)",
            Rating = _selectedRating,
            Comment = comment,
            CreatedAt = DateTime.Now
        };

        // 3. Chèn vào giao diện (Cho nổi lên đầu)
        ListReviewsContainer.Children.Insert(0, CreateReviewItemView(newReview));

        // 4. Thông báo và reset form
        ResetReviewForm();
        await DisplayAlert("Cảm ơn!", "Đánh giá của bạn đã được ghi nhận thành công.", "OK");

        // [GHI CHÚ] Sau này chỗ này sẽ gọi API để lưu vào Database.
        // Backend sẽ tự tính lại AverageRating của POI dựa trên bảng Reviews mới.
    }

    private void ResetReviewForm()
    {
        _selectedRating = 0;
        UpdateStarVisibility(0); // Reset sao về xám
        CommentEditor.Text = string.Empty;
        ReviewInputFrame.IsVisible = false;
        BtnShowReviewInput.IsVisible = true; // Hiện lại nút bấm
    }
}