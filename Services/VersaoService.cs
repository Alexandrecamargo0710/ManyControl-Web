using System.Net.Http.Json;
using ManyControl_Web.Models;

namespace ManyControl_Web.Services;

public class VersaoService
{
    private readonly HttpClient _http;
    private List<VersaoInfo> _versoes = [];
    private bool _loaded = false;

    public VersaoService(HttpClient http)
    {
        _http = http;
        _versoes = GetFallbackVersoes();
    }

    public string VersaoAtual => GetVersaoAtualInfo().Numero;

    public async Task<List<VersaoInfo>> CarregarChangelogAsync()
    {
        try
        {
            // Busca o changelog.json com timestamp para nunca ficar preso em cache do navegador
            var url = $"changelog.json?t={DateTime.UtcNow.Ticks}";
            var dados = await _http.GetFromJsonAsync<List<VersaoInfo>>(url);
            if (dados != null && dados.Count > 0)
            {
                _versoes = dados;
                _loaded = true;
                return _versoes;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Aviso ao carregar changelog dinâmico: {ex.Message}");
        }

        if (!_loaded)
        {
            _versoes = GetFallbackVersoes();
        }

        return _versoes;
    }

    public List<VersaoInfo> GetHistoricoVersoes()
    {
        return _versoes;
    }

    public VersaoInfo GetVersaoAtualInfo()
    {
        return _versoes.FirstOrDefault(v => v.IsAtual) ?? _versoes.FirstOrDefault() ?? new VersaoInfo
        {
            Numero = "v1.0.17",
            DataLancamento = "02/09/2026",
            Titulo = "Tema Claro (Light Mode) & Alternador Dinâmico",
            IsAtual = true
        };
    }

    private List<VersaoInfo> GetFallbackVersoes()
    {
        return
        [
            new VersaoInfo
            {
                Numero = "v1.0.17",
                DataLancamento = "02/09/2026",
                Titulo = "Tema Claro (Light Mode) & Alternador Dinâmico",
                IsAtual = true,
                Destaques =
                [
                    new VersaoItemDestaque
                    {
                        Tipo = "Novidade",
                        Titulo = "Tema Claro (Light Mode)",
                        Descricao = "Implementação completa do tema claro elegante com alto contraste para todas as telas, cards de métricas, extratos, formulários e modais.",
                        Icone = "bi-sun-fill"
                    },
                    new VersaoItemDestaque
                    {
                        Tipo = "Melhoria",
                        Titulo = "Alternador Rápido de Temas",
                        Descricao = "Botão de alternância instantânea no cabeçalho sincronizado em tempo real com a tela de configurações e persistência automática no seu dispositivo.",
                        Icone = "bi-moon-stars-fill"
                    },
                    new VersaoItemDestaque
                    {
                        Tipo = "Ajuste",
                        Titulo = "Alinhamento e Centralização dos Botões",
                        Descricao = "Botões com altura mínima de 44px e texto 100% centralizado na horizontal e vertical em smartphones.",
                        Icone = "bi-layout-text-window"
                    }
                ]
            },
            new VersaoInfo
            {
                Numero = "v1.0.16",
                DataLancamento = "02/09/2026",
                Titulo = "Alinhamento de Botões, Remoção de Emojis & PWA Instantâneo",
                IsAtual = false,
                Destaques =
                [
                    new VersaoItemDestaque
                    {
                        Tipo = "Ajuste",
                        Titulo = "Alinhamento e Centralização dos Botões",
                        Descricao = "Correção do alinhamento do botão 'Sincronizar Agora' e botões em tela de celulares.",
                        Icone = "bi-layout-text-window"
                    }
                ]
            },
            new VersaoInfo
            {
                Numero = "v1.0.15",
                DataLancamento = "02/09/2026",
                Titulo = "Ajuste nos Campos de Data & Dark Mode",
                IsAtual = false,
                Destaques =
                [
                    new VersaoItemDestaque
                    {
                        Tipo = "Ajuste",
                        Titulo = "Campos de Data sem Transbordo",
                        Descricao = "Ajuste no dimensionamento dos seletores de data e suporte a Dark Mode nativo.",
                        Icone = "bi-calendar-date-fill"
                    }
                ]
            }
        ];
    }
}
