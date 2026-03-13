using client.lib.services;
using System.Linq; // Cần thiết để dùng lệnh Where, Any

namespace client.lib.screens;

public partial class FavoritesPage : ContentPage
{
    public FavoritesPage(AppViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    // Logic lọc dữ liệu siêu nhanh mỗi khi gõ phím
    private void OnFavoriteSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var viewModel = BindingContext as AppViewModel;
        if (viewModel == null || viewModel.FavoritePOIs == null) return;

        string query = e.NewTextValue?.ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(query))
        {
            // Nếu xóa trắng thanh tìm kiếm thì trả lại danh sách ban đầu
            FavoriteCollectionView.ItemsSource = viewModel.FavoritePOIs;
        }
        else
        {
            // Quét tên quán HOẶC tên món ăn có chứa từ khóa
            FavoriteCollectionView.ItemsSource = viewModel.FavoritePOIs.Where(p =>
                (p.Name != null && p.Name.ToLowerInvariant().Contains(query)) ||
                (p.Restaurants != null && p.Restaurants.Any(r =>
                    r.Foods != null && r.Foods.Any(f => f.Name != null && f.Name.ToLowerInvariant().Contains(query))
                ))
            ).ToList();
        }
    }
}