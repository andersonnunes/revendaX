namespace VendasApi.Domain.Exceptions;

/// <summary>Conflito de estado (RFC 9110, 409): veículo `Vendido` não pode ser editado (US2.2).</summary>
public class VeiculoVendidoException : Exception;
