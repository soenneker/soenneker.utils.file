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
    private const int _defaultBuffer = 4096;

    private readonly ILogger<FileUtil> _logger;
    private readonly IMemoryStreamUtil _memoryStreamUtil;

    public FileUtil(ILogger<FileUtil> logger, IMemoryStreamUtil memoryStreamUtil)
    {
        _logger = logger;
        _memoryStreamUtil = memoryStreamUtil;
    }

    public async ValueTask<string> Read(string path, bool log = true, CancellationToken cancellationToken = default)
    {
        if (log)
            _logger.LogDebug("{name} start for {path} ...", nameof(Read), path);

        await using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: _defaultBuffer,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        int length = fileStream.Length > 0 ? (int) fileStream.Length : _defaultBuffer;
        char[] buffer = ArrayPool<char>.Shared.Rent(length);

        try
        {
            using var reader = new StreamReader(fileStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: length);
            StringBuilder result = new(length);

            int readCount;
            while ((readCount = await reader.ReadAsync(buffer, cancellationToken).NoSync()) > 0)
            {
                result.Append(buffer, 0, readCount);
                cancellationToken.ThrowIfCancellationRequested();
            }

            return result.ToString();
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer);
        }
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
        if (log)
            _logger.LogDebug("{name} start for {path} ...", nameof(ReadAsLines), path);

        await using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, _defaultBuffer,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        using var reader = new StreamReader(fileStream, Encoding.UTF8, true, _defaultBuffer);
        var lines = new List<string>();

        while (await reader.ReadLineAsync(cancellationToken).NoSync() is { } line)
        {
            lines.Add(line);
            cancellationToken.ThrowIfCancellationRequested();
        }

        return lines;
    }

    public async ValueTask<byte[]> ReadToBytes(string path, bool log = true, CancellationToken cancellationToken = default)
    {
        if (log)
            _logger.LogDebug("{name} start for {path} ...", nameof(ReadToBytes), path);

        await using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, _defaultBuffer,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        int fileLength = fileStream.Length > 0 ? (int) fileStream.Length : _defaultBuffer;
        byte[] result = new byte[fileLength];
        int totalRead = 0;

        while (totalRead < fileLength)
        {
            int bytesRead = await fileStream.ReadAsync(result.AsMemory(totalRead), cancellationToken).NoSync();
            if (bytesRead == 0)
                break;
            totalRead += bytesRead;
        }

        if (totalRead < fileLength)
            Array.Resize(ref result, totalRead);

        return result;
    }

    public async ValueTask<System.IO.MemoryStream> ReadToMemoryStream(string path, bool log = true, CancellationToken cancellationToken = default)
    {
        if (log)
            _logger.LogDebug("{name} start for {path} ...", nameof(ReadToMemoryStream), path);

        var memoryStream = await _memoryStreamUtil.Get(cancellationToken).NoSync();
        byte[] buffer = ArrayPool<byte>.Shared.Rent(_defaultBuffer * 20); // 80 kB

        try
        {
            await using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, buffer.Length, true);
            int bytesRead;
            while ((bytesRead = await fileStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).NoSync()) > 0)
            {
                await memoryStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).NoSync();
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        memoryStream.ToStart();
        return memoryStream;
    }

    public async ValueTask Write(string path, string content, bool log = true, CancellationToken cancellationToken = default)
    {
        if (log)
            _logger.LogDebug("{name} start for {path} ...", nameof(Write), path);

        await using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, _defaultBuffer,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        await using var writer = new StreamWriter(fileStream, Encoding.UTF8, _defaultBuffer, leaveOpen: false);
        await writer.WriteAsync(content.AsMemory(), cancellationToken).NoSync();
        await writer.FlushAsync(cancellationToken).NoSync();
    }

    public async ValueTask Write(string path, Stream stream, bool log = true, CancellationToken cancellationToken = default)
    {
        if (log)
            _logger.LogDebug("{name} start for {path} ...", nameof(Write), path);

        await using var destination = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, _defaultBuffer, true);
        await stream.CopyToAsync(destination, cancellationToken).NoSync();
    }

    public async ValueTask Write(string path, byte[] bytes, bool log = true, CancellationToken cancellationToken = default)
    {
        if (log)
            _logger.LogDebug("{name} start for {path} ...", nameof(Write), path);

        await using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, _defaultBuffer,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        await fileStream.WriteAsync(bytes, cancellationToken).NoSync();
        await fileStream.FlushAsync(cancellationToken).NoSync();
    }

    public async ValueTask WriteAllLines(string path, IEnumerable<string> lines, bool log = true, CancellationToken cancellationToken = default)
    {
        if (log)
            _logger.LogDebug("{name} start for {path} ...", nameof(WriteAllLines), path);

        await using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, _defaultBuffer,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);
        await using var writer = new StreamWriter(fileStream, Encoding.UTF8, _defaultBuffer);

        foreach (string line in lines)
        {
            await writer.WriteLineAsync(line.AsMemory(), cancellationToken).NoSync();
            cancellationToken.ThrowIfCancellationRequested();
        }

        await writer.FlushAsync(cancellationToken).NoSync();
    }

    public async ValueTask Append(string path, string content, bool log = true, CancellationToken cancellationToken = default)
    {
        if (log)
            _logger.LogDebug("{name} start for {path} ...", nameof(Append), path);

        await using var fileStream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.None, _defaultBuffer,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);
        await using var writer = new StreamWriter(fileStream, Encoding.UTF8, _defaultBuffer);
        await writer.WriteAsync(content.AsMemory(), cancellationToken).NoSync();
        await writer.FlushAsync(cancellationToken).NoSync();
    }

    public async ValueTask Append(string path, IEnumerable<string> lines, bool log = true, CancellationToken cancellationToken = default)
    {
        if (log)
            _logger.LogDebug("{name} start for {path} ...", nameof(Append), path);

        await using var fileStream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.None, _defaultBuffer,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);
        await using var writer = new StreamWriter(fileStream, Encoding.UTF8, _defaultBuffer);

        foreach (var line in lines)
        {
            await writer.WriteLineAsync(line.AsMemory(), cancellationToken).NoSync();
            cancellationToken.ThrowIfCancellationRequested();
        }

        await writer.FlushAsync(cancellationToken).NoSync();
    }

    public async ValueTask Copy(string sourcePath, string destinationPath, bool log = true, CancellationToken cancellationToken = default)
    {
        if (log)
            _logger.LogDebug("{name} start from {source} to {dest} ...", nameof(Copy), sourcePath, destinationPath);

        string destDir = Path.GetDirectoryName(destinationPath) ?? string.Empty;
        if (!Directory.Exists(destDir))
            Directory.CreateDirectory(destDir);

        await using var source = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, _defaultBuffer, true);
        await using var dest = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, _defaultBuffer, true);
        await source.CopyToAsync(dest, cancellationToken).NoSync();
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
        new(System.IO.File.Exists(path) ? new FileInfo(path).LastWriteTimeUtc : (DateTimeOffset?) null);

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