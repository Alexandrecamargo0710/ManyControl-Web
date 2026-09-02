using Microsoft.JSInterop;

namespace ManyControl_Web.Services;

public class ThemeService
{
    private const string ThemeKey = "manycontrol_theme";
    private readonly IJSRuntime _jsRuntime;
    private string _currentTheme = "dark";
    private string? _customAccentColor = null;
    private bool _initialized = false;

    public event Action? OnThemeChanged;

    public ThemeService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public string CurrentTheme => _currentTheme;
    public bool IsDark => _currentTheme == "dark";
    public bool IsLight => _currentTheme == "light";

    public string? CustomAccentColor => _customAccentColor;
    public string ActiveAccentColor => !string.IsNullOrWhiteSpace(_customAccentColor)
        ? _customAccentColor
        : (IsDark ? "#38bdf8" : "#0284c7");

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

            var savedAccent = await _jsRuntime.InvokeAsync<string?>("manyControlJs.getAccentColor");
            if (!string.IsNullOrWhiteSpace(savedAccent))
            {
                _customAccentColor = savedAccent;
            }
        }
        catch
        {
            _currentTheme = "dark";
        }

        _initialized = true;
        await ApplyThemeAsync(_currentTheme);
        if (!string.IsNullOrWhiteSpace(_customAccentColor))
        {
            await ApplyAccentColorAsync(_customAccentColor);
        }
        OnThemeChanged?.Invoke();
    }

    public async Task SetThemeAsync(string theme)
    {
        _currentTheme = theme == "light" ? "light" : "dark";
        await ApplyThemeAsync(_currentTheme);
        if (!string.IsNullOrWhiteSpace(_customAccentColor))
        {
            await ApplyAccentColorAsync(_customAccentColor);
        }
        OnThemeChanged?.Invoke();
    }

    public async Task ToggleThemeAsync()
    {
        var newTheme = _currentTheme == "dark" ? "light" : "dark";
        await SetThemeAsync(newTheme);
    }

    public async Task SetAccentColorAsync(string? color)
    {
        _customAccentColor = string.IsNullOrWhiteSpace(color) ? null : color.Trim();
        await ApplyAccentColorAsync(_customAccentColor);
        OnThemeChanged?.Invoke();
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

    private async Task ApplyAccentColorAsync(string? color)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("manyControlJs.setAccentColor", color ?? "");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao aplicar cor de destaque: {ex.Message}");
        }
    }
}
