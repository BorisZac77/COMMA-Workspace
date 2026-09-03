using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace COMMA.App.Services.Attachments;

public sealed class WindowsAttachmentSourceStager
{
    private readonly Func<bool> isWindows;
    private readonly Action<string, string> shellCopy;
    private readonly Func<string> temporaryDirectory;

    public WindowsAttachmentSourceStager()
        : this(() => OperatingSystem.IsWindows(), CopyWithWindowsShell, Path.GetTempPath)
    {
    }

    public WindowsAttachmentSourceStager(Func<bool> isWindows, Action<string, string> shellCopy, Func<string> temporaryDirectory)
    {
        this.isWindows = isWindows;
        this.shellCopy = shellCopy;
        this.temporaryDirectory = temporaryDirectory;
    }

    public StagedAttachmentSources Stage(IReadOnlyList<string> sourcePaths)
    {
        if (!isWindows())
            return new StagedAttachmentSources(sourcePaths, []);

        var stagedPaths = new List<string>(sourcePaths.Count);
        try
        {
            foreach (var sourcePath in sourcePaths)
            {
                var stagedPath = CreateStagedPath(sourcePath);
                stagedPaths.Add(stagedPath);
                shellCopy(sourcePath, stagedPath);
            }

            return new StagedAttachmentSources(stagedPaths, stagedPaths);
        }
        catch
        {
            DeleteFiles(stagedPaths);
            throw;
        }
    }

    private string CreateStagedPath(string sourcePath)
    {
        var directory = Path.Combine(temporaryDirectory(), "COMMA Workspace", "attachment-staging");
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, $"{Guid.NewGuid():N}{Path.GetExtension(sourcePath)}");
    }

    private static void CopyWithWindowsShell(string sourcePath, string destinationPath)
    {
        var operation = new ShellFileOperation
        {
            Function = ShellFileOperation.Copy,
            From = sourcePath + '\0' + '\0',
            To = destinationPath + '\0' + '\0',
            Flags = ShellFileOperation.NoConfirmation | ShellFileOperation.NoErrorUi | ShellFileOperation.NoConfirmMakeDirectory
        };
        var result = SHFileOperation(ref operation);
        if (result != 0 || operation.AnyOperationsAborted)
            throw new IOException($"Windows Shell could not stage the attachment (error {result}).");
    }

    private static void DeleteFiles(IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            try { File.Delete(path); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHFileOperation(ref ShellFileOperation operation);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ShellFileOperation
    {
        public const uint Copy = 0x0002;
        public const ushort NoConfirmation = 0x0010;
        public const ushort NoErrorUi = 0x0400;
        public const ushort NoConfirmMakeDirectory = 0x0200;
        public IntPtr WindowHandle;
        public uint Function;
        public string From;
        public string To;
        public ushort Flags;
        [MarshalAs(UnmanagedType.Bool)] public bool AnyOperationsAborted;
        public IntPtr NameMappings;
        public string? ProgressTitle;
    }

    public sealed class StagedAttachmentSources : IDisposable
    {
        private readonly IReadOnlyList<string> stagedPaths;
        private bool disposed;

        internal StagedAttachmentSources(IReadOnlyList<string> paths, IReadOnlyList<string> stagedPaths)
        {
            Paths = paths;
            this.stagedPaths = stagedPaths;
        }

        public IReadOnlyList<string> Paths { get; }

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            DeleteFiles(stagedPaths);
        }
    }
}
