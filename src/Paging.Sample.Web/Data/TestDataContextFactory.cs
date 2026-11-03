namespace Paging.Sample.Web.Data;

public interface ITestDataContextFactory : IDisposable {
    ITestDataContext CreateDbContext();
}
public class TestDataContextFactory : ITestDataContextFactory {
    private readonly IServiceScope _scope;
    private readonly ITestDataContext _context;
    private bool _disposed;

    public TestDataContextFactory(IServiceScopeFactory scopeFactory) {
        _scope = scopeFactory?.CreateScope()
            ?? throw new ArgumentNullException(nameof(scopeFactory));
        _context = _scope.ServiceProvider.GetService<ITestDataContext>()
            ?? throw new InvalidOperationException($"No service for type '{nameof(ITestDataContext)}' has been registered.");
    }

    public ITestDataContext CreateDbContext() {
        if (_disposed)
            throw new ObjectDisposedException(nameof(TestDataContextFactory));

        return _context;
    }

    public void Dispose() {
        if (_disposed)
            return;

        _scope.Dispose();
        _disposed = true;
    }
}
