using System;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Running;
using TestConsoleApp.Benchmarks;

namespace TestConsoleApp
{
    class Program
    {
        /// <summary>
        /// main
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static async Task Main(string[] args)
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

            await TestEfInsertionExtenstionAsync(CancellationToken.None);
            new EfInsertBenchmark().SaveEfCopy(100);
            var summary = BenchmarkRunner.Run(typeof(EfInsertBenchmark));
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }

        public static Task TestEfInsertionExtenstionAsync(CancellationToken cancellationToken = default)
        {
            var posts = EfInsertionTests.GeneratePosts();
            var blogs = EfInsertionTests.GenerateBlogs();
            var t1 = EfInsertionTests.SaveAllAsync(posts, blogs, false, false, cancellationToken);
            var t2 = EfInsertionTests.SaveAllAsync(posts, blogs, true, false, cancellationToken);
            return Task.WhenAll(t1, t2);
        }

        public static void TestEfInsertionExtenstion()
        {
            var list = EfInsertionTests.GeneratePosts();
            EfInsertionTests.Save(list, false);
            EfInsertionTests.Save(list);
        }
    }
}