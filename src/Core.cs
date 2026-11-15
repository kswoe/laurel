// Lógica de Hydra Logros: detección de emuladores, diagnóstico, reparación segura y registro de cambios.
// Regla de oro: la app NUNCA sobrescribe ni borra archivos del usuario. Solo crea archivos que faltan,
// y "Deshacer" solo envía a la papelera archivos creados por la app que nadie ha modificado después.
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace HydraLogros;

public enum Nivel { Info, Exito, Aviso, Error, Detalle }

public enum Estado { Ok, Listo, SteamOriginal, Reparable, SinAppId, NoSoportado, SinSteam, Error }

public delegate void Logger(Nivel nivel, string mensaje);

// ------------------------------------------------------------------ configuración
public class Config
{
    public List<string> Bibliotecas { get; set; } = new();

    // La clave de API vive SOLO en memoria durante la sesión: nunca se escribe en disco.
    [System.Text.Json.Serialization.JsonIgnore]
    public string SteamApiKey { get; set; } = "";
    public string IdiomaInterfaz { get; set; }          // es, en, pt, fr, de, ru, zh, ja
    public bool PreguntarIdioma { get; set; } = true;  // mostrar el selector al iniciar
    public int IntervaloSegundos { get; set; } = 60;
    public bool Vigilar { get; set; } = true;
    public bool RepararAutomaticamente { get; set; } = true;
    public bool SoloSimular { get; set; } = false;
    public bool Puentes { get; set; } = true;           // copiar a Hydra logros de GOG Galaxy / RLE
    public Dictionary<string, string> AppIdsManuales { get; set; } = new();

    public static string Carpeta => AppContext.BaseDirectory;
    static string Ruta => Path.Combine(Carpeta, "config.json");
    static readonly JsonSerializerOptions Opciones = new() { WriteIndented = true };

    // avisos: mensajes para mostrar cuando ya se conozca el idioma
    public static Config Cargar(List<Aviso> avisos)
    {
        if (!File.Exists(Ruta)) return new Config();
        try
        {
            if (new FileInfo(Ruta).Length > Seguridad.MaxTextoBytes) throw new InvalidDataException("config.json > 1 MB");
            string texto = File.ReadAllText(Ruta);
            var cfg = JsonSerializer.Deserialize<Config>(texto) ?? new Config();

            // Valores manipulados o corruptos se ignoran en memoria (el archivo no se toca por ello).
            cfg.Bibliotecas = (cfg.Bibliotecas ?? new()).Where(b => !string.IsNullOrWhiteSpace(b)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            cfg.AppIdsManuales ??= new();
            foreach (var mal in cfg.AppIdsManuales.Where(kv => !Seguridad.EsAppId(kv.Value?.Trim())).Select(kv => kv.Key).ToList())
            {
                cfg.AppIdsManuales.Remove(mal);
                avisos.Add(new Aviso(Nivel.Aviso, "sec.appIdInvalid", new object[] { mal }));
            }
            cfg.IntervaloSegundos = Math.Clamp(cfg.IntervaloSegundos, 10, 3600);
            if (cfg.IdiomaInterfaz != null && L.Indice(cfg.IdiomaInterfaz) < 0) cfg.IdiomaInterfaz = null;
            // Versiones anteriores guardaban la clave: se elimina del archivo conservando lo demás.
            using (var doc = JsonDocument.Parse(texto))
            {
                if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                    doc.RootElement.EnumerateObject().Any(p => p.Name.Equals("SteamApiKey", StringComparison.OrdinalIgnoreCase)))
                {
                    cfg.Guardar();
                    avisos.Add(new Aviso(Nivel.Aviso, "log.keyPurged", Array.Empty<object>()));
                }
            }
            return cfg;
        }
        catch (Exception e)
        {
            // No se borra ni se reemplaza el config del usuario: se guarda una copia y se avisa.
            string copia = Ruta + ".danado-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            try { File.Copy(Ruta, copia); } catch { }
            avisos.Add(new Aviso(Nivel.Error, "log.configError", new object[] { e.Message, Path.GetFileName(copia) }));
            return new Config();
        }
    }

    public void Guardar() => Util.EscribirAtomico(Ruta, JsonSerializer.Serialize(this, Opciones));

    public string AppIdManual(string carpetaJuego)
    {
        string nombre = Path.GetFileName(carpetaJuego);
        foreach (var kv in AppIdsManuales)
            if (string.Equals(kv.Key, nombre, StringComparison.OrdinalIgnoreCase)) return kv.Value;
        return null;
    }
}

// ------------------------------------------------------------------ utilidades
public static class Util
{
    // Lee archivos de texto pequeños de los juegos. Los enormes (más de 1 MB) se ignoran:
    // un .ini/.txt legítimo nunca es tan grande y así un archivo malicioso no puede bloquear la app.
    public static string LeerTexto(string ruta)
    {
        try
        {
            var fi = new FileInfo(ruta);
            if (!fi.Exists || fi.Length > Seguridad.MaxTextoBytes || (fi.Attributes & Seguridad.AtributosEvitados) != 0) return "";
            return File.ReadAllText(ruta).TrimStart('﻿');
        }
        catch { return ""; }
    }

    public static string Sha256(string ruta)
    {
        using var s = File.OpenRead(ruta);
        return Convert.ToHexString(SHA256.HashData(s));
    }

    // Escribe vía archivo temporal para no dejar nunca un archivo a medias.
    public static void EscribirAtomico(string ruta, string contenido)
    {
        string tmp = ruta + ".tmp-hydralogros";
        File.WriteAllText(tmp, contenido, new UTF8Encoding(false));
        File.Move(tmp, ruta, true);
    }

    // Crea un archivo SOLO si no existe. Devuelve false si ya existía (y no lo toca).
    public static bool CrearArchivoNuevo(string ruta, string contenido)
    {
        if (File.Exists(ruta)) return false;
        string tmp = ruta + ".tmp-hydralogros";
        File.WriteAllText(tmp, contenido, new UTF8Encoding(false));
        try
        {
            File.Move(tmp, ruta, false);
            return true;
        }
        catch (IOException) when (File.Exists(ruta))
        {
            File.Delete(tmp);
            return false;
        }
    }

    // Traduce excepciones a razones claras para el usuario.
    public static string Explicar(Exception e) => e switch
    {
        SteamException s => s.Message,
        SeguridadException s => s.Message,
        UnauthorizedAccessException => L.T("err.noWrite"),
        DirectoryNotFoundException => L.T("err.dirGone"),
        PathTooLongException => L.T("err.pathLong"),
        IOException io => L.T("err.io", io.Message),
        _ => e.Message,
    };
}

// Quita datos personales de los mensajes antes de mostrarlos o guardarlos en registro.txt:
// la clave de API, la carpeta de usuario de Windows y el nombre de usuario en rutas.
public static class Privacidad
{
    static readonly string Perfil = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    static readonly Regex RutaUsuario = new(@"(?i)\b([A-Z]:[\\/]Users[\\/])(?!Public\b)[^\\/:*?""<>|\r\n]+");
    static readonly Regex Correo = new(@"[\w.+-]+@[\w-]+\.[\w.-]+");

    public static string Limpiar(string texto, string clave)
    {
        if (string.IsNullOrEmpty(texto)) return texto;
        if (!string.IsNullOrWhiteSpace(clave) && clave.Trim().Length >= 6)
            texto = texto.Replace(clave.Trim(), L.T("priv.key"), StringComparison.OrdinalIgnoreCase);
        texto = Regex.Replace(texto, @"(?i)([?&]key=)[^&\s""]+", "$1" + L.T("priv.key"));
        if (!string.IsNullOrEmpty(Perfil))
        {
            texto = texto.Replace(Perfil, "%USERPROFILE%", StringComparison.OrdinalIgnoreCase);
            texto = texto.Replace(Perfil.Replace('\\', '/'), "%USERPROFILE%", StringComparison.OrdinalIgnoreCase);
        }
        texto = RutaUsuario.Replace(texto, "$1" + L.T("priv.user"));
        texto = Correo.Replace(texto, L.T("priv.mail"));
        return texto;
    }
}

public class SteamException : Exception
{
    public SteamException(string m) : base(m) { }
}

// ------------------------------------------------------------------ rutas que vigila Hydra
public static class RutasHydra
{
    static readonly string Home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    static readonly string AppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    static readonly string LocalAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    static readonly string Docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    static readonly string PublicDocs = Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments);
    static readonly string ProgramData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

    // Sacadas de hydralauncher/hydra: src/main/services/achievements/find-achievement-files.ts
    public static readonly Dictionary<string, string[][]> PorEmulador = new()
    {
        ["goldberg"] = new[] {
            new[] { AppData, "Goldberg SteamEmu Saves", "{id}", "achievements.json" },
            new[] { AppData, "GSE Saves", "{id}", "achievements.json" } },
        ["codex"] = new[] {
            new[] { PublicDocs, "Steam", "CODEX", "{id}", "achievements.ini" },
            new[] { AppData, "Steam", "CODEX", "{id}", "achievements.ini" } },
        ["rune"] = new[] { new[] { PublicDocs, "Steam", "RUNE", "{id}", "achievements.ini" } },
        ["onlineFix"] = new[] {
            new[] { PublicDocs, "OnlineFix", "{id}", "Stats", "Achievements.ini" },
            new[] { PublicDocs, "OnlineFix", "{id}", "Achievements.ini" } },
        ["rld"] = new[] {
            new[] { ProgramData, "RLD!", "{id}", "achievements.ini" },
            new[] { ProgramData, "Steam", "Player", "{id}", "stats", "achievements.ini" },
            new[] { ProgramData, "Steam", "RLD!", "{id}", "stats", "achievements.ini" },
            new[] { ProgramData, "Steam", "dodi", "{id}", "stats", "achievements.ini" } },
        ["empress"] = new[] {
            new[] { AppData, "EMPRESS", "remote", "{id}", "achievements.json" },
            new[] { PublicDocs, "EMPRESS", "{id}", "remote", "{id}", "achievements.json" } },
        ["skidrow"] = new[] {
            new[] { Docs, "SKIDROW", "{id}", "SteamEmu", "UserStats", "achiev.ini" },
            new[] { Docs, "Player", "{id}", "SteamEmu", "UserStats", "achiev.ini" },
            new[] { LocalAppData, "SKIDROW", "{id}", "SteamEmu", "UserStats", "achiev.ini" } },
        ["creamAPI"] = new[] { new[] { AppData, "CreamAPI", "{id}", "stats", "CreamAPI.Achievements.cfg" } },
        ["smartSteamEmu"] = new[] { new[] { AppData, "SmartSteamEmu", "{id}", "User", "Achievements.ini" } },
        ["razor1911"] = new[] { new[] { AppData, ".1911", "{id}", "achievement" } },
    };

    // Emuladores que Hydra busca solo dentro de la carpeta del juego.
    public static readonly Dictionary<string, string[]> EnCarpetaJuego = new()
    {
        ["userstats"] = new[] { "SteamData", "user_stats.ini" },
        ["_3dm"] = new[] { "3DMGAME", "*", "stats", "achievements.ini" },
        ["ali213"] = new[] { "Profile", "*", "Stats", "Achievements.Bin" },
    };

    public static readonly Dictionary<string, string> Nombres = new()
    {
        ["goldberg"] = "Goldberg / gbe_fork", ["codex"] = "CODEX", ["rune"] = "RUNE", ["onlineFix"] = "OnlineFix",
        ["rld"] = "RLD!", ["empress"] = "EMPRESS", ["skidrow"] = "SKIDROW", ["creamAPI"] = "CreamAPI",
        ["smartSteamEmu"] = "SmartSteamEmu", ["razor1911"] = "Razor1911", ["userstats"] = "TENOKE / userstats",
        ["_3dm"] = "3DM", ["ali213"] = "ALI213", ["desconocido"] = "desconocido", ["steam"] = "Steam original (Valve)",
        // plataformas de juegos sin Steam
        ["gog-logros"] = "GOG (Galaxy, con logros)", ["gog"] = "GOG (sin Galaxy)", ["epic"] = "Epic (EOS)",
        ["ubisoft"] = "Ubisoft Connect", ["xbox"] = "Microsoft Store / Xbox", ["ninguna"] = "sin plataforma de logros",
    };

    static readonly Dictionary<string, string> NombresTraducibles = new()
    {
        ["desconocido"] = "emu.unknown", ["steam"] = "emu.steam", ["gog-logros"] = "emu.gogAch", ["gog"] = "emu.gog", ["ninguna"] = "emu.none",
    };

    public static string Nombre(string clave) =>
        NombresTraducibles.TryGetValue(clave, out var t) ? L.T(t) : Nombres.GetValueOrDefault(clave, clave);

    // Busca si Steam tiene datos de logros de este juego (Hydra los lee de ahí para juegos de Steam).
    public static List<string> ArchivosDeSteam(string appId)
    {
        var res = new List<string>();
        try
        {
            using var k = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            if (k?.GetValue("SteamPath") is not string steam) return res;
            var userdata = Path.Combine(steam.Replace('/', '\\'), "userdata");
            if (!Directory.Exists(userdata)) return res;
            foreach (var u in Directory.GetDirectories(userdata))
            {
                var f = Path.Combine(u, "config", "librarycache", appId + ".json");
                if (File.Exists(f)) res.Add(f);
            }
        }
        catch { }
        return res;
    }

    public static IEnumerable<string> Archivos(string emu, string appId) =>
        PorEmulador.TryGetValue(emu, out var lista)
            ? lista.Select(p => Path.Combine(p.Select(x => x.Replace("{id}", appId)).ToArray()))
            : Enumerable.Empty<string>();

    // Lee (solo lectura) la base de datos de Hydra para encontrar su carpeta de descargas.
    public static List<string> DetectarBibliotecasDeHydra(Logger log)
    {
        var res = new List<string>();
        var candidatas = new[] { "hydralauncher", "Hydra", "hydra" }
            .Select(n => Path.Combine(AppData, n, "hydra-db"))
            .Where(Directory.Exists).ToList();
        if (candidatas.Count == 0)
            log(Nivel.Aviso, L.T("hy.notFound"));

        var re = new Regex("\"downloadsPath\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)+)\"");
        foreach (var db in candidatas)
        {
            foreach (var f in Directory.EnumerateFiles(db).Where(f => f.EndsWith(".log") || f.EndsWith(".ldb")))
            {
                string texto;
                try
                {
                    // Abrir en modo compartido: Hydra puede estar abierto y no se le molesta.
                    if (new FileInfo(f).Length > Seguridad.MaxBaseDatosBytes) continue;
                    using var fs = new FileStream(f, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                    using var ms = new MemoryStream();
                    fs.CopyTo(ms);
                    texto = Encoding.Latin1.GetString(ms.ToArray());
                }
                catch { continue; }
                foreach (Match m in re.Matches(texto))
                {
                    try
                    {
                        string ruta = JsonSerializer.Deserialize<string>("\"" + m.Groups[1].Value + "\"");
                        if (Directory.Exists(ruta) && !res.Contains(ruta, StringComparer.OrdinalIgnoreCase)) res.Add(ruta);
                    }
                    catch { }
                }
            }
        }

        string descargas = Path.Combine(Home, "Downloads");
        if (res.Count == 0 && Directory.Exists(descargas))
        {
            log(Nivel.Info, L.T("hy.downloads"));
            res.Add(descargas);
        }
        return res;
    }
}

// ------------------------------------------------------------------ juego
public class Juego
{
    public string Nombre;
    public string Carpeta;
    public List<string> Emuladores = new();
    public string AppId;
    public string AppIdOrigen;
    public List<string> Dlls = new();
    public List<string> SteamSettings = new();
    public List<InfoDll> InfoDlls = new();
    public bool IdentificadoPorDll;
    public string Plataforma;        // solo para juegos sin Steam: gog-logros, gog, epic, ubisoft, xbox, ninguna
    public string PlataformaDetalle; // p. ej. nombre e ID de GOG
    public string GogId;             // ID de GOG (de goggame-<id>.info)
    public int Entradas;
    public int Ejecutables;          // nº de .exe encontrados
    public bool Truncado;            // el recorrido se cortó por tamaño o tiempo
    public bool PareceJuego => Ejecutables > 0 || Dlls.Count > 0 || Emuladores.Count > 0 || Plataforma is not (null or "ninguna");

    public Estado Estado;
    public bool Reparable;
    public List<string> Mensajes = new();
    public List<string> ArchivosHydra = new();

    public string EmuladoresTexto => Emuladores.Count == 0
        ? (Plataforma != null ? RutasHydra.Nombre(Plataforma) : "—") :
        string.Join(", ", Emuladores.Select(RutasHydra.Nombre));

    public IEnumerable<string> CarpetasSteamSettings() =>
        (SteamSettings.Count > 0 ? SteamSettings : Dlls.Select(d => Path.Combine(Path.GetDirectoryName(d), "steam_settings")))
        .Distinct(StringComparer.OrdinalIgnoreCase);
}

// ------------------------------------------------------------------ escáner
public static class Escaner
{
    const int ProfundidadMax = 8;     // los juegos Unreal guardan steam_api en Engine\Binaries\ThirdParty\Steamworks\SteamvXXX\Win64
    const int EntradasMax = 60000;    // tope por juego (las carpetas de recursos ya no se recorren)
    const int TiempoMaxMs = 15000;    // tope de tiempo por juego (discos lentos o de red)

    // Carpetas que nunca contienen lo que buscamos (DLL de Steam, .ini de emuladores, SDK de plataformas):
    // audio, vídeo, recursos empaquetados, instaladores de dependencias, imágenes, registros…
    static readonly HashSet<string> CarpetasSinInteres = new(StringComparer.OrdinalIgnoreCase)
    {
        "_Redist", "Redist", "_CommonRedist", "CommonRedist", "__Installer", "Installer", "Installers", "DirectX", "DXSETUP",
        "vcredist", "VCRedist", "DotNetFX", "PhysX", "sound", "sounds", "audio", "music", "movie", "movies", "video", "videos",
        "cinematics", "WwiseAudio", "Wwise", "Content", "Paks", "textures", "shaders", "ShaderCache", "images",
        "achievement_images", "Logs", "Crashes", "CrashReports", "Screenshots", "Language", "Languages", "Localization",
        "Localisation", "$RECYCLE.BIN", "System Volume Information", ".git", ".svn", "node_modules", "__MACOSX",
    };

    static bool SinInteres(string nombre) =>
        CarpetasSinInteres.Contains(nombre) || nombre.EndsWith(".app", StringComparison.OrdinalIgnoreCase) || nombre.StartsWith('$');

    // Únicos archivos que el escáner necesita conocer (se ignora todo lo demás).
    static readonly HashSet<string> ArchivosDeInteres = new(StringComparer.OrdinalIgnoreCase)
    {
        "steam_api64.dll", "steam_api.dll", "steam_appid.txt", "steam_interfaces.txt", "local_save.txt", "steam_emu.ini",
        "steam_api.cdx", "steam_api64.cdx", "OnlineFix.ini", "OnlineFix64.dll", "OnlineFix.dll", "cream_api.ini",
        "SmartSteamEmu.ini", "3DMGAME.ini", "ALI213.ini", "SKIDROW.ini", "rld.ini", "EMPRESS.ini", "tenoke.ini",
        "Galaxy64.dll", "Galaxy.dll", "EOSSDK-Win64-Shipping.dll", "EOSSDK-Win32-Shipping.dll", "uplay_r1_loader64.dll",
        "uplay_r1_loader.dll", "upc_r2_loader64.dll", "upc_r1_loader64.dll", "uplay_r2_loader64.dll",
        "MicrosoftGame.config", "appxmanifest.xml",
    };

    // Subcarpetas de las bibliotecas (cada una, un posible juego). Las bibliotecas no válidas
    // (carpetas del sistema, enlaces) se ignoran aunque estén en config.json.
    public static List<string> ListarCarpetasDeJuegos(Config cfg, Logger log)
    {
        var res = new List<string>();
        foreach (var bib in cfg.Bibliotecas)
        {
            var problema = Seguridad.ProblemaBiblioteca(bib);
            if (problema != null)
            {
                log?.Invoke(problema == "lib.missing" ? Nivel.Aviso : Nivel.Error, L.T(problema, bib));
                continue;
            }
            try
            {
                res.AddRange(new DirectoryInfo(bib).EnumerateDirectories("*", Seguridad.Enumeracion)
                    .Where(d => !SinInteres(d.Name) && (d.Attributes & FileAttributes.Hidden) == 0)
                    .Select(d => d.FullName));
            }
            catch (Exception e) { log?.Invoke(Nivel.Error, L.T("lib.readFail", bib, Util.Explicar(e))); }
        }
        return res;
    }

    // Revisa varios juegos en paralelo (lectura de disco), devolviendo los resultados en el mismo orden.
    public static List<Juego> DetectarVarios(IReadOnlyList<string> carpetas, Config cfg, Action<int, string> progreso = null)
    {
        var res = new Juego[carpetas.Count];
        int hechos = 0;
        var opciones = new ParallelOptions { MaxDegreeOfParallelism = Math.Clamp(Environment.ProcessorCount / 2, 2, 4) };
        Parallel.For(0, carpetas.Count, opciones, i =>
        {
            try { res[i] = Detectar(carpetas[i], cfg); }
            catch { res[i] = null; }
            progreso?.Invoke(Interlocked.Increment(ref hechos), Path.GetFileName(carpetas[i]));
        });
        return res.ToList();
    }

    // Huella rápida (solo el primer nivel) para saber si una carpeta cambió sin recorrerla entera.
    public static string Huella(string carpeta)
    {
        try
        {
            var di = new DirectoryInfo(carpeta);
            long n = 0, max = di.LastWriteTimeUtc.Ticks;
            foreach (var e in di.EnumerateFileSystemInfos("*", Seguridad.Enumeracion)) { n++; max = Math.Max(max, e.LastWriteTimeUtc.Ticks); }
            return n + ":" + max;
        }
        catch { return ""; }
    }

    public static Juego Detectar(string carpeta, Config cfg)
    {
        var j = new Juego { Carpeta = carpeta, Nombre = Path.GetFileName(carpeta) };
        var porNombre = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var carpetasClave = new List<string>(); // solo steam_settings y SteamData
        var infosGog = new List<string>();
        var reloj = System.Diagnostics.Stopwatch.StartNew();

        // Recorrido sin seguir enlaces/junctions, sin archivos de sistema ni de la nube, y sin
        // entrar en carpetas de recursos. Una sola consulta al disco por carpeta.
        var pila = new Stack<(DirectoryInfo, int)>();
        pila.Push((new DirectoryInfo(carpeta), 0));
        while (pila.Count > 0)
        {
            if (j.Entradas >= EntradasMax || reloj.ElapsedMilliseconds > TiempoMaxMs) { j.Truncado = true; break; }
            var (dir, prof) = pila.Pop();
            try
            {
                foreach (var e in dir.EnumerateFileSystemInfos("*", Seguridad.Enumeracion))
                {
                    j.Entradas++;
                    string nombre = e.Name;
                    if (e is DirectoryInfo d)
                    {
                        if (nombre.Equals("steam_settings", StringComparison.OrdinalIgnoreCase) || nombre.Equals("SteamData", StringComparison.OrdinalIgnoreCase))
                            carpetasClave.Add(d.FullName);
                        if (prof < ProfundidadMax && !SinInteres(nombre)) pila.Push((d, prof + 1));
                    }
                    else
                    {
                        if (nombre.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) j.Ejecutables++;
                        if (ArchivosDeInteres.Contains(nombre))
                        {
                            if (!porNombre.TryGetValue(nombre, out var l)) porNombre[nombre] = l = new List<string>();
                            l.Add(e.FullName);
                        }
                        else if (nombre.StartsWith("goggame-", StringComparison.OrdinalIgnoreCase) && nombre.EndsWith(".info", StringComparison.OrdinalIgnoreCase))
                            infosGog.Add(e.FullName);
                    }
                }
            }
            catch { /* carpeta inaccesible: se salta */ }
        }

        List<string> Por(string nombre) => porNombre.TryGetValue(nombre, out var l) ? l : new List<string>();

        j.Dlls = Por("steam_api64.dll").Concat(Por("steam_api.dll")).ToList();
        j.SteamSettings = carpetasClave.Where(d => string.Equals(Path.GetFileName(d), "steam_settings", StringComparison.OrdinalIgnoreCase)).ToList();
        // Solo cuentan las steam_settings que están junto a una steam_api.dll (se ignoran, p. ej., las de versiones para Mac).
        var junto = j.SteamSettings.Where(d => j.Dlls.Any(dll =>
            string.Equals(Path.GetDirectoryName(dll), Path.GetDirectoryName(d), StringComparison.OrdinalIgnoreCase))).ToList();
        if (junto.Count > 0) j.SteamSettings = junto;

        var emus = new List<string>();
        var fuentesAppId = new List<string>();
        void Add(string e) { if (!emus.Contains(e)) emus.Add(e); }
        void AddIni(string nombre, string emu) { foreach (var f in Por(nombre)) { Add(emu); fuentesAppId.Add(f); } }

        if (j.SteamSettings.Count > 0 || Por("steam_interfaces.txt").Count > 0 || Por("local_save.txt").Count > 0) Add("goldberg");
        foreach (var f in Por("steam_emu.ini"))
        {
            Add(Regex.IsMatch(Util.LeerTexto(f), "RUNE", RegexOptions.IgnoreCase) ? "rune" : "codex");
            fuentesAppId.Add(f);
        }
        if (Por("steam_api.cdx").Count > 0 || Por("steam_api64.cdx").Count > 0) Add("codex");
        AddIni("OnlineFix.ini", "onlineFix");
        if (Por("OnlineFix64.dll").Count > 0 || Por("OnlineFix.dll").Count > 0) Add("onlineFix");
        AddIni("cream_api.ini", "creamAPI");
        AddIni("SmartSteamEmu.ini", "smartSteamEmu");
        AddIni("3DMGAME.ini", "_3dm");
        AddIni("ALI213.ini", "ali213");
        AddIni("SKIDROW.ini", "skidrow");
        AddIni("rld.ini", "rld");
        AddIni("EMPRESS.ini", "empress");
        AddIni("tenoke.ini", "userstats");
        if (carpetasClave.Any(d => string.Equals(Path.GetFileName(d), "SteamData", StringComparison.OrdinalIgnoreCase))) Add("userstats");

        // Identificador de DLL (solo lectura): firma digital + firmas de emuladores dentro del archivo.
        j.InfoDlls = j.Dlls.Select(IdentificadorDll.Identificar).ToList();
        if (j.Dlls.Count > 0 && emus.Count == 0)
        {
            var porDll = j.InfoDlls.SelectMany(i => i.Emuladores).Distinct().ToList();
            if (porDll.Count > 0) { foreach (var e in porDll) Add(e); j.IdentificadoPorDll = true; }
            else if (j.InfoDlls.All(i => i.FirmaValve)) Add("steam");
            else Add("desconocido");
        }
        j.Emuladores = emus;

        // Juegos sin Steam: ¿qué plataforma de logros usan?
        if (j.Dlls.Count == 0 && emus.Count == 0)
        {
            bool Hay(params string[] nombres) => nombres.Any(n => Por(n).Count > 0);
            var infoGog = infosGog.FirstOrDefault(f => Regex.IsMatch(Path.GetFileName(f), @"^goggame-\d+\.info$", RegexOptions.IgnoreCase));

            if (Hay("Galaxy64.dll", "Galaxy.dll")) j.Plataforma = "gog-logros";
            else if (infoGog != null) j.Plataforma = "gog";
            else if (Hay("EOSSDK-Win64-Shipping.dll", "EOSSDK-Win32-Shipping.dll")) j.Plataforma = "epic";
            else if (Hay("uplay_r1_loader64.dll", "uplay_r1_loader.dll", "upc_r2_loader64.dll", "upc_r1_loader64.dll", "uplay_r2_loader64.dll")) j.Plataforma = "ubisoft";
            else if (Hay("MicrosoftGame.config", "appxmanifest.xml")) j.Plataforma = "xbox";
            else j.Plataforma = "ninguna";

            if (infoGog != null)
            {
                // Un juego puede traer varios goggame-*.info (juego base + DLC). Los logros están bajo el
                // juego base, que es el "rootGameId" común; se usa el .info del juego base para el nombre.
                string nombre = null, id = null;
                foreach (var f in infosGog.Where(f => Regex.IsMatch(Path.GetFileName(f), @"^goggame-\d+\.info$", RegexOptions.IgnoreCase)))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(Util.LeerTexto(f));
                        string S(string k) => doc.RootElement.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
                        string gameId = S("gameId"), raiz = S("rootGameId") ?? gameId;
                        if (id == null || gameId == raiz) { id = raiz; nombre = S("name"); }
                        if (gameId == raiz) break; // este es el juego base
                    }
                    catch { }
                }
                j.GogId = id != null && Regex.IsMatch(id, @"^\d{1,20}$") ? id : null;
                j.PlataformaDetalle = $"GOG: {nombre ?? Path.GetFileName(infoGog)} (ID {id ?? "?"})";
            }
        }

        // AppID: manual > steam_appid.txt > claves de los .ini
        string manual = cfg.AppIdManual(carpeta);
        if (!string.IsNullOrWhiteSpace(manual)) { j.AppId = manual.Trim(); j.AppIdOrigen = L.T("appid.fromConfig"); }
        if (j.AppId == null)
            foreach (var f in Por("steam_appid.txt"))
            {
                var m = Regex.Match(Util.LeerTexto(f), @"\d+");
                if (m.Success) { j.AppId = m.Value; j.AppIdOrigen = Path.GetRelativePath(carpeta, f); break; }
            }
        if (j.AppId == null)
            foreach (var f in fuentesAppId)
            {
                var m = Regex.Match(Util.LeerTexto(f), @"^\s*(?:RealAppId|AppId|appid)\s*=\s*(\d+)", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                if (m.Success && m.Groups[1].Value != "0") { j.AppId = m.Groups[1].Value; j.AppIdOrigen = Path.GetRelativePath(carpeta, f); break; }
            }
        // Un AppID solo puede ser un número: así nunca puede usarse para formar rutas (p. ej. "..\..\").
        if (!Seguridad.EsAppId(j.AppId)) { j.AppId = null; j.AppIdOrigen = null; }

        Diagnosticar(j);
        return j;
    }

    static IEnumerable<string> BuscarEnJuego(string raiz, string[] partes)
    {
        IEnumerable<string> actuales = new[] { raiz };
        foreach (var p in partes)
            actuales = p == "*"
                ? actuales.SelectMany(c => { try { return Directory.GetDirectories(c); } catch { return Array.Empty<string>(); } })
                : actuales.Select(c => Path.Combine(c, p));
        return actuales.Where(File.Exists);
    }

    public static void Diagnosticar(Juego j)
    {
        j.Mensajes.Clear(); j.ArchivosHydra.Clear(); j.Reparable = false; j.Estado = Estado.Ok;
        if (j.Truncado) j.Mensajes.Add(L.T("diag.partial"));

        if (j.Dlls.Count == 0 && j.Emuladores.Count == 0)
        {
            j.Estado = Estado.SinSteam;
            switch (j.Plataforma)
            {
                case "gog-logros":
                    j.Mensajes.Add(L.T("diag.gogAch"));
                    if (j.GogId != null)
                        j.Mensajes.Add(j.AppId != null ? L.T("diag.gogBridge", j.AppId) : L.T("diag.gogBridgeNeedsId", L.T("menu.setAppId").TrimEnd('…')));
                    break;
                case "gog":
                    j.Mensajes.Add(L.T("diag.gogNo"));
                    break;
                case "epic":
                    j.Mensajes.Add(L.T("diag.epic"));
                    break;
                case "ubisoft":
                    j.Mensajes.Add(L.T("diag.ubi"));
                    break;
                case "xbox":
                    j.Mensajes.Add(L.T("diag.xbox"));
                    break;
                default:
                    j.Mensajes.Add(L.T("diag.none"));
                    break;
            }
            if (j.PlataformaDetalle != null) j.Mensajes.Add($"[{j.PlataformaDetalle}]");
            return;
        }
        if (j.Emuladores.Contains("desconocido"))
        {
            j.Estado = Estado.NoSoportado;
            var info = j.InfoDlls.FirstOrDefault();
            if (info != null && info.FirmaValida && !info.FirmaValve)
                j.Mensajes.Add(L.T("diag.signedOther", info.Firmante));
            else if (info != null && !info.FirmaValida &&
                     (info.Firmante != null || (info.Empresa ?? "").Contains("Valve", StringComparison.OrdinalIgnoreCase)))
                j.Mensajes.Add(L.T("diag.valveModified"));
            else
                j.Mensajes.Add(L.T("diag.unknownEmu"));
            j.Mensajes.Add(L.T("diag.options"));
            j.Mensajes.Add(L.T("diag.guideHint", L.T("menu.guide").TrimEnd('…')));
            return;
        }
        if (j.Emuladores.Contains("steam"))
        {
            j.Estado = Estado.SteamOriginal;
            j.Mensajes.Add(L.T("diag.steamOriginal"));
            if (j.AppId != null && RutasHydra.ArchivosDeSteam(j.AppId) is { Count: > 0 } fs)
            {
                j.ArchivosHydra.AddRange(fs);
                j.Mensajes.Add(L.T("diag.steamReads"));
            }
            else j.Mensajes.Add(L.T("diag.steamWill"));
            return;
        }
        if (j.AppId == null)
        {
            j.Estado = Estado.SinAppId;
            j.Mensajes.Add(L.T("diag.noAppId", L.T("menu.setAppId").TrimEnd('…')));
            return;
        }

        foreach (var emu in j.Emuladores)
        {
            if (RutasHydra.EnCarpetaJuego.TryGetValue(emu, out var partes)) j.ArchivosHydra.AddRange(BuscarEnJuego(j.Carpeta, partes));
            else j.ArchivosHydra.AddRange(RutasHydra.Archivos(emu, j.AppId).Where(File.Exists));
        }

        if (j.Emuladores.Contains("goldberg"))
        {
            foreach (var d in j.SteamSettings)
            {
                try
                {
                    foreach (var sub in Directory.GetDirectories(d).Where(s => Regex.IsMatch(Path.GetFileName(s), @"^\d+$")))
                    {
                        var f = Path.Combine(sub, "achievements.json");
                        if (File.Exists(f)) j.ArchivosHydra.Add(f);
                    }
                }
                catch { }
            }
            var dirs = j.CarpetasSteamSettings().ToList();
            if (dirs.Any(d => !File.Exists(Path.Combine(d, "achievements.json"))))
            {
                j.Estado = Estado.Reparable; j.Reparable = true;
                j.Mensajes.Add(L.T("diag.missingAch"));
            }
            else if (dirs.Any(d => !File.Exists(Path.Combine(d, "steam_appid.txt"))))
            {
                j.Estado = Estado.Reparable; j.Reparable = true;
                j.Mensajes.Add(L.T("diag.missingAppIdFile"));
            }
        }

        if (j.IdentificadoPorDll)
            j.Mensajes.Add(L.T("diag.byDll", j.EmuladoresTexto));

        if (j.Estado == Estado.Ok)
        {
            if (j.ArchivosHydra.Count > 0) j.Mensajes.Add(L.T("diag.hydraReads", j.ArchivosHydra.Count));
            else { j.Estado = Estado.Listo; j.Mensajes.Add(L.T("diag.allReady")); }
        }
    }
}

// ------------------------------------------------------------------ registro de cambios (para deshacer)
public class Cambio
{
    public DateTime Fecha { get; set; }
    public string Juego { get; set; }
    public string Ruta { get; set; }
    public string Tipo { get; set; } // "archivo" | "carpeta"
    public string Sha256 { get; set; }
}

public static class Registro
{
    static string Ruta => Path.Combine(Config.Carpeta, "cambios.json");
    static readonly object Bloqueo = new();
    static readonly JsonSerializerOptions Opciones = new() { WriteIndented = true };

    public static List<Cambio> Leer()
    {
        lock (Bloqueo)
        {
            try { return File.Exists(Ruta) ? JsonSerializer.Deserialize<List<Cambio>>(File.ReadAllText(Ruta)) ?? new() : new(); }
            catch { return new(); }
        }
    }

    static void Escribir(List<Cambio> lista)
    {
        lock (Bloqueo) Util.EscribirAtomico(Ruta, JsonSerializer.Serialize(lista, Opciones));
    }

    public static void Anotar(Cambio c)
    {
        lock (Bloqueo) { var l = Leer(); l.Add(c); Escribir(l); }
    }

    // ¿Lo creó la app y nadie lo ha modificado desde la última vez que la app lo escribió?
    public static bool EsDeLaApp(string ruta)
    {
        lock (Bloqueo)
        {
            var c = Leer().LastOrDefault(x => x.Tipo == "archivo" && string.Equals(x.Ruta, ruta, StringComparison.OrdinalIgnoreCase));
            return c != null && File.Exists(ruta) && string.Equals(Util.Sha256(ruta), c.Sha256, StringComparison.OrdinalIgnoreCase);
        }
    }

    // La app reescribió un archivo suyo (p. ej. un puente con logros nuevos): se actualiza su huella.
    public static void Actualizar(string ruta, string sha)
    {
        lock (Bloqueo)
        {
            var l = Leer();
            var c = l.LastOrDefault(x => x.Tipo == "archivo" && string.Equals(x.Ruta, ruta, StringComparison.OrdinalIgnoreCase));
            if (c == null) return;
            c.Sha256 = sha; c.Fecha = DateTime.Now;
            Escribir(l);
        }
    }

    public static (int quitados, int conservados) Deshacer(Logger log)
    {
        var lista = Leer();
        if (lista.Count == 0) { log(Nivel.Info, L.T("log.noChanges")); return (0, 0); }
        var pendientes = new List<Cambio>();
        int quitados = 0, conservados = 0;

        foreach (var c in Enumerable.Reverse(lista))
        {
            // cambios.json podría haber sido manipulado: solo se deshace lo que la app pudo crear.
            if (!Seguridad.EsDestinoPermitido(c.Ruta, c.Tipo == "carpeta"))
            {
                log(Nivel.Error, L.T("sec.undoSuspicious", c.Ruta));
                pendientes.Add(c);
                continue;
            }
            try
            {
                if (c.Tipo == "archivo")
                {
                    if (!File.Exists(c.Ruta)) { log(Nivel.Detalle, L.T("undo.gone", c.Ruta)); continue; }
                    if (!string.Equals(Util.Sha256(c.Ruta), c.Sha256, StringComparison.OrdinalIgnoreCase))
                    {
                        log(Nivel.Aviso, L.T("undo.keptModified", c.Ruta));
                        conservados++;
                        continue;
                    }
                    Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(c.Ruta,
                        Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                        Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
                    log(Nivel.Exito, L.T("undo.recycled", c.Ruta));
                    quitados++;
                }
                else if (c.Tipo == "carpeta")
                {
                    if (!Directory.Exists(c.Ruta)) continue;
                    if (Directory.EnumerateFileSystemEntries(c.Ruta).Any())
                    {
                        log(Nivel.Aviso, L.T("undo.keptFolder", c.Ruta));
                        conservados++;
                        continue;
                    }
                    Directory.Delete(c.Ruta);
                    log(Nivel.Exito, L.T("undo.folderRemoved", c.Ruta));
                    quitados++;
                }
            }
            catch (Exception e)
            {
                log(Nivel.Error, L.T("undo.fail", c.Ruta, Util.Explicar(e)));
                pendientes.Add(c);
            }
        }
        pendientes.Reverse();
        Escribir(pendientes);
        return (quitados, conservados);
    }
}

// ------------------------------------------------------------------ reparación
public static class Reparador
{
    // Sin redirecciones automáticas (la clave nunca puede acabar en otro servidor) y con tope de tamaño.
    static readonly HttpClient Http = new(new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false })
    {
        Timeout = TimeSpan.FromSeconds(20),
        MaxResponseContentBufferSize = 8 * 1024 * 1024,
    };
    static readonly Dictionary<string, string> CacheEsquemas = new();
    static readonly Dictionary<string, string> CacheFallos = new(); // AppID -> motivo (no se repite la consulta en la sesión)

    // Uso prudente de la API de Steam: una petición cada vez, con pausa entre ellas,
    // un tope por sesión, y sin reintentar con una clave rechazada ni durante un bloqueo por exceso.
    const int PausaEntrePeticionesMs = 3000;
    const int MaxPeticionesPorSesion = 150;
    static readonly SemaphoreSlim UnaALaVez = new(1, 1);
    static DateTime ultimaPeticion = DateTime.MinValue;
    static DateTime pausadoHasta = DateTime.MinValue;
    static int peticiones;
    static string claveRechazada;

    static async Task<(string json, int total)> ObtenerListaDeLogros(string appId, Config cfg)
    {
        string clave = (cfg.SteamApiKey ?? "").Trim();
        if (clave == "")
            throw new SteamException(L.T("steam.noKey"));

        if (CacheEsquemas.TryGetValue(appId, out var cache)) return (cache, JsonDocument.Parse(cache).RootElement.GetArrayLength());
        if (CacheFallos.TryGetValue(appId, out var motivo)) throw new SteamException(L.T("steam.cached", motivo));
        if (clave == claveRechazada)
            throw new SteamException(L.T("steam.keyBlocked"));
        if (DateTime.Now < pausadoHasta)
            throw new SteamException(L.T("steam.paused", pausadoHasta.ToString("HH:mm")));
        if (peticiones >= MaxPeticionesPorSesion)
            throw new SteamException(L.T("steam.cap", MaxPeticionesPorSesion));

        await UnaALaVez.WaitAsync();
        try
        {
            var espera = ultimaPeticion.AddMilliseconds(PausaEntrePeticionesMs) - DateTime.Now;
            if (espera > TimeSpan.Zero) await Task.Delay(espera);
            ultimaPeticion = DateTime.Now;
            peticiones++;

            string url = "https://api.steampowered.com/ISteamUserStats/GetSchemaForGame/v2/?key="
                + Uri.EscapeDataString(clave) + "&appid=" + appId + "&l=" + Uri.EscapeDataString(L.Steam);
            if (!Seguridad.EsAppId(appId)) throw new SteamException(L.T("steam.badAppId", "?", 0));
            var uri = new Uri(url);
            Seguridad.ExigirUrlApi(uri); // solo HTTPS y solo api.steampowered.com
            HttpResponseMessage res;
            try { res = await Http.GetAsync(uri); }
            catch (TaskCanceledException) { throw new SteamException(L.T("steam.timeout")); }
            catch (HttpRequestException) { throw new SteamException(L.T("steam.noConn")); }

            if ((int)res.StatusCode is >= 300 and < 400)
                throw new SteamException(L.T("steam.redirect"));
            if (res.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized)
            {
                claveRechazada = clave;
                throw new SteamException(L.T("steam.keyRejected"));
            }
            if ((int)res.StatusCode == 429)
            {
                pausadoHasta = DateTime.Now.AddMinutes(15);
                throw new SteamException(L.T("steam.rateLimited", pausadoHasta.ToString("HH:mm")));
            }
            if (!res.IsSuccessStatusCode)
            {
                CacheFallos[appId] = L.T("steam.badAppId", appId, (int)res.StatusCode);
                throw new SteamException(CacheFallos[appId]);
            }
            return await ProcesarRespuesta(res, appId);
        }
        finally { UnaALaVez.Release(); }
    }

    // Nombres (API) de los logros del juego en Steam; usa la misma caché y protecciones que la preparación.
    public static async Task<HashSet<string>> NombresDeLogros(string appId, Config cfg)
    {
        var (json, _) = await ObtenerListaDeLogros(appId, cfg);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.EnumerateArray().Select(e => e.GetProperty("name").GetString()).ToHashSet(StringComparer.Ordinal);
    }

    static async Task<(string json, int total)> ProcesarRespuesta(HttpResponseMessage res, string appId)
    {
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        if (!doc.RootElement.TryGetProperty("game", out var game) ||
            !game.TryGetProperty("availableGameStats", out var stats) ||
            !stats.TryGetProperty("achievements", out var achs) || achs.GetArrayLength() == 0)
        {
            CacheFallos[appId] = L.T("steam.noAch", appId);
            throw new SteamException(CacheFallos[appId]);
        }

        var lista = new List<Dictionary<string, object>>();
        foreach (var a in achs.EnumerateArray())
        {
            string S(string k) => a.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : "";
            int hidden = a.TryGetProperty("hidden", out var h) && h.ValueKind == JsonValueKind.Number ? h.GetInt32() : 0;
            lista.Add(new()
            {
                ["name"] = S("name"),
                ["displayName"] = S("displayName") == "" ? S("name") : S("displayName"),
                ["description"] = S("description"),
                ["hidden"] = hidden,
                ["icon"] = S("icon"),
                ["icon_gray"] = S("icongray"),
                ["icongray"] = S("icongray"),
            });
        }
        string json = JsonSerializer.Serialize(lista, new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
        CacheEsquemas[appId] = json;
        return (json, lista.Count);
    }

    // Devuelve true si el juego quedó preparado. Nunca sobrescribe nada.
    public static async Task<bool> Reparar(Juego j, Config cfg, Logger log)
    {
        bool simular = cfg.SoloSimular;

        if (!j.Emuladores.Contains("goldberg") || j.AppId == null)
        {
            log(Nivel.Aviso, L.T("fix.cannot", j.Nombre, string.Join(" ", j.Mensajes)));
            return false;
        }

        try
        {
            foreach (var dir in j.CarpetasSteamSettings())
            {
                if (!Directory.Exists(dir))
                {
                    if (!simular)
                    {
                        Seguridad.Exigir(dir, esCarpeta: true);
                        Directory.CreateDirectory(dir);
                        Registro.Anotar(new Cambio { Fecha = DateTime.Now, Juego = j.Nombre, Ruta = dir, Tipo = "carpeta" });
                    }
                    log(Nivel.Detalle, L.T(simular ? "fix.simFolder" : "fix.folder", Path.GetRelativePath(j.Carpeta, dir)));
                }

                await CrearSiFalta(j, Path.Combine(dir, "steam_appid.txt"), () => Task.FromResult((j.AppId, "AppID " + j.AppId)), simular, log);
                await CrearSiFalta(j, Path.Combine(dir, "achievements.json"), async () =>
                {
                    if (string.IsNullOrWhiteSpace(cfg.SteamApiKey))
                        throw new SteamException(L.T("steam.noKey"));
                    log(Nivel.Detalle, L.T("fix.downloading", j.Nombre, j.AppId));
                    var (json, total) = await ObtenerListaDeLogros(j.AppId, cfg);
                    return (json, L.T("fix.nAch", total));
                }, simular, log);
            }
        }
        catch (Exception e)
        {
            log(Nivel.Error, L.T("fix.failed", j.Nombre, Util.Explicar(e)));
            return false;
        }

        if (!simular) { Escaner.Diagnosticar(j); }
        log(Nivel.Exito, simular
            ? L.T("fix.simDone", j.Nombre)
            : L.T("fix.done", j.Nombre));
        return true;
    }

    static async Task CrearSiFalta(Juego j, string ruta, Func<Task<(string contenido, string desc)>> generar, bool simular, Logger log)
    {
        string rel = Path.GetRelativePath(j.Carpeta, ruta);
        if (File.Exists(ruta)) { log(Nivel.Detalle, L.T("fix.exists", rel)); return; }
        var (contenido, desc) = await generar();
        if (simular) { log(Nivel.Detalle, L.T("fix.simCreate", rel, desc)); return; }
        Seguridad.Exigir(ruta);
        if (Util.CrearArchivoNuevo(ruta, contenido))
        {
            Registro.Anotar(new Cambio { Fecha = DateTime.Now, Juego = j.Nombre, Ruta = ruta, Tipo = "archivo", Sha256 = Util.Sha256(ruta) });
            log(Nivel.Detalle, L.T("fix.created", rel, desc));
        }
        else log(Nivel.Detalle, L.T("fix.appeared", rel));
    }
}

// ------------------------------------------------------------------ vigilancia de instalaciones nuevas
public class EstadoCarpeta
{
    public int Conteo { get; set; } = -1;
    public bool Procesado { get; set; }
    public bool SinJuego { get; set; }   // la última vez no tenía archivos de juego
    public string Huella { get; set; }   // huella rápida del primer nivel en ese momento
}

public static class Vigilancia
{
    static string Ruta => Path.Combine(Config.Carpeta, "estado.json");

    public static Dictionary<string, EstadoCarpeta> Cargar()
    {
        try { return File.Exists(Ruta) ? JsonSerializer.Deserialize<Dictionary<string, EstadoCarpeta>>(File.ReadAllText(Ruta)) ?? new() : new(); }
        catch { return new(); }
    }

    public static void Guardar(Dictionary<string, EstadoCarpeta> e) => Util.EscribirAtomico(Ruta, JsonSerializer.Serialize(e));
}
