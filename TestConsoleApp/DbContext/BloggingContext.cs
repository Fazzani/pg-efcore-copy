using System;
using Microsoft.EntityFrameworkCore;

namespace TestConsoleApp.DbContext
{
    public class BloggingContext : Microsoft.EntityFrameworkCore.DbContext
    {
        private const string ConnectionString = "Host=localhost;Port=54322;Database=dbtest;Username=db_user;Password=dtpass";

        public DbSet<Blog> Blogs { get; set; }
        public DbSet<Post> Posts { get; set; }

        public BloggingContext(DbContextOptions<BloggingContext> options)
            : base(options)
        {
        }

        public BloggingContext()
        {
            
        }
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(ConnectionString, opt =>
             opt.CommandTimeout((int) TimeSpan.FromMinutes(1).TotalSeconds));
        }
    }
}