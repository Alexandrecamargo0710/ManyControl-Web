using ManyControl_Web.Models;

namespace ManyControl_Web.Services;

public class VersaoService
{
    public string VersaoAtual => "v1.0.15";

    public List<VersaoInfo> GetHistoricoVersoes()
    {
        return
        [
            new VersaoInfo
            {
                Numero = "v1.0.15",
                DataLancamento = "02/09/2026",
                Titulo = "Ajuste nos Campos de Data & Melhorias de Layout",
                IsAtual = true,
                Destaques =
                [
                    new VersaoItemDestaque
                    {
                        Tipo = "Ajuste",
                        Titulo = "Campos de Data sem Transbordo",
                        Descricao = "Ajuste no dimensionamento e espaçamento de todos os seletores de data (lançamentos, vencimentos e edição), corrigindo o transbordo lateral em telas de smartphones (iOS/Android) e no desktop.",
                        Icone = "bi-calendar-date-fill"
                    },
                    new VersaoItemDestaque
                    {
                        Tipo = "Melhoria",
                        Titulo = "Suporte Nativo a Dark Mode em Inputs",
                        Descricao = "Inclusão de esquema escuro nativo para os seletores de calendário e botões de alternância (toggles) integrados ao design do app.",
                        Icone = "bi-moon-stars-fill"
                    },
                    new VersaoItemDestaque
                    {
                        Tipo = "Novidade",
                        Titulo = "Notas de Atualização Detalhadas",
                        Descricao = "Substituição do texto genérico pelo histórico completo e discriminado de mudanças a cada nova versão.",
                        Icone = "bi-journal-check"
                    }
                ]
            },
            new VersaoInfo
            {
                Numero = "v1.0.14",
                DataLancamento = "28/08/2026",
                Titulo = "Sincronização Google Drive & Balanço Visual",
                IsAtual = false,
                Destaques =
                [
                    new VersaoItemDestaque
                    {
                        Tipo = "Novidade",
                        Titulo = "Sincronização com Google Drive",
                        Descricao = "Backup automático e sincronização em nuvem diretamente na sua pasta do Google Drive.",
                        Icone = "bi-google"
                    },
                    new VersaoItemDestaque
                    {
                        Tipo = "Melhoria",
                        Titulo = "Card de Balanço do Mês",
                        Descricao = "Visualização rápida de comprometimento de renda e saldo líquido mensal.",
                        Icone = "bi-wallet2"
                    }
                ]
            },
            new VersaoInfo
            {
                Numero = "v1.0.13",
                DataLancamento = "20/08/2026",
                Titulo = "Despesas Recorrentes & Filtros no Extrato",
                IsAtual = false,
                Destaques =
                [
                    new VersaoItemDestaque
                    {
                        Tipo = "Novidade",
                        Titulo = "Despesas Recorrentes Automáticas",
                        Descricao = "Criação automática de despesas fixas a cada virada de mês.",
                        Icone = "bi-arrow-repeat"
                    },
                    new VersaoItemDestaque
                    {
                        Tipo = "Melhoria",
                        Titulo = "Busca e Filtros no Extrato",
                        Descricao = "Filtragem instantânea por status de pagamento (paga/pendente) e busca textual por descrição.",
                        Icone = "bi-search"
                    }
                ]
            }
        ];
    }

    public VersaoInfo GetVersaoAtualInfo()
    {
        return GetHistoricoVersoes().First(v => v.IsAtual);
    }
}
