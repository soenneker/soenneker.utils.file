using Microsoft.Extensions.Logging;
using Soenneker.Extensions.Stream;
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
using Soenneker.Extensions.String;

namespace Soenneker.Utils.File;

/// <inheritdoc cref="IFileUtil"/>
public sealed class FileUtil : IFileUtil
{
    private const int _defaultBuffer = 128 * 1024; // 128 kB

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
        if (log) _logger.LogDebug("{name} for {path}", nameof(ReadAsLines), path);

        long len = System.IO.File.Exists(path) ? new FileInfo(path).Length : 0L;

        List<string> lines = len > 0 ? new List<string>((int)Math.Min(int.MaxValue, len / 48 + 16)) : [];

        using var reader = new StreamReader(path, Encoding.UTF8, true, _defaultBuffer);
        while (await reader.ReadLineAsync(ct) is { } line) lines.Add(line);
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
        if (log) _logger.LogDebug("{name} for {path}", nameof(ReadToMemoryStream), path);

        var fi = new FileInfo(path);
        System.IO.MemoryStream ms = await _memoryStreamUtil.Get(cancellationToken)
            .NoSync(); // assumed cleared/position=0

        if (fi is { Exists: true, Length: <= int.MaxValue } && fi.Length > ms.Capacity)
            ms.Capacity = (int)fi.Length; // capacity hint only

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
        if (log) _logger.LogDebug("{name} for {path}", nameof(Write), path);
        return System.IO.File.WriteAllTextAsync(path, content, Encoding.UTF8, cancellationToken);
    }

    public async ValueTask Write(string path, Stream source, bool log = true, CancellationToken ct = default)
    {
        if (log) _logger.LogDebug("{name} for {path}", nameof(Write), path);

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
            long remaining = Math.Max(0, source.Length - source.Position);
            // Guard against extremely large values if you care, else just assign
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

        return System.IO.File.WriteAllLinesAsync(path, lines, Encoding.UTF8, cancellationToken);
    }


    public Task Append(string path, string content, bool log = true, CancellationToken cancellationToken = default)
    {
        if (log) _logger.LogDebug("{name} for {path}", nameof(Append), path);
        return System.IO.File.AppendAllTextAsync(path, content, Encoding.UTF8, cancellationToken);
    }

    public Task Append(string path, IEnumerable<string> lines, bool log = true, CancellationToken cancellationToken = default)
    {
        if (log) _logger.LogDebug("{name} for {path}", nameof(Append), path);
        return System.IO.File.AppendAllLinesAsync(path, lines, Encoding.UTF8, cancellationToken);
    }

    public async ValueTask Copy(string srcPath, string dstPath, bool log = true, CancellationToken ct = default)
    {
        if (log) _logger.LogDebug("{name} {src} -> {dst}", nameof(Copy), srcPath, dstPath);

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
            PreallocationSize = Math.Min(srcInfo.Length, int.MaxValue) // safe on all platforms
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
        if (log) _logger.LogDebug("{name} start for {path} ...", nameof(Delete), path);
        return RunInlineOrOffload(() =>
        {
            if (!System.IO.File.Exists(path))
            {
                if (!ignoreMissing) throw new FileNotFoundException("File not found", path);
                return;
            }

            System.IO.File.Delete(path);
        }, ct);
    }

    public ValueTask<bool> Exists(string path, CancellationToken ct = default) => RunInlineOrOffload(() => System.IO.File.Exists(path), ct);

    public async ValueTask CopyRecursively(string sourceDir, string destinationDir, bool log = true, CancellationToken ct = default)
    {
        if (log)
            _logger.LogDebug("{name} {source} -> {dest}", nameof(CopyRecursively), sourceDir, destinationDir);

        // Directory pass is unnecessary if we ensure parents before each file copy.
        var opts = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.ReparsePoint // optional: avoid loops
        };

        IEnumerable<string> files = Directory.EnumerateFiles(sourceDir, "*", opts);

        // IO is not CPU-bound: keep concurrency modest to avoid disk thrash.
        int dop = Math.Min(8, Math.Max(2, Environment.ProcessorCount / 2));

        await Parallel.ForEachAsync(files, new ParallelOptions { MaxDegreeOfParallelism = dop, CancellationToken = ct }, async (file, token) =>
        {
            string rel = Path.GetRelativePath(sourceDir, file);
            string destFile = Path.Combine(destinationDir, rel);

            string? parent = Path.GetDirectoryName(destFile);

            if (parent.HasContent())
                Directory.CreateDirectory(parent); // fast, idempotent

            await Copy(file, destFile, log: false, token);
        });
    }

    public ValueTask<long?> GetSize(string path, CancellationToken ct = default) => RunInlineOrOffload(() =>
    {
        var fi = new FileInfo(path);
        return fi.Exists ? fi.Length : (long?)null;
    }, ct);

    public ValueTask<DateTimeOffset?> GetLastModified(string path, CancellationToken ct = default) => RunInlineOrOffload(() =>
    {
        var fi = new FileInfo(path);
        return fi.Exists ? fi.LastWriteTimeUtc : (DateTimeOffset?)null;
    }, ct);

    public async ValueTask<bool> DeleteIfExists(string path, bool log = true, CancellationToken cancellationToken = default)
    {
        if (!await Exists(path, cancellationToken)
                .NoSync())
            return false;

        if (log) _logger.LogDebug("{name} start for {path} …", nameof(DeleteIfExists), path);

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
        if (log) _logger.LogDebug("{name} for {path}", nameof(ReadToHashSet), path);

        long len = System.IO.File.Exists(path) ? new FileInfo(path).Length : 0L;
        int capacity = len > 0 ? (int)Math.Min(int.MaxValue, len / 32 + 16) : 0;

        HashSet<string> set = capacity > 0
            ? new HashSet<string>(capacity, comparer ?? StringComparer.Ordinal)
            : new HashSet<string>(comparer ?? StringComparer.Ordinal);

        using var reader = new StreamReader(path, Encoding.UTF8, true, _defaultBuffer);

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
        RunInlineOrOffload(() => Directory.CreateDirectory(path), ct);

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

    private static bool OnUiContext() => SynchronizationContext.Current is not null;

    // Use this for tiny sync ops that must never block UI:
    private static ValueTask RunInlineOrOffload(Action action, CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
            return ValueTask.FromCanceled(ct);

        if (OnUiContext())
            return new ValueTask(Task.Run(action, ct));

        action();
        return ValueTask.CompletedTask;
    }

    private static ValueTask<T> RunInlineOrOffload<T>(Func<T> func, CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
            return ValueTask.FromCanceled<T>(ct);

        if (OnUiContext())
            return new ValueTask<T>(Task.Run(func, ct));

        return new ValueTask<T>(func());
    }

    [Pure]
    public string[] GetAllFileNamesInDirectoryRecursively(string directory, bool log = true)
    {
        if (log)
            _logger.LogDebug("Getting all files from directory ({directory}) recursively...", directory);

        return Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories);
    }

    [Pure]
    public List<FileInfo> GetAllFileInfoInDirectoryRecursivelySafe(string directory, bool log = true)
    {
        if (log)
            _logger.LogDebug("Getting all FileInfos in {directory} recursively...", directory);

        var list = new List<FileInfo>();

        try
        {
            var diTop = new DirectoryInfo(directory);
            foreach (FileInfo fi in diTop.EnumerateFiles())
            {
                try
                {
                    list.Add(new FileInfo(fi.FullName));
                }
                catch (UnauthorizedAccessException)
                {
                    _logger.LogWarning("Unauthorized Exception for {fullName}", fi.FullName);
                }
            }

            foreach (DirectoryInfo di in diTop.EnumerateDirectories("*"))
            {
                try
                {
                    foreach (FileInfo fi in di.EnumerateFiles("*", SearchOption.AllDirectories))
                    {
                        try
                        {
                            list.Add(new FileInfo(fi.FullName));
                        }
                        catch (UnauthorizedAccessException)
                        {
                            _logger.LogWarning("Unauthorized Exception for {fullName}", fi.FullName);
                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    _logger.LogWarning("Unauthorized Exception for {fullName}", di.FullName);
                }
            }
        }
        catch (Exception e) when (e is DirectoryNotFoundException || e is UnauthorizedAccessException || e is PathTooLongException)
        {
            _logger.LogWarning(e, "{message}", e.Message);
        }

        if (log)
            _logger.LogDebug("Completed getting all files in {directory}, number: {number}", directory, list.Count);
        return list;
    }
}