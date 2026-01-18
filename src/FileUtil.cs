using Microsoft.Extensions.Logging;
using Soenneker.Extensions.Stream;
using Soenneker.Extensions.String;
using Soenneker.Extensions.Task;
using Soenneker.Extensions.ValueTask;
using Soenneker.Utils.File.Abstract;
using Soenneker.Utils.MemoryStream.Abstract;
using System;
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
    private const int _defaultBuffer = 128 * 1024; // 128kB

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

        // Avoid File.Exists + new FileInfo double-hit.
        var fi = new FileInfo(path);
        long len = fi.Exists ? fi.Length : 0L;

        // Heuristic capacity to reduce list growth. Still just a heuristic.
        int capacity = len > 0 ? (int)Math.Min(int.MaxValue, (len / 48) + 16) : 0;
        List<string> lines = capacity > 0 ? new List<string>(capacity) : new List<string>();

        using var reader = new StreamReader(path, _utf8NoBom, detectEncodingFromByteOrderMarks: true, bufferSize: _defaultBuffer);

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

        var fi = new FileInfo(path);

        System.IO.MemoryStream ms = await _memoryStreamUtil.Get(cancellationToken)
                                                           .NoSync();

        // Never assume pooled streams are cleared.
        ms.Position = 0;
        ms.SetLength(0);

        if (fi.Exists && fi.Length is > 0 and <= int.MaxValue)
        {
            int needed = (int)fi.Length;
            if (needed > ms.Capacity)
                ms.Capacity = needed;
        }

        await using var fs = new FileStream(path, new FileStreamOptions
        {
            Mode = FileMode.Open,
            Access = FileAccess.Read,
            Share = FileShare.Read,
            BufferSize = _defaultBuffer,
            Options = FileOptions.Asynchronous | FileOptions.SequentialScan
        });

        await fs.CopyToAsync(ms, _defaultBuffer, cancellationToken)
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
            BufferSize = _defaultBuffer,
            Options = FileOptions.Asynchronous | FileOptions.SequentialScan
        };

        if (source.CanSeek)
        {
            long remaining = source.Length - source.Position;
            if (remaining > 0)
                fso.PreallocationSize = remaining;
        }

        await using var dest = new FileStream(path, fso);
        await source.CopyToAsync(dest, _defaultBuffer, ct)
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
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var srcInfo = new FileInfo(srcPath);

        await using var src = new FileStream(srcPath, new FileStreamOptions
        {
            Mode = FileMode.Open,
            Access = FileAccess.Read,
            Share = FileShare.Read,
            BufferSize = _defaultBuffer,
            Options = FileOptions.Asynchronous | FileOptions.SequentialScan
        });

        await using var dst = new FileStream(dstPath, new FileStreamOptions
        {
            Mode = FileMode.Create,
            Access = FileAccess.Write,
            Share = FileShare.None,
            BufferSize = _defaultBuffer,
            Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
            PreallocationSize = srcInfo.Exists ? Math.Min(srcInfo.Length, int.MaxValue) : 0
        });

        await src.CopyToAsync(dst, _defaultBuffer, ct)
                 .NoSync();
    }

    public async ValueTask Move(string sourcePath, string destinationPath, bool log = true, CancellationToken cancellationToken = default)
    {
        if (log)
            _logger.LogDebug("{name} start from {source} to {dest} ...", nameof(Move), sourcePath, destinationPath);

        await Copy(sourcePath, destinationPath, log, cancellationToken)
            .NoSync();
        await Delete(sourcePath, ignoreMissing: false, log: false, cancellationToken)
            .NoSync();
    }

    public ValueTask Delete(string path, bool ignoreMissing = true, bool log = true, CancellationToken ct = default)
    {
        if (log)
            _logger.LogDebug("{name} start for {path} ...", nameof(Delete), path);

        // No closure: state passed in.
        return ExecutionContextUtil.RunInlineOrOffload(static s =>
        {
            (string p, bool ignore) = ((string Path, bool Ignore))s!;
            if (!System.IO.File.Exists(p))
            {
                if (!ignore)
                    throw new FileNotFoundException("File not found", p);

                return;
            }

            System.IO.File.Delete(p);
        }, (path, ignoreMissing), ct);
    }

    public ValueTask<bool> Exists(string path, CancellationToken ct = default) =>
        ExecutionContextUtil.RunInlineOrOffload(static s => System.IO.File.Exists((string)s!), path, ct);

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

        int dop = Math.Min(8, Math.Max(2, Environment.ProcessorCount / 2));

        await Parallel.ForEachAsync(files, new ParallelOptions { MaxDegreeOfParallelism = dop, CancellationToken = ct }, async (file, token) =>
        {
            string rel = Path.GetRelativePath(sourceDir, file);
            string destFile = Path.Combine(destinationDir, rel);

            string? parent = Path.GetDirectoryName(destFile);
            if (parent.HasContent())
                Directory.CreateDirectory(parent);

            await Copy(file, destFile, log: false, token)
                .NoSync();
        });
    }

    public ValueTask<long?> GetSize(string path, CancellationToken ct = default) =>
        ExecutionContextUtil.RunInlineOrOffload(static s =>
        {
            var fi = new FileInfo((string)s!);
            return fi.Exists ? fi.Length : (long?)null;
        }, path, ct);

    public ValueTask<DateTimeOffset?> GetLastModified(string path, CancellationToken ct = default) =>
        ExecutionContextUtil.RunInlineOrOffload(static s =>
        {
            var fi = new FileInfo((string)s!);
            return fi.Exists ? fi.LastWriteTimeUtc : (DateTimeOffset?)null;
        }, path, ct);

    public async ValueTask<bool> DeleteIfExists(string path, bool log = true, CancellationToken cancellationToken = default)
    {
        if (!await Exists(path, cancellationToken)
                .NoSync())
            return false;

        if (log)
            _logger.LogDebug("{name} start for {path} …", nameof(DeleteIfExists), path);

        await Delete(path, ignoreMissing: false, log: false, cancellationToken)
            .NoSync();
        return true;
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
            (string dir, CancellationToken token) = ((string Directory, CancellationToken Token))s!;

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
                (string dir, CancellationToken token) = ((string Directory, CancellationToken Token))s!;

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

        var fi = new FileInfo(path);
        long len = fi.Exists ? fi.Length : 0L;

        int capacity = len > 0 ? (int)Math.Min(int.MaxValue, (len / 32) + 16) : 0;

        comparer ??= StringComparer.Ordinal;

        HashSet<string> set = capacity > 0 ? new HashSet<string>(capacity, comparer) : new HashSet<string>(comparer);

        using var reader = new StreamReader(path, _utf8NoBom, detectEncodingFromByteOrderMarks: true, bufferSize: _defaultBuffer);

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
        ExecutionContextUtil.RunInlineOrOffload(static s => Directory.CreateDirectory((string)s!), path, ct);

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
            var (dir, token) = ((string Directory, CancellationToken Token))s!;

            var opts = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
                AttributesToSkip = FileAttributes.ReparsePoint
            };

            var list = new List<string>();

            foreach (string file in Directory.EnumerateFiles(dir, "*", opts))
            {
                token.ThrowIfCancellationRequested();
                list.Add(file);
            }

            return list.ToArray();
        }, (directory, ct), ct);
    }

    public ValueTask<List<FileInfo>> GetAllFileInfoInDirectoryRecursivelySafe(
        string directory,
        bool log = true,
        CancellationToken ct = default)
    {
        if (log)
            _logger.LogDebug("Getting all FileInfos in {directory} recursively...", directory);

        return ExecutionContextUtil.RunInlineOrOffload(static s =>
        {
            var (dir, token) = ((string Directory, CancellationToken Token))s!;

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
}