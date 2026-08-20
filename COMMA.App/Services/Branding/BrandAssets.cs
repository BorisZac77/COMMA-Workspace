using System;
using System.IO;
using Avalonia.Platform;

namespace COMMA.App.Services.Branding;

public static class BrandAssets
{
    public static readonly Uri CompanyLogoUri =
        new(
            "avares://COMMA.App/" +
            "Assets/Branding/PimpLogoExact.png");

    public static byte[] LoadCompanyLogo()
    {
        try
        {
            if (!AssetLoader.Exists(CompanyLogoUri))
                return Array.Empty<byte>();

            using var sourceStream =
                AssetLoader.Open(CompanyLogoUri);

            using var memoryStream =
                new MemoryStream();

            sourceStream.CopyTo(memoryStream);

            return memoryStream.ToArray();
        }
        catch
        {
            return Array.Empty<byte>();
        }
    }
}