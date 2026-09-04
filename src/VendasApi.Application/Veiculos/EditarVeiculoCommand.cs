using System.ComponentModel.DataAnnotations;

namespace VendasApi.Application.Veiculos;

/// <summary>
/// Comando de entrada do caso de uso "editar veículo" — corpo de PUT /veiculos/{id}. Sem
/// `Placa`/`Status`: são imutáveis por este endpoint (ver `Veiculo.AtualizarDados` e o
/// refinamento da US2.2). `Ano`/`Preco` sem `[Required]` pelo mesmo motivo do
/// `CadastrarVeiculoCommand` (US2.1) — a validação de intervalo/positividade é regra de
/// negócio (422), não checagem de formato.
/// </summary>
public class EditarVeiculoCommand
{
    [Required(ErrorMessage = "marca é obrigatória")]
    public string Marca { get; set; } = string.Empty;

    [Required(ErrorMessage = "modelo é obrigatório")]
    public string Modelo { get; set; } = string.Empty;

    public int Ano { get; set; }

    [Required(ErrorMessage = "cor é obrigatória")]
    public string Cor { get; set; } = string.Empty;

    public decimal Preco { get; set; }
}
