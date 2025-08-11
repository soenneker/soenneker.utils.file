using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Soenneker.Extensions.Stream;
using Soenneker.Extensions.Task;
using Soenneker.Extensions.ValueTask;
using Soenneker.Utils.File.Abstract;
using Soenneker.Utils.MemoryStream.Abstract;

namespace Soenneker.Utils.File;

/// <inheritdoc cref="IFileUtil"/>
public sealed class FileUtil : IFileUtil
{
    private const int _defaultBuffer = 80 * 1024; // 80 kB

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
            return await Read(path, log, cancellationToken).NoSync();
        }
        catch (Exception e)
        {
            if (log)
                _logger.LogWarning(e, "Could not read file {path}", path);
            return null;
        }
    }

    public async ValueTask<List<string>> ReadAsLines(string path, bool log = true, CancellationToken cancellationToken = default)
    {
        if (log) _logger.LogDebug("{name} for {path}", nameof(ReadAsLines), path);

        var lines = new List<string>();
        using var reader = new StreamReader(path, Encoding.UTF8, true, _defaultBuffer);

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
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

        System.IO.MemoryStream destination = await _memoryStreamUtil.Get(cancellationToken).NoSync();
        byte[] buffer = ArrayPool<byte>.Shared.Rent(_defaultBuffer);

        try
        {
            await using FileStream source = System.IO.File.OpenRead(path);
            await source.CopyToAsync(destination, buffer.Length, cancellationToken);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        destination.ToStart();
        return destination;
    }

    public Task Write(string path, string content, bool log = true, CancellationToken cancellationToken = default)
    {
        if (log) _logger.LogDebug("{name} for {path}", nameof(Write), path);
        return System.IO.File.WriteAllTextAsync(path, content, Encoding.UTF8, cancellationToken);
    }

    public async ValueTask Write(string path, Stream source, bool log = true, CancellationToken cancellationToken = default)
    {
        if (log) _logger.LogDebug("{name} for {path}", nameof(Write), path);

        await using FileStream dest = System.IO.File.Create(path);
        await source.CopyToAsync(dest, _defaultBuffer, cancellationToken).NoSync();
    }

    public Task Write(string path, byte[] bytes, bool log = true, CancellationToken cancellationToken = default)
    {
        if (log) _logger.LogDebug("{name} for {path}", nameof(Write), path);
        return System.IO.File.WriteAllBytesAsync(path, bytes, cancellationToken);
    }

    public Task WriteAllLines(string path, IEnumerable<string> lines, bool log = true, CancellationToken cancellationToken = default)
    {
        if (log) _logger.LogDebug("{name} for {path}", nameof(WriteAllLines), path);
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

    public async ValueTask Copy(string sourcePath, string destinationPath, bool log = true, CancellationToken cancellationToken = default)
    {
        if (log)
            _logger.LogDebug("{name} start from {source} to {dest} ...", nameof(Copy), sourcePath, destinationPath);

        string destDir = Path.GetDirectoryName(destinationPath) ?? string.Empty;
        if (!Directory.Exists(destDir))
            Directory.CreateDirectory(destDir);

        await using FileStream src = System.IO.File.OpenRead(sourcePath);
        await using FileStream dst = System.IO.File.Create(destinationPath);
        await src.CopyToAsync(dst, _defaultBuffer, cancellationToken).NoSync();
    }

    public async ValueTask Move(string sourcePath, string destinationPath, bool log = true, CancellationToken cancellationToken = default)
    {
        if (log)
            _logger.LogDebug("{name} start from {source} to {dest} ...", nameof(Move), sourcePath, destinationPath);

        await Copy(sourcePath, destinationPath, log, cancellationToken).NoSync();
        await Delete(sourcePath, ignoreMissing: false, log: false, cancellationToken).NoSync();
    }

    public async ValueTask Delete(string path, bool ignoreMissing = true, bool log = true, CancellationToken cancellationToken = default)
    {
        if (log)
            _logger.LogDebug("{name} start for {path} ...", nameof(Delete), path);

        await Task.Run(() =>
                  {
                      if (!System.IO.File.Exists(path))
                      {
                          if (!ignoreMissing)
                              throw new FileNotFoundException("File not found", path);
                          return;
                      }

                      System.IO.File.Delete(path);
                  }, cancellationToken)
                  .NoSync();
    }

    public ValueTask<bool> FileExists(string path, CancellationToken cancellationToken = default)
    {
        // Path check is fast – still wrap in Task.FromResult for consistency
        return ValueTask.FromResult(System.IO.File.Exists(path));
    }

    public async ValueTask CopyRecursively(string sourceDir, string destinationDir, bool log = true, CancellationToken cancellationToken = default)
    {
        if (log)
            _logger.LogDebug("{name} start from {source} to {dest} ...", nameof(CopyRecursively), sourceDir, destinationDir);

        string[] allDirs = Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories);

        foreach (string dir in allDirs)
        {
            string target = dir.Replace(sourceDir, destinationDir);
            Directory.CreateDirectory(target);
        }

        string[] allFiles = Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories);
        foreach (string file in allFiles)
        {
            string destFile = file.Replace(sourceDir, destinationDir);
            await Copy(file, destFile, log: false, cancellationToken).NoSync();
        }
    }

    public ValueTask<long?> GetFileSize(string path) => new(System.IO.File.Exists(path) ? new FileInfo(path).Length : null);

    public ValueTask<DateTimeOffset?> GetLastModified(string path) =>
        new(System.IO.File.Exists(path) ? new FileInfo(path).LastWriteTimeUtc : null);

    public async ValueTask<bool> DeleteIfExists(string path, bool log = true, CancellationToken cancellationToken = default)
    {
        if (!await FileExists(path, cancellationToken).NoSync())
            return false;

        if (log)
            _logger.LogDebug("{name} start for {path} …", nameof(DeleteIfExists), path);

        await Delete(path, ignoreMissing: false, log: false, cancellationToken).NoSync();
        return true;
    }

    public async ValueTask<bool> TryDeleteIfExists(string path, bool log = true, CancellationToken cancellationToken = default)
    {
        if (!await FileExists(path, cancellationToken).NoSync())
            return false;

        return await TryDelete(path, log, cancellationToken).NoSync();
    }

    public async ValueTask<bool> TryDelete(string path, bool log = true, CancellationToken cancellationToken = default)
    {
        if (log)
            _logger.LogDebug("Trying to delete {path} …", path);

        try
        {
            await Delete(path, ignoreMissing: true, log: false, cancellationToken).NoSync();
            return true;
        }
        catch (Exception ex)
        {
            if (log)
                _logger.LogError(ex, "Exception deleting {path}", path);
            return false;
        }
    }
}