using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EF.Extensions.PgCopy;
using Microsoft.EntityFrameworkCore;
using TestConsoleApp.DbContext;

namespace TestConsoleApp
{
    public class EfInsertionTests
    {
        public static readonly string ConnectionString =
            "Host=localhost;Port=54322;Database=dbtest;Username=db_user;Password=dtpass";

        public static IEnumerable<Post> GeneratePosts(string title = "default title", long count = 100)
        {
            for (var i = 0; i < count; i++)
            {
                yield return new Post
                {
                    Online = i % 2 == 0,
                    // BlogId = 1,
                    Content = $"Post some content {Guid.NewGuid().ToString()} into {title}-{i}",
                    PostDate = DateTime.UtcNow,
                    Title = $"{title}-{i}",
                    CreationDateTime = DateTime.UtcNow
                };
            }
        }
        
        public static IEnumerable<Blog> GenerateBlogs(string url = "default url", long count = 100)
        {
            for (var i = 0; i < count; i++)
            {
                yield return new Blog
                {
                    Url = $"https://{url}/{i}",
                    CreationDateTime = DateTime.UtcNow
                };
            }
        }

        public static async Task<int> SaveAsync(IEnumerable<Post> posts, bool byCopy = true,
            CancellationToken cancellationToken = default)
        {
            var options = new DbContextOptionsBuilder<BloggingContext>()
                .UseNpgsql(ConnectionString, opt => opt.CommandTimeout((int) TimeSpan.FromMinutes(1).TotalSeconds))
                .Options;

            await using var dbContext = new BloggingContext(options);
            await dbContext.Posts.AddRangeAsync(posts, cancellationToken);
            if (byCopy)
                return await dbContext.SaveByCopyChangesAsync(cancellationToken);
            return await dbContext.SaveChangesAsync(cancellationToken);
        }

        public static int Save(IEnumerable<Post> posts, bool byCopy = true)
        {
            var options = new DbContextOptionsBuilder<BloggingContext>()
                .UseNpgsql(ConnectionString)
                .Options;

            using var dbContext = new BloggingContext(options);
            dbContext.Posts.AddRange(posts);
            return byCopy ? dbContext.SaveByCopyChanges() : dbContext.SaveChanges();
        }

        /// <summary>
        /// Saving blogs and posts
        /// </summary>
        /// <param name="posts"></param>
        /// <param name="byCopy"></param>
        /// <param name="callSaveChangeMethodAnyway">Call SaveChanges anyway</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<int> SaveAllAsync(IEnumerable<Post> posts, IEnumerable<Blog> blogs, bool byCopy = true,
           bool callSaveChangeMethodAnyway =false, CancellationToken cancellationToken = default)
        {
            var options = new DbContextOptionsBuilder<BloggingContext>()
                .UseNpgsql(ConnectionString, opt => opt.CommandTimeout((int) TimeSpan.FromMinutes(1).TotalSeconds))
                .Options;

            await using var dbContext = new BloggingContext(options);
            await dbContext.Posts.AddRangeAsync(posts, cancellationToken);
            await dbContext.Blogs.AddRangeAsync(blogs, cancellationToken);
            
            if (!byCopy) return await dbContext.SaveChangesAsync(cancellationToken);
            
            var result = await dbContext.SaveByCopyChangesAsync(cancellationToken);
            if (callSaveChangeMethodAnyway)
            {
                result += await dbContext.SaveChangesAsync(cancellationToken);
            }
            return result;
        }
    }
}