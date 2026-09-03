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
            Numero = "v1.0.19",
            DataLancamento = "03/09/2026",
            Titulo = "Correção de Sincronização e Backup Local",
            IsAtual = true
        };
    }

    private List<VersaoInfo> GetFallbackVersoes()
    {
        return
        [
            new VersaoInfo
            {
                Numero = "v1.0.19",
                DataLancamento = "03/09/2026",
                Titulo = "Correção de Sincronização e Backup Local",
                IsAtual = true,
                Destaques =
                [
                    new VersaoItemDestaque
                    {
                        Tipo = "Ajuste",
                        Titulo = "Restauração e Exportação de Backup Local",
                        Descricao = "Seletor de arquivos nativo e download de backup local 100% compatível com iPhone, Android e computadores.",
                        Icone = "bi-shield-check"
                    },
                    new VersaoItemDestaque
                    {
                        Tipo = "Melhoria",
                        Titulo = "Sincronização com o Google Drive",
                        Descricao = "Melhoria no fluxo de login e detecção automática de sessão expirada para reconexão sem travamentos.",
                        Icone = "bi-google"
                    },
                    new VersaoItemDestaque
                    {
                        Tipo = "Novidade",
                        Titulo = "Compartilhamento Nativo no Celular",
                        Descricao = "Opção de salvar o backup diretamente no aplicativo 'Arquivos' do iOS ou enviar via WhatsApp e AirDrop.",
                        Icone = "bi-share"
                    }
                ]
            },
            new VersaoInfo
            {
                Numero = "v1.0.18",
                DataLancamento = "02/09/2026",
                Titulo = "Cores Personalizadas e Melhorias de UI",
                IsAtual = false,
                Destaques =
                [
                    new VersaoItemDestaque
                    {
                        Tipo = "Novidade",
                        Titulo = "Cor do Seletor e Destaque Personalizada",
                        Descricao = "Escolha entre várias cores pré-definidas ou use o seletor livre para personalizar a cor de destaque do seu ManyControl.",
                        Icone = "bi-palette-fill"
                    },
                    new VersaoItemDestaque
                    {
                        Tipo = "Ajuste",
                        Titulo = "Navegação da Barra Inferior",
                        Descricao = "Correção no indicador ativo da barra de navegação, acompanhando perfeitamente a mudança de telas.",
                        Icone = "bi-compass-fill"
                    },
                    new VersaoItemDestaque
                    {
                        Tipo = "Melhoria",
                        Titulo = "Contraste dos Cards no Modo Claro",
                        Descricao = "Cards com maior definição, contraste e sombras suaves no tema claro tanto no computador quanto no celular.",
                        Icone = "bi-card-heading"
                    }
                ]
            },
            new VersaoInfo
            {
                Numero = "v1.0.17",
                DataLancamento = "02/09/2026",
                Titulo = "Tema Claro (Light Mode)",
                IsAtual = false,
                Destaques =
                [
                    new VersaoItemDestaque
                    {
                        Tipo = "Novidade",
                        Titulo = "Tema Claro (Modo Dia)",
                        Descricao = "Novo visual claro com alto contraste e leitura suave, ideal para usar durante o dia ou em locais iluminados.",
                        Icone = "bi-sun-fill"
                    },
                    new VersaoItemDestaque
                    {
                        Tipo = "Melhoria",
                        Titulo = "Alternador Rápido de Tema",
                        Descricao = "Troque entre o modo claro e escuro a qualquer momento pelo botão no topo da tela ou na aba de Sincronização.",
                        Icone = "bi-moon-stars-fill"
                    }
                ]
            },
            new VersaoInfo
            {
                Numero = "v1.0.16",
                DataLancamento = "02/09/2026",
                Titulo = "Melhorias de Visual e Botões",
                IsAtual = false,
                Destaques =
                [
                    new VersaoItemDestaque
                    {
                        Tipo = "Ajuste",
                        Titulo = "Alinhamento dos Botões no Celular",
                        Descricao = "Ajuste no formato e espaçamento dos botões para facilitar o toque no celular.",
                        Icone = "bi-phone"
                    },
                    new VersaoItemDestaque
                    {
                        Tipo = "Melhoria",
                        Titulo = "Ícones Modernos",
                        Descricao = "Substituição de emojis antigos por novos ícones padronizados.",
                        Icone = "bi-check2-circle"
                    }
                ]
            }
        ];
    }
}
