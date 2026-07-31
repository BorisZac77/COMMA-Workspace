using System.Collections.Generic;
using System.IO;
using System.Linq;
using COMMA.App.Models;

namespace COMMA.App.Services;

public static class DrawingScanner
{
    public static List<DrawingFile> Scan(string productFolder)
    {
        if (!Directory.Exists(productFolder))
            return new();

        return Directory
            .GetFiles(productFolder)
            .Where(file =>
                file.EndsWith(".jpg", System.StringComparison.OrdinalIgnoreCase) ||
                file.EndsWith(".jpeg", System.StringComparison.OrdinalIgnoreCase) ||
                file.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
            .Select(file => new DrawingFile
            {
                Name = Path.GetFileName(file),
                FullPath = file
            })
            .OrderBy(file => file.Name)
            .ToList();
    }
}