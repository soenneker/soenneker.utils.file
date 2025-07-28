using AwesomeAssertions;
using Soenneker.Tests.FixturedUnit;
using Soenneker.Utils.File.Abstract;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Soenneker.Utils.Path;
using System.Threading;


namespace Soenneker.Utils.File.Tests;

[Collection("Collection")]
public class FileUtilTests : FixturedUnitTest
{
    private readonly IFileUtil _fileUtil;
    private readonly PathUtil _pathUtil = new();

    public FileUtilTests(Fixture fixture, ITestOutputHelper output) : base(fixture, output)
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

    [Fact]
    public async ValueTask ReadFile_ShouldReturnFileContent()
    {
        string path = await Setup(CancellationToken);

        const string expectedContent = "Test file content";

        string content = await _fileUtil.Read(path, cancellationToken: CancellationToken);

        content.Should().Be(expectedContent);
    }

    [Fact]
    public async ValueTask TryReadFile_WhenFileExists_ShouldReturnFileContent()
    {
        string path = await Setup(CancellationToken);

        const string expectedContent = "Test file content";

        string? content = await _fileUtil.TryRead(path, cancellationToken: CancellationToken);

        content.Should().Be(expectedContent);
    }

    [Fact]
    public async ValueTask TryReadFile_WhenFileDoesNotExist_ShouldReturnNull()
    {
        string path = await _pathUtil.GetRandomTempFilePath("txt", CancellationToken);

        string? content = await _fileUtil.TryRead(path, cancellationToken: CancellationToken);

        content.Should().BeNull();
    }

    [Fact]
    public async ValueTask WriteAllLines_ShouldWriteAllLinesToFile()
    {
        string path = await _pathUtil.GetRandomTempFilePath("txt", CancellationToken);

        var lines = new List<string> {"Line 1", "Line 2", "Line 3"};

        await _fileUtil.WriteAllLines(path, lines, cancellationToken: CancellationToken);

        string[]? writtenLines = await System.IO.File.ReadAllLinesAsync(path, CancellationToken);
        writtenLines.Should().BeEquivalentTo(lines);
    }

    [Fact]
    public async ValueTask ReadFileToBytes_ShouldReturnFileContentAsBytes()
    {
        string path = await Setup(CancellationToken);

        const string expectedContent = "Test file content";
        byte[]? expectedBytes = expectedContent.Select(c => (byte) c).ToArray();

        byte[]? contentBytes = await _fileUtil.ReadToBytes(path, cancellationToken: CancellationToken);

        contentBytes.Should().BeEquivalentTo(expectedBytes);
    }

    [Fact]
    public async ValueTask ReadFileToMemoryStream_ShouldReturnFileContentAsMemoryStream()
    {
        string path = await Setup(CancellationToken);

        const string expectedContent = "Test file content";

        using System.IO.MemoryStream? memoryStream = await _fileUtil.ReadToMemoryStream(path, cancellationToken: CancellationToken);
        using var reader = new StreamReader(memoryStream);

        string content = await reader.ReadToEndAsync(CancellationToken);

        content.Should().Be(expectedContent);
    }

    [Fact]
    public async ValueTask ReadFileAsLines_ShouldReturnFileContentAsList()
    {
        string path = await _pathUtil.GetRandomTempFilePath("txt", CancellationToken);
        var lines = new List<string> {"Line 1", "Line 2", "Line 3"};

        await _fileUtil.WriteAllLines(path, lines, cancellationToken: CancellationToken);

        var expectedContent = new List<string> {"Line 1", "Line 2", "Line 3"};

        List<string> content = await _fileUtil.ReadAsLines(path, cancellationToken: CancellationToken);

        content.Should().BeEquivalentTo(expectedContent);
    }

    [Fact]
    public async ValueTask WriteFile_ShouldWriteContentToFile()
    {
        string path = await _pathUtil.GetRandomTempFilePath("txt", CancellationToken);
        const string content = "Test content to write";

        await _fileUtil.Write(path, content, cancellationToken: CancellationToken);

        string writtenContent = await System.IO.File.ReadAllTextAsync(path, CancellationToken);
        writtenContent.Should().Be(content);
    }

    [Fact]
    public async ValueTask WriteFile_WithStream_ShouldWriteStreamContentToFile()
    {
        string path = await _pathUtil.GetRandomTempFilePath("txt", CancellationToken);
        const string content = "Test content to write with stream";
        using var stream = new System.IO.MemoryStream();
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(content);
        await writer.FlushAsync(CancellationToken);
        stream.Position = 0;

        await _fileUtil.Write(path, stream, cancellationToken: CancellationToken);

        string? writtenContent = await System.IO.File.ReadAllTextAsync(path, CancellationToken);
        writtenContent.Should().Be(content);
    }

    [Fact]
    public async ValueTask WriteFile_WithByteArray_ShouldWriteByteArrayToFile()
    {
        string path = await _pathUtil.GetRandomTempFilePath("txt", CancellationToken);
        const string content = "Test content to write with byte array";
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(content);

        await _fileUtil.Write(path, bytes, cancellationToken: CancellationToken);

        string? writtenContent = await System.IO.File.ReadAllTextAsync(path, CancellationToken);
        writtenContent.Should().Be(content);
    }
}