using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace TestConsoleApp.Benchmarks
{
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn]
    public class EfInsertBenchmark
    {
        [Benchmark(Baseline = true)]
        [Arguments(100)]
        [Arguments(10_000)]
        public void Save(long count)
        {
            var posts = EfInsertionTests.GeneratePosts(count: count == default ? 100 : count);
            EfInsertionTests.Save(posts, false);
        }

        [Benchmark]
        [Arguments(100)]
        [Arguments(10_000)]
        public void SaveEfCopy(long count)
        {
            var posts = EfInsertionTests.GeneratePosts(count: count == default ? 100 : count);
            EfInsertionTests.Save(posts);
        }
    }
}