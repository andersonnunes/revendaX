namespace VendasApi.Domain.Exceptions;

/// <summary>Ano fora do intervalo permitido (1950 até o ano atual + 1) — ver US2.1.</summary>
public class AnoInvalidoException : Exception;
