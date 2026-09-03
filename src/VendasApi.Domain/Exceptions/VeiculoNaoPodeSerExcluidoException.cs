namespace VendasApi.Domain.Exceptions;

/// <summary>Conflito de estado (RFC 9110, 409): só veículo `Disponivel` pode ser excluído (US2.5).</summary>
public class VeiculoNaoPodeSerExcluidoException : Exception;
