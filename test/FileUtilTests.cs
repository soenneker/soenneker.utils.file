using FluentAssertions;
using Soenneker.Tests.FixturedUnit;
using Soenneker.Utils.File.Abstract;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Soenneker.Utils.File.Tests
{
    [Collection("Collection")]
    public class FileUtilTests : FixturedUnitTest
    {
        private readonly IFileUtil _fileUtil;

        public FileUtilTests(Fixture fixture, ITestOutputHelper output) : base(fixture, output)
        {
            _fileUtil = Resolve<IFileUtil>(true);
        }

        private void Setup()
        {
            var path = "test.txt";
            var content = "Test file content";
            System.IO.File.WriteAllText(path, content);
        }

        [Fact]
        public async Task ReadFile_ShouldReturnFileContent()
        {
            Setup();

            var path = "test.txt";
            var expectedContent = "Test file content";

            var content = await _fileUtil.ReadFile(path);

            content.Should().Be(expectedContent);
        }

        [Fact]
        public async Task TryReadFile_WhenFileExists_ShouldReturnFileContent()
        {
            Setup();

            var path = "test.txt";
            var expectedContent = "Test file content";

            var content = await _fileUtil.TryReadFile(path);

            content.Should().Be(expectedContent);
        }

        [Fact]
        public async Task TryReadFile_WhenFileDoesNotExist_ShouldReturnNull()
        {
            var path = "nonexistent.txt";

            var content = await _fileUtil.TryReadFile(path);

            content.Should().BeNull();
        }

        [Fact]
        public async Task WriteAllLines_ShouldWriteAllLinesToFile()
        {
            var path = "testWriteAllLines.txt";
            var lines = new List<string> { "Line 1", "Line 2", "Line 3" };

            await _fileUtil.WriteAllLines(path, lines);

            var writtenLines = await System.IO.File.ReadAllLinesAsync(path);
            writtenLines.Should().BeEquivalentTo(lines);
        }

        [Fact]
        public async Task ReadFileToBytes_ShouldReturnFileContentAsBytes()
        {
            var path = "test.txt";
            var expectedContent = "Test file content";
            var expectedBytes = expectedContent.Select(c => (byte)c).ToArray();

            var contentBytes = await _fileUtil.ReadFileToBytes(path);

            contentBytes.Should().BeEquivalentTo(expectedBytes);
        }

        [Fact]
        public async Task ReadFileToMemoryStream_ShouldReturnFileContentAsMemoryStream()
        {
            var path = "test.txt";
            var expectedContent = "Test file content";

            using var memoryStream = await _fileUtil.ReadFileToMemoryStream(path);
            using var reader = new StreamReader(memoryStream);

            var content = await reader.ReadToEndAsync();

            content.Should().Be(expectedContent);
        }

        [Fact]
        public async Task ReadFileAsLines_ShouldReturnFileContentAsList()
        {
            var path = "testReadFileAsLines.txt";
            var lines = new List<string> { "Line 1", "Line 2", "Line 3" };

            await _fileUtil.WriteAllLines(path, lines);

            var expectedContent = new List<string> { "Line 1", "Line 2", "Line 3" };

            var content = await _fileUtil.ReadFileAsLines(path);

            content.Should().BeEquivalentTo(expectedContent);
        }

        [Fact]
        public async Task WriteFile_ShouldWriteContentToFile()
        {
            var path = "testWriteFile.txt";
            var content = "Test content to write";

            await _fileUtil.WriteFile(path, content);

            var writtenContent = await System.IO.File.ReadAllTextAsync(path);
            writtenContent.Should().Be(content);
        }

        [Fact]
        public async Task WriteFile_WithStream_ShouldWriteStreamContentToFile()
        {
            var path = "testWriteFileWithStream.txt";
            var content = "Test content to write with stream";
            using var stream = new System.IO.MemoryStream();
            using var writer = new StreamWriter(stream);
            writer.Write(content);
            writer.Flush();
            stream.Position = 0;

            await _fileUtil.WriteFile(path, stream);

            var writtenContent = await System.IO.File.ReadAllTextAsync(path);
            writtenContent.Should().Be(content);
        }

        [Fact]
        public async Task WriteFile_WithByteArray_ShouldWriteByteArrayToFile()
        {
            var path = "testWriteFileWithByteArray.txt";
            var content = "Test content to write with byte array";
            var bytes = System.Text.Encoding.UTF8.GetBytes(content);

            await _fileUtil.WriteFile(path, bytes);

            var writtenContent = await System.IO.File.ReadAllTextAsync(path);
            writtenContent.Should().Be(content);
        }

    }
}