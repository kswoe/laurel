// Puentes de logros: copian a Hydra logros que el juego YA registró en un sitio que Hydra no lee.
// Nunca inventan logros: solo trasladan desbloqueos reales al formato que Hydra entiende
// (%AppData%\GSE Saves\<AppID>\achievements.json, formato Goldberg).
//
// Fuentes:
//  - GOG Galaxy: tabla UserAchievements de galaxy-2.0.db (se lee una COPIA temporal, nunca el original).
//  - RLE: %AppData%\RLE\<AppID>\achievements.ini (Hydra conoce la ruta pero no la revisa).
//
// Reglas: el archivo de destino solo se escribe si no existe o si lo creó la app (registro de cambios).
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace HydraLogros;

public static class Puentes
{
    // Carpeta base de destino y de RLE (configurable solo para pruebas aisladas).
    public static string AppData { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    static readonly string ProgramData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
    static string GogDb => Path.Combine(ProgramData, "GOG.com", "Galaxy", "storage", "galaxy-2.0.db");
    static string Destino(string appId) => Path.Combine(AppData, "GSE Saves", appId, "achievements.json");

    // Último estado anunciado por cada puente, para no repetir mensajes en cada revisión.
    static readonly Dictionary<string, string> anunciado = new();
    static readonly object bloqueo = new();

    // ---- caché de la base de datos de GOG (solo se vuelve a leer si cambia)
    static DateTime gogMarca = DateTime.MinValue;
    static Dictionary<string, List<(string apikey, DateTime? fecha)>> gogCache;

    public static bool HayGogGalaxy => File.Exists(GogDb);

    public static async Task SincronizarTodo(IEnumerable<Juego> juegos, Config cfg, Logger log)
    {
        if (!cfg.Puentes) return;
        foreach (var j in juegos.Where(j => j.Plataforma == "gog-logros" && j.GogId != null && j.AppId != null))
        {
            try { await SincronizarGog(j, cfg, log); }
            catch (Exception e) { Anunciar("gog:" + j.Carpeta, "err:" + e.Message, () => log(Nivel.Error, L.T("br.gogDbFail", Util.Explicar(e)))); }
        }
        try { SincronizarRle(cfg, log); }
        catch (Exception e) { Anunciar("rle", "err:" + e.Message, () => log(Nivel.Error, L.T("br.rleFail", Util.Explicar(e)))); }
    }

    // ------------------------------------------------------------------ GOG Galaxy
    static async Task SincronizarGog(Juego j, Config cfg, Logger log)
    {
        var datos = LeerGog();
        string clave = "gog_" + j.GogId;
        if (!datos.TryGetValue(clave, out var logros) || logros.Count == 0)
        {
            Anunciar("gog:" + j.Carpeta, "vacio", () => log(Nivel.Detalle, L.T("br.gogNone", j.Nombre)));
            return;
        }

        // Si hay clave de API, se comprueba que los nombres coinciden con los de Steam (y se usa su forma exacta).
        var validos = logros;
        if (!string.IsNullOrWhiteSpace(cfg.SteamApiKey))
        {
            HashSet<string> nombresSteam;
            try { nombresSteam = await Reparador.NombresDeLogros(j.AppId, cfg); }
            catch (SteamException e) { Anunciar("gog:" + j.Carpeta, "steam:" + e.Message, () => log(Nivel.Aviso, e.Message)); return; }
            var mapa = nombresSteam.ToDictionary(n => n, n => n, StringComparer.OrdinalIgnoreCase);
            validos = logros.Where(l => mapa.ContainsKey(l.apikey)).Select(l => (mapa[l.apikey], l.fecha)).ToList();
            if (validos.Count == 0)
            {
                Anunciar("gog:" + j.Carpeta, "nomatch", () => log(Nivel.Aviso, L.T("br.gogNoMatch", j.Nombre, j.AppId)));
                return;
            }
            if (validos.Count < logros.Count)
                Anunciar("gog-parcial:" + j.Carpeta, $"{validos.Count}/{logros.Count}", () => log(Nivel.Aviso, L.T("br.gogPartial", validos.Count, logros.Count, j.Nombre)));
        }

        Escribir(j.AppId, validos, cfg, log, "gog:" + j.Carpeta,
            (n, destino) => L.T("br.gogCopied", n, j.Nombre, destino));
    }

    static Dictionary<string, List<(string apikey, DateTime? fecha)>> LeerGog()
    {
        var marca = new[] { GogDb, GogDb + "-wal" }.Where(File.Exists).Select(File.GetLastWriteTimeUtc).DefaultIfEmpty().Max();
        lock (bloqueo)
        {
            if (gogCache != null && marca == gogMarca) return gogCache;
        }

        if (new FileInfo(GogDb).Length > Seguridad.MaxBaseDatosBytes) throw new InvalidDataException("galaxy-2.0.db > 256 MB");
        // Copia temporal: nunca se abre ni se bloquea la base de datos de GOG Galaxy.
        string tmp = Path.Combine(Path.GetTempPath(), "HydraLogros-gog-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmp);
        var res = new Dictionary<string, List<(string apikey, DateTime? fecha)>>(StringComparer.OrdinalIgnoreCase);
        try
        {
            string copia = Path.Combine(tmp, "galaxy-2.0.db");
            foreach (var sufijo in new[] { "", "-wal", "-shm" })
            {
                if (!File.Exists(GogDb + sufijo)) continue;
                using var origen = new FileStream(GogDb + sufijo, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using var dest = File.Create(copia + sufijo);
                origen.CopyTo(dest);
            }
            foreach (var fila in Sqlite.Consultar(copia, "SELECT gameReleaseKey, apikey, unlockTime FROM UserAchievements WHERE isUnlocked = 1", 3))
            {
                if (!res.TryGetValue(fila[0], out var lista)) res[fila[0]] = lista = new();
                if (!lista.Any(x => x.Item1 == fila[1])) lista.Add((fila[1], LeerFecha(fila[2])));
            }
        }
        finally
        {
            try { Directory.Delete(tmp, true); } catch { }
        }
        lock (bloqueo) { gogCache = res; gogMarca = marca; }
        return res;
    }

    static DateTime? LeerFecha(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (long.TryParse(s, out long n)) return DateTimeOffset.FromUnixTimeSeconds(n > 100000000000 ? n / 1000 : n).UtcDateTime;
        if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var d)) return d;
        return null;
    }

    // ------------------------------------------------------------------ RLE
    static void SincronizarRle(Config cfg, Logger log)
    {
        string raiz = Path.Combine(AppData, "RLE");
        if (!Directory.Exists(raiz)) return;
        foreach (var dir in Directory.GetDirectories(raiz).Where(d => Regex.IsMatch(Path.GetFileName(d), @"^\d+$")))
        {
            string appId = Path.GetFileName(dir);
            var ini = new[] { "achievements.ini", "Achievements.ini" }.Select(n => Path.Combine(dir, n)).FirstOrDefault(File.Exists);
            if (ini == null) continue;
            var logros = LeerIniPorDefecto(Util.LeerTexto(ini));
            if (logros.Count == 0) continue;
            Escribir(appId, logros, cfg, log, "rle:" + appId, (n, destino) => L.T("br.rleCopied", n, appId, destino));
        }
    }

    // Formato "por defecto" de Hydra: [NOMBRE] Achieved=1 UnlockTime=<segundos>
    static List<(string, DateTime?)> LeerIniPorDefecto(string texto)
    {
        var res = new List<(string, DateTime?)>();
        string seccion = null; bool conseguido = false; DateTime? fecha = null;
        void Cerrar() { if (seccion != null && conseguido) res.Add((seccion, fecha)); }
        foreach (var linea0 in texto.Split('\n'))
        {
            var linea = linea0.Trim();
            var m = Regex.Match(linea, @"^\[(.+)\]$");
            if (m.Success) { Cerrar(); seccion = m.Groups[1].Value; conseguido = false; fecha = null; continue; }
            var kv = linea.Split('=', 2);
            if (kv.Length != 2 || seccion == null) continue;
            if (kv[0].Trim().Equals("Achieved", StringComparison.OrdinalIgnoreCase)) conseguido = kv[1].Trim() == "1";
            if (kv[0].Trim().Equals("UnlockTime", StringComparison.OrdinalIgnoreCase)) fecha = LeerFecha(kv[1].Trim());
        }
        Cerrar();
        return res;
    }

    // ------------------------------------------------------------------ escritura segura en formato Goldberg
    static void Escribir(string appId, List<(string nombre, DateTime? fecha)> logros, Config cfg, Logger log, string id, Func<int, string, string> mensajeOk)
    {
        if (!Seguridad.EsAppId(appId)) return;
        string destino = Destino(appId);
        string mostrado = Path.Combine("GSE Saves", appId, "achievements.json");

        var obj = new SortedDictionary<string, object>(StringComparer.Ordinal);
        foreach (var (nombre, fecha) in logros)
            obj[nombre] = new Dictionary<string, object>
            {
                ["earned"] = true,
                ["earned_time"] = fecha.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(fecha.Value, DateTimeKind.Utc)).ToUnixTimeSeconds() : 0,
            };
        string json = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });

        if (cfg.SoloSimular)
        {
            Anunciar(id, "sim:" + logros.Count, () => log(Nivel.Detalle, L.T("br.sim", logros.Count, mostrado)));
            return;
        }

        if (File.Exists(destino))
        {
            if (!Registro.EsDeLaApp(destino))
            {
                Anunciar(id, "ajeno", () => log(Nivel.Aviso, L.T("br.notOurs", mostrado)));
                return;
            }
            if (Util.LeerTexto(destino) == json) { Anunciar(id, "ok:" + logros.Count, () => { }); return; }
            Seguridad.Exigir(destino);
            Util.EscribirAtomico(destino, json);
            Registro.Actualizar(destino, Util.Sha256(destino));
        }
        else
        {
            string dir = Path.GetDirectoryName(destino);
            if (!Directory.Exists(dir))
            {
                Seguridad.Exigir(dir, esCarpeta: true);
                Directory.CreateDirectory(dir);
                Registro.Anotar(new Cambio { Fecha = DateTime.Now, Juego = appId, Ruta = dir, Tipo = "carpeta" });
            }
            Seguridad.Exigir(destino);
            if (!Util.CrearArchivoNuevo(destino, json)) { Anunciar(id, "ajeno", () => log(Nivel.Aviso, L.T("br.notOurs", mostrado))); return; }
            Registro.Anotar(new Cambio { Fecha = DateTime.Now, Juego = appId, Ruta = destino, Tipo = "archivo", Sha256 = Util.Sha256(destino) });
        }
        Anunciar(id, "ok:" + logros.Count, () => log(Nivel.Exito, mensajeOk(logros.Count, mostrado)));
    }

    static void Anunciar(string id, string estado, Action accion)
    {
        lock (bloqueo)
        {
            if (anunciado.TryGetValue(id, out var antes) && antes == estado) return;
            anunciado[id] = estado;
        }
        accion();
    }
}

// Acceso mínimo a SQLite usando la biblioteca que ya trae Windows 10/11 (winsqlite3.dll).
static class Sqlite
{
    const string Dll = "winsqlite3.dll";
    const int SQLITE_OK = 0, SQLITE_ROW = 100, SQLITE_OPEN_READWRITE = 2;

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)] static extern int sqlite3_open_v2(byte[] archivo, out IntPtr db, int flags, IntPtr vfs);
    [DllImport(Dll, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)] static extern int sqlite3_prepare16_v2(IntPtr db, string sql, int bytes, out IntPtr stmt, IntPtr resto);
    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)] static extern int sqlite3_step(IntPtr stmt);
    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)] static extern IntPtr sqlite3_column_text16(IntPtr stmt, int col);
    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)] static extern int sqlite3_finalize(IntPtr stmt);
    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)] static extern int sqlite3_close(IntPtr db);
    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)] static extern IntPtr sqlite3_errmsg16(IntPtr db);

    public static List<string[]> Consultar(string archivo, string sql, int columnas)
    {
        var filas = new List<string[]>();
        var nombre = Encoding.UTF8.GetBytes(archivo + "\0");
        // Se abre la COPIA temporal (SQLite necesita poder leer su registro WAL); la original de GOG nunca se abre.
        if (sqlite3_open_v2(nombre, out var db, SQLITE_OPEN_READWRITE, IntPtr.Zero) != SQLITE_OK)
        {
            string err = db != IntPtr.Zero ? Marshal.PtrToStringUni(sqlite3_errmsg16(db)) : "open";
            sqlite3_close(db);
            throw new IOException("SQLite: " + err);
        }
        try
        {
            if (sqlite3_prepare16_v2(db, sql, -1, out var stmt, IntPtr.Zero) != SQLITE_OK)
                throw new IOException("SQLite: " + Marshal.PtrToStringUni(sqlite3_errmsg16(db)));
            try
            {
                while (sqlite3_step(stmt) == SQLITE_ROW)
                {
                    var fila = new string[columnas];
                    for (int c = 0; c < columnas; c++) fila[c] = Marshal.PtrToStringUni(sqlite3_column_text16(stmt, c)) ?? "";
                    filas.Add(fila);
                }
            }
            finally { sqlite3_finalize(stmt); }
        }
        finally { sqlite3_close(db); }
        return filas;
    }
}
