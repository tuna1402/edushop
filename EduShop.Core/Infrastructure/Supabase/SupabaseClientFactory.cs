using Supabase;

namespace EduShop.Core.Infrastructure.Supabase;

public static class SupabaseClientFactory
{
    public static async Task<Client> CreateAsync(SupabaseConfig config, string? accessToken = null)
    {
        if (string.IsNullOrWhiteSpace(config.Url))
            throw new InvalidOperationException("Supabase URL이 비어 있습니다.");

        if (string.IsNullOrWhiteSpace(config.AnonKey))
            throw new InvalidOperationException("Supabase anon key가 비어 있습니다.");

        var options = new SupabaseOptions
        {
            AutoConnectRealtime = false,
            AutoRefreshToken = false
        };

        var client = new Client(config.Url, config.AnonKey, options);
        await client.InitializeAsync();

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            client.Auth.SetSession(accessToken, string.Empty);
        }

        return client;
    }
}
