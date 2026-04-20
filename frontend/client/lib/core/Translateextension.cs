namespace client.lib.core;
[ContentProperty(nameof(Key))]
public sealed class TranslateExtension : IMarkupExtension<BindingBase>
{
    // ── The translation key, e.g. "SettingsTitle" ─────────────────────
    public string Key { get; set; } = string.Empty;

    // ── Returns a live Binding, not a one-time value ──────────────────
    public BindingBase ProvideValue(IServiceProvider serviceProvider)
    {
        return new Binding
        {
            Mode = BindingMode.OneWay,
            Path = $"[{Key}]",                        // indexer syntax
            Source = LocalizationResourceManager.Instance
        };
    }

    // Explicit interface implementation required by IMarkupExtension
    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
        => ProvideValue(serviceProvider);
}