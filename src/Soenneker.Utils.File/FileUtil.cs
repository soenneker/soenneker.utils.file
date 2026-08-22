using Microsoft.Extensions.Logging;
using Soenneker.Extensions.Stream;
using Soenneker.Extensions.String;
using Soenneker.Extensions.Task;
using Soenneker.Extensions.ValueTask;
using Soenneker.Utils.File.Abstract;
using Soenneker.Utils.MemoryStream.Abstract;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Utils.ExecutionContexts;

namespace Soenneker.Utils.File;

/// <inheritdoc cref="IFileUtil"/>
public sealed class FileUtil : IFileUtil
{
    private const int _copyBufferSize = 128 * 1024;
    private const int _textBufferSize = 16 * 1024;
    private const int _maxInitialLineCapacity = 4 * 1024;

    // Predictable UTF-8 without BOM; also avoids repeatedly touching Encoding.UTF8 (minor).
    private static readonly Encoding _utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);

    private readonly ILogger<FileUtil> _logger;
    private readonly IMemoryStreamUtil _memoryStreamUtil;

    public FileUtil(ILogger<FileUtil> logger, IMemoryStreamUtil memoryStreamUtil)
    {
        _logger = logger;
        _memoryStreamUtil = memoryStreamUtil;
    }

    public Task<string> Read(string path, bool log = true, CancellationToken cancellationToken = default)
    {
        if (log)
            _logger.LogDebug("{name} for {path}", nameof(Read), path);

        return System.IO.File.ReadAllTextAsync(path, cancellationToken);
    }

    public async ValueTask<string?> TryRead(string path, bool log = true, CancellationToken cancellationToken = default)
    {
        try
        {
            return await Read(path, log, cancellationToken)
                .NoSync();
        }
        catch (Exception e)
        {
            if (log)
                _logger.LogWarning(e, "Could not read file {path}", path);

            return null;
        }
    }

    public async ValueTask<List<string>> ReadAsLines(string path, bool log = true, CancellationToken ct = default)
    {
        if (log)
            _logger.LogDebug("{name} for {path}", nameof(ReadAsLines), path);

        // Keep the text buffers below the LOH threshold. The previous 128 KB buffer
        // caused both the byte and char buffers to be allocated on the LOH.
        using var reader = new StreamReader(path, _utf8NoBom, detectEncodingFromByteOrderMarks: true, bufferSize: _textBufferSize);
        int capacity = GetInitialLineCapacity(reader.BaseStream.Length, 48);
        var lines = new List<string>(capacity);

        while (await reader.ReadLineAsync(ct) is { } line)
            lines.Add(line);

        return lines;
    }

    public Task<byte[]> ReadToBytes(string path, bool log = true, CancellationToken cancellationToken = default)
    {
        if (log)
            _logger.LogDebug("{name} for {path}", nameof(ReadToBytes), path);

        return System.IO.File.ReadAllBytesAsync(path, cancellationToken);
    }

    public async ValueTask<System.IO.MemoryStream> ReadToMemoryStream(string path, bool log = true, CancellationToken cancellationToken = default)
    {
        if (log)
            _logger.LogDebug("{name} for {path}", nameof(ReadToMemoryStream), path);

        System.IO.MemoryStream ms = await _memoryStreamUtil.Get(cancellationToken)
                                                           .NoSync();

        // Never assume pooled streams are cleared.
        ms.Position = 0;
        ms.SetLength(0);

        await using var fs = new FileStream(path, new FileStreamOptions
        {
            Mode = FileMode.Open,
            Access = FileAccess.Read,
            Share = FileShare.Read,
            // CopyToAsync supplies its own pooled buffer, so a second FileStream
            // buffer only adds memory and an extra copy.
            BufferSize = 1,
            Options = FileOptions.Asynchronous | FileOptions.SequentialScan
        });

        if (fs.Length is > 0 and <= int.MaxValue)
        {
            var needed = (int)fs.Length;
            if (needed > ms.Capacity)
                ms.Capacity = needed;
        }

        await fs.CopyToAsync(ms, _copyBufferSize, cancellationToken)
                .NoSync();

        ms.ToStart();
        return ms;
    }

    public Task Write(string path, string content, bool log = true, CancellationToken cancellationToken = default)
    {
        if (log)
            _logger.LogDebug("{name} for {path}", nameof(Write), path);

        return System.IO.File.WriteAllTextAsync(path, content, _utf8NoBom, cancellationToken);
    }

    public async ValueTask Write(string path, Stream source, bool log = true, CancellationToken ct = default)
    {
        if (log)
            _logger.LogDebug("{name} for {path}", nameof(Write), path);

        var fso = new FileStreamOptions
        {
            Mode = FileMode.Create,
            Access = FileAccess.Write,
            Share = FileShare.None,
            BufferSize = 1,
            Options = FileOptions.Asynchronous | FileOptions.SequentialScan
        };

        if (source.CanSeek)
        {
            long remaining = source.Length - source.Position;
            if (remaining > 0)
                fso.PreallocationSize = remaining;
        }

        await using var dest = new FileStream(path, fso);
        await source.CopyToAsync(dest, _copyBufferSize, ct)
                    .NoSync();
    }

    public Task Write(string path, byte[] bytes, bool log = true, CancellationToken cancellationToken = default)
    {
        if (log)
            _logger.LogDebug("{name} for {path}", nameof(Write), path);

        return System.IO.File.WriteAllBytesAsync(path, bytes, cancellationToken);
    }

    public Task WriteAllLines(string path, IEnumerable<string> lines, bool log = true, CancellationToken cancellationToken = default)
    {
        if (log)
            _logger.LogDebug("{name} for {path}", nameof(WriteAllLines), path);

        return System.IO.File.WriteAllLinesAsync(path, lines, _utf8NoBom, cancellationToken);
    }

    public Task Append(string path, string content, bool log = true, CancellationToken cancellationToken = default)
    {
        if (log)
            _logger.LogDebug("{name} for {path}", nameof(Append), path);

        return System.IO.File.AppendAllTextAsync(path, content, _utf8NoBom, cancellationToken);
    }

    public Task Append(string path, IEnumerable<string> lines, bool log = true, CancellationToken cancellationToken = default)
    {
        if (log)
            _logger.LogDebug("{name} for {path}", nameof(Append), path);

        return System.IO.File.AppendAllLinesAsync(path, lines, _utf8NoBom, cancellationToken);
    }

    public async ValueTask Copy(string srcPath, string dstPath, bool log = true, CancellationToken ct = default)
    {
        if (log)
            _logger.LogDebug("{name} {src} -> {dst}", nameof(Copy), srcPath, dstPath);

        string? dir = Path.GetDirectoryName(dstPath);

        if (dir.HasContent())
            Directory.CreateDirectory(dir);

        await using var src = new FileStream(srcPath, new FileStreamOptions
        {
            Mode = FileMode.Open,
            Access = FileAccess.Read,
            Share = FileShare.Read,
            BufferSize = 1,
            Options = FileOptions.Asynchronous | FileOptions.SequentialScan
        });

        await using var dst = new FileStream(dstPath, new FileStreamOptions
        {
            Mode = FileMode.Create,
            Access = FileAccess.Write,
            Share = FileShare.None,
            BufferSize = 1,
            Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
            PreallocationSize = src.Length
        });

        await src.CopyToAsync(dst, _copyBufferSize, ct)
                 .NoSync();
    }

    public async ValueTask Move(string sourcePath, string destinationPath, bool log = true, CancellationToken cancellationToken = default)
    {
        if (log)
            _logger.LogDebug("{name} start from {source} to {dest} ...", nameof(Move), sourcePath, destinationPath);

        string? parent = Path.GetDirectoryName(destinationPath);
        if (parent.HasContent())
            Directory.CreateDirectory(parent);

        try
        {
            // Same-volume moves are metadata-only and avoid opening, buffering,
            // copying, and deleting the file. Overwrite matches the old copy path.
            await ExecutionContextUtil.RunInlineOrOffload(static s =>
                                      {
                                          (string source, string destination) = ((string Source, string Destination))s;
                                          System.IO.File.Move(source, destination, overwrite: true);
                                      }, (sourcePath, destinationPath), cancellationToken)
                                      .NoSync();
        }
        catch (IOException) when (System.IO.File.Exists(sourcePath))
        {
            // Some filesystems cannot perform the native move. Retain the previous
            // cross-volume behavior as a cancellable copy followed by a delete.
            await Copy(sourcePath, destinationPath, log: false, cancellationToken)
                .NoSync();
            await Delete(sourcePath, ignoreMissing: false, log: false, cancellationToken)
                .NoSync();
        }
    }

    public ValueTask Delete(string path, bool ignoreMissing = true, bool log = true, CancellationToken ct = default)
    {
        if (log)
            _logger.LogDebug("{name} start for {path} ...", nameof(Delete), path);

        // No closure: state passed in.
        return ExecutionContextUtil.RunInlineOrOffload(static s =>
        {
            (string p, bool ignore) = ((string Path, bool Ignore))s;

            // File.Delete is already a no-op for a missing file. Avoid a separate
            // metadata lookup for the overwhelmingly common ignore-missing path.
            if (ignore)
            {
                System.IO.File.Delete(p);
                return;
            }

            if (!System.IO.File.Exists(p))
                throw new FileNotFoundException("File not found", p);

            System.IO.File.Delete(p);
        }, (path, ignoreMissing), ct);
    }

    public ValueTask<bool> Exists(string path, CancellationToken ct = default) =>
        ExecutionContextUtil.RunInlineOrOffload(static s => System.IO.File.Exists(s), path, ct);

    public async ValueTask CopyRecursively(string sourceDir, string destinationDir, bool log = true, CancellationToken ct = default)
    {
        if (log)
            _logger.LogDebug("{name} {source} -> {dest}", nameof(CopyRecursively), sourceDir, destinationDir);

        var opts = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.ReparsePoint
        };

        IEnumerable<string> files = Directory.EnumerateFiles(sourceDir, "*", opts);

        int dop = GetCopyDegreeOfParallelism(sourceDir, destinationDir);

        await Parallel.ForEachAsync(files, new ParallelOptions { MaxDegreeOfParallelism = dop, CancellationToken = ct }, async (file, token) =>
                      {
                          string rel = Path.GetRelativePath(sourceDir, file);
                          string destFile = Path.Combine(destinationDir, rel);

                          string? parent = Path.GetDirectoryName(destFile);
                          if (parent.HasContent())
                              Directory.CreateDirectory(parent);

                          await Copy(file, destFile, log: false, token)
                              .NoSync();
                      })
                      .NoSync();
    }

    public ValueTask<long?> GetSize(string path, CancellationToken ct = default) =>
        ExecutionContextUtil.RunInlineOrOffload(static s =>
        {
            var fi = new FileInfo(s);
            return fi.Exists ? fi.Length : (long?)null;
        }, path, ct);

    public ValueTask<DateTimeOffset?> GetLastModified(string path, CancellationToken ct = default) =>
        ExecutionContextUtil.RunInlineOrOffload(static s =>
        {
            var fi = new FileInfo(s);
            return fi.Exists ? fi.LastWriteTimeUtc : (DateTimeOffset?)null;
        }, path, ct);

    public ValueTask<bool> DeleteIfExists(string path, bool log = true, CancellationToken cancellationToken = default)
    {
        if (log)
            _logger.LogDebug("{name} start for {path} …", nameof(DeleteIfExists), path);

        return ExecutionContextUtil.RunInlineOrOffload(static s =>
        {
            if (!System.IO.File.Exists(s))
                return false;

            System.IO.File.Delete(s);
            return true;
        }, path, cancellationToken);
    }

    public async ValueTask<bool> TryDeleteIfExists(string path, bool log = true, CancellationToken cancellationToken = default)
    {
        if (!await Exists(path, cancellationToken)
                .NoSync())
            return false;

        return await TryDelete(path, log, cancellationToken)
            .NoSync();
    }

    public ValueTask DeleteAll(string directory, bool log = true, CancellationToken ct = default)
    {
        if (log)
            _logger.LogDebug("{name} start for {directory} ...", nameof(DeleteAll), directory);

        return ExecutionContextUtil.RunInlineOrOffload(static s =>
        {
            (string dir, CancellationToken token) = ((string Directory, CancellationToken Token))s;

            foreach (string file in Directory.EnumerateFiles(dir))
            {
                token.ThrowIfCancellationRequested();
                System.IO.File.Delete(file);
            }
        }, (directory, ct), ct);
    }

    public async ValueTask<bool> TryDeleteAll(string directory, bool log = true, CancellationToken cancellationToken = default)
    {
        if (log)
            _logger.LogDebug("Trying to delete all files in {directory} ...", directory);

        try
        {
            await DeleteAll(directory, log: false, cancellationToken)
                .NoSync();
            return true;
        }
        catch (Exception ex)
        {
            if (log)
                _logger.LogError(ex, "Exception deleting all files in {directory}", directory);

            return false;
        }
    }

    public async ValueTask<bool> TryRemoveReadonlyAndArchiveAttributesFromAll(string directory, bool log = true, CancellationToken cancellationToken = default)
    {
        if (log)
            _logger.LogDebug("Trying to remove readonly/archive attributes from {directory} ...", directory);

        try
        {
            await ExecutionContextUtil.RunInlineOrOffload(static s =>
                                      {
                                          (string dir, CancellationToken token) = ((string Directory, CancellationToken Token))s;

                                          var opts = new EnumerationOptions
                                          {
                                              RecurseSubdirectories = true,
                                              IgnoreInaccessible = true,
                                              AttributesToSkip = FileAttributes.ReparsePoint
                                          };

                                          foreach (string file in Directory.EnumerateFiles(dir, "*", opts))
                                          {
                                              token.ThrowIfCancellationRequested();

                                              FileAttributes attrs = System.IO.File.GetAttributes(file);
                                              FileAttributes updated = attrs & ~(FileAttributes.ReadOnly | FileAttributes.Archive);

                                              if (updated != attrs)
                                                  System.IO.File.SetAttributes(file, updated);
                                          }
                                      }, (directory, cancellationToken), cancellationToken)
                                      .NoSync();

            return true;
        }
        catch (Exception ex)
        {
            if (log)
                _logger.LogError(ex, "Exception removing readonly/archive attributes in {directory}", directory);

            return false;
        }
    }

    public ValueTask RenameAllInDirectoryRecursively(string directory, string oldValue, string newValue, bool log = true, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(oldValue))
            throw new ArgumentException("oldValue must be non-empty", nameof(oldValue));

        if (log)
            _logger.LogDebug("{name} {old} -> {new} in {directory} ...", nameof(RenameAllInDirectoryRecursively), oldValue, newValue, directory);

        return ExecutionContextUtil.RunInlineOrOffload(static s =>
        {
            (string dir, string oldVal, string newVal, CancellationToken token) = ((string Directory, string OldValue, string NewValue, CancellationToken Token))s;

            var opts = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
                AttributesToSkip = FileAttributes.ReparsePoint
            };

            foreach (string file in Directory.EnumerateFiles(dir, "*", opts))
            {
                token.ThrowIfCancellationRequested();

                string fileName = Path.GetFileName(file);
                if (!fileName.Contains(oldVal, StringComparison.Ordinal))
                    continue;

                string newFileName = fileName.Replace(oldVal, newVal, StringComparison.Ordinal);
                string? parent = Path.GetDirectoryName(file);
                string dest = parent is null ? newFileName : Path.Combine(parent, newFileName);

                System.IO.File.Move(file, dest, overwrite: false);
            }

            var directories = new List<string>(Directory.EnumerateDirectories(dir, "*", opts));
            directories.Sort(static (a, b) => b.Length.CompareTo(a.Length));

            foreach (string subdir in directories)
            {
                token.ThrowIfCancellationRequested();

                string name = Path.GetFileName(subdir);
                if (!name.Contains(oldVal, StringComparison.Ordinal))
                    continue;

                string newName = name.Replace(oldVal, newVal, StringComparison.Ordinal);
                string? parent = Path.GetDirectoryName(subdir);
                string dest = parent is null ? newName : Path.Combine(parent, newName);

                Directory.Move(subdir, dest);
            }
        }, (directory, oldValue, newValue, ct), ct);
    }

    public ValueTask SetLastWriteTimeUtc(string path, DateTime dateTimeUtc, CancellationToken ct = default) =>
        ExecutionContextUtil.RunInlineOrOffload(static s =>
        {
            (string p, DateTime dt) = ((string Path, DateTime Dt))s;
            System.IO.File.SetLastWriteTimeUtc(p, dt);
        }, (path, dateTimeUtc), ct);

    public async ValueTask<bool> TryDelete(string path, bool log = true, CancellationToken cancellationToken = default)
    {
        if (log)
            _logger.LogDebug("Trying to delete {path} …", path);

        try
        {
            await Delete(path, ignoreMissing: true, log: false, cancellationToken)
                .NoSync();
            return true;
        }
        catch (Exception ex)
        {
            if (log)
                _logger.LogError(ex, "Exception deleting {path}", path);

            return false;
        }
    }

    public async ValueTask<HashSet<string>> ReadToHashSet(string path, IEqualityComparer<string>? comparer = null, bool trim = true, bool ignoreEmpty = true,
        bool log = true, CancellationToken cancellationToken = default)
    {
        if (log)
            _logger.LogDebug("{name} for {path}", nameof(ReadToHashSet), path);

        comparer ??= StringComparer.Ordinal;
        using var reader = new StreamReader(path, _utf8NoBom, detectEncodingFromByteOrderMarks: true, bufferSize: _textBufferSize);
        int capacity = GetInitialLineCapacity(reader.BaseStream.Length, 32);
        var set = new HashSet<string>(capacity, comparer);

        while (await reader.ReadLineAsync(cancellationToken)
                           .NoSync() is { } line)
        {
            if (trim)
                line = line.Trim();

            if (ignoreEmpty && line.Length == 0)
                continue;

            set.Add(line);
        }

        return set;
    }

    public ValueTask<DirectoryInfo> CreateDirectory(string path, CancellationToken ct = default) =>
        ExecutionContextUtil.RunInlineOrOffload(static s => Directory.CreateDirectory(s), path, ct);

    public async ValueTask<HashSet<string>?> TryReadToHashSet(string path, IEqualityComparer<string>? comparer = null, bool trim = true,
        bool ignoreEmpty = true, bool log = true, CancellationToken cancellationToken = default)
    {
        try
        {
            return await ReadToHashSet(path, comparer, trim, ignoreEmpty, log, cancellationToken)
                .NoSync();
        }
        catch (Exception ex)
        {
            if (log)
                _logger.LogWarning(ex, "Could not read file {path} to HashSet", path);

            return null;
        }
    }

    [Pure]
    public ValueTask<string[]> GetAllFileNamesInDirectoryRecursively(string directory, bool log = true, CancellationToken ct = default)
    {
        if (log)
            _logger.LogDebug("Getting all files from directory ({directory}) recursively...", directory);

        return ExecutionContextUtil.RunInlineOrOffload(static s =>
        {
            (string dir, CancellationToken token) = ((string Directory, CancellationToken Token))s;

            var opts = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
                AttributesToSkip = FileAttributes.ReparsePoint
            };

            string[] buffer = ArrayPool<string>.Shared.Rent(256);
            var count = 0;

            try
            {
                foreach (string file in Directory.EnumerateFiles(dir, "*", opts))
                {
                    token.ThrowIfCancellationRequested();

                    if (count == buffer.Length)
                    {
                        string[] larger = ArrayPool<string>.Shared.Rent(checked(buffer.Length * 2));
                        Array.Copy(buffer, larger, count);
                        ArrayPool<string>.Shared.Return(buffer, clearArray: true);
                        buffer = larger;
                    }

                    buffer[count++] = file;
                }

                if (count == 0)
                    return Array.Empty<string>();

                var result = new string[count];
                Array.Copy(buffer, result, count);
                return result;
            }
            finally
            {
                ArrayPool<string>.Shared.Return(buffer, clearArray: true);
            }
        }, (directory, ct), ct);
    }

    public ValueTask<List<FileInfo>> GetAllFileInfoInDirectoryRecursivelySafe(string directory, bool log = true, CancellationToken ct = default)
    {
        if (log)
            _logger.LogDebug("Getting all FileInfos in {directory} recursively...", directory);

        return ExecutionContextUtil.RunInlineOrOffload(static s =>
        {
            (string dir, CancellationToken token) = ((string Directory, CancellationToken Token))s;

            var list = new List<FileInfo>();

            try
            {
                var opts = new EnumerationOptions
                {
                    RecurseSubdirectories = true,
                    IgnoreInaccessible = true,
                    AttributesToSkip = FileAttributes.ReparsePoint
                };

                foreach (string file in Directory.EnumerateFiles(dir, "*", opts))
                {
                    token.ThrowIfCancellationRequested();
                    list.Add(new FileInfo(file));
                }
            }
            catch (Exception e) when (e is DirectoryNotFoundException or UnauthorizedAccessException or PathTooLongException)
            {
                // Can't log here (static delegate). Caller can log after await if desired.
            }

            return list;
        }, (directory, ct), ct);
    }

    public FileStream OpenRead(string path, bool log = true)
    {
        if (log)
            _logger.LogDebug("{name} for {path}", nameof(OpenRead), path);

        return new FileStream(path, new FileStreamOptions
        {
            Mode = FileMode.Open,
            Access = FileAccess.Read,
            Share = FileShare.Read,
            BufferSize = _copyBufferSize,
            Options = FileOptions.Asynchronous | FileOptions.SequentialScan
        });
    }

    public FileStream OpenWrite(string path, bool log = true)
    {
        if (log)
            _logger.LogDebug("{name} for {path}", nameof(OpenWrite), path);

        string? dir = Path.GetDirectoryName(path);

        if (dir.HasContent())
            Directory.CreateDirectory(dir);

        return new FileStream(path, new FileStreamOptions
        {
            Mode = FileMode.Create,
            Access = FileAccess.Write,
            Share = FileShare.None,
            BufferSize = _copyBufferSize,
            Options = FileOptions.Asynchronous | FileOptions.SequentialScan
        });
    }

    private static int GetInitialLineCapacity(long byteLength, int estimatedBytesPerLine)
    {
        if (byteLength <= 0)
            return 0;

        // A bounded hint avoids repeated growth for normal text files without
        // reserving hundreds of MB for a large file containing very few lines.
        return (int)Math.Min(_maxInitialLineCapacity, (byteLength / estimatedBytesPerLine) + 1);
    }

    private static int GetCopyDegreeOfParallelism(string sourceDirectory, string destinationDirectory)
    {
        try
        {
            DriveType sourceType = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(sourceDirectory))!).DriveType;
            DriveType destinationType = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(destinationDirectory))!).DriveType;

            if (sourceType is DriveType.Removable or DriveType.CDRom || destinationType is DriveType.Removable or DriveType.CDRom)
                return 1;

            if (sourceType == DriveType.Network || destinationType == DriveType.Network)
                return 2;
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or UnauthorizedAccessException)
        {
            // Unknown and virtual filesystems use the conservative default below.
        }

        return Math.Min(8, Math.Max(2, Environment.ProcessorCount / 2));
    }
}
