using System.Runtime.InteropServices;

// Candado contra "DLL hijacking": las DLL nativas (wintrust, winsqlite3) solo se cargan desde System32,
// nunca desde la carpeta de la app ni desde la carpeta actual.
[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.System32)]

namespace HydraLogros;

static class Program
{
    [STAThread]
    static void Main()
    {
        // Ninguna búsqueda de texto puede quedarse colgada con archivos maliciosos (ReDoS).
        AppContext.SetData("REGEX_DEFAULT_MATCH_TIMEOUT", TimeSpan.FromSeconds(2));
        ApplicationConfiguration.Initialize();

        // Una sola instancia: evita dos vigilancias escribiendo a la vez.
        // (Se mantiene el nombre antiguo para que tampoco convivan con una versión anterior abierta.)
#if PRUEBA
        const string nombreMutex = "HydraLogros_Prueba"; // solo en compilaciones de prueba aisladas
#else
        const string nombreMutex = "HydraLogros_UnaInstancia";
#endif
        using var mutex = new Mutex(true, nombreMutex, out bool primera);
        var avisos = new List<Aviso>();
        var cfg = Config.Cargar(avisos);
        L.Usar(cfg.IdiomaInterfaz ?? L.Detectar());

        if (!primera)
        {
            MessageBox.Show(L.T("app.running"), Tema.Nombre, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Seguridad.LimpiarTemporales();
        Seguridad.Bibliotecas = () => cfg.Bibliotecas.ToList();
        if (Seguridad.EsAdministrador()) avisos.Add(new Aviso(Nivel.Aviso, "sec.admin", Array.Empty<object>()));

        // Selector de idioma antes de la ventana principal.
        if (cfg.PreguntarIdioma || cfg.IdiomaInterfaz == null)
        {
            using var selector = new SelectorIdioma(L.Codigo, !cfg.PreguntarIdioma);
            if (selector.ShowDialog() != DialogResult.OK) return;
            Aplicar(cfg, selector, avisos);
        }

        // Se repite si el usuario cambia de idioma desde la ventana principal (la clave en memoria se conserva).
        bool reiniciar;
        do
        {
            var ventana = new MainForm(cfg, avisos);
            avisos = new List<Aviso>();
            Application.Run(ventana);
            reiniciar = ventana.Reiniciar;
        } while (reiniciar);
    }

    public static void Aplicar(Config cfg, SelectorIdioma selector, List<Aviso> avisos)
    {
        cfg.IdiomaInterfaz = selector.Codigo;
        cfg.PreguntarIdioma = !selector.NoPreguntar;
        L.Usar(cfg.IdiomaInterfaz);
        try { cfg.Guardar(); }
        catch (Exception e) { avisos.Add(new Aviso(Nivel.Error, "log.configSaveFail", new object[] { Util.Explicar(e) })); }
    }
}
