namespace HydraLogros;

static class Recursos
{
    static Icon icono;

    // Icono de la app, incrustado en el ejecutable (app.ico).
    public static Icon Icono
    {
        get
        {
            if (icono != null) return icono;
            using var s = typeof(Recursos).Assembly.GetManifestResourceStream("HydraLogros.app.ico");
            icono = s != null ? new Icon(s) : SystemIcons.Application;
            return icono;
        }
    }
}
