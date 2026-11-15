// Identificador de steam_api.dll — SOLO LECTURA: nunca modifica ni mueve la DLL.
// 1) Verifica la firma digital (Authenticode) con Windows y mira si el firmante es Valve.
// 2) Busca dentro del archivo textos característicos de emuladores conocidos.
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace HydraLogros;

public class InfoDll
{
    public string Ruta;
    public bool FirmaValida;      // Windows confirma que la firma es correcta y el archivo no fue alterado
    public string Firmante;       // nombre del certificado, si tiene firma (válida o no)
    public bool FirmaValve => FirmaValida && Firmante != null && Firmante.Contains("Valve", StringComparison.OrdinalIgnoreCase);
    public string Empresa;
    public string Version;
    public List<string> Emuladores = new();
    public List<string> Pistas = new(); // textos encontrados, para mostrar en el detalle
    public string Error;

    public string Resumen()
    {
        var sb = new StringBuilder();
        sb.Append(Path.GetFileName(Ruta)).Append(": ");
        if (Error != null) return sb.Append(L.T("dll.unreadable", Error)).ToString();
        sb.Append(FirmaValve ? L.T("dll.valve")
            : FirmaValida ? L.T("dll.signedBy", Firmante)
            : Firmante != null ? L.T("dll.badSig", Firmante)
            : L.T("dll.unsigned"));
        if (!string.IsNullOrWhiteSpace(Version)) sb.Append(L.T("dll.version", Version));
        if (!string.IsNullOrWhiteSpace(Empresa)) sb.Append(L.T("dll.company", Empresa));
        if (Pistas.Count > 0) sb.Append(L.T("dll.contains", string.Join(", ", Pistas)));
        return sb.ToString();
    }
}

public static class IdentificadorDll
{
    const long TamanoMax = 64L * 1024 * 1024;

    // (emulador, texto, exigir que no esté pegado a otras letras)
    static readonly (string emu, string texto, bool palabra)[] Firmas =
    {
        ("goldberg", "Goldberg", false), ("goldberg", "gbe_fork", false), ("goldberg", "GSE Saves", false), ("goldberg", "steam_settings", false),
        ("codex", "CODEX", true), ("rune", "RUNE", true),
        ("empress", "EMPRESS", true),
        ("userstats", "TENOKE", true), ("userstats", "tenoke.ini", false), ("userstats", "user_stats.ini", false),
        ("onlineFix", "OnlineFix", false),
        ("smartSteamEmu", "SmartSteamEmu", false),
        ("creamAPI", "CreamAPI", false), ("creamAPI", "cream_api", false),
        ("_3dm", "3DMGAME", false),
        ("ali213", "ALI213", false),
        ("skidrow", "SKIDROW", true),
        ("rld", "RLD!", false),
        ("razor1911", "Razor1911", false), ("razor1911", "RAZOR1911", false),
    };

    static readonly Dictionary<string, InfoDll> Cache = new(StringComparer.OrdinalIgnoreCase);

    public static InfoDll Identificar(string ruta)
    {
        // Cache por ruta + fecha + tamaño: no se relee la DLL si no cambió.
        string clave;
        try { var fi = new FileInfo(ruta); clave = $"{ruta}|{fi.Length}|{fi.LastWriteTimeUtc.Ticks}"; }
        catch { clave = ruta; }
        lock (Cache) if (Cache.TryGetValue(clave, out var c)) return c;

        var info = new InfoDll { Ruta = ruta };
        try
        {
            try
            {
                var v = FileVersionInfo.GetVersionInfo(ruta);
                info.Empresa = v.CompanyName?.Trim();
                info.Version = v.FileVersion?.Trim();
            }
            catch { }

            info.FirmaValida = Authenticode.EsValida(ruta);
            try
            {
#pragma warning disable SYSLIB0057
                info.Firmante = X509Certificate.CreateFromSignedFile(ruta).Subject;
#pragma warning restore SYSLIB0057
                info.Firmante = NombreComun(info.Firmante);
            }
            catch { info.Firmante = null; }

            if (!info.FirmaValve)
            {
                var len = new FileInfo(ruta).Length;
                if (len <= TamanoMax)
                {
                    byte[] datos;
                    using (var fs = new FileStream(ruta, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                    {
                        datos = new byte[len];
                        fs.ReadExactly(datos);
                    }
                    foreach (var (emu, texto, palabra) in Firmas)
                    {
                        if (Contiene(datos, Encoding.ASCII.GetBytes(texto), palabra) || Contiene(datos, Encoding.Unicode.GetBytes(texto), false))
                        {
                            if (!info.Emuladores.Contains(emu)) info.Emuladores.Add(emu);
                            if (!info.Pistas.Contains(texto)) info.Pistas.Add(texto);
                        }
                    }
                }
            }
        }
        catch (Exception e) { info.Error = Util.Explicar(e); }

        lock (Cache) Cache[clave] = info;
        return info;
    }

    static string NombreComun(string subject)
    {
        foreach (var parte in subject.Split(','))
        {
            var p = parte.Trim();
            if (p.StartsWith("CN=", StringComparison.OrdinalIgnoreCase)) return p[3..].Trim('"');
        }
        return subject;
    }

    static bool EsLetra(byte b) => (b >= 'A' && b <= 'Z') || (b >= 'a' && b <= 'z');

    static bool Contiene(byte[] datos, byte[] patron, bool palabra)
    {
        ReadOnlySpan<byte> span = datos;
        int desde = 0;
        while (desde < datos.Length)
        {
            int i = span[desde..].IndexOf(patron);
            if (i < 0) return false;
            int pos = desde + i;
            if (!palabra) return true;
            bool antesOk = pos == 0 || !EsLetra(datos[pos - 1]);
            int fin = pos + patron.Length;
            bool despuesOk = fin >= datos.Length || !EsLetra(datos[fin]);
            if (antesOk && despuesOk) return true;
            desde = pos + 1;
        }
        return false;
    }
}

// Verificación de firma con la API de Windows (WinVerifyTrust), sin consultar internet.
static class Authenticode
{
    static readonly Guid AccionVerificar = new("00AAC56B-CD44-11d0-8CC2-00C04FC295EE");

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct WINTRUST_FILE_INFO
    {
        public uint cbStruct;
        [MarshalAs(UnmanagedType.LPWStr)] public string pcwszFilePath;
        public IntPtr hFile;
        public IntPtr pgKnownSubject;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct WINTRUST_DATA
    {
        public uint cbStruct;
        public IntPtr pPolicyCallbackData;
        public IntPtr pSIPClientData;
        public uint dwUIChoice;
        public uint fdwRevocationChecks;
        public uint dwUnionChoice;
        public IntPtr pFile;
        public uint dwStateAction;
        public IntPtr hWVTStateData;
        public IntPtr pwszURLReference;
        public uint dwProvFlags;
        public uint dwUIContext;
        public IntPtr pSignatureSettings;
    }

    [DllImport("wintrust.dll", CharSet = CharSet.Unicode)]
    static extern int WinVerifyTrust(IntPtr hwnd, [In] ref Guid accion, [In] ref WINTRUST_DATA datos);

    public static bool EsValida(string ruta)
    {
        var archivo = new WINTRUST_FILE_INFO { cbStruct = (uint)Marshal.SizeOf<WINTRUST_FILE_INFO>(), pcwszFilePath = ruta };
        IntPtr pArchivo = Marshal.AllocHGlobal(Marshal.SizeOf<WINTRUST_FILE_INFO>());
        try
        {
            Marshal.StructureToPtr(archivo, pArchivo, false);
            var datos = new WINTRUST_DATA
            {
                cbStruct = (uint)Marshal.SizeOf<WINTRUST_DATA>(),
                dwUIChoice = 2,            // WTD_UI_NONE
                fdwRevocationChecks = 0,   // WTD_REVOKE_NONE
                dwUnionChoice = 1,         // WTD_CHOICE_FILE
                pFile = pArchivo,
                dwProvFlags = 0x1000,      // WTD_CACHE_ONLY_URL_RETRIEVAL: no conectarse a internet
            };
            var accion = AccionVerificar;
            return WinVerifyTrust(IntPtr.Zero, ref accion, ref datos) == 0;
        }
        catch { return false; }
        finally
        {
            Marshal.DestroyStructure<WINTRUST_FILE_INFO>(pArchivo);
            Marshal.FreeHGlobal(pArchivo);
        }
    }
}
