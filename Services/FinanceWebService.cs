using ManyControl_Web.Models;

namespace ManyControl_Web.Services;

public class FinanceWebService
{
    private const string CategoriasKey = "manycontrol_categorias";
    private const string ReceitasKey = "manycontrol_receitas";
    private const string DespesasKey = "manycontrol_despesas";

    private readonly StorageService _storage;
    private List<Categoria> _categorias = [];
    private List<Receita> _receitas = [];
    private List<Despesa> _despesas = [];
    private bool _isInitialized;

    public event Action? OnChange;

    public FinanceWebService(StorageService storage)
    {
        _storage = storage;
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
        NotifyStateChanged();
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
        var receitas = _receitas.Where(r => r.DeletedAt == null).Sum(r => r.Valor);
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
            item.UpdatedAt = DateTime.UtcNow;
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
            item.CategoriaId = receita.CategoriaId;
            item.Categoria = receita.CategoriaId.HasValue ? _categorias.FirstOrDefault(c => c.Id == receita.CategoriaId.Value) : null;
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
            item.UpdatedAt = DateTime.UtcNow;
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
            item.CategoriaId = despesa.CategoriaId;
            item.Categoria = despesa.CategoriaId.HasValue ? _categorias.FirstOrDefault(c => c.Id == despesa.CategoriaId.Value) : null;
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
            item.UpdatedAt = DateTime.UtcNow;
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

        var novasDespesasAdicionadas = false;

        foreach (var descRecorrente in recorrentes)
        {
            if (descRecorrente.Data.Year == anoAtual && descRecorrente.Data.Month == mesAtual)
            {
                continue;
            }

            var jaExisteNoMes = _despesas.Any(d =>
                d.DeletedAt == null &&
                d.Data.Year == anoAtual &&
                d.Data.Month == mesAtual &&
                d.Descricao.Equals(descRecorrente.Descricao, StringComparison.OrdinalIgnoreCase) &&
                d.CategoriaId == descRecorrente.CategoriaId);

            if (!jaExisteNoMes)
            {
                var dia = Math.Min(descRecorrente.Data.Day, DateTime.DaysInMonth(anoAtual, mesAtual));
                var novaData = new DateTime(anoAtual, mesAtual, dia);
                DateTime? novoVencimento = descRecorrente.Vencimento.HasValue
                    ? new DateTime(anoAtual, mesAtual, Math.Min(descRecorrente.Vencimento.Value.Day, DateTime.DaysInMonth(anoAtual, mesAtual)))
                    : null;

                _despesas.Add(new Despesa
                {
                    Descricao = descRecorrente.Descricao,
                    Valor = descRecorrente.Valor,
                    Data = novaData,
                    Vencimento = novoVencimento,
                    CategoriaId = descRecorrente.CategoriaId,
                    Recorrente = true,
                    Paga = false,
                    DataPagamento = null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });

                novasDespesasAdicionadas = true;
            }
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
            foreach (var cat in package.Categorias ?? [])
            {
                var idx = _categorias.FindIndex(c => c.Id == cat.Id);
                if (idx >= 0) _categorias[idx] = cat; else _categorias.Add(cat);
            }

            foreach (var rec in package.Receitas ?? [])
            {
                var idx = _receitas.FindIndex(r => r.Id == rec.Id);
                if (idx >= 0) _receitas[idx] = rec; else _receitas.Add(rec);
            }

            foreach (var desp in package.Despesas ?? [])
            {
                var idx = _despesas.FindIndex(d => d.Id == desp.Id);
                if (idx >= 0) _despesas[idx] = desp; else _despesas.Add(desp);
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
