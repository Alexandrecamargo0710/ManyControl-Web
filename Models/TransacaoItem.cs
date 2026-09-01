namespace ManyControl_Web.Models;

public class TransacaoItem
{
    public Guid Id { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public DateTime Data { get; set; }
    public string Tipo { get; set; } = "Despesa"; // "Receita" ou "Despesa"
    public string CategoriaNome { get; set; } = "Sem categoria";
    public Guid? CategoriaId { get; set; }
    public bool Paga { get; set; }
    public bool Recebida { get; set; } = true;
    public bool Recorrente { get; set; }
    public DateTime? Vencimento { get; set; }
    public DateTime? DataPagamento { get; set; }
}
