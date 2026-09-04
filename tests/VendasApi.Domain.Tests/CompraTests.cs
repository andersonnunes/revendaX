using VendasApi.Domain.Compras;
using VendasApi.Domain.Exceptions;

namespace VendasApi.Domain.Tests;

public class CompraTests
{
    [Fact]
    public void Iniciar_DadosValidos_CriaCompraPendente()
    {
        var veiculoId = Guid.NewGuid();

        var compra = Compra.Iniciar(veiculoId, "cliente-123", 89900.00m);

        Assert.NotEqual(Guid.Empty, compra.Id);
        Assert.Equal(veiculoId, compra.VeiculoId);
        Assert.Equal("cliente-123", compra.ClienteId);
        Assert.Equal(89900.00m, compra.Preco);
        Assert.Equal(StatusCompra.Pendente, compra.Status);
    }

    [Fact]
    public void Iniciar_GeraIdsDiferentesParaCadaCompra()
    {
        var primeira = Compra.Iniciar(Guid.NewGuid(), "cliente-123", 89900.00m);
        var segunda = Compra.Iniciar(Guid.NewGuid(), "cliente-123", 89900.00m);

        Assert.NotEqual(primeira.Id, segunda.Id);
    }

    [Fact]
    public void ConfirmarPagamento_CompraPendente_TornaConcluida()
    {
        var compra = Compra.Iniciar(Guid.NewGuid(), "cliente-123", 89900.00m);

        compra.ConfirmarPagamento();

        Assert.Equal(StatusCompra.Concluida, compra.Status);
    }

    [Fact]
    public void ConfirmarPagamento_CompraCancelada_LancaCompraCanceladaException()
    {
        var compra = Compra.Iniciar(Guid.NewGuid(), "cliente-123", 89900.00m);
        AjustarStatus(compra, StatusCompra.Cancelada);

        Assert.Throws<CompraCanceladaException>(compra.ConfirmarPagamento);
    }

    [Fact]
    public void Cancelar_CompraPendente_TornaCancelada()
    {
        var compra = Compra.Iniciar(Guid.NewGuid(), "cliente-123", 89900.00m);

        compra.Cancelar();

        Assert.Equal(StatusCompra.Cancelada, compra.Status);
    }

    [Theory]
    [InlineData(StatusCompra.Concluida)]
    [InlineData(StatusCompra.Cancelada)]
    public void Cancelar_CompraNaoPendente_NaoAlteraStatus(StatusCompra status)
    {
        var compra = Compra.Iniciar(Guid.NewGuid(), "cliente-123", 89900.00m);
        AjustarStatus(compra, status);

        compra.Cancelar(); // idempotente — não deve lançar nem alterar

        Assert.Equal(status, compra.Status);
    }

    /// <summary>
    /// Não existe (ainda) nenhuma operação pública que leve uma compra a `Cancelada`/`Concluida`
    /// diretamente para fins deste teste — reflexão é o jeito honesto de testar os guard
    /// clauses sem vazar um setter de teste na entidade.
    /// </summary>
    private static void AjustarStatus(Compra compra, StatusCompra status)
    {
        var propriedade = typeof(Compra).GetProperty(nameof(Compra.Status))!;
        propriedade.SetValue(compra, status);
    }
}
