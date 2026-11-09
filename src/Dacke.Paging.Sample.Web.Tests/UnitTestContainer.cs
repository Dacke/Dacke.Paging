using Dacke.Paging.Sample.Web.Mappings;
using Mapster;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace Dacke.Paging.Sample.Web.Tests;

public class UnitTestContainer {
    public required IHttpContextAccessor contextAccessor;

    public UnitTestContainer() {
        //  Http Context Accessor
        var context = BuildMockHttpContext();
        contextAccessor = Substitute.For<IHttpContextAccessor>();
        contextAccessor.HttpContext.Returns(context);

        //  Mapster Config
        var mappingConfig = new MappingConfig();
        TypeAdapterConfig.GlobalSettings.Apply(mappingConfig);
        TypeAdapterConfig.GlobalSettings.Compile(failFast: true);
    }


    private static HttpContext BuildMockHttpContext() {
        var mockRequest = Substitute.For<HttpRequest>();
        mockRequest.Scheme.Returns("https");
        mockRequest.Host.Returns(new HostString("localhost"));
        var httpContext = Substitute.For<HttpContext>();
        httpContext.Request.Returns(mockRequest);

        return httpContext;
    }
}

[CollectionDefinition(GlobalConstants.UnitTestCollection)]
public class UnitTestCollection : ICollectionFixture<UnitTestContainer> { }
