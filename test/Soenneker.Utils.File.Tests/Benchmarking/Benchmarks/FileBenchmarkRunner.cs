using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Soenneker.Benchmarking.Extensions.Summary;
using Soenneker.Tests.Benchmark;
using System.Threading.Tasks;

namespace Soenneker.Utils.File.Tests.Benchmarking.Benchmarks;

public class FileBenchmarkRunner : BenchmarkTest
{
    [Skip("Manual")]
    public async ValueTask FileUtil()
    {
        Summary summary = BenchmarkRunner.Run<FileUtilBenchmarks>(DefaultConf);
        await summary.OutputSummaryToLog();
    }

    [Skip("Manual")]
    public async ValueTask MoveComparison()
    {
        Summary summary = BenchmarkRunner.Run<MoveComparisonBenchmarks>(DefaultConf);
        await summary.OutputSummaryToLog();
    }
}
