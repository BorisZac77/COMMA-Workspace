using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;

namespace COMMA.App.Services.Attachments;

public sealed class OrderAttachmentContentStore : IDisposable
{
    private const int SourceOpenAttemptCount = 3;
    private static readonly TimeSpan SourceOpenRetryDelay =
        TimeSpan.FromMilliseconds(75);

    private readonly string rootPath;
    private readonly Func<string, FileMode, FileAccess, FileShare, Stream>
        openSourceStream;
    private readonly Action<TimeSpan> waitBeforeRetry;
    private readonly Dictionary<Guid, string> paths = new();
    private bool disposed;

    public OrderAttachmentContentStore()
        : this(
            static (path, mode, access, share) =>
                new FileStream(path, mode, access, share),
            Thread.Sleep)
    {
    }

    internal OrderAttachmentContentStore(
        Func<string, FileMode, FileAccess, FileShare, Stream> openSourceStream,
        Action<TimeSpan> waitBeforeRetry)
    {
        ArgumentNullException.ThrowIfNull(openSourceStream);
        ArgumentNullException.ThrowIfNull(waitBeforeRetry);

        this.openSourceStream = openSourceStream;
        this.waitBeforeRetry = waitBeforeRetry;
        rootPath = Path.Combine(
            Path.GetTempPath(),
            $"comma-workspace-attachments-{Guid.NewGuid():N}");
        Directory.CreateDirectory(rootPath);
    }

    public StoredAttachmentContent ImportFile(
        Guid id,
        string sourcePath,
        string extension)
    {
        using var source = OpenSourceWithRetry(sourcePath);
        return ImportStream(id, source, extension);
    }

    public StoredAttachmentContent ImportStream(
        Guid id,
        Stream source,
        string extension)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(source);

        var normalizedExtension =
            OrderAttachmentValidator.NormalizeExtension(extension);
        var destinationPath = Path.Combine(
            rootPath,
            $"{id:N}{normalizedExtension}");
        var temporaryPath = destinationPath + ".part";

        Remove(id);

        try
        {
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            using var destination = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None);
            var buffer = new byte[81920];
            long length = 0;

            while (true)
            {
                var read = source.Read(buffer, 0, buffer.Length);
                if (read == 0)
                    break;

                length += read;
                if (length > OrderAttachmentLimits.MaximumFileBytes)
                {
                    throw new InvalidDataException(
                        "Załącznik przekracza maksymalny rozmiar 50 MB.");
                }

                destination.Write(buffer, 0, read);
                hash.AppendData(buffer, 0, read);
            }

            destination.Flush(flushToDisk: true);
            File.Move(temporaryPath, destinationPath);
            paths[id] = destinationPath;

            return new StoredAttachmentContent(
                length,
                Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant());
        }
        catch
        {
            TryDelete(temporaryPath);
            TryDelete(destinationPath);
            paths.Remove(id);
            throw;
        }
    }

    public Stream OpenRead(Guid id)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if (!paths.TryGetValue(id, out var path) || !File.Exists(path))
        {
            throw new InvalidDataException(
                "Brakuje oryginalnej zawartości załącznika.");
        }

        return new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);
    }

    public bool Contains(Guid id) =>
        !disposed &&
        paths.TryGetValue(id, out var path) &&
        File.Exists(path);

    public void Remove(Guid id)
    {
        if (!paths.Remove(id, out var path))
            return;

        TryDelete(path);
    }

    public void Clear()
    {
        foreach (var path in paths.Values.ToArray())
            TryDelete(path);

        paths.Clear();
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        Clear();

        try
        {
            if (Directory.Exists(rootPath))
                Directory.Delete(rootPath, recursive: true);
        }
        catch
        {
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
        }
    }

    private Stream OpenSourceWithRetry(string sourcePath)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return openSourceStream(
                    sourcePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete);
            }
            catch (IOException exception) when (
                IsFileSharingViolation(exception) &&
                attempt < SourceOpenAttemptCount)
            {
                waitBeforeRetry(SourceOpenRetryDelay);
            }
            catch (IOException exception) when (
                IsFileSharingViolation(exception))
            {
                throw new IOException(
                    $"Nie można odczytać pliku „{Path.GetFileName(sourcePath)}”, " +
                    "ponieważ jest używany przez inny program. Zamknij program " +
                    "korzystający z pliku i spróbuj ponownie.",
                    exception);
            }
        }
    }

    private static bool IsFileSharingViolation(IOException exception)
    {
        var errorCode = exception.HResult & 0xFFFF;
        return errorCode is 32 or 33;
    }
}

public readonly record struct StoredAttachmentContent(
    long Length,
    string Sha256);
