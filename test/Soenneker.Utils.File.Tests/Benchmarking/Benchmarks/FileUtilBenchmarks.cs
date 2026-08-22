using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Soenneker.Utils.File.Tests.Benchmarking.Benchmarks;

[MemoryDiagnoser]
public class FileUtilBenchmarks
{
    private readonly FileUtil _util = new(NullLogger<FileUtil>.Instance, null!);
    private string _root = null!;
    private string _textPath = null!;
    private string _moveSource = null!;
    private string _moveDestination = null!;
    private string _copyDestination = null!;

    [GlobalSetup]
    public void Setup()
    {
        _root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"file-util-benchmarks-{Guid.NewGuid():N}");
        System.IO.Directory.CreateDirectory(_root);
        _textPath = System.IO.Path.Combine(_root, "lines.txt");
        System.IO.File.WriteAllLines(_textPath, Enumerable.Range(0, 10_000).Select(static index => $"line-{index}"));

        for (var directoryIndex = 0; directoryIndex < 32; directoryIndex++)
        {
            string directory = System.IO.Path.Combine(_root, $"directory-{directoryIndex}");
            System.IO.Directory.CreateDirectory(directory);

            for (var fileIndex = 0; fileIndex < 32; fileIndex++)
                System.IO.File.WriteAllText(System.IO.Path.Combine(directory, $"file-{fileIndex}.txt"), "content");
        }

        _moveSource = System.IO.Path.Combine(_root, "move-source.bin");
        _moveDestination = System.IO.Path.Combine(_root, "move-destination.bin");
        _copyDestination = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"file-util-copy-benchmarks-{Guid.NewGuid():N}");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (System.IO.Directory.Exists(_root))
            System.IO.Directory.Delete(_root, recursive: true);

        if (System.IO.Directory.Exists(_copyDestination))
            System.IO.Directory.Delete(_copyDestination, recursive: true);
    }

    [IterationSetup(Target = nameof(MoveSameVolume))]
    public void SetupMove()
    {
        System.IO.File.Delete(_moveDestination);
        using FileStream stream = System.IO.File.Create(_moveSource);
        stream.SetLength(16 * 1024 * 1024);
    }

    [IterationSetup(Target = nameof(CopyRecursively))]
    public void SetupRecursiveCopy()
    {
        if (System.IO.Directory.Exists(_copyDestination))
            System.IO.Directory.Delete(_copyDestination, recursive: true);
    }

    [Benchmark]
    public async Task<int> ReadAsLines() => (await _util.ReadAsLines(_textPath, log: false)).Count;

    [Benchmark]
    public async Task<int> GetAllFileNames() => (await _util.GetAllFileNamesInDirectoryRecursively(_root, log: false)).Length;

    [Benchmark]
    public async Task MoveSameVolume() => await _util.Move(_moveSource, _moveDestination, log: false);

    [Benchmark]
    public async Task CopyRecursively() => await _util.CopyRecursively(_root, _copyDestination, log: false);
}
