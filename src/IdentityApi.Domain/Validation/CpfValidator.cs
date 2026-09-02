using System.Text.RegularExpressions;

namespace IdentityApi.Domain.Validation;

/// <summary>
/// Validação de formato e dígitos verificadores de CPF — regra de negócio pura, sem
/// dependência de infraestrutura. A unicidade de CPF é responsabilidade da camada de
/// Infrastructure (consulta ao provedor de identidade), não deste validador.
/// </summary>
public static class CpfValidator
{
    public static bool IsValid(string? cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
        {
            return false;
        }

        var digits = OnlyDigits(cpf);

        if (digits.Length != 11 || digits.Distinct().Count() == 1)
        {
            return false;
        }

        var numbers = digits.Select(c => c - '0').ToArray();

        var firstCheck = CalculateCheckDigit(numbers, 9);
        if (firstCheck != numbers[9])
        {
            return false;
        }

        var secondCheck = CalculateCheckDigit(numbers, 10);
        return secondCheck == numbers[10];
    }

    /// <summary>Normaliza para só os 11 dígitos, descartando pontuação.</summary>
    public static string OnlyDigits(string cpf) => Regex.Replace(cpf, "[^0-9]", "");

    private static int CalculateCheckDigit(int[] numbers, int length)
    {
        var sum = 0;
        for (var i = 0; i < length; i++)
        {
            sum += numbers[i] * (length + 1 - i);
        }

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
}
