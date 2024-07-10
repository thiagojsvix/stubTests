using FluentAssertions;

using JustEat.HttpClientInterception;

namespace StubTests;

/// <summary>
/// Para maiores informações consultar Site do Projeto <see href="https://github.com/justeattakeaway/httpclient-interception">httpclient-interception</see>
/// </summary>
public class HttpClientInterceptionTests
{
    [Fact]
    public async Task Teste_Happy_Path() {
        // Arrange
        var options = new HttpClientInterceptorOptions();
        var builder = new HttpRequestInterceptionBuilder();

        builder
            .Requests()
            .ForGet()
            .ForHttps()
            .ForHost("public.je-apis.com")
            .ForPath("terms")
            .Responds()
            .WithJsonContent(new { Id = 1, Link = "https://www.just-eat.co.uk/privacy-policy" })
            .RegisterWith(options);

        using var client = options.CreateHttpClient();

        // Act
        string json = await client.GetStringAsync("https://public.je-apis.com/terms");

        //Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Be("{\"Id\":1,\"Link\":\"https://www.just-eat.co.uk/privacy-policy\"}");
    }
}
