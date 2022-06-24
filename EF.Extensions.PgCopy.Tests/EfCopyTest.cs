using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EF.Extensions.PgCopy.Tests.DbContext;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EF.Extensions.PgCopy.Tests
{
    public class EfCopyTest : IDisposable
    {
        public static readonly string ConnectionString =
            "Host=localhost;Port=54322;Database=dbtest;Username=db_user;Password=dtpass";

        private BloggingContext _dbContext;

        public EfCopyTest()
        {
            var options = new DbContextOptionsBuilder<BloggingContext>()
                .UseNpgsql(ConnectionString, opt => opt.CommandTimeout((int) TimeSpan.FromMinutes(1).TotalSeconds))
                .Options;

            _dbContext = new BloggingContext(options);
        }

        [Fact]
        public void SaveCopyTest()
        {
            var posts = GeneratePosts();
            var blogs = GenerateBlogs();
            _dbContext.Posts.AddRange(posts);
            _dbContext.Blogs.AddRange(blogs);

            _dbContext.SaveByCopyChanges();

            var blogs_count = _dbContext.Blogs.Count();
            var posts_count = _dbContext.Posts.Count();

            Assert.Equal(blogs.Count(), blogs_count);
            Assert.Equal(posts.Count(), posts_count);
        }

        [Fact]
        public void SaveCopyAsyncTest()
        {
            var posts = GeneratePosts();
            var blogs = GenerateBlogs();
            _dbContext.Posts.AddRange(posts);
            _dbContext.Blogs.AddRange(blogs);

            _dbContext.SaveByCopyChanges();

            var blogs_count = _dbContext.Blogs.Count();
            var posts_count = _dbContext.Posts.Count();

            Assert.Equal(blogs.Count(), blogs_count);
            Assert.Equal(posts.Count(), posts_count);
        }

        [Fact]
        public async Task SaveCopyGraphAsyncTest()
        {
            var post = new Post
            {
                Online = true,
                Content = @"Some Content for unit test",
                Title = "Post title",
                Blog = new Blog
                {
                    Url = "http://blog.com"
                }
            };

            await _dbContext.Posts.AddAsync(post);

            await _dbContext.SaveByCopyChangesAsync();

            var blogsCount = await _dbContext.Blogs.CountAsync();
            var postsCount = await _dbContext.Posts.CountAsync();

            Assert.Equal(1, blogsCount);
            Assert.Equal(1, postsCount);

            var expected = await _dbContext.Blogs
                .Where(x => Microsoft.EntityFrameworkCore.EF.Functions.Like(x.Url, "%blog.com%"))
                .CountAsync();

            Assert.Equal(1, expected);
        }

        private static IEnumerable<Post> GeneratePosts(string title = "default title", long count = 100)
        {
            for (var i = 0; i < count; i++)
            {
                yield return new Post
                {
                    Online = i % 2 == 0,
                    Content = $"Post some content {Guid.NewGuid().ToString()} into {title}-{i}",
                    PostDate = DateTime.UtcNow,
                    Title = $"{title}-{i}",
                    CreationDateTime = DateTime.UtcNow
                };
            }
        }

        private static IEnumerable<Blog> GenerateBlogs(string url = "default url", long count = 100)
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

        public void Dispose()
        {
            _dbContext.Database.ExecuteSqlRaw(@"TRUNCATE TABLE post RESTART IDENTITY CASCADE;
                TRUNCATE TABLE blog RESTART IDENTITY CASCADE;");
            _dbContext.Dispose();
        }
    }
}