using EmberTrace.Abstractions.Attributes;
using Harness.Core;
using Harness.Scenarios;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

[assembly: TraceId(EfScopes.ColdOpen, "ef.cold.open-connection-create-schema", "ef")]
[assembly: TraceId(EfScopes.ColdFirstQuery, "ef.cold.first-query", "ef")]
[assembly: TraceId(EfScopes.NewContext, "ef.new-context.x1k", "ef")]
[assembly: TraceId(EfScopes.PooledContext, "ef.pooled-context.x1k", "ef")]
[assembly: TraceId(EfScopes.Query, "ef.query-where-orderby-take.x1k", "ef")]
[assembly: TraceId(EfScopes.SaveChanges, "ef.save-changes-1000-entities.x1", "ef")]
[assembly: TraceId(EfScopes.DetectChanges, "ef.detect-changes-10k-tracked.x10", "ef")]
[assembly: TraceId(EfScopes.ToQueryString, "ef.to-query-string.x1k", "ef")]

namespace Harness.Scenarios;

public static class EfScopes
{
    public const int ColdOpen = 1500;
    public const int ColdFirstQuery = 1501;
    public const int NewContext = 1510;
    public const int PooledContext = 1511;
    public const int Query = 1512;
    public const int SaveChanges = 1513;
    public const int DetectChanges = 1514;
    public const int ToQueryString = 1515;
}

public sealed class Blog
{
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public string Url { get; set; } = "";

    public int Rating { get; set; }

    public List<Post> Posts { get; set; } = [];
}

public sealed class Post
{
    public int Id { get; set; }

    public string Title { get; set; } = "";

    public string Content { get; set; } = "";

    public int BlogId { get; set; }

    public Blog? Blog { get; set; }
}

public sealed class BenchContext(DbContextOptions<BenchContext> options) : DbContext(options)
{
    public DbSet<Blog> Blogs => Set<Blog>();

    public DbSet<Post> Posts => Set<Post>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Blog>(blog =>
        {
            blog.ToTable("Blogs");
            blog.HasKey(b => b.Id);
            blog.HasIndex(b => b.Rating);
            blog.HasMany(b => b.Posts).WithOne(p => p.Blog!).HasForeignKey(p => p.BlogId);
        });
        modelBuilder.Entity<Post>().ToTable("Posts");
    }
}

public sealed class EfScenario : IScenario
{
    private const int Blogs = 1000;
    private const string Schema = """
        CREATE TABLE "Blogs" ("Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, "Name" TEXT NOT NULL, "Url" TEXT NOT NULL, "Rating" INTEGER NOT NULL);
        CREATE INDEX "IX_Blogs_Rating" ON "Blogs" ("Rating");
        CREATE TABLE "Posts" ("Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, "Title" TEXT NOT NULL, "Content" TEXT NOT NULL, "BlogId" INTEGER NOT NULL REFERENCES "Blogs" ("Id") ON DELETE CASCADE);
        CREATE INDEX "IX_Posts_BlogId" ON "Posts" ("BlogId");
        """;

    private SqliteConnection _connection = null!;
    private DbContextOptions<BenchContext> _options = null!;
    private PooledDbContextFactory<BenchContext> _pool = null!;
    private BenchContext _queryContext = null!;
    private BenchContext _trackingContext = null!;
    private Post[] _tracked = [];

    public string Name => "ef";

    public void Cold(Recorder recorder)
    {
        recorder.Cold(EfScopes.ColdOpen, () =>
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();
            Execute(Schema);
            using var transaction = _connection.BeginTransaction();
            using var insert = _connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText = "INSERT INTO \"Blogs\" (\"Name\", \"Url\", \"Rating\") VALUES ($name, $url, $rating)";
            var name = insert.Parameters.Add("$name", SqliteType.Text);
            var url = insert.Parameters.Add("$url", SqliteType.Text);
            var rating = insert.Parameters.Add("$rating", SqliteType.Integer);
            for (var i = 0; i < Blogs; i++)
            {
                name.Value = $"blog-{i:0000}";
                url.Value = $"https://example.invalid/{i}";
                rating.Value = i % 100;
                insert.ExecuteNonQuery();
            }

            transaction.Commit();
        });

        _options = new DbContextOptionsBuilder<BenchContext>().UseSqlite(_connection).Options;
        recorder.Cold(EfScopes.ColdFirstQuery, () =>
        {
            using var context = new BenchContext(_options);
            Sink.Keep(context.Blogs.AsNoTracking().Where(b => b.Rating > 50).OrderBy(b => b.Name).Take(10).ToList());
        });

        _pool = new PooledDbContextFactory<BenchContext>(_options);
        _queryContext = new BenchContext(_options);
        _trackingContext = new BenchContext(_options);
        _tracked = Enumerable.Range(1, 10_000)
            .Select(i => new Post { Id = 1_000_000 + i, Title = $"title-{i}", Content = "content", BlogId = 1 + i % Blogs })
            .ToArray();
        _trackingContext.Posts.AttachRange(_tracked);
    }

    public void Iteration(Recorder recorder)
    {
        var options = _options;
        recorder.Measure(EfScopes.NewContext, 1000, () =>
        {
            for (var i = 0; i < 1000; i++)
            {
                using var context = new BenchContext(options);
                Sink.Keep(context.Model);
            }
        });

        var pool = _pool;
        recorder.Measure(EfScopes.PooledContext, 1000, () =>
        {
            for (var i = 0; i < 1000; i++)
            {
                using var context = pool.CreateDbContext();
                Sink.Keep(context.Model);
            }
        });

        var query = _queryContext;
        recorder.Measure(EfScopes.Query, 1000, () =>
        {
            for (var i = 0; i < 1000; i++)
            {
                var threshold = i % 90;
                Sink.Keep(query.Blogs.AsNoTracking().Where(b => b.Rating > threshold).OrderBy(b => b.Name).Take(10).ToList());
            }
        });

        using (var context = new BenchContext(options))
        {
            for (var i = 0; i < 1000; i++)
            {
                context.Posts.Add(new Post { Title = $"new-{i}", Content = "body", BlogId = 1 + i % Blogs });
            }

            recorder.Measure(EfScopes.SaveChanges, 1, () => Sink.Keep(context.SaveChanges()));
        }

        Execute("DELETE FROM \"Posts\"");

        var tracking = _trackingContext;
        var tracked = _tracked;
        recorder.Measure(EfScopes.DetectChanges, 10, () =>
        {
            for (var i = 0; i < 10; i++)
            {
                tracked[i * 997 % tracked.Length].Title = $"changed-{i}";
                tracking.ChangeTracker.DetectChanges();
            }
        });

        var queryable = query.Blogs.AsNoTracking().Where(b => b.Rating > 10).OrderBy(b => b.Name).Take(10);
        recorder.Measure(EfScopes.ToQueryString, 1000, () =>
        {
            for (var i = 0; i < 1000; i++)
            {
                Sink.Keep(queryable.ToQueryString());
            }
        });
    }

    public void Dispose()
    {
        _trackingContext.Dispose();
        _queryContext.Dispose();
        _connection.Dispose();
    }

    private void Execute(string sql)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}
