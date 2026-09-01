namespace IdentityApi.Domain.Exceptions;

/// <summary>Violação de regra de negócio: já existe um cliente com esse CPF.</summary>
public class CpfJaCadastradoException : Exception;
