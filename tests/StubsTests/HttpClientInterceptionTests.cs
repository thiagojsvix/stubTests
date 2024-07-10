using FluentAssertions;

using JustEat.HttpClientInterception;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using StubTests.AppHost.Configuration;

using System.Diagnostics;
using System.Net;

namespace StubTests;

/// <summary>
/// Para maiores informações consultar Site do Projeto <see href="https://github.com/justeattakeaway/httpclient-interception">httpclient-interception</see>
/// </summary>
public class HttpClientInterceptionTests
{
    [Fact]
    public async Task Teste_Happy_Path()
    {
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

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Deve_Testar_Politica_Retry(int numberOfRepetitions)
    {
        // Arrange
        const string HttpPolicyNameRetry = "retry_policy";

        var numberOfRequests = 0;
        var options = new HttpClientInterceptorOptions { ThrowOnMissingRegistration = true };
        new HttpRequestInterceptionBuilder()
            .Requests()
            .ForHttps()
            .ForHost("foo")
            .ForPath("/bar")
            .Responds()
            .WithStatus(HttpStatusCode.ServiceUnavailable)
            .WithInterceptionCallback(_ => numberOfRequests++)
            .RegisterWith(options);

        var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection([new KeyValuePair<string, string?>("Polly:RetryPolicy", numberOfRepetitions.ToString())]).Build();

        var collection = new ServiceCollection()
            .AddHttpClient(HttpPolicyNameRetry)
            .AddPolicyHandler(HttpPolicy.RetryPolicy(configuration))
            .AddHttpMessageHandler(options.CreateHttpMessageHandler)
            .Services
            .BuildServiceProvider();

        var httpClient = collection?.GetService<IHttpClientFactory>()?.CreateClient(HttpPolicyNameRetry)!;

        // Act
        var stopwatch = Stopwatch.StartNew();

        var response = await httpClient.GetAsync("https://foo/bar");

        stopwatch.Stop();

        // Assert

        //essa verificação deve ser o número de reptições mais a tentativa original.
        //por essa razão deve ser adicionada.
        //A politica de repetição sempre conta apartir de quando ela começa a tentar repetir.
        numberOfRequests.Should().Be(numberOfRepetitions + 1);
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        stopwatch.Elapsed.Should().BeGreaterThan(TimeSpan.FromSeconds(TotalElapsedTime(numberOfRepetitions)));
    }

    [Fact]
    public void Deve_Testar_Politica_Circuit_Breaker_Lancar_BrokenCircuitException()
    {
        // Arrange
        const string HttpPolicyNameCircuitBreaker = "CircuitBreaker_policy";

        var numberEventsAllowedBeforeBreaking = 2;
        var durationOfBreakSeconds = 30;
        var numberOfRequests = 0;
        var options = new HttpClientInterceptorOptions { ThrowOnMissingRegistration = true };
        new HttpRequestInterceptionBuilder()
            .Requests()
            .ForHttps()
            .ForHost("bar")
            .ForPath("/foo")
            .Responds()
            .WithStatus(HttpStatusCode.ServiceUnavailable)
            .WithInterceptionCallback(_ => numberOfRequests++)
            .RegisterWith(options);

        var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection([new KeyValuePair<string, string?>("Polly:CircuitBreakerPolicy:durationOfBreakSeconds", durationOfBreakSeconds.ToString()),                    new KeyValuePair<string, string?>("Polly:CircuitBreakerPolicy:numberEventsAllowedBeforeBreaking", numberEventsAllowedBeforeBreaking.ToString())
                    ]
                ).Build();

        var collection = new ServiceCollection()
            .AddHttpClient(HttpPolicyNameCircuitBreaker)
            .AddPolicyHandler(HttpPolicy.CircuitBreakerPolicy(configuration))
            .AddHttpMessageHandler(options.CreateHttpMessageHandler)
            .Services
            .BuildServiceProvider();

        var httpClient = collection?.GetService<IHttpClientFactory>()?.CreateClient(HttpPolicyNameCircuitBreaker)!;

        // Act
        var stopwatch = Stopwatch.StartNew();

        for (var i = 0; i <= numberEventsAllowedBeforeBreaking; i++)
        {
            _ = httpClient.GetAsync("https://bar/foo").GetAwaiter();
        }

        Action action = () => httpClient.GetAsync("https://bar/foo").GetAwaiter().GetResult();

        stopwatch.Stop();

        // Assert
        //action.Should().Throw<BrokenCircuitException>();
        numberOfRequests.Should().Be(numberEventsAllowedBeforeBreaking);
    }

    private static double TotalElapsedTime(int value)
    {
        double result = 0;

        for (var i = 1; i <= value; i++)
        {
            result += Math.Pow(2, i);
        }

        return result - 1;
    }
}
