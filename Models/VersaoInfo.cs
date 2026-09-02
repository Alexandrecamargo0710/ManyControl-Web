namespace ManyControl_Web.Models;

public class VersaoItemDestaque
{
    public string Tipo { get; set; } = "Melhoria"; // "Ajuste", "Novidade", "Correcao", "Seguranca"
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Icone { get; set; } = "bi-check-circle";
}

public class VersaoInfo
{
    public string Numero { get; set; } = string.Empty;
    public string DataLancamento { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public bool IsAtual { get; set; }
    public List<VersaoItemDestaque> Destaques { get; set; } = [];
}
