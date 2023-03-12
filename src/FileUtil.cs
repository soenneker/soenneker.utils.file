using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Soenneker.Extensions.Stream;
using Soenneker.Utils.File.Abstract;
using Soenneker.Utils.FileSync;
using Soenneker.Utils.MemoryStream.Abstract;

namespace Soenneker.Utils.File;

/// <inheritdoc cref="IFileUtil"/>
public class FileUtil : FileUtilSync, IFileUtil
{
    private readonly ILogger<FileUtil> _logger;
    private readonly IMemoryStreamUtil _memoryStreamUtil;

    public FileUtil(ILogger<FileUtil> logger, IMemoryStreamUtil memoryStreamUtil) : base(logger)
    {
        _logger = logger;
        _memoryStreamUtil = memoryStreamUtil;
    }

    public new Task<string> ReadFile(string path)
    {
        _logger.LogDebug("{name} start for {path} ...", nameof(ReadFile), path);

        return System.IO.File.ReadAllTextAsync(path);
    }

    public new Task WriteAllLines(string path, IEnumerable<string> lines)
    {
        _logger.LogDebug("{name} start for {path} ...", nameof(WriteAllLines), path);

        return System.IO.File.WriteAllLinesAsync(path, lines);
    }

    public new Task<byte[]> ReadFileToBytes(string path)
    {
        _logger.LogDebug("ReadFile start for {name} ...", path);

        return System.IO.File.ReadAllBytesAsync(path);
    }

    public async ValueTask<System.IO.MemoryStream> ReadFileToMemoryStream(string path)
    {
        _logger.LogDebug("ReadFile starting for {name} ...", path);

        System.IO.MemoryStream memoryStream = await _memoryStreamUtil.Get();

        FileStream fileStream = System.IO.File.OpenRead(path);

        await fileStream.CopyToAsync(memoryStream);

        fileStream.Close();
        memoryStream.ToStart();

        return memoryStream;
    }

    public new async ValueTask<List<string>> ReadFileAsLines(string path)
    {
        _logger.LogDebug("ReadFileInLines start for {name} ...", path);

        List<string> content = (await System.IO.File.ReadAllLinesAsync(path)).ToList();

        return content;
    }

    public new Task WriteFile(string path, string content)
    {
        _logger.LogDebug("{name} start for {path} ...", nameof(System.IO.File.WriteAllTextAsync), path);

        return System.IO.File.WriteAllTextAsync(path, content);
    }

    public new async Task WriteFile(string path, Stream stream)
    {
        stream.ToStart();

        var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write);
        await stream.CopyToAsync(fileStream);
        await fileStream.DisposeAsync();
    }

    public new Task WriteFile(string path, byte[] byteArray)
    {
        _logger.LogDebug("{name} start for {path} ...", nameof(System.IO.File.WriteAllBytesAsync), path);

        return System.IO.File.WriteAllBytesAsync(path, byteArray);
    }
}