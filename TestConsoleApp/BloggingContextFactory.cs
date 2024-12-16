using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TestConsoleApp.DbContext;

namespace TestConsoleApp;

public class BloggingContextFactory : IDesignTimeDbContextFactory<BloggingContext>
{
    public BloggingContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BloggingContext>();
        optionsBuilder.UseNpgsql(BloggingContext.ConnectionString, opt =>
            opt.CommandTimeout((int) TimeSpan.FromMinutes(1).TotalSeconds));
        return new BloggingContext(optionsBuilder.Options);
    }
}