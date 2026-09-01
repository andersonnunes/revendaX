namespace IdentityApi.Domain.Exceptions;

/// <summary>
/// Falha de infraestrutura ao falar com o provedor de identidade (rede, timeout,
/// indisponibilidade) — não é uma violação de regra de negócio do chamador, mapeia para
/// 502/503, não para 400/409/422.
/// </summary>
public class ProvedorIdentidadeIndisponivelException(string message, Exception? innerException = null)
    : Exception(message, innerException);
