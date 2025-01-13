using FluentAssertions;
using Soenneker.Tests.FixturedUnit;
using Soenneker.Utils.File.Abstract;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;


namespace Soenneker.Utils.File.Tests;

[Collection("Collection")]
public class FileUtilTests : FixturedUnitTest
{
    private readonly IFileUtil _fileUtil;

    public FileUtilTests(Fixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
        _fileUtil = Resolve<IFileUtil>(true);
    }

    private static void Setup()
    {
        const string path = "test.txt";
        const string content = "Test file content";
        System.IO.File.WriteAllText(path, content);
    }

    [Fact]
    public async Task ReadFile_ShouldReturnFileContent()
    {
        Setup();

        const string path = "test.txt";
        const string expectedContent = "Test file content";

        string content = await _fileUtil.Read(path, CancellationToken);

        content.Should().Be(expectedContent);
    }

    [Fact]
    public async Task TryReadFile_WhenFileExists_ShouldReturnFileContent()
    {
        Setup();

        const string path = "test.txt";
        const string expectedContent = "Test file content";

        string? content = await _fileUtil.TryRead(path, cancellationToken: CancellationToken);

        content.Should().Be(expectedContent);
    }

    [Fact]
    public async Task TryReadFile_WhenFileDoesNotExist_ShouldReturnNull()
    {
        const string path = "nonexistent.txt";

        string? content = await _fileUtil.TryRead(path, cancellationToken: CancellationToken);

        content.Should().BeNull();
    }

    [Fact]
    public async Task WriteAllLines_ShouldWriteAllLinesToFile()
    {
        const string path = "testWriteAllLines.txt";
        var lines = new List<string> { "Line 1", "Line 2", "Line 3" };

        await _fileUtil.WriteAllLines(path, lines, CancellationToken);

        string[]? writtenLines = await System.IO.File.ReadAllLinesAsync(path, CancellationToken);
        writtenLines.Should().BeEquivalentTo(lines);
    }

    [Fact]
    public async Task ReadFileToBytes_ShouldReturnFileContentAsBytes()
    {
        const string path = "test.txt";
        const string expectedContent = "Test file content";
        byte[]? expectedBytes = expectedContent.Select(c => (byte)c).ToArray();

        byte[]? contentBytes = await _fileUtil.ReadToBytes(path, CancellationToken);

        contentBytes.Should().BeEquivalentTo(expectedBytes);
    }

    [Fact]
    public async Task ReadFileToMemoryStream_ShouldReturnFileContentAsMemoryStream()
    {
        const string path = "test.txt";
        const string expectedContent = "Test file content";

        using System.IO.MemoryStream? memoryStream = await _fileUtil.ReadToMemoryStream(path, CancellationToken);
        using var reader = new StreamReader(memoryStream);

        string content = await reader.ReadToEndAsync(CancellationToken);

        content.Should().Be(expectedContent);
    }

    [Fact]
    public async Task ReadFileAsLines_ShouldReturnFileContentAsList()
    {
        var path = "testReadFileAsLines.txt";
        var lines = new List<string> { "Line 1", "Line 2", "Line 3" };

        await _fileUtil.WriteAllLines(path, lines, CancellationToken);

        var expectedContent = new List<string> { "Line 1", "Line 2", "Line 3" };

        List<string> content = await _fileUtil.ReadAsLines(path, CancellationToken);

        content.Should().BeEquivalentTo(expectedContent);
    }

    [Fact]
    public async Task WriteFile_ShouldWriteContentToFile()
    {
        const string path = "testWriteFile.txt";
        const string content = "Test content to write";

        await _fileUtil.Write(path, content, CancellationToken);

        string? writtenContent = await System.IO.File.ReadAllTextAsync(path, CancellationToken);
        writtenContent.Should().Be(content);
    }

    [Fact]
    public async Task WriteFile_WithStream_ShouldWriteStreamContentToFile()
    {
        const string path = "testWriteFileWithStream.txt";
        const string content = "Test content to write with stream";
        using var stream = new System.IO.MemoryStream();
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(content);
        await writer.FlushAsync(CancellationToken);
        stream.Position = 0;

        await _fileUtil.Write(path, stream, CancellationToken);

        string? writtenContent = await System.IO.File.ReadAllTextAsync(path, CancellationToken);
        writtenContent.Should().Be(content);
    }

    [Fact]
    public async Task WriteFile_WithByteArray_ShouldWriteByteArrayToFile()
    {
        const string path = "testWriteFileWithByteArray.txt";
        const string content = "Test content to write with byte array";
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(content);

        await _fileUtil.Write(path, bytes, cancellationToken: CancellationToken);

        string? writtenContent = await System.IO.File.ReadAllTextAsync(path, CancellationToken);
        writtenContent.Should().Be(content);
    }

}