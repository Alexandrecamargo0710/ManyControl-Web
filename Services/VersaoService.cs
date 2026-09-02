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
            Numero = "v1.0.16",
            DataLancamento = "02/09/2026",
            Titulo = "Atualização do Sistema",
            IsAtual = true
        };
    }

    private List<VersaoInfo> GetFallbackVersoes()
    {
        return
        [
            new VersaoInfo
            {
                Numero = "v1.0.16",
                DataLancamento = "02/09/2026",
                Titulo = "Alinhamento de Botões, Remoção de Emojis & PWA Instantâneo",
                IsAtual = true,
                Destaques =
                [
                    new VersaoItemDestaque
                    {
                        Tipo = "Ajuste",
                        Titulo = "Alinhamento e Centralização dos Botões",
                        Descricao = "Correção do alinhamento do botão 'Sincronizar Agora' e botões em tela de celulares, eliminando quebras de linha e centralizando o texto com padrão de toque móvel.",
                        Icone = "bi-layout-text-window"
                    },
                    new VersaoItemDestaque
                    {
                        Tipo = "Melhoria",
                        Titulo = "Remoção de Emojis dos Botões",
                        Descricao = "Substituição de emojis por ícones vetoriais elegantes e padronizados do Bootstrap Icons em todas as telas e seletores.",
                        Icone = "bi-slash-circle"
                    },
                    new VersaoItemDestaque
                    {
                        Tipo = "Novidade",
                        Titulo = "Atualização Imediata do PWA",
                        Descricao = "Implementação de ativação instantânea no Service Worker (skipWaiting e clients.claim) e limpeza forçada de cache com recarregamento em 1 clique.",
                        Icone = "bi-lightning-charge-fill"
                    },
                    new VersaoItemDestaque
                    {
                        Tipo = "Ajuste",
                        Titulo = "Campos de Data sem Transbordo",
                        Descricao = "Ajuste definitivo no dimensionamento dos campos de data para impedir qualquer transbordo lateral em telas de smartphones (iOS/Android) e no desktop.",
                        Icone = "bi-calendar-date-fill"
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
                    }
                ]
            }
        ];
    }
}
