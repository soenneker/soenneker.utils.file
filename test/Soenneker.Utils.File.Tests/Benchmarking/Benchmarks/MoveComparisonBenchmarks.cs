using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Soenneker.Utils.File.Tests.Benchmarking.Benchmarks;

[MemoryDiagnoser]
public class MoveComparisonBenchmarks
{
    private readonly FileUtil _util = new(NullLogger<FileUtil>.Instance, null!);
    private string _root = null!;
    private string _source = null!;
    private string _destination = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"file-move-comparison-{Guid.NewGuid():N}");
        System.IO.Directory.CreateDirectory(_root);
        _source = System.IO.Path.Combine(_root, "source.bin");
        _destination = System.IO.Path.Combine(_root, "destination.bin");
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        if (System.IO.Directory.Exists(_root))
            System.IO.Directory.Delete(_root, recursive: true);
    }

    [IterationSetup(Target = nameof(CopyThenDelete))]
    public void SetupCopyThenDelete() => CreateSource();

    [IterationSetup(Target = nameof(NativeMove))]
    public void SetupNativeMove() => CreateSource();

    [Benchmark(Baseline = true)]
    public async Task CopyThenDelete()
    {
        await _util.Copy(_source, _destination, log: false);
        await _util.Delete(_source, ignoreMissing: false, log: false);
    }

    [Benchmark]
    public async Task NativeMove() => await _util.Move(_source, _destination, log: false);

    private void CreateSource()
    {
        System.IO.File.Delete(_source);
        System.IO.File.Delete(_destination);
        using FileStream stream = System.IO.File.Create(_source);
        stream.SetLength(16 * 1024 * 1024);
    }
}
