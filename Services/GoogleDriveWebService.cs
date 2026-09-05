using System.Text.Json;
using ManyControl_Web.Models;
using Microsoft.JSInterop;

namespace ManyControl_Web.Services;

public class GoogleDriveWebService
{
    private const string GoogleTokenKey = "manycontrol_google_token";
    private const string GoogleEmailKey = "manycontrol_google_email";
    private const string LastSyncTimeKey = "manycontrol_last_sync_time";
    public const string DefaultClientId = "467782905209-n8n3i4thm5ga7bphtbqqk7jqq1karjbl.apps.googleusercontent.com";

    private readonly IJSRuntime _jsRuntime;
    private readonly FinanceWebService _financeService;

    public event Action? OnStatusChanged;

    public GoogleDriveWebService(IJSRuntime jsRuntime, FinanceWebService financeService)
    {
        _jsRuntime = jsRuntime;
        _financeService = financeService;
    }

    public async Task<bool> IsConnectedAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<bool>("manyControlGoogleDrive.isTokenValid");
        }
        catch
        {
            var token = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", GoogleTokenKey);
            return !string.IsNullOrWhiteSpace(token);
        }
    }

    public async Task<string?> GetConnectedEmailAsync()
    {
        var email = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", GoogleEmailKey);
        if (!string.IsNullOrWhiteSpace(email)) return email;

        var token = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", GoogleTokenKey);
        if (!string.IsNullOrWhiteSpace(token))
        {
            email = await _jsRuntime.InvokeAsync<string?>("manyControlGoogleDrive.getUserEmail", token);
            return email;
        }
        return null;
    }

    public async Task ConectarAsync(DotNetObjectReference<GoogleDriveWebService> dotNetRef)
    {
        await _jsRuntime.InvokeVoidAsync("manyControlGoogleDrive.requestToken", dotNetRef, DefaultClientId);
    }

    [JSInvokable]
    public async Task OnGoogleAuthSuccess(string token)
    {
        var email = await _jsRuntime.InvokeAsync<string?>("manyControlGoogleDrive.getUserEmail", token);
        await SincronizarAsync();
        OnStatusChanged?.Invoke();
    }

    [JSInvokable]
    public void OnGoogleSessionExpired()
    {
        OnStatusChanged?.Invoke();
    }

    public async Task DesconectarAsync()
    {
        await _jsRuntime.InvokeVoidAsync("manyControlGoogleDrive.disconnect");
        OnStatusChanged?.Invoke();
    }

    public async Task<string> SincronizarAsync()
    {
        try
        {
            // 1. Baixar dados remotos existentes no Google Drive (se houver)
            var remoteJson = await _jsRuntime.InvokeAsync<string?>("manyControlGoogleDrive.downloadDriveFile");

            if (!string.IsNullOrWhiteSpace(remoteJson))
            {
                var remotePackage = JsonSerializer.Deserialize<SyncPackage>(remoteJson);
                if (remotePackage != null)
                {
                    // Mescla com Last-Write-Wins (LWW) no banco local
                    await _financeService.ImportarPacoteAsync(remotePackage, false);
                }
            }

            // 2. Exporta o pacote consolidado com todas as alterações
            var mergedPackage = await _financeService.ExportarPacoteAsync();
            var mergedJson = JsonSerializer.Serialize(mergedPackage, new JsonSerializerOptions { WriteIndented = true });

            // 3. Envia o pacote mesclado de volta para a nuvem
            await _jsRuntime.InvokeVoidAsync("manyControlGoogleDrive.uploadDriveFile", mergedJson);

            var nowStr = $"Hoje às {DateTime.Now:HH:mm}";
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", LastSyncTimeKey, nowStr);
            OnStatusChanged?.Invoke();
            return "Tudo sincronizado";
        }
        catch (Exception ex)
        {
            return $"Falha na sincronização: {ex.Message}";
        }
    }

    public async Task<string> GetLastSyncTextAsync()
    {
        var time = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", LastSyncTimeKey);
        return time ?? "Nunca";
    }
}
