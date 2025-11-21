using System;
using System.IO;
using System.Text.Json;

namespace EduShop.WinForms;

public class AppSettings
{
    /// <summary>
    /// 만료 예정 계정 기준 (오늘 + N일 이내)
    /// </summary>
    public int ExpiringDays { get; set; } = 30;

    /// <summary>
    /// CSV 입출력 기본 인코딩 이름 (예: UTF-8, euc-kr)
    /// </summary>
    public string CsvEncodingName { get; set; } = "UTF-8";

    /// <summary>
    /// 견적서 PDF 기본 저장 폴더
    /// </summary>
    public string QuoteOutputFolder { get; set; } =
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
}

public static class AppSettingsManager
{
    private static readonly string SettingsFilePath =
        Path.Combine(AppContext.BaseDirectory, "edushop.settings.json");

    public static AppSettings Current { get; private set; } = new();

    public static void Load()
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
            {
                Current = new AppSettings();
                return;
            }

            var json = File.ReadAllText(SettingsFilePath);
            var loaded = JsonSerializer.Deserialize<AppSettings>(json);
            if (loaded != null)
                Current = loaded;
        }
        catch
        {
            // 파일이 깨져 있거나 하면 기본값으로 복구
            Current = new AppSettings();
        }
    }

    public static void Save()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(Current, options);
        File.WriteAllText(SettingsFilePath, json);
    }
}
