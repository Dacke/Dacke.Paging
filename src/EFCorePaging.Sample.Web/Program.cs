using EFCorePaging.Sample.Web.Data;
using EFCorePaging.Sample.Web.Mappings;
using FastEndpoints;
using Mapster;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRouting();
builder.Services.AddResponseCompression();
builder.Services.AddDbContext<ITestDataContext, TestDataContext>((serviceProvider, options) => {
    var connectionStr = @"Data Source=localhost\SQL2022;Initial Catalog=TestData;Integrated Security=SSPI;Encrypt=true;TrustServerCertificate=true;app=Prototype";
    options.UseSqlServer(connectionStr, optionsBuilder => {
        optionsBuilder.CommandTimeout(20);
    });
});
builder.Services.AddScoped<ITestDataContextFactory, TestDataContextFactory>();
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
