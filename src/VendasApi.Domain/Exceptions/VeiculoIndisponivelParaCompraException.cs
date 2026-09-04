namespace VendasApi.Domain.Exceptions;

/// <summary>Conflito de estado (RFC 9110, 409): só veículo `Disponivel` pode ser comprado (US3.1).</summary>
public class VeiculoIndisponivelParaCompraException : Exception;
