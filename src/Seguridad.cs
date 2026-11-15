// Candados de seguridad de Hydra Logros.
// Principios: la app solo lee lo necesario, solo escribe archivos concretos en sitios concretos,
// nunca sigue enlaces simbólicos/junctions, nunca ejecuta nada de las carpetas de juegos
// y solo se conecta a la API oficial de Steam.
using System.Security.Principal;
using System.Text.RegularExpressions;

namespace HydraLogros;

public class SeguridadException : Exception
{
    public SeguridadException(string m) : base(m) { }
}

public static class Seguridad
{
    // ------------------------------------------------------------------ límites
    public const long MaxTextoBytes = 1 * 1024 * 1024;      // .ini / .txt / .info / .json de juegos
    public const long MaxBaseDatosBytes = 256L * 1024 * 1024; // bases de datos de Hydra / GOG
    public const long MaxRegistroBytes = 2 * 1024 * 1024;   // registro.txt antes de rotar

    static readonly Regex AppIdValido = new(@"^\d{1,10}$");
    public static bool EsAppId(string s) => s != null && AppIdValido.IsMatch(s);

    // Atributos que nunca se recorren: enlaces (junctions/symlinks), archivos de sistema y
    // marcadores de la nube (OneDrive "solo en línea"), que se descargarían al leerlos.
    public const FileAttributes AtributosEvitados = FileAttributes.ReparsePoint | FileAttributes.System | FileAttributes.Offline
        | (FileAttributes)0x00040000  /* RECALL_ON_OPEN */
        | (FileAttributes)0x00400000; /* RECALL_ON_DATA_ACCESS */

    public static readonly EnumerationOptions Enumeracion = new()
    {
        IgnoreInaccessible = true,
        RecurseSubdirectories = false,
        AttributesToSkip = AtributosEvitados,
        ReturnSpecialDirectories = false,
    };

    // ------------------------------------------------------------------ carpetas del sistema
    static string F(Environment.SpecialFolder f) { try { return Norm(Environment.GetFolderPath(f)); } catch { return null; } }

    public static string Norm(string ruta)
    {
        if (string.IsNullOrWhiteSpace(ruta)) return null;
        try { return Path.TrimEndingDirectorySeparator(Path.GetFullPath(ruta)); } catch { return null; }
    }

    static bool Igual(string a, string b) => a != null && b != null && string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    public static bool Dentro(string ruta, string raiz) =>
        ruta != null && raiz != null && (Igual(ruta, raiz) || ruta.StartsWith(raiz + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));

    static readonly Lazy<string[]> SistemaYDescendientes = new(() => new[]
    {
        F(Environment.SpecialFolder.Windows),
        Norm(Path.Combine(Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\", "$Recycle.Bin")),
        Norm(Path.Combine(Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\", "System Volume Information")),
        Norm(Path.Combine(Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\", "Recovery")),
    }.Where(x => x != null).ToArray());

    static readonly Lazy<string[]> SistemaExactas = new(() => new[]
    {
        F(Environment.SpecialFolder.ProgramFiles), F(Environment.SpecialFolder.ProgramFilesX86),
        F(Environment.SpecialFolder.CommonApplicationData), F(Environment.SpecialFolder.UserProfile),
        F(Environment.SpecialFolder.ApplicationData), F(Environment.SpecialFolder.LocalApplicationData),
        F(Environment.SpecialFolder.MyDocuments), F(Environment.SpecialFolder.Desktop),
        Norm(Path.GetDirectoryName(F(Environment.SpecialFolder.UserProfile) ?? "")), // C:\Users
        Norm(Path.GetTempPath()),
    }.Where(x => x != null).ToArray());

    // ¿Es una carpeta que no debe usarse como biblioteca (raíz de disco, sistema, perfil…)?
    public static bool EsCarpetaProhibida(string ruta)
    {
        var r = Norm(ruta);
        if (r == null) return true;
        if (Igual(r, Norm(Path.GetPathRoot(r)))) return true;                // raíz de un disco
        if (SistemaYDescendientes.Value.Any(s => Dentro(r, s))) return true; // Windows y lo que cuelga
        if (SistemaExactas.Value.Any(s => Igual(r, s))) return true;         // raíces de perfil / programas
        return false;
    }

    // Valida una biblioteca: existe, no es del sistema y no es (ni pasa por) un enlace.
    public static string ProblemaBiblioteca(string ruta)
    {
        var r = Norm(ruta);
        if (r == null || !Directory.Exists(r)) return "lib.missing";
        if (EsCarpetaProhibida(r)) return "sec.libSystem";
        if (PasaPorEnlace(r)) return "sec.libLink";
        return null;
    }

    // ¿Algún tramo de la ruta (incluida ella) es un enlace simbólico o junction?
    public static bool PasaPorEnlace(string ruta)
    {
        var r = Norm(ruta);
        var raiz = Norm(Path.GetPathRoot(r ?? ""));
        while (r != null && !Igual(r, raiz))
        {
            try
            {
                if (Directory.Exists(r) || File.Exists(r))
                    if ((File.GetAttributes(r) & FileAttributes.ReparsePoint) != 0) return true;
            }
            catch { return true; }
            r = Norm(Path.GetDirectoryName(r));
        }
        return false;
    }

    // ------------------------------------------------------------------ escritura permitida
    static readonly string[] ArchivosEnSteamSettings = { "achievements.json", "steam_appid.txt" };

    // Bibliotecas válidas actuales (las fija la ventana principal) y raíz de GSE Saves.
    public static Func<IEnumerable<string>> Bibliotecas = () => Array.Empty<string>();
    static string RaizGse => Norm(Path.Combine(Puentes.AppData, "GSE Saves"));

    // Solo se permite escribir/crear/borrar:
    //  - <biblioteca>\...\steam_settings\{achievements.json, steam_appid.txt} (y la carpeta steam_settings)
    //  - <biblioteca>\...\steam_api(64).dll.hydralogros-original (copia de seguridad)
    //  - %AppData%\GSE Saves\<AppID>\achievements.json (y su carpeta <AppID>)
    // y nunca a través de un enlace simbólico/junction.
    public static bool EsDestinoPermitido(string ruta, bool esCarpeta)
    {
        var r = Norm(ruta);
        if (r == null) return false;
        string nombre = Path.GetFileName(r);
        string padre = Norm(Path.GetDirectoryName(r));

        bool enGse = false, enBiblioteca = false;
        var gse = RaizGse;
        if (gse != null && Dentro(r, gse) && !Igual(r, gse))
        {
            var rel = Path.GetRelativePath(gse, r).Split(Path.DirectorySeparatorChar);
            enGse = esCarpeta
                ? rel.Length == 1 && EsAppId(rel[0])
                : rel.Length == 2 && EsAppId(rel[0]) && nombre.Equals("achievements.json", StringComparison.OrdinalIgnoreCase);
        }
        if (!enGse)
        {
            var biblio = Bibliotecas().Select(Norm).FirstOrDefault(b => b != null && Dentro(r, b) && !Igual(r, b));
            if (biblio != null && ProblemaBiblioteca(biblio) == null)
            {
                if (esCarpeta) enBiblioteca = nombre.Equals("steam_settings", StringComparison.OrdinalIgnoreCase);
                else if (ArchivosEnSteamSettings.Contains(nombre, StringComparer.OrdinalIgnoreCase))
                    enBiblioteca = Path.GetFileName(padre ?? "").Equals("steam_settings", StringComparison.OrdinalIgnoreCase);
                else enBiblioteca = Regex.IsMatch(nombre, @"^steam_api(64)?\.dll\.hydralogros-original$", RegexOptions.IgnoreCase);
            }
        }
        if (!enGse && !enBiblioteca) return false;
        return !PasaPorEnlace(esCarpeta ? r : padre) && !(File.Exists(r) && (File.GetAttributes(r) & FileAttributes.ReparsePoint) != 0);
    }

    public static void Exigir(string ruta, bool esCarpeta = false)
    {
        if (!EsDestinoPermitido(ruta, esCarpeta))
            throw new SeguridadException(L.T("sec.writeBlocked", ruta));
    }

    // ------------------------------------------------------------------ red y enlaces externos
    public const string HostSteamApi = "api.steampowered.com";

    static readonly string[] UrlsDeConfianza =
    {
        "https://steamcommunity.com/dev/apikey",
        "https://github.com/Detanup01/gbe_fork/releases",
    };

    public static bool EsUrlDeConfianza(string url) => UrlsDeConfianza.Contains(url, StringComparer.Ordinal);

    public static void ExigirUrlApi(Uri uri)
    {
        if (uri.Scheme != Uri.UriSchemeHttps || !string.Equals(uri.Host, HostSteamApi, StringComparison.OrdinalIgnoreCase))
            throw new SeguridadException(L.T("sec.netBlocked"));
    }

    // ------------------------------------------------------------------ entorno
    public static bool EsAdministrador()
    {
        try { return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator); }
        catch { return false; }
    }

    // Borra restos de copias temporales de GOG de ejecuciones interrumpidas (solo carpetas propias).
    public static void LimpiarTemporales()
    {
        try
        {
            foreach (var d in Directory.GetDirectories(Path.GetTempPath(), "HydraLogros-gog-*"))
                if (Regex.IsMatch(Path.GetFileName(d), @"^HydraLogros-gog-[0-9a-f]{32}$") && !PasaPorEnlace(d))
                    Directory.Delete(d, true);
        }
        catch { }
    }
}
