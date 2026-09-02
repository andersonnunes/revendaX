namespace IdentityApi.Domain.Exceptions;

/// <summary>Violação de regra de negócio: já existe um cliente com esse e-mail.</summary>
public class EmailJaCadastradoException : Exception;
