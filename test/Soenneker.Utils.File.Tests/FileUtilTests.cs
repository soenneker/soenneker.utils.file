using AwesomeAssertions;
using Soenneker.Tests.HostedUnit;
using Soenneker.Utils.File.Abstract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Soenneker.Utils.Path;
using System.Threading;


namespace Soenneker.Utils.File.Tests;

[ClassDataSource<Host>(Shared = SharedType.PerTestSession)]
public class FileUtilTests : HostedUnitTest
{
    private readonly IFileUtil _fileUtil;
    private readonly PathUtil _pathUtil = new();

    public FileUtilTests(Host host) : base(host)
    {
        _fileUtil = Resolve<IFileUtil>(true);
    }

    private async ValueTask<string> Setup(CancellationToken cancellationToken = default)
    {
        string path = await _pathUtil.GetRandomTempFilePath("txt", cancellationToken);

        const string content = "Test file content";
        await System.IO.File.WriteAllTextAsync(path, content, cancellationToken);

        return path;
    }

    [Test]
    public async ValueTask ReadFile_ShouldReturnFileContent()
    {
        string path = await Setup(System.Threading.CancellationToken.None);

        const string expectedContent = "Test file content";

        string content = await _fileUtil.Read(path, cancellationToken: System.Threading.CancellationToken.None);

        content.Should().Be(expectedContent);
    }

    [Test]
    public async ValueTask TryReadFile_WhenFileExists_ShouldReturnFileContent()
    {
        string path = await Setup(System.Threading.CancellationToken.None);

        const string expectedContent = "Test file content";

        string? content = await _fileUtil.TryRead(path, cancellationToken: System.Threading.CancellationToken.None);

        content.Should().Be(expectedContent);
    }

    [Test]
    public async ValueTask TryReadFile_WhenFileDoesNotExist_ShouldReturnNull()
    {
        string path = await _pathUtil.GetRandomTempFilePath("txt", System.Threading.CancellationToken.None);

        string? content = await _fileUtil.TryRead(path, cancellationToken: System.Threading.CancellationToken.None);

        content.Should().BeNull();
    }

    [Test]
    public async ValueTask WriteAllLines_ShouldWriteAllLinesToFile()
    {
        string path = await _pathUtil.GetRandomTempFilePath("txt", System.Threading.CancellationToken.None);

        var lines = new List<string> {"Line 1", "Line 2", "Line 3"};

        await _fileUtil.WriteAllLines(path, lines, cancellationToken: System.Threading.CancellationToken.None);

        string[]? writtenLines = await System.IO.File.ReadAllLinesAsync(path, System.Threading.CancellationToken.None);
        writtenLines.Should().BeEquivalentTo(lines);
    }

    [Test]
    public async ValueTask ReadFileToBytes_ShouldReturnFileContentAsBytes()
    {
        string path = await Setup(System.Threading.CancellationToken.None);

        const string expectedContent = "Test file content";
        byte[]? expectedBytes = expectedContent.Select(c => (byte) c).ToArray();

        byte[]? contentBytes = await _fileUtil.ReadToBytes(path, cancellationToken: System.Threading.CancellationToken.None);

        contentBytes.Should().BeEquivalentTo(expectedBytes);
    }

    [Test]
    public async ValueTask ReadFileToMemoryStream_ShouldReturnFileContentAsMemoryStream()
    {
        string path = await Setup(System.Threading.CancellationToken.None);

        const string expectedContent = "Test file content";

        using System.IO.MemoryStream? memoryStream = await _fileUtil.ReadToMemoryStream(path, cancellationToken: System.Threading.CancellationToken.None);
        using var reader = new StreamReader(memoryStream);

        string content = await reader.ReadToEndAsync(System.Threading.CancellationToken.None);

        content.Should().Be(expectedContent);
    }

    [Test]
    public async ValueTask ReadFileAsLines_ShouldReturnFileContentAsList()
    {
        string path = await _pathUtil.GetRandomTempFilePath("txt", System.Threading.CancellationToken.None);
        var lines = new List<string> {"Line 1", "Line 2", "Line 3"};

        await _fileUtil.WriteAllLines(path, lines, cancellationToken: System.Threading.CancellationToken.None);

        var expectedContent = new List<string> {"Line 1", "Line 2", "Line 3"};

        List<string> content = await _fileUtil.ReadAsLines(path, cancellationToken: System.Threading.CancellationToken.None);

        content.Should().BeEquivalentTo(expectedContent);
    }

    [Test]
    public async ValueTask WriteFile_ShouldWriteContentToFile()
    {
        string path = await _pathUtil.GetRandomTempFilePath("txt", System.Threading.CancellationToken.None);
        const string content = "Test content to write";

        await _fileUtil.Write(path, content, cancellationToken: System.Threading.CancellationToken.None);

        string writtenContent = await System.IO.File.ReadAllTextAsync(path, System.Threading.CancellationToken.None);
        writtenContent.Should().Be(content);
    }

    [Test]
    public async ValueTask WriteFile_WithStream_ShouldWriteStreamContentToFile()
    {
        string path = await _pathUtil.GetRandomTempFilePath("txt", System.Threading.CancellationToken.None);
        const string content = "Test content to write with stream";
        using var stream = new System.IO.MemoryStream();
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(content);
        await writer.FlushAsync(System.Threading.CancellationToken.None);
        stream.Position = 0;

        await _fileUtil.Write(path, stream, cancellationToken: System.Threading.CancellationToken.None);

        string? writtenContent = await System.IO.File.ReadAllTextAsync(path, System.Threading.CancellationToken.None);
        writtenContent.Should().Be(content);
    }

    [Test]
    public async ValueTask WriteFile_WithStream_ShouldOffloadSynchronousSetupFromSynchronizationContext()
    {
        string path = await _pathUtil.GetRandomTempFilePath("txt", CancellationToken.None);
        await using var stream = new SynchronizationContextTrackingStream(System.Text.Encoding.UTF8.GetBytes("content"));
        SynchronizationContext? previous = SynchronizationContext.Current;
        ValueTask write;

        try
        {
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
            write = _fileUtil.Write(path, stream, log: false, CancellationToken.None);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }

        await write;

        stream.WasInspectedOnSynchronizationContext.Should().BeFalse();
    }

    [Test]
    public async ValueTask WriteFile_WithByteArray_ShouldWriteByteArrayToFile()
    {
        string path = await _pathUtil.GetRandomTempFilePath("txt", System.Threading.CancellationToken.None);
        const string content = "Test content to write with byte array";
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(content);

        await _fileUtil.Write(path, bytes, cancellationToken: System.Threading.CancellationToken.None);

        string? writtenContent = await System.IO.File.ReadAllTextAsync(path, System.Threading.CancellationToken.None);
        writtenContent.Should().Be(content);
    }

    [Test]
    public async ValueTask WriteAtomically_ShouldReplaceDestination()
    {
        string path = await _pathUtil.GetRandomTempFilePath("txt", CancellationToken.None);
        await System.IO.File.WriteAllTextAsync(path, "old", CancellationToken.None);

        await _fileUtil.WriteAtomically(path, "new", log: false, CancellationToken.None);

        (await System.IO.File.ReadAllTextAsync(path, CancellationToken.None)).Should().Be("new");
    }

    [Test]
    public async ValueTask WriteAtomically_WhenWriterFails_ShouldPreserveDestination()
    {
        string path = await _pathUtil.GetRandomTempFilePath("txt", CancellationToken.None);
        await System.IO.File.WriteAllTextAsync(path, "old", CancellationToken.None);
        var failed = false;

        try
        {
            await _fileUtil.WriteAtomically(path, (_, _) => throw new InvalidOperationException("Expected failure"), log: false,
                CancellationToken.None);
        }
        catch (InvalidOperationException)
        {
            failed = true;
        }

        failed.Should().BeTrue();
        (await System.IO.File.ReadAllTextAsync(path, CancellationToken.None)).Should().Be("old");
    }

    [Test]
    public async ValueTask Move_ShouldOverwriteDestinationAndDeleteSource()
    {
        string source = await _pathUtil.GetRandomTempFilePath("txt", CancellationToken.None);
        string destination = await _pathUtil.GetRandomTempFilePath("txt", CancellationToken.None);
        await System.IO.File.WriteAllTextAsync(source, "source", CancellationToken.None);
        await System.IO.File.WriteAllTextAsync(destination, "destination", CancellationToken.None);

        await _fileUtil.Move(source, destination, log: false, CancellationToken.None);

        System.IO.File.Exists(source).Should().BeFalse();
        (await System.IO.File.ReadAllTextAsync(destination, CancellationToken.None)).Should().Be("source");
    }

    [Test]
    public async ValueTask GetAllFileNamesInDirectoryRecursively_ShouldReturnEveryFile()
    {
        string root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"file-util-tests-{System.Guid.NewGuid():N}");

        try
        {
            string child = System.IO.Path.Combine(root, "child");
            System.IO.Directory.CreateDirectory(child);
            await System.IO.File.WriteAllTextAsync(System.IO.Path.Combine(root, "one.txt"), "one", CancellationToken.None);
            await System.IO.File.WriteAllTextAsync(System.IO.Path.Combine(child, "two.txt"), "two", CancellationToken.None);

            string[] files = await _fileUtil.GetAllFileNamesInDirectoryRecursively(root, log: false, CancellationToken.None);

            files.Should().HaveCount(2);
        }
        finally
        {
            if (System.IO.Directory.Exists(root))
                System.IO.Directory.Delete(root, recursive: true);
        }
    }

    private sealed class SynchronizationContextTrackingStream(byte[] buffer) : System.IO.MemoryStream(buffer)
    {
        public bool WasInspectedOnSynchronizationContext { get; private set; }

        public override long Length
        {
            get
            {
                RecordSynchronizationContext();
                return base.Length;
            }
        }

        public override long Position
        {
            get
            {
                RecordSynchronizationContext();
                return base.Position;
            }
            set => base.Position = value;
        }

        private void RecordSynchronizationContext()
        {
            if (SynchronizationContext.Current is not null)
                WasInspectedOnSynchronizationContext = true;
        }
    }
}

