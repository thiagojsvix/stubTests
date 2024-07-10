using Polly;
using Polly.Extensions.Http;

using System.Diagnostics.CodeAnalysis;
using System.Net;

using WebAPI.DTO;

namespace StubTests.AppHost.Configuration;

public static class HttpPolicy
{
    /// <summary>
    /// Método repsonsável por configurar Policita de Retentatva
    /// </summary>
    /// <param name="configuration">utilizado para pegar as configuração do AppSettings</param>
    public static IAsyncPolicy<HttpResponseMessage> RetryPolicy(IConfiguration configuration)
    {
        var count = 1;

        _ = int.TryParse(configuration["Polly:RetryPolicy"], out var retryPolicy);

        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg =>
            {
                _ = msg.Headers.TryAddWithoutValidation("retry", count.ToString());
                return msg.StatusCode == HttpStatusCode.NotFound;
            })
            .WaitAndRetryAsync(retryPolicy, retryAttempt =>
            {
                count++;
                return TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
            });
    }

    /// <summary>
    /// Método utilizado para configuração o CircutBreake de forma Básica.
    /// </summary>
    /// <param name="configuration">utilizado para pegar as configuração do AppSettings</param>
    public static IAsyncPolicy<HttpResponseMessage> CircuitBreakerPolicy(IConfiguration configuration)
    {
        _ = int.TryParse(configuration["Polly:CircuitBreakerPolicy:numberEventsAllowedBeforeBreaking"], out var numberEventsAllowedBeforeBreaking);
        _ = int.TryParse(configuration["Polly:CircuitBreakerPolicy:durationOfBreakSeconds"], out var durationOfBreakSeconds);

        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(numberEventsAllowedBeforeBreaking, TimeSpan.FromSeconds(durationOfBreakSeconds), OnBreak, OnReset, OnHalfOpen);
    }

    /// <summary>
    /// <para>Implementa padrão avançado de Circuit Break.</para>
    /// <para>Consulte a implementação do método para exemplo do funcionamento</para>
    /// </summary>
    /// <param name="configuration">Parametros para configuração do Circuite Breaker</param>
    /// <returns>Retorna uma Politica Avançada para o Circuite Breaker</returns>
    [ExcludeFromCodeCoverage]
    public static IAsyncPolicy<HttpResponseMessage> AdvancedCircuitBreakerPolicy(IConfiguration configuration)
    {
        /*
         *  Considere o exemplo abaixo para explicação
         * 
         *  Esta é a política do Advanced Circuit Breaker: o circuito será cortado se 30% das solicitações falharem 
         *  em uma janela de 60 segundos, com um mínimo de 10 solicitações na janela de 30 segundos, então 
         *  o circuito deverá ser aberto por 30 segundos:
         * 
         *   return HttpPolicyExtensions
         *           .HandleTransientHttpError()
         *           .AdvancedCircuitBreakerAsync(0.30, TimeSpan.FromSeconds(60), 10, TimeSpan.FromSeconds(30), OnBreak, OnReset, OnHalfOpen);
         */
        var advancedCircuiteBreaker = new AdvancedCircuitBreakerPolicy();
        configuration.GetSection("AdvancedCircuitBreakerPolicy").Bind(advancedCircuiteBreaker);

        return HttpPolicyExtensions
                .HandleTransientHttpError()
                .AdvancedCircuitBreakerAsync(advancedCircuiteBreaker.FailureThreshold,
                TimeSpan.FromSeconds(advancedCircuiteBreaker.SamplingDurationSeconds),
                advancedCircuiteBreaker.SamplingDurationSeconds,
                TimeSpan.FromSeconds(advancedCircuiteBreaker.DurationOfBreakSeconds),
                OnBreak,
                OnReset,
                OnHalfOpen);
    }

    [ExcludeFromCodeCoverage]
    private static void OnHalfOpen()
    {
        ShowCircuitState("Circuit in test mode, one request will be allowed.", ConsoleColor.Yellow);
    }

    [ExcludeFromCodeCoverage]
    private static void OnReset()
    {
        ShowCircuitState("Circuit closed, requests flow normally.", ConsoleColor.Green);
    }

    [ExcludeFromCodeCoverage]
    private static void OnBreak(DelegateResult<HttpResponseMessage> result, TimeSpan ts)
    {
        ShowCircuitState("Circuit cut, requests will not flow.", ConsoleColor.Red);
    }

    private static void ShowCircuitState(string descStatus, ConsoleColor backgroundColor)
    {
        var previousBackgroundColor = Console.BackgroundColor;
        var previousForegroundColor = Console.ForegroundColor;

        Console.BackgroundColor = backgroundColor;
        Console.ForegroundColor = ConsoleColor.Black;

        Console.Out.WriteLine(descStatus);

        Console.BackgroundColor = previousBackgroundColor;
        Console.ForegroundColor = previousForegroundColor;
    }
}
