using Microsoft.Extensions.Logging;

namespace Seminario.UnitTests.Helpers;

/// <summary>
/// ILogger que guarda cada entrada con sus valores estructurados, para poder leer
/// datos que el codigo solo emite por log (por ejemplo el codigo MFA en desarrollo).
/// </summary>
internal sealed class CapturingLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, string Message, IReadOnlyDictionary<string, object?> Values)> Entries { get; } = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var values = new Dictionary<string, object?>();
        if (state is IEnumerable<KeyValuePair<string, object?>> pares)
        {
            foreach (var par in pares)
            {
                values[par.Key] = par.Value;
            }
        }

        Entries.Add((logLevel, formatter(state, exception), values));
    }
}
