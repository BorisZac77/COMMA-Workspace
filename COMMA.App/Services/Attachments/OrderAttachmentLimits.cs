namespace COMMA.App.Services.Attachments;

public static class OrderAttachmentLimits
{
    public const int MaximumAttachmentCount = 25;
    public const long MaximumFileBytes = 50L * 1024 * 1024;
    public const long MaximumTotalBytes = 200L * 1024 * 1024;
    public const int MaximumPdfPagesPerFile = 200;
    public const int MaximumTotalPdfPages = 500;
    public const long MaximumImagePixels = 100_000_000;
}
