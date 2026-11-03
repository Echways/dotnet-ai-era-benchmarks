using Microsoft.EntityFrameworkCore;

namespace Harness.Scenarios;

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
