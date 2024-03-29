# Ef Core postgres COPY binary extension

[![Codacy Badge](https://api.codacy.com/project/badge/Grade/37b67ef9712e4b54abb810523cad311e)](https://app.codacy.com/manual/tunisienheni/pg-efcore-copy?utm_source=github.com&utm_medium=referral&utm_content=Fazzani/pg-efcore-copy&utm_campaign=Badge_Grade_Dashboard)

![NuGet Generation](https://github.com/Fazzani/pg-efcore-copy/workflows/NuGet%20Generation/badge.svg) 
![Build and test](https://github.com/Fazzani/pg-efcore-copy/workflows/Build%20and%20test/badge.svg)

Entity framework core extension to perform [Postgres copy](https://kb.objectrocket.com/postgresql/postgresql-copy-example-826)

Based on [PostgreSQLCopyHelper](https://github.com/PostgreSQLCopyHelper/PostgreSQLCopyHelper)

## Quick Start

### Installation

```shell
dotnet add package EF.Extensions.PgCopy --version 1.0.4
```

### Usage

```csharp
var options = new DbContextOptionsBuilder<BloggingContext>()
                .UseNpgsql(ConnectionString, opt => opt.CommandTimeout((int) TimeSpan.FromMinutes(1).TotalSeconds))
                .Options;

await using var dbContext = new BloggingContext(options);
await dbContext.Posts.AddRangeAsync(posts, cancellationToken);
await dbContext.Blogs.AddRangeAsync(blogs, cancellationToken);

// classic use with multiple Insert sql commands
if (!byCopy) return await dbContext.SaveChangesAsync(cancellationToken);

// function based on Postgres Copy command
var result = await dbContext.SaveByCopyChangesAsync(cancellationToken);
```

## Benchmark

``` ini

BenchmarkDotNet=v0.12.1, OS=macOS Catalina 10.15.4 (19E287) [Darwin 19.4.0]
Intel Core i7-9750H CPU 2.60GHz, 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=3.1.201
  [Host]     : .NET Core 3.1.3 (CoreCLR 4.700.20.11803, CoreFX 4.700.20.12001), X64 RyuJIT
  DefaultJob : .NET Core 3.1.3 (CoreCLR 4.700.20.11803, CoreFX 4.700.20.12001), X64 RyuJIT


```

|     Method |   count |          Mean |         Error |         StdDev |        Median |
|----------- |-------- |--------------:|--------------:|---------------:|--------------:|
|       Save |     100 |      31.87 ms |      0.934 ms |       2.726 ms |      31.27 ms |
| SaveEfCopy |     100 |      17.50 ms |      1.054 ms |       3.074 ms |      17.57 ms |
|       **Save** | **100000** | **26,117.05 ms** | **739.131 ms** | **2,156.079 ms** |
| SaveEfCopy | 100000 |  9,157.41 ms | 410.181 ms | 1,196.518 ms |
|       Save | 1000000 | 372,285.08 ms | 47,559.949 ms | 137,221.328 ms | 319,992.52 ms |
| SaveEfCopy | 1000000 |  82,974.09 ms |  2,410.985 ms |   6,878.675 ms |  81,184.00 ms |

