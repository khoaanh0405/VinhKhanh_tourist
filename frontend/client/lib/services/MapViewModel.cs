using client.lib.model;
using client.lib.services;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

public partial class MapViewModel : ObservableObject
{
    private readonly ApiService _apiService;

    [ObservableProperty]
    private ObservableCollection<POI> _pois = new();

    public MapViewModel(ApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task LoadPOIsAsync()
    {
        var data = await _apiService.FetchPOIsAsync();
        Pois = new ObservableCollection<POI>(data);
    }
}