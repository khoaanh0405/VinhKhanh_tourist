using client.lib.model;
using client.lib.services;
using client.lib.core; // Thêm dòng này để gọi được đa ngôn ngữ
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

        _appViewModel = Application.Current.Handler.MauiContext.Services.GetService<AppViewModel>();

        LoadData();
        UpdateFavoriteButtonState();
    }

    private void LoadData()
    {
        if (_poi == null) return;

        LblName.Text = _poi.Name;

        LblDescription.Text = !string.IsNullOrEmpty(_poi.Description)
            ? _poi.Description
            : client.Resources.String.AppResources.DetailDefaultDesc;

        if (_poi.ImageUrls != null && _poi.ImageUrls.Any())
        {
            ImgCarousel.ItemsSource = _poi.ImageUrls;
        }
        else
        {
            ImgCarousel.ItemsSource = new List<string> { "placeholder_img.webp" };
        }

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
                ListFoodsContainer.Children.Add(new Label { Text = client.Resources.String.AppResources.DetailMenuUpdating, TextColor = Colors.Gray, FontAttributes = FontAttributes.Italic });
            }
        }
        else
        {
            ListFoodsContainer.Children.Add(new Label { Text = client.Resources.String.AppResources.DetailMenuEmpty, TextColor = Colors.Gray, FontAttributes = FontAttributes.Italic });
        }
        LoadReviews();
    }

    private View CreateFoodItemView(Food food)
    {
        var frame = new Frame { CornerRadius = 12, Padding = 15, BackgroundColor = Color.FromArgb("#F8F9FA"), HasShadow = false, BorderColor = Color.FromArgb("#E0E0E0"), Margin = new Thickness(0, 0, 0, 10) };
        var grid = new Grid { ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Star }, new ColumnDefinition { Width = GridLength.Auto } }, RowDefinitions = { new RowDefinition { Height = GridLength.Auto }, new RowDefinition { Height = GridLength.Auto } } };
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
            BtnFavorite.Text = client.Resources.String.AppResources.DetailRemoveFavorite; // Sửa
            BtnFavorite.BackgroundColor = Color.FromArgb("#636E72");
        }
        else
        {
            BtnFavorite.Text = client.Resources.String.AppResources.DetailAddFavorite; // Sửa
            BtnFavorite.BackgroundColor = Color.FromArgb("#FF4757");
        }
    }

    private async void OnFavoriteClicked(object sender, EventArgs e)
    {
        bool isLoggedIn = Preferences.Get("IsLoggedIn", false);
        if (!isLoggedIn)
        {
            bool wantToLogin = await DisplayAlert(
                LocalizationResourceManager.Instance["AlertLoginRequiredTitle"],
                LocalizationResourceManager.Instance["AlertLoginToFavorite"],
                LocalizationResourceManager.Instance["AlertAgree"],
                LocalizationResourceManager.Instance["AlertLater"]);

            if (wantToLogin) await Shell.Current.GoToAsync("LoginScreen");
            return;
        }

        if (_appViewModel == null) return;

        _appViewModel.TogglePOIFavorite(_poi);
        UpdateFavoriteButtonState();

        // Sử dụng string.Format để truyền tên địa điểm vào thông báo
        string msgFormat = _appViewModel.IsPOIFavorite(_poi.PoiId)
            ? LocalizationResourceManager.Instance["ToastAddedFavorite"]
            : LocalizationResourceManager.Instance["ToastRemovedFavorite"];

        string message = string.Format(msgFormat, _poi.Name);
        var toast = Toast.Make(message, ToastDuration.Short, 14);
        await toast.Show();
    }

    private int _selectedRating = 0;

    private void LoadReviews()
    {
        ListReviewsContainer.Children.Clear();
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

    private async void OnShowReviewInputClicked(object sender, EventArgs e)
    {
        bool isLoggedIn = Preferences.Get("IsLoggedIn", false);
        if (!isLoggedIn)
        {
            bool wantToLogin = await DisplayAlert(
                LocalizationResourceManager.Instance["AlertLoginRequiredTitle"],
                LocalizationResourceManager.Instance["AlertLoginToReview"],
                LocalizationResourceManager.Instance["AlertAgree"],
                LocalizationResourceManager.Instance["AlertLater"]);

            if (wantToLogin) await Shell.Current.GoToAsync("LoginScreen");
            return;
        }

        ReviewInputFrame.IsVisible = true;
        BtnShowReviewInput.IsVisible = false;
    }

    private void OnCancelReviewClicked(object sender, EventArgs e)
    {
        ResetReviewForm();
    }

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
            if (i < selectedRating)
            {
                stars[i].Text = "⭐";
                stars[i].TextColor = Color.FromArgb("#FFD700");
            }
            else
            {
                stars[i].Text = "☆";
                stars[i].TextColor = Colors.Gray;
            }
        }
    }

    private async void OnSubmitReviewClicked(object sender, EventArgs e)
    {
        if (_selectedRating == 0)
        {
            await DisplayAlert(
                LocalizationResourceManager.Instance["AlertNotice"],
                LocalizationResourceManager.Instance["AlertSelectStar"],
                LocalizationResourceManager.Instance["AlertOK"]);
            return;
        }

        string comment = CommentEditor.Text;

        var newReview = new Review
        {
            UserName = LocalizationResourceManager.Instance["DetailYouDemo"],
            Rating = _selectedRating,
            Comment = comment,
            CreatedAt = DateTime.Now
        };

        ListReviewsContainer.Children.Insert(0, CreateReviewItemView(newReview));
        ResetReviewForm();

        await DisplayAlert(
            LocalizationResourceManager.Instance["AlertThankYou"],
            LocalizationResourceManager.Instance["AlertReviewSuccess"],
            LocalizationResourceManager.Instance["AlertOK"]);
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
        await Navigation.PopAsync();
    }
}