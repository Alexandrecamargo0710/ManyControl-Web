using Microsoft.JSInterop;

namespace ManyControl_Web.Services;

public class ThemeService
{
    private const string ThemeKey = "manycontrol_theme";
    private readonly IJSRuntime _jsRuntime;
    private string _currentTheme = "dark";
    private bool _initialized = false;

    public event Action? OnThemeChanged;

    public ThemeService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public string CurrentTheme => _currentTheme;
    public bool IsDark => _currentTheme == "dark";
    public bool IsLight => _currentTheme == "light";

    public async Task InicializarAsync()
    {
        if (_initialized) return;

        try
        {
            var savedTheme = await _jsRuntime.InvokeAsync<string?>("manyControlJs.getTheme");
            if (!string.IsNullOrWhiteSpace(savedTheme))
            {
                _currentTheme = savedTheme;
            }
            else
            {
                _currentTheme = "dark";
            }
        }
        catch
        {
            _currentTheme = "dark";
        }

        _initialized = true;
        await ApplyThemeAsync(_currentTheme);
        OnThemeChanged?.Invoke();
    }

    public async Task SetThemeAsync(string theme)
    {
        _currentTheme = theme == "light" ? "light" : "dark";
        await ApplyThemeAsync(_currentTheme);
        OnThemeChanged?.Invoke();
    }

    public async Task ToggleThemeAsync()
    {
        var newTheme = _currentTheme == "dark" ? "light" : "dark";
        await SetThemeAsync(newTheme);
    }

    private async Task ApplyThemeAsync(string theme)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("manyControlJs.setTheme", theme);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao aplicar tema: {ex.Message}");
        }
    }
}
