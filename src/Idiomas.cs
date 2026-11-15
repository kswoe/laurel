using System.Globalization;

namespace HydraLogros;

// Idioma de la interfaz. Los textos están en Textos.cs (un valor por idioma, en el orden de Todos).
public static class L
{
    public record Idioma(string Codigo, string Nativo, string Steam);

    public static readonly Idioma[] Todos =
    {
        new("es", "Español", "spanish"),
        new("en", "English", "english"),
        new("pt", "Português (Brasil)", "brazilian"),
        new("fr", "Français", "french"),
        new("de", "Deutsch", "german"),
        new("ru", "Русский", "russian"),
        new("zh", "简体中文", "schinese"),
        new("ja", "日本語", "japanese"),
    };

    static int actual = 1; // inglés mientras no se elija otro

    public static string Codigo => Todos[actual].Codigo;
    public static string Steam => Todos[actual].Steam;   // idioma para la lista de logros de Steam
    public static string Nativo => Todos[actual].Nativo;

    public static int Indice(string codigo)
    {
        for (int i = 0; i < Todos.Length; i++)
            if (string.Equals(Todos[i].Codigo, codigo, StringComparison.OrdinalIgnoreCase)) return i;
        return -1;
    }

    public static void Usar(string codigo)
    {
        int i = Indice(codigo);
        if (i >= 0) actual = i;
    }

    // Idioma de Windows si está disponible; si no, inglés.
    public static string Detectar()
    {
        string c = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return Indice(c) >= 0 ? c : "en";
    }

    public static string T(string clave, params object[] args) => En(actual, clave, args);

    public static string En(int idioma, string clave, params object[] args)
    {
        if (!Textos.D.TryGetValue(clave, out var t)) return clave;
        string s = idioma < t.Length && !string.IsNullOrEmpty(t[idioma]) ? t[idioma] : t[1];
        return args.Length > 0 ? string.Format(s, args) : s;
    }
}

// Aviso producido antes de conocer el idioma (al cargar la configuración); se traduce al mostrarlo.
public record Aviso(Nivel Nivel, string Clave, object[] Args);
