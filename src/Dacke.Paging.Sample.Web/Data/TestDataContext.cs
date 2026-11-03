using Dacke.Paging.Sample.Web.Data.Models;
using Microsoft.EntityFrameworkCore;
using Dacke.Paging.Sample.Web.Data.Configuration;

namespace Dacke.Paging.Sample.Web.Data;

public interface ITestDataContext : IDisposable, IAsyncDisposable {
    DbSet<Person> People { get; set; }

    Task<bool> EnsureCreatedAsync(CancellationToken cancellationToken = default);
    Task MigrateAsync(CancellationToken cancellation = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default);
}

public class TestDataContext : DbContext, ITestDataContext {
    public DbSet<Person> People { get; set; }

    public Task<bool> EnsureCreatedAsync(CancellationToken cancellationToken = default) => Database.EnsureCreatedAsync(cancellationToken);

    public Task MigrateAsync(CancellationToken cancellation = default) => Database.MigrateAsync(cancellation);

    public TestDataContext(DbContextOptions<TestDataContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new PersonConfiguration());
    }
}