using ManyControl_Web.Models;
using Microsoft.JSInterop;

namespace ManyControl_Web.Services;

public class FinanceWebService : IDisposable
{
    private const string CategoriasKey = "manycontrol_categorias";
    private const string ReceitasKey = "manycontrol_receitas";
    private const string DespesasKey = "manycontrol_despesas";

    private readonly StorageService _storage;
    private readonly IJSRuntime _jsRuntime;
    private List<Categoria> _categorias = [];
    private List<Receita> _receitas = [];
    private List<Despesa> _despesas = [];
    private bool _isInitialized;
    private DotNetObjectReference<FinanceWebService>? _dotNetRef;

    public event Action? OnChange;

    public FinanceWebService(StorageService storage, IJSRuntime jsRuntime)
    {
        _storage = storage;
        _jsRuntime = jsRuntime;
    }

    public async Task InicializarAsync()
    {
        if (_isInitialized) return;

        _categorias = await _storage.GetItemAsync<List<Categoria>>(CategoriasKey) ?? [];
        _receitas = await _storage.GetItemAsync<List<Receita>>(ReceitasKey) ?? [];
        _despesas = await _storage.GetItemAsync<List<Despesa>>(DespesasKey) ?? [];

        if (_categorias.Count == 0)
        {
            CarregarCategoriasPadrao();
            await SalvarCategoriasAsync();
        }

        await ProcessarDespesasRecorrentesAsync();
        _isInitialized = true;

        try
        {
            _dotNetRef = DotNetObjectReference.Create(this);
            await _jsRuntime.InvokeVoidAsync("manyControlJs.registerStorageListener", _dotNetRef);
        }
        catch
        {
            // Ignora se JS runtime não estiver pronto ou indisponível
        }

        NotifyStateChanged();
    }

    [JSInvokable]
    public async Task OnStorageChanged(string key)
    {
        if (key == DespesasKey || key == ReceitasKey || key == CategoriasKey)
        {
            await RecarregarDoStorageAsync();
        }
    }

    public async Task RecarregarDoStorageAsync()
    {
        _categorias = await _storage.GetItemAsync<List<Categoria>>(CategoriasKey) ?? [];
        _receitas = await _storage.GetItemAsync<List<Receita>>(ReceitasKey) ?? [];
        _despesas = await _storage.GetItemAsync<List<Despesa>>(DespesasKey) ?? [];
        NotifyStateChanged();
    }

    public void Dispose()
    {
        _dotNetRef?.Dispose();
    }

    private void CarregarCategoriasPadrao()
    {
        _categorias =
        [
            new Categoria { Nome = "Salário", Tipo = "Receita" },
            new Categoria { Nome = "Investimentos", Tipo = "Receita" },
            new Categoria { Nome = "Freelance / Extra", Tipo = "Receita" },
            new Categoria { Nome = "Outras Receitas", Tipo = "Receita" },

            new Categoria { Nome = "Alimentação", Tipo = "Despesa" },
            new Categoria { Nome = "Moradia & Contas", Tipo = "Despesa" },
            new Categoria { Nome = "Transporte", Tipo = "Despesa" },
            new Categoria { Nome = "Lazer & Entretenimento", Tipo = "Despesa" },
            new Categoria { Nome = "Saúde & Farmácia", Tipo = "Despesa" },
            new Categoria { Nome = "Educação", Tipo = "Despesa" },
            new Categoria { Nome = "Assinaturas & Serviços", Tipo = "Despesa" },
            new Categoria { Nome = "Outras Despesas", Tipo = "Despesa" }
        ];
    }

    private void NotifyStateChanged() => OnChange?.Invoke();

    // --- Métricas Globais ---
    public async Task<decimal> GetSaldoGeralAcumuladoAsync()
    {
        await InicializarAsync();
        var receitas = _receitas.Where(r => r.DeletedAt == null && r.Recebida).Sum(r => r.Valor);
        var despesas = _despesas.Where(d => d.DeletedAt == null).Sum(d => d.Valor);
        return receitas - despesas;
    }

    public async Task<decimal> GetTotalReceitasGeraisAsync()
    {
        await InicializarAsync();
        return _receitas.Where(r => r.DeletedAt == null).Sum(r => r.Valor);
    }

    public async Task<decimal> GetTotalDespesasGeraisAsync()
    {
        await InicializarAsync();
        return _despesas.Where(d => d.DeletedAt == null).Sum(d => d.Valor);
    }

    // --- Categorias ---
    public async Task<List<Categoria>> GetCategoriasAsync()
    {
        await InicializarAsync();
        return _categorias.Where(c => c.DeletedAt == null).OrderBy(c => c.Nome).ToList();
    }

    public async Task<List<Categoria>> GetCategoriasPorTipoAsync(string tipo)
    {
        await InicializarAsync();
        return _categorias
            .Where(c => c.DeletedAt == null && (string.Equals(c.Tipo, tipo, StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(c.Tipo)))
            .OrderBy(c => c.Nome)
            .ToList();
    }

    public async Task<Categoria> AddCategoriaAsync(string nome, string tipo)
    {
        await InicializarAsync();
        var cat = new Categoria
        {
            Nome = nome.Trim(),
            Tipo = tipo,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _categorias.Add(cat);
        await SalvarCategoriasAsync();
        NotifyStateChanged();
        return cat;
    }

    public async Task UpdateCategoriaAsync(Categoria categoria)
    {
        await InicializarAsync();
        var item = _categorias.FirstOrDefault(c => c.Id == categoria.Id);
        if (item != null)
        {
            item.Nome = categoria.Nome.Trim();
            item.Tipo = categoria.Tipo;
            item.UpdatedAt = DateTime.UtcNow;
            await SalvarCategoriasAsync();
            NotifyStateChanged();
        }
    }

    public async Task DeleteCategoriaAsync(Guid id)
    {
        await InicializarAsync();
        var item = _categorias.FirstOrDefault(c => c.Id == id);
        if (item != null)
        {
            item.DeletedAt = DateTime.UtcNow;
            item.UpdatedAt = item.DeletedAt.Value;
            await SalvarCategoriasAsync();
            NotifyStateChanged();
        }
    }

    private async Task SalvarCategoriasAsync()
    {
        await _storage.SetItemAsync(CategoriasKey, _categorias);
    }

    // --- Receitas ---
    public async Task<List<Receita>> GetReceitasMesAsync(int ano, int mes)
    {
        await InicializarAsync();
        return _receitas
            .Where(r => r.DeletedAt == null && r.Data.Year == ano && r.Data.Month == mes)
            .OrderByDescending(r => r.Data)
            .ThenByDescending(r => r.CreatedAt)
            .ToList();
    }

    public async Task AddReceitaAsync(Receita receita)
    {
        await InicializarAsync();
        receita.CreatedAt = DateTime.UtcNow;
        receita.UpdatedAt = DateTime.UtcNow;
        if (receita.CategoriaId.HasValue)
        {
            receita.Categoria = _categorias.FirstOrDefault(c => c.Id == receita.CategoriaId.Value);
        }
        _receitas.Add(receita);
        await SalvarReceitasAsync();
        NotifyStateChanged();
    }

    public async Task UpdateReceitaAsync(Receita receita)
    {
        await InicializarAsync();
        var item = _receitas.FirstOrDefault(r => r.Id == receita.Id);
        if (item != null)
        {
            item.Descricao = receita.Descricao;
            item.Valor = receita.Valor;
            item.Data = receita.Data;
            item.Recebida = receita.Recebida;
            item.DataRecebimento = receita.Recebida ? (receita.DataRecebimento ?? receita.Data) : null;
            if (receita.CategoriaId.HasValue)
            {
                item.CategoriaId = receita.CategoriaId;
                item.Categoria = _categorias.FirstOrDefault(c => c.Id == receita.CategoriaId.Value);
            }
            item.UpdatedAt = DateTime.UtcNow;
            await SalvarReceitasAsync();
            NotifyStateChanged();
        }
    }

    public async Task ToggleRecebidaReceitaAsync(Guid id)
    {
        await InicializarAsync();
        var item = _receitas.FirstOrDefault(r => r.Id == id);
        if (item != null)
        {
            item.Recebida = !item.Recebida;
            item.DataRecebimento = item.Recebida ? (item.DataRecebimento ?? DateTime.Today) : null;
            item.UpdatedAt = DateTime.UtcNow;
            await SalvarReceitasAsync();
            NotifyStateChanged();
        }
    }

    public async Task DeleteReceitaAsync(Guid id)
    {
        await InicializarAsync();
        var item = _receitas.FirstOrDefault(r => r.Id == id);
        if (item != null)
        {
            item.DeletedAt = DateTime.UtcNow;
            item.UpdatedAt = item.DeletedAt.Value;
            await SalvarReceitasAsync();
            NotifyStateChanged();
        }
    }

    private async Task SalvarReceitasAsync()
    {
        await _storage.SetItemAsync(ReceitasKey, _receitas);
    }

    // --- Despesas ---
    public async Task<List<Despesa>> GetDespesasMesAsync(int ano, int mes)
    {
        await InicializarAsync();
        return _despesas
            .Where(d => d.DeletedAt == null && d.Data.Year == ano && d.Data.Month == mes)
            .OrderByDescending(d => d.Data)
            .ThenByDescending(d => d.CreatedAt)
            .ToList();
    }

    public async Task AddDespesaAsync(Despesa despesa)
    {
        await InicializarAsync();
        despesa.CreatedAt = DateTime.UtcNow;
        despesa.UpdatedAt = DateTime.UtcNow;
        if (despesa.Paga && despesa.DataPagamento == null)
        {
            despesa.DataPagamento = despesa.Data;
        }
        if (despesa.CategoriaId.HasValue)
        {
            despesa.Categoria = _categorias.FirstOrDefault(c => c.Id == despesa.CategoriaId.Value);
        }
        _despesas.Add(despesa);
        await SalvarDespesasAsync();
        NotifyStateChanged();
    }

    public async Task UpdateDespesaAsync(Despesa despesa)
    {
        await InicializarAsync();
        var item = _despesas.FirstOrDefault(d => d.Id == despesa.Id);
        if (item != null)
        {
            item.Descricao = despesa.Descricao;
            item.Valor = despesa.Valor;
            item.Data = despesa.Data;
            item.Vencimento = despesa.Vencimento;
            if (despesa.CategoriaId.HasValue)
            {
                item.CategoriaId = despesa.CategoriaId;
                item.Categoria = _categorias.FirstOrDefault(c => c.Id == despesa.CategoriaId.Value);
            }
            item.Recorrente = despesa.Recorrente;
            item.Paga = despesa.Paga;
            item.DataPagamento = despesa.Paga ? (despesa.DataPagamento ?? despesa.Data) : null;
            item.UpdatedAt = DateTime.UtcNow;
            await SalvarDespesasAsync();
            NotifyStateChanged();
        }
    }

    public async Task TogglePagaDespesaAsync(Guid id)
    {
        await InicializarAsync();
        var item = _despesas.FirstOrDefault(d => d.Id == id);
        if (item != null)
        {
            item.Paga = !item.Paga;
            item.DataPagamento = item.Paga ? DateTime.Today : null;
            item.UpdatedAt = DateTime.UtcNow;
            await SalvarDespesasAsync();
            NotifyStateChanged();
        }
    }

    public async Task DeleteDespesaAsync(Guid id)
    {
        await InicializarAsync();
        var item = _despesas.FirstOrDefault(d => d.Id == id);
        if (item != null)
        {
            item.DeletedAt = DateTime.UtcNow;
            item.UpdatedAt = item.DeletedAt.Value;

            if (item.Recorrente)
            {
                item.Recorrente = false;
                var descricaoNorm = item.Descricao.Trim().ToLowerInvariant();
                var outrosMoldes = _despesas.Where(d => d.Recorrente && d.Descricao.Trim().ToLowerInvariant() == descricaoNorm).ToList();
                foreach (var m in outrosMoldes)
                {
                    m.Recorrente = false;
                    m.UpdatedAt = DateTime.UtcNow;
                }
            }

            await SalvarDespesasAsync();
            NotifyStateChanged();
        }
    }

    private async Task SalvarDespesasAsync()
    {
        await _storage.SetItemAsync(DespesasKey, _despesas);
    }

    // --- Transações Combinadas ---
    public async Task<List<TransacaoItem>> GetTransacoesMesAsync(int ano, int mes)
    {
        await InicializarAsync();
        var receitas = _receitas
            .Where(r => r.DeletedAt == null && r.Data.Year == ano && r.Data.Month == mes)
            .Select(r => new TransacaoItem
            {
                Id = r.Id,
                Descricao = r.Descricao,
                Valor = r.Valor,
                Data = r.Data,
                Tipo = "Receita",
                Recebida = r.Recebida,
                CategoriaNome = _categorias.FirstOrDefault(c => c.Id == r.CategoriaId)?.Nome ?? "Receita Geral",
                CategoriaId = r.CategoriaId,
                Paga = true,
                DataPagamento = r.DataRecebimento
            });

        var despesas = _despesas
            .Where(d => d.DeletedAt == null && d.Data.Year == ano && d.Data.Month == mes)
            .Select(d => new TransacaoItem
            {
                Id = d.Id,
                Descricao = d.Descricao,
                Valor = d.Valor,
                Data = d.Data,
                Tipo = "Despesa",
                CategoriaNome = _categorias.FirstOrDefault(c => c.Id == d.CategoriaId)?.Nome ?? "Sem categoria",
                CategoriaId = d.CategoriaId,
                Paga = d.Paga,
                Recorrente = d.Recorrente,
                Vencimento = d.Vencimento,
                DataPagamento = d.DataPagamento
            });

        return receitas.Concat(despesas)
            .OrderByDescending(t => t.Data)
            .ToList();
    }

    // --- Despesas Recorrentes ---
    public async Task ProcessarDespesasRecorrentesAsync()
    {
        var hoje = DateTime.Today;
        var anoAtual = hoje.Year;
        var mesAtual = hoje.Month;

        var recorrentes = _despesas
            .Where(d => d.DeletedAt == null && d.Recorrente)
            .ToList();

        if (recorrentes.Count == 0) return;

        // Busca todas as despesas do mês alvo (incluindo deletadas) para respeitar exclusões manuais
        var despesasDoMesAlvo = _despesas
            .Where(d => d.Data.Year == anoAtual && d.Data.Month == mesAtual)
            .Select(d => d.Descricao.Trim().ToLowerInvariant())
            .ToHashSet();

        var grupos = recorrentes
            .GroupBy(d => d.Descricao.Trim().ToLowerInvariant())
            .ToList();

        var novasDespesasAdicionadas = false;

        foreach (var grupo in grupos)
        {
            var descricaoNorm = grupo.Key;

            // Se já existe uma despesa com esse nome no mês alvo (mesmo se foi deletada), não recria
            if (despesasDoMesAlvo.Contains(descricaoNorm))
            {
                continue;
            }

            var modelo = grupo.OrderByDescending(d => d.Data).First();
            var dataModelo = new DateTime(modelo.Data.Year, modelo.Data.Month, 1);
            var dataAlvoMes = new DateTime(anoAtual, mesAtual, 1);

            if (dataModelo >= dataAlvoMes)
            {
                continue;
            }

            var diasNoMes = DateTime.DaysInMonth(anoAtual, mesAtual);
            var diaData = Math.Min(modelo.Data.Day, diasNoMes);
            var novaData = new DateTime(anoAtual, mesAtual, diaData);

            DateTime? novoVencimento = null;
            if (modelo.Vencimento.HasValue)
            {
                var diaVenc = Math.Min(modelo.Vencimento.Value.Day, diasNoMes);
                novoVencimento = new DateTime(anoAtual, mesAtual, diaVenc);
            }

            _despesas.Add(new Despesa
            {
                Id = Guid.NewGuid(),
                Descricao = modelo.Descricao,
                Valor = modelo.Valor,
                Data = novaData,
                Vencimento = novoVencimento,
                CategoriaId = modelo.CategoriaId,
                Recorrente = true,
                Paga = false,
                DataPagamento = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            novasDespesasAdicionadas = true;
        }

        if (novasDespesasAdicionadas)
        {
            await SalvarDespesasAsync();
        }
    }

    // --- Backup & Sincronização ---
    public async Task<SyncPackage> ExportarPacoteAsync()
    {
        await InicializarAsync();
        return new SyncPackage
        {
            ExportedAtUtc = DateTime.UtcNow,
            LastChangedAtUtc = DateTime.UtcNow,
            Categorias = _categorias.ToList(),
            Receitas = _receitas.ToList(),
            Despesas = _despesas.ToList()
        };
    }

    public async Task ImportarPacoteAsync(SyncPackage package, bool sobrescrever = false)
    {
        await InicializarAsync();

        if (sobrescrever)
        {
            _categorias = package.Categorias ?? [];
            _receitas = package.Receitas ?? [];
            _despesas = package.Despesas ?? [];
        }
        else
        {
            // 1. Categorias
            foreach (var cat in package.Categorias ?? [])
            {
                var idx = _categorias.FindIndex(c => c.Id == cat.Id);
                if (idx >= 0)
                {
                    var local = _categorias[idx];
                    if (local.DeletedAt != null && cat.DeletedAt == null)
                    {
                        if (cat.UpdatedAt > local.DeletedAt.Value) _categorias[idx] = cat;
                        continue;
                    }
                    if (local.DeletedAt == null && cat.DeletedAt != null)
                    {
                        if (!(local.UpdatedAt > cat.DeletedAt.Value)) _categorias[idx] = cat;
                        continue;
                    }
                    if (cat.UpdatedAt > local.UpdatedAt)
                    {
                        _categorias[idx] = cat;
                    }
                }
                else
                {
                    _categorias.Add(cat);
                }
            }

            // 2. Receitas
            foreach (var rec in package.Receitas ?? [])
            {
                var idx = _receitas.FindIndex(r => r.Id == rec.Id);
                if (idx >= 0)
                {
                    var local = _receitas[idx];
                    if (local.DeletedAt != null && rec.DeletedAt == null)
                    {
                        if (rec.UpdatedAt > local.DeletedAt.Value) _receitas[idx] = rec;
                        continue;
                    }
                    if (local.DeletedAt == null && rec.DeletedAt != null)
                    {
                        if (!(local.UpdatedAt > rec.DeletedAt.Value)) _receitas[idx] = rec;
                        continue;
                    }
                    if (rec.UpdatedAt > local.UpdatedAt)
                    {
                        _receitas[idx] = rec;
                    }
                }
                else
                {
                    _receitas.Add(rec);
                }
            }

            // 3. Despesas
            foreach (var desp in package.Despesas ?? [])
            {
                var idx = _despesas.FindIndex(d => d.Id == desp.Id);
                if (idx >= 0)
                {
                    var local = _despesas[idx];
                    // Tombstone conflict resolution
                    if (local.DeletedAt != null && desp.DeletedAt == null)
                    {
                        if (desp.UpdatedAt > local.DeletedAt.Value) _despesas[idx] = desp;
                        continue;
                    }
                    if (local.DeletedAt == null && desp.DeletedAt != null)
                    {
                        if (!(local.UpdatedAt > desp.DeletedAt.Value)) _despesas[idx] = desp;
                        continue;
                    }
                    if (desp.UpdatedAt > local.UpdatedAt)
                    {
                        _despesas[idx] = desp;
                    }
                }
                else
                {
                    _despesas.Add(desp);
                }
            }
        }

        await SalvarCategoriasAsync();
        await SalvarReceitasAsync();
        await SalvarDespesasAsync();
        NotifyStateChanged();
    }

    public async Task LimparTodosDadosAsync()
    {
        _categorias.Clear();
        _receitas.Clear();
        _despesas.Clear();
        CarregarCategoriasPadrao();
        await SalvarCategoriasAsync();
        await SalvarReceitasAsync();
        await SalvarDespesasAsync();
        NotifyStateChanged();
    }
}
