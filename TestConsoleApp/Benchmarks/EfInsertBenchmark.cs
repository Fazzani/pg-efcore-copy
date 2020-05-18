using BenchmarkDotNet.Attributes;

namespace TestConsoleApp.Benchmarks
{
    public class EfInsertBenchmark
    {
        [Benchmark]
        [Arguments(100)]
        [Arguments(1_000_000)]
        public void Save(long count)
        {
            var posts = EfInsertionTests.GeneratePosts(count: count == default ? 100 : count);
            EfInsertionTests.Save(posts, false);
        }

        [Benchmark]
        [Arguments(100)]
        [Arguments(1_000_000)]
        public void SaveEfCopy(long count)
        {
            var posts = EfInsertionTests.GeneratePosts(count: count == default ? 100 : count);
            EfInsertionTests.Save(posts);
        }
    }
}