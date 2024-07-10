using System.Text;
using System.Text.Json;

namespace WireMockTests;

public static class StringContentExtensions
{
    public static StringContent ToStringContent<T>(this T value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        string content = JsonSerializer.Serialize(value);

        return new StringContent(content, Encoding.UTF8, "application/json");
    }
}
