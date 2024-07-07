using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
public class FileUtil : IFileUtil
{
    private readonly ILogger<FileUtil> _logger;
    private readonly IMemoryStreamUtil _memoryStreamUtil;

    public FileUtil(ILogger<FileUtil> logger, IMemoryStreamUtil memoryStreamUtil)
    {
        _logger = logger;
        _memoryStreamUtil = memoryStreamUtil;
    }

    public Task<string> ReadFile(string path, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("{name} start for {path} ...", nameof(ReadFile), path);

        return System.IO.File.ReadAllTextAsync(path, cancellationToken);
    }

    public async ValueTask<string?> TryReadFile(string path, bool log = true, CancellationToken cancellationToken = default)
    {
        if (log)
            _logger.LogDebug("{name} start for {path} ...", nameof(TryReadFile), path);

        string? result = null;

        try
        {
            result = await System.IO.File.ReadAllTextAsync(path, cancellationToken).NoSync();
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Could not read file {path}", path);
        }

        return result;
    }

    public Task WriteAllLines(string path, IEnumerable<string> lines, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("{name} start for {path} ...", nameof(WriteAllLines), path);

        return System.IO.File.WriteAllLinesAsync(path, lines, cancellationToken);
    }

    public Task<byte[]> ReadFileToBytes(string path, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("ReadFile start for {name} ...", path);

        return System.IO.File.ReadAllBytesAsync(path, cancellationToken);
    }

    public async ValueTask<System.IO.MemoryStream> ReadFileToMemoryStream(string path, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("{name} starting for {path} ...", nameof(ReadFileToMemoryStream), path);

        System.IO.MemoryStream memoryStream = await _memoryStreamUtil.Get(cancellationToken).NoSync();

        await using (FileStream fileStream = System.IO.File.OpenRead(path))
        {
            const int bufferSize = 81920;
            var buffer = new byte[bufferSize];
            Memory<byte> memoryBuffer = buffer.AsMemory();
            int bytesRead;

            while ((bytesRead = await fileStream.ReadAsync(memoryBuffer, cancellationToken).NoSync()) > 0)
            {
                await memoryStream.WriteAsync(memoryBuffer.Slice(0, bytesRead), cancellationToken).NoSync();
            }
        }

        memoryStream.ToStart();

        return memoryStream;
    }

    public async ValueTask<List<string>> ReadFileAsLines(string path, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("ReadFileInLines start for {name} ...", path);

        List<string> content = (await System.IO.File.ReadAllLinesAsync(path, cancellationToken).NoSync()).ToList();

        return content;
    }

    public Task WriteFile(string path, string content, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("{name} start for {path} ...", nameof(System.IO.File.WriteAllTextAsync), path);

        return System.IO.File.WriteAllTextAsync(path, content, cancellationToken);
    }

    public async ValueTask WriteFile(string path, Stream stream, CancellationToken cancellationToken = default)
    {
        stream.ToStart();

        await using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write))
        {
            const int bufferSize = 81920;
            var buffer = new byte[bufferSize];
            Memory<byte> memoryBuffer = buffer.AsMemory();
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(memoryBuffer, cancellationToken).NoSync()) > 0)
            {
                await fileStream.WriteAsync(memoryBuffer.Slice(0, bytesRead), cancellationToken).NoSync();
            }
        }
    }

    public Task WriteFile(string path, byte[] byteArray, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("{name} start for {path} ...", nameof(System.IO.File.WriteAllBytesAsync), path);

        return System.IO.File.WriteAllBytesAsync(path, byteArray, cancellationToken);
    }
}