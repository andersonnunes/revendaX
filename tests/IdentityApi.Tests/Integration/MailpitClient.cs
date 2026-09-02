using System.Text.Json;

namespace IdentityApi.Tests.Integration;

/// <summary>Cliente mínimo da API REST do Mailpit — só o que os testes de US1.4 precisam.</summary>
public class MailpitClient(HttpClient httpClient)
{
    /// <summary>Quantas mensagens (de qualquer momento da vida do container) têm esse endereço em "To".</summary>
    public async Task<int> ContarMensagensParaAsync(string email)
    {
        using var response = await httpClient.GetAsync("api/v1/messages?limit=100");
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var mensagens = doc.RootElement.GetProperty("messages");

        var count = 0;
        foreach (var mensagem in mensagens.EnumerateArray())
        {
            foreach (var destinatario in mensagem.GetProperty("To").EnumerateArray())
            {
                if (string.Equals(destinatario.GetProperty("Address").GetString(), email, StringComparison.OrdinalIgnoreCase))
                {
                    count++;
                }
            }
        }

        return count;
    }
}
