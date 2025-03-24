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
public class FileUtil : IFileUtil
{
    private readonly ILogger<FileUtil> _logger;
    private readonly IMemoryStreamUtil _memoryStreamUtil;

    public FileUtil(ILogger<FileUtil> logger, IMemoryStreamUtil memoryStreamUtil)
    {
        _logger = logger;
        _memoryStreamUtil = memoryStreamUtil;
    }

    public async ValueTask<string> Read(string path, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("{name} start for {path} ...", nameof(Read), path);

        // Use FileStream for granular control
        await using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, // Use an optimal buffer size
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        // Pre-allocate a buffer based on the file size, if known
        int length = fileStream.Length > 0 ? (int) fileStream.Length : 4096;
        char[] buffer = ArrayPool<char>.Shared.Rent(length);

        try
        {
            using var reader = new StreamReader(fileStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: length);
            StringBuilder result = new(length);
            int readCount;

            // Read in chunks to reduce allocations
            while ((readCount = await reader.ReadAsync(buffer, 0, buffer.Length).NoSync()) > 0)
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
        if (log)
            _logger.LogDebug("{name} start for {path} ...", nameof(TryRead), path);

        try
        {
            // Use FileStream for better control over file operations
            await using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
                bufferSize: 4096, // Optimal buffer size for most files
                options: FileOptions.Asynchronous | FileOptions.SequentialScan);

            // Pre-allocate a buffer based on file size or use a sensible default
            int fileLength = fileStream.Length > 0 ? (int) fileStream.Length : 4096;
            char[] buffer = ArrayPool<char>.Shared.Rent(fileLength);

            try
            {
                using var reader = new StreamReader(fileStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: fileLength);
                StringBuilder result = new(fileLength);

                // Read file content in chunks
                int readCount;
                while ((readCount = await reader.ReadAsync(buffer, 0, buffer.Length).NoSync()) > 0)
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
        catch (Exception e)
        {
            _logger.LogWarning(e, "Could not read file {path}", path);
        }

        return null;
    }

    public async ValueTask WriteAllLines(string path, IEnumerable<string> lines, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("{name} start for {path} ...", nameof(WriteAllLines), path);


        // Use FileStream for better control over the file writing process
        await using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, // Optimal buffer size
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        // Use a StreamWriter for writing text data
        await using var writer = new StreamWriter(fileStream, Encoding.UTF8, bufferSize: 4096, leaveOpen: false);

        // Iterate through the lines and write each line to the file
        foreach (string line in lines)
        {
            // Write line with async support
            await writer.WriteLineAsync(line.AsMemory(), cancellationToken).NoSync();

            // Check for cancellation after each line
            cancellationToken.ThrowIfCancellationRequested();
        }

        await writer.FlushAsync(cancellationToken).NoSync();
    }

    public async ValueTask<byte[]> ReadToBytes(string path, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("ReadFile start for {path} ...", path);

        // Open the file with a FileStream for precise control
        await using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 4096, // Optimal buffer size for performance
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        // Allocate buffer to match the file size if known
        int fileLength = fileStream.Length > 0 ? (int) fileStream.Length : 4096;
        var result = new byte[fileLength];

        var totalRead = 0;
        while (totalRead < fileLength)
        {
            // Read in chunks until the file is completely read
            int bytesRead = await fileStream.ReadAsync(result.AsMemory(totalRead), cancellationToken).NoSync();
            if (bytesRead == 0)
            {
                break; // End of file
            }

            totalRead += bytesRead;
        }

        // If the file size is unknown, trim the buffer to the actual size read
        if (totalRead < fileLength)
        {
            Array.Resize(ref result, totalRead);
        }

        return result;
    }

    public async ValueTask<System.IO.MemoryStream> ReadToMemoryStream(string path, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("{name} starting for {path} ...", nameof(ReadToMemoryStream), path);

        System.IO.MemoryStream memoryStream = await _memoryStreamUtil.Get(cancellationToken).NoSync();

        const int bufferSize = 81920;
        byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);

        try
        {
            await using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, useAsync: true);

            int bytesRead;
            while ((bytesRead = await fileStream.ReadAsync(buffer.AsMemory(0, bufferSize), cancellationToken).NoSync()) > 0)
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

    public async ValueTask<List<string>> ReadAsLines(string path, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("ReadFileInLines start for {path} ...", path);

        // Open the file using FileStream for optimal control
        await using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, // Optimal buffer size
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        // Use StreamReader to read lines efficiently
        using var reader = new StreamReader(fileStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        var lines = new List<string>();

        // Read lines one by one to minimize memory allocations
        while (await reader.ReadLineAsync(cancellationToken).NoSync() is { } line)
        {
            lines.Add(line);
            cancellationToken.ThrowIfCancellationRequested();
        }

        return lines;
    }

    public async ValueTask Write(string path, string content, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("{name} start for {path} ...", nameof(Write), path);

        // Open the file using FileStream with optimal options
        await using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None,
            bufferSize: 4096, // Optimal buffer size for performance
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        // Use StreamWriter for efficient text writing
        await using var writer = new StreamWriter(fileStream, Encoding.UTF8, bufferSize: 4096, leaveOpen: false);

        // Write content to the file
        await writer.WriteAsync(content.AsMemory(), cancellationToken).NoSync();

        // Flush the writer to ensure all content is written to the file
        await writer.FlushAsync(cancellationToken).NoSync();
    }

    public async ValueTask Write(string path, Stream stream, CancellationToken cancellationToken = default)
    {
        const int bufferSize = 8192;
        byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);

        try
        {
            Memory<byte> memoryBuffer = buffer.AsMemory(0, bufferSize); // Store AsMemory result once
            await using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, useAsync: true);

            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(memoryBuffer, cancellationToken).NoSync()) > 0)
            {
                await fileStream.WriteAsync(memoryBuffer[..bytesRead], cancellationToken).NoSync();
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public async ValueTask Write(string path, byte[] byteArray, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("{name} start for {path} ...", nameof(Write), path);

        // Open the file with FileStream for optimal control over the writing process
        await using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None,
            bufferSize: 4096, // Optimal buffer size for performance
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        // Write the byte array to the file
        await fileStream.WriteAsync(byteArray.AsMemory(), cancellationToken).NoSync();

        // Flush the FileStream to ensure all data is written to disk
        await fileStream.FlushAsync(cancellationToken).NoSync();
    }

    public async ValueTask Move(string sourcePath, string destinationPath, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("{name} start for {sourcePath} to {destinationPath} ...", nameof(Move), sourcePath, destinationPath);

        await Copy(sourcePath, destinationPath, cancellationToken).NoSync();

        // Delete the source file
        System.IO.File.Delete(sourcePath);
    }

    public async ValueTask Copy(string sourcePath, string destinationPath, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("{name} start for {sourcePath} to {destinationPath} ...", nameof(Copy), sourcePath, destinationPath);

        // Create the destination directory if it doesn't exist
        string destinationDirectory = Path.GetDirectoryName(destinationPath) ?? "";
        if (!Directory.Exists(destinationDirectory))
        {
            Directory.CreateDirectory(destinationDirectory);
        }

        await using var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
        await using var destinationStream =
            new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);
        await sourceStream.CopyToAsync(destinationStream, cancellationToken).NoSync();
    }

    public async ValueTask CopyRecursively(string sourceDir, string destinationDir, CancellationToken cancellationToken = default)
    {
        // Copy the directory structure
        string[] allDirectories = Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories);

        for (var i = 0; i < allDirectories.Length; i++)
        {
            string dir = allDirectories[i];
            string dirToCreate = dir.Replace(sourceDir, destinationDir);
            Directory.CreateDirectory(dirToCreate);
        }

        _logger.LogDebug("Getting all files from directory ({directory}) recursively...", sourceDir);

        string[] allFiles = Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories);

        for (var i = 0; i < allFiles.Length; i++)
        {
            string newPath = allFiles[i];
            await Copy(newPath, newPath.Replace(sourceDir, destinationDir), cancellationToken).NoSync();
        }
    }
}