using Microsoft.Extensions.Logging;

namespace IdentityApi.Tests.Integration;

/// <summary>
/// Captura toda mensagem logada pela aplicação durante o teste (qualquer categoria, a
/// partir de Trace) — usado para provar de verdade o critério de aceite "a senha nunca
/// aparece em nenhum log", em vez de só confiar que nenhum código loga o corpo da
/// requisição.
/// </summary>
public class CapturingLoggerProvider : ILoggerProvider
{
    private readonly List<string> _sink;
    private readonly Lock _lock = new();

    public CapturingLoggerProvider(List<string> sink) => _sink = sink;

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, _sink, _lock);

    public void Dispose()
    {
    }

    private class CapturingLogger(string categoryName, List<string> sink, Lock @lock) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            var linha = exception is null
                ? $"[{categoryName}] {message}"
                : $"[{categoryName}] {message} | EX: {exception}";

            lock (@lock)
            {
                sink.Add(linha);
            }
        }
    }
}
