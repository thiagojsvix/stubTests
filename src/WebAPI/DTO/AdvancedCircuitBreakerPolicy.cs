namespace WebAPI.DTO;

public record class AdvancedCircuitBreakerPolicy()
{
    public const string Description = "AdvancedCircuitBreakerPolicy";

    public double FailureThreshold { get; set; }
    public int SamplingDurationSeconds { get; set; }
    public int MinimumThroughput { get; set; }
    public int DurationOfBreakSeconds { get; set; }
}