using EduShop.Core.Infrastructure.Supabase;

namespace EduShop.WinForms;

public sealed class SupabaseSessionState
{
    public SupabaseSessionState(SupabaseConfig config)
    {
        Config = config;
    }

    public SupabaseConfig Config { get; private set; }
    public string? AccessToken { get; private set; }

    public bool IsEnabled => Config.Enabled;
    public bool HasAccessToken => !string.IsNullOrWhiteSpace(AccessToken);
    public bool IsSupabaseActive => IsEnabled && HasAccessToken;

    public void UpdateConfig(SupabaseConfig config)
    {
        Config = config;
        if (!config.Enabled)
            AccessToken = null;
    }

    public void SetAccessToken(string? accessToken)
    {
        AccessToken = accessToken;
    }
}
