using System;
using System.IO;

namespace EduShop.Core.Infrastructure;

public static class AppPaths
{
    public static string GetDefaultDbPath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dbDir = Path.Combine(localAppData, "EduShop");
        Directory.CreateDirectory(dbDir);

        return Path.Combine(dbDir, "edushop.db");
    }
}
