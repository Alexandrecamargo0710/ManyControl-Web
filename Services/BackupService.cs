using System.Text.Json;
using ManyControl_Web.Models;
using Microsoft.JSInterop;

namespace ManyControl_Web.Services;

public class BackupService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly IJSRuntime _jsRuntime;
    private readonly FinanceWebService _financeService;

    public BackupService(IJSRuntime jsRuntime, FinanceWebService financeService)
    {
        _jsRuntime = jsRuntime;
        _financeService = financeService;
    }

    public async Task ExportarBackupAsync()
    {
        var package = await _financeService.ExportarPacoteAsync();
        var json = JsonSerializer.Serialize(package, JsonOptions);
        var fileName = $"manycontrol-backup-{DateTime.Now:yyyy-MM-dd-HHmm}.json";

        await _jsRuntime.InvokeVoidAsync("manyControlJs.downloadFile", fileName, json, "application/json");
    }

    public async Task<bool> ImportarBackupJsonAsync(string jsonContent, bool sobrescrever = false)
    {
        try
        {
            var package = JsonSerializer.Deserialize<SyncPackage>(jsonContent, JsonOptions);
            if (package == null) return false;

            await _financeService.ImportarPacoteAsync(package, sobrescrever);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
