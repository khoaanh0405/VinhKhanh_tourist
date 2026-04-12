using client.lib.core; // FuzzySearchHelper
using client.lib.services;
using System.Linq;

namespace client.lib.screens;

public partial class FavoritesPage : ContentPage
{
    public FavoritesPage(AppViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    // ══════════════════════════════════════════════════════════════
    // 🔥 NÂNG CẤP: Fuzzy search cho Favorites
    //    - Hỗ trợ không dấu / có dấu
    //    - Partial match (một phần tên)
    //    - Tìm theo tên quán + tên món ăn
    //    - Sắp xếp kết quả theo độ liên quan
    // ══════════════════════════════════════════════════════════════
    private void OnFavoriteSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var viewModel = BindingContext as AppViewModel;
        if (viewModel == null || viewModel.FavoritePOIs == null) return;

        string query = e.NewTextValue?.Trim();

        if (string.IsNullOrWhiteSpace(query))
        {
            // Xóa trắng → trả lại danh sách ban đầu
            FavoriteCollectionView.ItemsSource = viewModel.FavoritePOIs;
        }
        else
        {
            string normalizedQuery = FuzzySearchHelper.NormalizeText(query);

            var scoredResults = viewModel.FavoritePOIs
                .Select(p => new
                {
                    Poi = p,
                    Score = FuzzySearchHelper.CalculateRelevanceScore(
                        name: p.Name,
                        description: p.Description,
                        foodNames: p.Restaurants?
                            .Where(r => r.Foods != null)
                            .SelectMany(r => r.Foods.Select(f => f.Name)),
                        normalizedQuery: normalizedQuery
                    )
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.Poi.AverageRating)
                .Select(x => x.Poi)
                .ToList();

            FavoriteCollectionView.ItemsSource = scoredResults;
        }
    }
}