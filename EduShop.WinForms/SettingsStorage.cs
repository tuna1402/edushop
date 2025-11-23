using System;
using System.IO;
using System.Text;
using System.Text.Json;
using EduShop.Core.Common;

namespace EduShop.WinForms;

public static class SettingsStorage
{
    private static readonly string ConfigFilePath =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "edushop.settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(ConfigFilePath))
                return new AppSettings();

            var json = File.ReadAllText(ConfigFilePath, Encoding.UTF8);
            var settings = JsonSerializer.Deserialize<AppSettings>(json);
            return settings ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public static void Save(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(
            settings,
            new JsonSerializerOptions { WriteIndented = true });

        File.WriteAllText(
            ConfigFilePath,
            json,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
}
