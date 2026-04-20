using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;

namespace client.lib.core;

public partial class LocalizationResourceManager : ObservableObject
{
    public static readonly LocalizationResourceManager Instance = new();
    private LocalizationResourceManager() { }

    private Dictionary<string, string> _translations = new(StringComparer.OrdinalIgnoreCase);

    public string this[string key]
        => _translations.TryGetValue(key, out var value) ? value : $"[{key}]";

    public void SetTranslations(Dictionary<string, string> translations)
    {
        if (translations != null)
        {
            _translations = new Dictionary<string, string>(translations, StringComparer.OrdinalIgnoreCase);
        }
        else
        {
            _translations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        // In ra log xem có lấy được bao nhiêu từ
        System.Diagnostics.Debug.WriteLine($"[LANG] Đã nạp thành công {_translations.Count} từ khóa UI.");

        OnPropertyChanged((string?)null);
    }

    public bool ContainsKey(string key) => _translations.ContainsKey(key);
}