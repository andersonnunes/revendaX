namespace IdentityApi.Domain.Exceptions;

/// <summary>
/// CPF presente mas com formato/dígito verificador inválido — distinto de CPF ausente
/// (400, via model binding) porque é uma regra de negócio (422), não um campo faltando.
/// </summary>
public class CpfInvalidoException : Exception;
