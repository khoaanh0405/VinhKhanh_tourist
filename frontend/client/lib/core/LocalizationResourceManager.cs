using System.ComponentModel;
using System.Globalization;
using client.Resources.String; // Trỏ đến file AppResources của bạn

namespace client.lib.core;

public class LocalizationResourceManager : INotifyPropertyChanged
{
    // Tạo Singleton để có thể truy cập từ mọi nơi
    public static LocalizationResourceManager Instance { get; } = new();

    // Indexer để lấy chuỗi dịch thuật từ file AppResources
    public string this[string resourceKey]
        => AppResources.ResourceManager.GetString(resourceKey, AppResources.Culture) ?? string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public void SetCulture(CultureInfo culture)
    {
        AppResources.Culture = culture;
        // Tham số null có nghĩa là: "Tất cả các thuộc tính đều đã thay đổi, hãy cập nhật lại toàn bộ UI"
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
    }
}