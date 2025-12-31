using System;
using System.IO;
using System.Text;
using System.Text.Json;
using EduShop.Core.Infrastructure.Supabase;

namespace EduShop.WinForms.Infrastructure;

public static class LocalSettingsStore
{
    private static readonly string SettingsFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "EduShop",
        "settings.json");

    public static SupabaseConfig LoadSupabaseConfig()
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
                return new SupabaseConfig();

            var json = File.ReadAllText(SettingsFilePath, Encoding.UTF8);
            var config = JsonSerializer.Deserialize<SupabaseConfig>(json);
            return config ?? new SupabaseConfig();
        }
        catch
        {
            return new SupabaseConfig();
        }
    }

    public static void SaveSupabaseConfig(SupabaseConfig config)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsFilePath)!);

        var json = JsonSerializer.Serialize(
            config,
            new JsonSerializerOptions { WriteIndented = true });

        File.WriteAllText(
            SettingsFilePath,
            json,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
}
