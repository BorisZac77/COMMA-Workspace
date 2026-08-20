using System.IO;

namespace COMMA.App.Models;

public class DrawingFile
{
    public string Name { get; set; } = "";

    public string FullPath { get; set; } = "";

    public string View { get; set; } = "";

    public bool IsFront { get; set; }

    public bool IsBack { get; set; }

    public bool IsLeft { get; set; }

    public bool IsRight { get; set; }

    public bool MirrorHorizontally { get; set; }

    public string FileName => Path.GetFileName(FullPath);

    public override string ToString()
    {
        if (string.IsNullOrWhiteSpace(View))
            return Name;

        return $"{View} - {Name}";
    }
}