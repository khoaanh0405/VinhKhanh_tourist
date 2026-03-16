namespace client.lib.widgets;

public partial class RestaurantCardView : ContentView
{
    public RestaurantCardView()
    {
        InitializeComponent();
    }

    private void OnRestaurantTapped(object sender, EventArgs e)
    {
        // Trong MAUI, dùng Navigation.PushAsync hoặc CommunityToolkit BottomSheet
        // Ví dụ: 
        // Navigation.PushModalAsync(new RestaurantDetailsPage(this.BindingContext as Restaurant));
    }
}