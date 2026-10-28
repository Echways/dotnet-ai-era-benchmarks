using BenchmarkDotNet.Attributes;
using Harness.Scenarios;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Bdn.TrackA;

[MemoryDiagnoser]
public class EfBenchmarks
{
    private SqliteConnection _connection = null!;
    private DbContextOptions<BenchContext> _options = null!;
    private PooledDbContextFactory<BenchContext> _pool = null!;
    private BenchContext _context = null!;
    private IQueryable<Blog> _queryable = null!;
    private int _cursor;

    [GlobalSetup]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<BenchContext>().UseSqlite(_connection).Options;
        using (var context = new BenchContext(_options))
        {
            context.Database.EnsureCreated();
            context.Blogs.AddRange(Enumerable.Range(0, 1000).Select(i => new Blog { Name = $"blog-{i:0000}", Url = $"https://example.invalid/{i}", Rating = i % 100 }));
            context.SaveChanges();
        }

        _pool = new PooledDbContextFactory<BenchContext>(_options);
        _context = new BenchContext(_options);
        _queryable = _context.Blogs.AsNoTracking().Where(b => b.Rating > 10).OrderBy(b => b.Name).Take(10);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Benchmark]
    public object NewContext()
    {
        using var context = new BenchContext(_options);
        return context.Model;
    }

    [Benchmark]
    public object PooledContext()
    {
        using var context = _pool.CreateDbContext();
        return context.Model;
    }

    [Benchmark]
    public List<Blog> QueryWhereOrderByTake()
    {
        var threshold = _cursor++ % 90;
        return _context.Blogs.AsNoTracking().Where(b => b.Rating > threshold).OrderBy(b => b.Name).Take(10).ToList();
    }

    [Benchmark]
    public string ToQueryString() => _queryable.ToQueryString();
}
