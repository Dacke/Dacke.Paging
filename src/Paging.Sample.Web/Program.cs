using FastEndpoints;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Paging.Sample.Web.Data;
using Paging.Sample.Web.Mappings;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRouting();
builder.Services.AddResponseCompression();
builder.Services.AddDbContext<ITestDataContext, TestDataContext>((serviceProvider, options) => {
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var connectionStr = configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    options.UseSqlServer(connectionStr, optionsBuilder => {
        optionsBuilder.CommandTimeout(20);
    });
});
builder.Services.AddScoped<ITestDataContextFactory, TestDataContextFactory>();
builder.Services.AddSingleton<IDbConnectionFactory, SqlServerConnectionFactory>();
builder.Services.AddSingleton<IRegister>(provider => {
    return new MappingConfig();
});
builder.Services.AddFastEndpoints();

var app = builder.Build();

// Configure the HTTP request pipeline.
app
    .UseRouting()
    .UseResponseCompression()
    .UseDefaultExceptionHandler()
    .UseFastEndpoints(config => {
        config.Endpoints.RoutePrefix = "api";
        config.Errors.UseProblemDetails(cfg => {
            cfg.AllowDuplicateErrors = true;
            cfg.IndicateErrorCode = true;
            cfg.IndicateErrorSeverity = true;
        });
        config.Endpoints.Configurator = ep => ep.AllowAnonymous();
    });

using (var scope = app.Services.CreateScope()) {
    using var context = scope.ServiceProvider.GetRequiredService<ITestDataContext>();
    await context.MigrateAsync();
}

var mappingConfig = app.Services.GetRequiredService<IRegister>();
TypeAdapterConfig.GlobalSettings.Apply(mappingConfig);

app.Run();
