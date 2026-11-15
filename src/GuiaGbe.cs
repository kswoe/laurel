using System.Diagnostics;

namespace HydraLogros;

// Guía asistida para juegos NO SOPORTADO (su DLL ya no es la original de Valve).
// La app ayuda con lo seguro (copia de seguridad, abrir carpetas, comprobar el resultado),
// pero NUNCA descarga, copia ni coloca un emulador: ese paso lo hace el usuario.
public class GuiaGbe : Form
{
    const string RepoGbe = "https://github.com/Detanup01/gbe_fork/releases";

    Juego juego;
    readonly Config cfg;
    readonly Logger log;
    readonly Action<Juego> alDetectar;
    readonly Label lblResultado = new() { AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), Margin = new Padding(0, 10, 0, 0) };

    int S(int px) => LogicalToDeviceUnits(px);

    public GuiaGbe(Juego juego, Config cfg, Logger log, Action<Juego> alDetectar)
    {
        this.juego = juego; this.cfg = cfg; this.log = log; this.alDetectar = alDetectar;
        Text = L.T("guide.title", juego.Nombre);
        Icon = Recursos.Icono;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 9.5f);
        AutoSize = true; AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(S(16));

        int ancho = S(560);
        var p = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false };
        Label Texto(string t, bool negrita = false, Color? color = null) => new()
        {
            Text = t, AutoSize = true, MaximumSize = new Size(ancho, 0), Margin = new Padding(0, 4, 0, 4),
            Font = negrita ? new Font("Segoe UI", 9.5f, FontStyle.Bold) : Font, ForeColor = color ?? SystemColors.ControlText,
        };
        Button Boton(string t, Action a) { var b = new Button { Text = t, AutoSize = true, Margin = new Padding(S(18), 2, 0, 6) }; b.Click += (_, _) => a(); return b; }

        p.Controls.Add(Texto(L.T("guide.intro"), true));
        p.Controls.Add(Texto(L.T("guide.step1")));
        p.Controls.Add(Boton(L.T("guide.backup"), HacerCopia));
        p.Controls.Add(Texto(L.T("guide.step2")));
        p.Controls.Add(Boton(L.T("guide.openRepo"), () => Abrir(RepoGbe)));
        p.Controls.Add(Texto(L.T("guide.step3")));
        p.Controls.Add(Boton(L.T("guide.openFolder"), AbrirCarpetaDll));
        p.Controls.Add(Texto(L.T("guide.step4")));
        p.Controls.Add(Boton(L.T("guide.check"), Comprobar));
        p.Controls.Add(lblResultado);
        p.Controls.Add(Texto(L.T("guide.note"), false, Color.DimGray));
        var cerrar = new Button { Text = L.T("guide.close"), AutoSize = true, DialogResult = DialogResult.Cancel, Margin = new Padding(0, 12, 0, 0) };
        p.Controls.Add(cerrar);
        CancelButton = cerrar;
        Controls.Add(p);
    }

    void HacerCopia()
    {
        foreach (var dll in juego.Dlls)
        {
            string copia = dll + ".hydralogros-original";
            try
            {
                if (File.Exists(copia)) { Informar(Nivel.Info, L.T("guide.backupExists", Path.GetFileName(copia))); continue; }
                Seguridad.Exigir(copia);
                File.Copy(dll, copia, false); // nunca sobrescribe
                Informar(Nivel.Exito, L.T("guide.backupDone", Path.GetRelativePath(juego.Carpeta, copia)));
            }
            catch (Exception e) { Informar(Nivel.Error, L.T("guide.backupFail", Path.GetFileName(dll), Util.Explicar(e))); }
        }
    }

    void AbrirCarpetaDll()
    {
        var dll = juego.Dlls.FirstOrDefault();
        if (dll == null) return;
        try { Process.Start("explorer.exe", "/select,\"" + dll + "\""); }
        catch (Exception e) { Informar(Nivel.Error, L.T("log.openFail", dll, e.Message)); }
    }

    void Comprobar()
    {
        juego = Escaner.Detectar(juego.Carpeta, cfg);
        if (juego.Emuladores.Contains("goldberg"))
        {
            Informar(Nivel.Exito, L.T("guide.detected"));
            alDetectar?.Invoke(juego);
        }
        else Informar(Nivel.Aviso, L.T("guide.stillUnsupported"));
    }

    void Informar(Nivel n, string texto)
    {
        lblResultado.Text = texto;
        lblResultado.ForeColor = n switch { Nivel.Exito => Color.ForestGreen, Nivel.Error => Color.Firebrick, Nivel.Aviso => Color.DarkOrange, _ => SystemColors.ControlText };
        log(n, $"{juego.Nombre}: {texto}");
    }

    void Abrir(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch (Exception e) { Informar(Nivel.Error, L.T("log.openFail", url, e.Message)); }
    }
}
