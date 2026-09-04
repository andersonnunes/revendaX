namespace VendasApi.Domain.Exceptions;

/// <summary>Conflito de estado (RFC 9110, 409): compra `Cancelada` não pode ter o pagamento confirmado (US3.3).</summary>
public class CompraCanceladaException : Exception;
