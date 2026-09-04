namespace VendasApi.Domain.Exceptions;

/// <summary>Conflito de estado (RFC 9110, 409): só veículo `Reservado` pode ser marcado como vendido (US3.3).</summary>
public class VeiculoNaoReservadoException : Exception;
