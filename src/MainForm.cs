using System.Diagnostics;

namespace HydraLogros;

public class MainForm : Form
{
    readonly Config cfg;
    readonly List<Aviso> avisosIniciales;
    bool ocupado;
    readonly object bloqueoLog = new();
    readonly string rutaLog = Path.Combine(Config.Carpeta, "registro.txt");
    // Todas las filas (visibles o no); la lista solo muestra las que pasan el filtro.
    readonly Dictionary<string, ListViewItem> filas = new(StringComparer.OrdinalIgnoreCase);
    HashSet<Estado> filtroEstados;
    string filtroTexto = "";
    HashSet<string> bibliotecasAusentes = new(StringComparer.OrdinalIgnoreCase);

    // true si la ventana se cerró para volver a abrirse en otro idioma
    public bool Reiniciar { get; private set; }

    // controles
    readonly ListBox lstBibliotecas = new() { IntegralHeight = false, Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, DrawMode = DrawMode.OwnerDrawFixed };
    readonly TextBox txtApiKey = new() { UseSystemPasswordChar = true, Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
    readonly CheckBox chkVigilar = new() { AutoSize = true };
    readonly CheckBox chkAuto = new() { AutoSize = true };
    readonly CheckBox chkSimular = new() { AutoSize = true };
    readonly CheckBox chkPuentes = new() { AutoSize = true };
    List<Juego> ultimosJuegos = new(); // última revisión, para los puentes
    readonly NumericUpDown numIntervalo = new() { Minimum = 10, Maximum = 3600 };
    readonly Boton btnRevisar = new() { Estilo = Boton.Tipo.Primario, Icono = Glifo.Revisar };
    readonly Boton btnReparar = new() { Estilo = Boton.Tipo.Primario, Icono = Glifo.Reparar, Color = Tema.Verde };
    readonly Boton btnDeshacer = new() { Icono = Glifo.Deshacer };
    readonly Boton btnAbrirCarpeta = new() { Icono = Glifo.Carpeta };
    readonly Boton btnIdioma = new() { Estilo = Boton.Tipo.Cabecera, Icono = Glifo.Idioma };
    readonly Boton btnVigilancia = new() { Estilo = Boton.Tipo.Cabecera };
    readonly ListaJuegos lista = new() { Dock = DockStyle.Fill };
    readonly Label lblVacio = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Tema.Suave, BackColor = Tema.Tarjeta, Visible = false };
    readonly CajaBusqueda busqueda = new();
    readonly List<TarjetaEstado> tarjetas = new();
    readonly RichTextBox log = new() { ReadOnly = true, Dock = DockStyle.Fill, BackColor = Tema.Consola, ForeColor = Tema.ConsolaTexto, BorderStyle = BorderStyle.None, DetectUrls = false };
    readonly Cabecera cabecera = new();
    readonly BarraActividad actividad = new();
    readonly ToolStripStatusLabel lblEstado = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
    readonly ToolTip pistas = new();
    readonly System.Windows.Forms.Timer temporizador = new();
    readonly NotifyIcon bandeja = new() { Icon = Recursos.Icono, Text = Tema.Nombre };

    public MainForm(Config cfg, List<Aviso> avisos)
    {
        this.cfg = cfg;
        avisosIniciales = avisos;
        Text = L.T("app.title");
        Size = new Size(S(1240), S(840));
        MinimumSize = new Size(S(980), S(640));
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9.5f);
        Icon = Recursos.Icono;
        BackColor = Tema.Fondo;

        ConstruirInterfaz();
        temporizador.Tick += async (_, _) => await TickVigilancia();
        bandeja.DoubleClick += (_, _) => Restaurar();
        bandeja.ContextMenuStrip = new ContextMenuStrip { Renderer = new ToolStripProfessionalRenderer(new ColoresMenu()) };
        bandeja.ContextMenuStrip.Items.Add(L.T("tray.open"), Tema.Imagen(Glifo.Abrir, Tema.Acento, S(16)), (_, _) => Restaurar());
        bandeja.ContextMenuStrip.Items.Add(L.T("tray.exit"), Tema.Imagen(Glifo.Salir, Tema.Suave, S(16)), (_, _) => { bandeja.Visible = false; Application.Exit(); });
        Resize += (_, _) =>
        {
            if (WindowState == FormWindowState.Minimized && chkVigilar.Checked)
            {
                Hide();
                bandeja.Visible = true;
                bandeja.ShowBalloonTip(3000, Tema.Nombre, L.T("tray.watching"), ToolTipIcon.Info);
            }
        };
        FormClosing += (_, _) => { temporizador.Stop(); bandeja.Visible = false; };
        FormClosed += (_, _) => bandeja.Dispose();
        Shown += async (_, _) => await Iniciar();
    }

    // ------------------------------------------------------------------ interfaz
    int S(int px) => LogicalToDeviceUnits(px);

    void ConstruirInterfaz()
    {
        chkVigilar.Text = L.T("ui.watch");
        chkAuto.Text = L.T("ui.auto");
        chkSimular.Text = L.T("ui.simulate");
        chkPuentes.Text = L.T("ui.bridges");
        btnRevisar.Text = L.T("ui.scan");
        btnReparar.Text = L.T("ui.fixAll");
        btnDeshacer.Text = L.T("ui.undo");
        btnAbrirCarpeta.Text = L.T("ui.openFolder");
        btnIdioma.Text = L.Nativo;
        pistas.SetToolTip(btnIdioma, L.T("ui.language"));
        pistas.SetToolTip(btnVigilancia, L.T("ui.watchTip"));
        log.Font = new Font(Tema.FuenteMono, 9.5f);

        // --- cabecera
        cabecera.Lema = L.T("app.tagline");
        cabecera.Sub = L.T("app.for");
        btnIdioma.Click += (_, _) => CambiarIdioma();
        btnVigilancia.Click += (_, _) => chkVigilar.Checked = !chkVigilar.Checked;
        var btnLeeme = new Boton { Estilo = Boton.Tipo.Cabecera, Icono = "", Text = L.T("ui.readme") };
        pistas.SetToolTip(btnLeeme, L.T("readme.tip"));
        btnLeeme.Click += (_, _) => AbrirLeeme();
        cabecera.Controls.Add(btnVigilancia);
        cabecera.Controls.Add(btnLeeme);
        cabecera.Controls.Add(btnIdioma);

        // --- barra lateral: bibliotecas, clave, opciones
        lstBibliotecas.ItemHeight = S(30);
        lstBibliotecas.DrawItem += DibujarBiblioteca;
        var btnAnadir = new Boton { Text = L.T("ui.add"), Icono = Glifo.Anadir, Estilo = Boton.Tipo.Fantasma };
        var btnQuitar = new Boton { Text = L.T("ui.remove"), Icono = Glifo.Quitar, Estilo = Boton.Tipo.Fantasma };
        var btnDetectar = new Boton { Text = L.T("ui.detect"), Icono = Glifo.Detectar, Estilo = Boton.Tipo.Fantasma };
        btnAnadir.Click += (_, _) => AnadirBiblioteca();
        btnQuitar.Click += (_, _) => QuitarBiblioteca();
        btnDetectar.Click += (_, _) => DetectarHydra();
        var (tBib, cBib) = Tarjeta.Crear(L.T("ui.librariesT"), Glifo.Biblioteca, L.T("ui.librariesH"));
        var marcoBib = Marco(lstBibliotecas);
        var botonesBib = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, BackColor = Tema.Tarjeta, Margin = new Padding(0, S(6), 0, 0) };
        botonesBib.Controls.AddRange(new Control[] { btnAnadir, btnQuitar, btnDetectar });
        foreach (Control b in botonesBib.Controls) b.Margin = new Padding(0, 0, S(2), 0);
        cBib.RowCount = 3;
        cBib.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        cBib.Controls.Add(marcoBib, 0, 1);
        cBib.Controls.Add(botonesBib, 0, 2);

        var btnVerClave = new Boton { Estilo = Boton.Tipo.Fantasma, Icono = Glifo.Ver, Color = Tema.Suave };
        pistas.SetToolTip(btnVerClave, L.T("ui.showKey"));
        btnVerClave.Click += (_, _) => txtApiKey.UseSystemPasswordChar = !txtApiKey.UseSystemPasswordChar;
        var lnkKey = new LinkLabel { Text = L.T("ui.getKey"), AutoSize = true, LinkColor = Tema.Acento, ActiveLinkColor = Tema.Indigo, LinkBehavior = LinkBehavior.HoverUnderline, Margin = new Padding(0, S(8), 0, 0) };
        lnkKey.LinkClicked += (_, _) => Abrir("https://steamcommunity.com/dev/apikey");
        var (tKey, cKey) = Tarjeta.Crear(L.T("ui.keyT"), Glifo.Llave, null);
        var lblKeyH = new Label { Text = L.T("ui.keyH"), AutoSize = true, ForeColor = Tema.Suave, Margin = new Padding(0, S(4), 0, 0) };
        tKey.Dock = DockStyle.Top; tKey.AutoSize = true; tKey.AutoSizeMode = AutoSizeMode.GrowAndShrink; cKey.AutoSize = true; cKey.Dock = DockStyle.Top;
        var filaKey = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2, BackColor = Tema.Tarjeta, Margin = Padding.Empty };
        filaKey.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        filaKey.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        txtApiKey.Margin = new Padding(0, S(3), S(4), 0);
        filaKey.Controls.Add(txtApiKey, 0, 0);
        filaKey.Controls.Add(btnVerClave, 1, 0);
        filaKey.Controls.Add(lblKeyH, 0, 1);
        filaKey.SetColumnSpan(lblKeyH, 2);
        filaKey.Controls.Add(lnkKey, 0, 2);
        cKey.RowCount = 3;
        cKey.RowStyles.Clear();
        cKey.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        cKey.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        cKey.Controls.Add(filaKey, 0, 1);

        var (tOpc, cOpc) = Tarjeta.Crear(L.T("ui.options"), Glifo.Ajustes, null);
        tOpc.Dock = DockStyle.Top; tOpc.AutoSize = true; tOpc.AutoSizeMode = AutoSizeMode.GrowAndShrink; cOpc.AutoSize = true; cOpc.Dock = DockStyle.Top;
        tOpc.Margin = Padding.Empty;
        var opc = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Tema.Tarjeta, Margin = Padding.Empty };
        foreach (var c in new[] { chkVigilar, chkAuto, chkPuentes, chkSimular }) { c.Margin = new Padding(0, S(2), 0, S(4)); c.ForeColor = Tema.Texto; }
        numIntervalo.Width = S(70);
        var filaInt = new FlowLayoutPanel { AutoSize = true, WrapContents = false, BackColor = Tema.Tarjeta, Margin = new Padding(0, S(6), 0, 0) };
        filaInt.Controls.AddRange(new Control[] { new Label { Text = L.T("ui.checkEvery"), AutoSize = true, Margin = new Padding(0, S(5), S(4), 0), ForeColor = Tema.Texto }, numIntervalo, new Label { Text = L.T("ui.seconds"), AutoSize = true, Margin = new Padding(S(4), S(5), 0, 0), ForeColor = Tema.Texto } });
        opc.Controls.AddRange(new Control[] { chkVigilar, chkAuto, chkPuentes, chkSimular, filaInt });
        cOpc.RowStyles.Clear();
        cOpc.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        cOpc.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        cOpc.Controls.Add(opc, 0, 1);

        var lateral = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = Tema.Fondo, Margin = new Padding(0, 0, S(14), 0) };
        lateral.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        lateral.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        lateral.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tBib.MinimumSize = new Size(0, S(150));
        lateral.Controls.Add(tBib, 0, 0);
        lateral.Controls.Add(tKey, 0, 1);
        lateral.Controls.Add(tOpc, 0, 2);

        // --- columna principal: acciones, contadores, juegos y registro
        btnRevisar.Click += async (_, _) => await Ejecutar(Revisar);
        btnReparar.Click += async (_, _) => await Ejecutar(RepararTodos);
        btnDeshacer.Click += async (_, _) => await DeshacerCambios();
        btnAbrirCarpeta.Click += (_, _) => Abrir(Config.Carpeta);
        var acciones = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, BackColor = Tema.Fondo, Margin = new Padding(0, 0, 0, S(12)) };
        acciones.Controls.AddRange(new Control[] { btnRevisar, btnReparar, btnDeshacer, btnAbrirCarpeta });
        foreach (Control b in acciones.Controls) b.Margin = new Padding(0, 0, S(8), S(4));

        var filaTarjetas = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 7, RowCount = 1, BackColor = Tema.Fondo, Margin = new Padding(0, 0, 0, S(12)) };
        void Contador(string etiqueta, params Estado[] estados)
        {
            var t = new TarjetaEstado { Etiqueta = etiqueta, Estados = estados.Length == 0 ? null : estados.ToHashSet(), Color = estados.Length == 0 ? Tema.Acento : Tema.DeEstado(estados[0]), Margin = new Padding(0, 0, tarjetas.Count < 6 ? S(10) : 0, 0) };
            t.Click += (_, _) => Filtrar(t);
            pistas.SetToolTip(t, etiqueta + "\n" + L.T("ui.filterTip"));
            filaTarjetas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 7));
            filaTarjetas.Controls.Add(t, tarjetas.Count, 0);
            tarjetas.Add(t);
        }
        Contador(L.T("ui.total"));
        Contador($"{L.T("st.ok")} · {L.T("st.ready")}", Estado.Ok, Estado.Listo);
        Contador(L.T("st.steam"), Estado.SteamOriginal);
        Contador(L.T("st.fixable"), Estado.Reparable);
        Contador(L.T("st.noAppId"), Estado.SinAppId);
        Contador(L.T("st.unsupported"), Estado.NoSoportado, Estado.Error);
        Contador(L.T("st.noSteam"), Estado.SinSteam);
        tarjetas[0].Activa = true;

        // lista de juegos
        lista.TextoEstado = TextoEstado;
        lista.Columns.Add(L.T("col.status"), S(150));
        lista.Columns.Add(L.T("col.game"), S(230));
        lista.Columns.Add(L.T("col.emu"), S(200));
        lista.Columns.Add("AppID", S(90));
        lista.Columns.Add(L.T("col.detail"), S(400));
        lista.DoubleClick += (_, _) => { if (Seleccionado() is Juego j) Abrir(j.Carpeta); };
        lista.ColumnClick += (_, e) =>
        {
            if (lista.ColumnaOrden == e.Column) lista.Ascendente = !lista.Ascendente;
            else { lista.ColumnaOrden = e.Column; lista.Ascendente = true; }
            Ordenar();
            lista.Invalidate();
        };
        var menu = new ContextMenuStrip { Renderer = new ToolStripProfessionalRenderer(new ColoresMenu()) };
        Image Ico(string g) => Tema.Imagen(g, Tema.Acento, S(16));
        menu.Items.Add(L.T("menu.openGame"), Ico(Glifo.Carpeta), (_, _) => { if (Seleccionado() is Juego j) Abrir(j.Carpeta); });
        menu.Items.Add(L.T("menu.fixOne"), Ico(Glifo.Reparar), async (_, _) => { if (Seleccionado() is Juego j) await Ejecutar(() => RepararUno(j)); });
        menu.Items.Add(L.T("menu.setAppId"), Ico(Glifo.Enlace), (_, _) => { if (Seleccionado() is Juego j) AsignarAppId(j); });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(L.T("menu.details"), Ico(Glifo.Info), (_, _) => { if (Seleccionado() is Juego j) MostrarDetalle(j); });
        menu.Items.Add(L.T("menu.guide"), Ico(Glifo.Guia), (_, _) => { if (Seleccionado() is Juego j) AbrirGuia(j); });
        menu.Opening += (_, e) => { if (Seleccionado() == null) e.Cancel = true; };
        lista.ContextMenuStrip = menu;

        busqueda.Caja.PlaceholderText = L.T("ui.search");
        busqueda.Caja.TextChanged += (_, _) => { filtroTexto = busqueda.Caja.Text.Trim(); AplicarFiltro(); };
        busqueda.Size = busqueda.GetPreferredSize(Size.Empty);
        var (tJuegos, cJuegos) = Tarjeta.Crear(L.T("ui.gamesT"), Glifo.Juegos, L.T("ui.gamesH"), null, busqueda);
        tJuegos.Margin = Padding.Empty;
        var marcoLista = new Panel { Dock = DockStyle.Fill, BackColor = Tema.Tarjeta, Margin = Padding.Empty };
        marcoLista.Controls.Add(lista);
        marcoLista.Controls.Add(lblVacio);
        cJuegos.Controls.Add(marcoLista, 0, 1);

        // registro en tiempo real
        var btnCopiar = new Boton { Text = L.T("ui.copyLog"), Icono = Glifo.Copiar, Estilo = Boton.Tipo.Fantasma, ColorTexto = Color.FromArgb(190, 194, 210) };
        var btnLimpiar = new Boton { Text = L.T("ui.clearLog"), Icono = Glifo.Limpiar, Estilo = Boton.Tipo.Fantasma, ColorTexto = Color.FromArgb(190, 194, 210) };
        btnCopiar.Click += (_, _) => { if (log.TextLength > 0) try { Clipboard.SetText(log.Text); } catch { } };
        btnLimpiar.Click += (_, _) => log.Clear(); // solo la vista: registro.txt no se toca
        var (tLog, cLog) = Tarjeta.Crear(L.T("ui.activity"), Glifo.Actividad, null, Tema.Consola, btnCopiar, btnLimpiar);
        tLog.Margin = Padding.Empty;
        cLog.Controls.Add(log, 0, 1);

        var division = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, BackColor = Tema.Fondo, SplitterWidth = S(12), Margin = Padding.Empty, FixedPanel = FixedPanel.None };
        division.Panel1.Controls.Add(tJuegos);
        division.Panel2.Controls.Add(tLog);
        Load += (_, _) =>
        {
            try
            {
                division.Panel1MinSize = S(160); division.Panel2MinSize = S(110);
                division.SplitterDistance = Math.Max(division.Panel1MinSize, (int)(division.Height * 0.6));
            }
            catch (InvalidOperationException) { } // ventana demasiado pequeña: se queda la división por defecto
        };

        var principal = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = Tema.Fondo, Margin = Padding.Empty };
        principal.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        principal.RowStyles.Add(new RowStyle(SizeType.Absolute, S(84)));
        principal.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        principal.Controls.Add(acciones, 0, 0);
        principal.Controls.Add(filaTarjetas, 0, 1);
        principal.Controls.Add(division, 0, 2);

        // ancho de la barra lateral: lo que pidan las opciones (según el idioma), dentro de unos límites
        int anchoLateral = Math.Clamp(opc.GetPreferredSize(Size.Empty).Width + S(34), S(330), S(460));
        var cuerpo = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Tema.Fondo, Padding = new Padding(S(16), S(14), S(16), S(12)) };
        cuerpo.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, anchoLateral + S(14)));
        cuerpo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        cuerpo.Controls.Add(lateral, 0, 0);
        cuerpo.Controls.Add(principal, 1, 0);

        var barra = new StatusStrip { BackColor = Tema.Tarjeta, SizingGrip = true, Padding = new Padding(S(10), 0, S(4), 0) };
        lblEstado.ForeColor = Tema.Suave;
#if PORTABLE
        const string edicion = " · portable";
#else
        const string edicion = "";
#endif
        var lblVersion = new ToolStripStatusLabel($"{Tema.Nombre} {Application.ProductVersion.Split('+')[0]}{edicion}") { ForeColor = Tema.Suave, Alignment = ToolStripItemAlignment.Right };
        lblEstado.Spring = false;
        barra.LayoutStyle = ToolStripLayoutStyle.Table;
        barra.Resize += (_, _) => lblEstado.Width = Math.Max(S(100), barra.ClientSize.Width - lblVersion.Width - S(60));
        lblEstado.AutoSize = false;
        barra.Items.AddRange(new ToolStripItem[] { lblEstado, lblVersion });

        Controls.Add(cuerpo);
        Controls.Add(actividad);
        Controls.Add(cabecera);
        Controls.Add(barra);
        ActualizarVacio();
        PintarVigilancia();
    }

    // Marco con borde fino para listas y cajas dentro de las tarjetas.
    Control Marco(Control interior)
    {
        var m = new Panel { Dock = DockStyle.Fill, Padding = new Padding(1), BackColor = Tema.Borde, Margin = Padding.Empty };
        interior.Dock = DockStyle.Fill;
        m.Controls.Add(interior);
        return m;
    }

    void DibujarBiblioteca(object sender, DrawItemEventArgs e)
    {
        if (e.Index < 0) return;
        bool sel = (e.State & DrawItemState.Selected) != 0;
        string ruta = lstBibliotecas.Items[e.Index].ToString();
        bool falta = bibliotecasAusentes.Contains(ruta);
        using (var b = new SolidBrush(sel ? Tema.Tenue(Tema.Acento, 0.86f) : Tema.Tarjeta)) e.Graphics.FillRectangle(b, e.Bounds);
        if (sel) using (var b = new SolidBrush(Tema.Acento)) e.Graphics.FillRectangle(b, e.Bounds.X, e.Bounds.Y, S(3), e.Bounds.Height);
        using var fi = new Font(Tema.FuenteIconos, 10f);
        TextRenderer.DrawText(e.Graphics, falta ? "" : "", fi, new Rectangle(e.Bounds.X + S(10), e.Bounds.Y, S(20), e.Bounds.Height),
            falta ? Tema.DeEstado(Estado.Error) : Tema.Oro, TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
        TextRenderer.DrawText(e.Graphics, ruta, lstBibliotecas.Font, new Rectangle(e.Bounds.X + S(36), e.Bounds.Y, e.Bounds.Width - S(40), e.Bounds.Height),
            falta ? Tema.DeEstado(Estado.Error) : Tema.Texto, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.PathEllipsis | TextFormatFlags.NoPadding);
    }

    // ------------------------------------------------------------------ arranque
    async Task Iniciar()
    {
        Log(Nivel.Info, L.T("log.started"));
        foreach (var a in avisosIniciales) Log(a.Nivel, L.T(a.Clave, a.Args));

        chkVigilar.Checked = cfg.Vigilar;
        chkAuto.Checked = cfg.RepararAutomaticamente;
        chkSimular.Checked = cfg.SoloSimular;
        chkPuentes.Checked = cfg.Puentes;
        numIntervalo.Value = Math.Clamp(cfg.IntervaloSegundos, 10, 3600);
        txtApiKey.Text = cfg.SteamApiKey; // solo en memoria (se conserva al cambiar de idioma)
        RefrescarBibliotecas();

        // La clave solo se guarda en memoria; nunca en config.json ni en el registro.
        string claveAnunciada = cfg.SteamApiKey;
        txtApiKey.TextChanged += (_, _) => cfg.SteamApiKey = txtApiKey.Text.Trim();
        txtApiKey.Leave += (_, _) =>
        {
            if (cfg.SteamApiKey == claveAnunciada) return;
            claveAnunciada = cfg.SteamApiKey;
            if (cfg.SteamApiKey == "") Log(Nivel.Info, L.T("log.keyCleared"));
            else Log(Nivel.Exito, L.T("log.keyReady"));
        };
        chkVigilar.CheckedChanged += (_, _) => { cfg.Vigilar = chkVigilar.Checked; GuardarConfig(); ActualizarVigilancia(); };
        chkAuto.CheckedChanged += (_, _) => { cfg.RepararAutomaticamente = chkAuto.Checked; GuardarConfig(); };
        chkPuentes.CheckedChanged += async (_, _) =>
        {
            cfg.Puentes = chkPuentes.Checked; GuardarConfig();
            Log(Nivel.Info, L.T(cfg.Puentes ? "log.bridgesOn" : "log.bridgesOff"));
            if (cfg.Puentes) await Ejecutar(() => Puentes.SincronizarTodo(ultimosJuegos, cfg, Log));
        };
        chkSimular.CheckedChanged += (_, _) =>
        {
            cfg.SoloSimular = chkSimular.Checked; GuardarConfig();
            Log(Nivel.Aviso, L.T(cfg.SoloSimular ? "log.simOn" : "log.simOff"));
        };
        numIntervalo.ValueChanged += (_, _) => { cfg.IntervaloSegundos = (int)numIntervalo.Value; GuardarConfig(); ActualizarVigilancia(); };

        if (cfg.Bibliotecas.Count == 0)
        {
            Log(Nivel.Info, L.T("log.detecting"));
            DetectarHydra();
        }
        if (string.IsNullOrWhiteSpace(cfg.SteamApiKey))
            Log(Nivel.Aviso, L.T("log.needKey"));

        await Ejecutar(Revisar);
        ActualizarVigilancia();
    }

    // Una sola ventana del LEEME; si ya está abierta, se trae al frente.
    VisorLeeme visor;
    void AbrirLeeme()
    {
        if (visor == null || visor.IsDisposed)
        {
            visor = new VisorLeeme();
            visor.Show(this);
        }
        else visor.Activate();
    }

    void CambiarIdioma()
    {
        if (ocupado) { Log(Nivel.Aviso, L.T("log.busy")); return; }
        string antes = L.Codigo;
        using var selector = new SelectorIdioma(L.Codigo, !cfg.PreguntarIdioma);
        if (selector.ShowDialog(this) != DialogResult.OK) return;
        var avisos = new List<Aviso>();
        Program.Aplicar(cfg, selector, avisos);
        foreach (var a in avisos) Log(a.Nivel, L.T(a.Clave, a.Args));
        if (selector.Codigo != antes) { Reiniciar = true; Close(); }
    }

    // ------------------------------------------------------------------ acciones
    async Task Ejecutar(Func<Task> accion)
    {
        if (ocupado) { Log(Nivel.Aviso, L.T("log.busy")); return; }
        ocupado = true;
        foreach (var b in new[] { btnRevisar, btnReparar, btnDeshacer, btnIdioma }) b.Enabled = false;
        actividad.Activa = true;
        try { await Task.Run(accion); }
        catch (Exception e) { Log(Nivel.Error, L.T("log.unexpected", Util.Explicar(e))); }
        finally
        {
            ocupado = false;
            if (!IsDisposed)
            {
                foreach (var b in new[] { btnRevisar, btnReparar, btnDeshacer, btnIdioma }) b.Enabled = true;
                actividad.Activa = false;
                ActualizarVacio();
            }
        }
    }

    async Task Revisar()
    {
        if (cfg.Bibliotecas.Count == 0) { Log(Nivel.Aviso, L.T("log.addLibrary")); return; }
        Estado_(L.T("st.scanning"));
        Log(Nivel.Info, L.T("log.scanHeader"));
        Invocar(() => { lista.Items.Clear(); filas.Clear(); ActualizarConteos(); ActualizarVacio(); });

        var carpetas = Escaner.ListarCarpetasDeJuegos(cfg, Log);
        var conteo = new Dictionary<Estado, int>();
        var plataformas = new Dictionary<string, int>();
        var revisados = new List<Juego>();
        var reloj = System.Diagnostics.Stopwatch.StartNew();
        var detectados = Escaner.DetectarVarios(carpetas, cfg, (n, nombre) => Estado_(L.T("st.scanningN", n, carpetas.Count, nombre)));
        int sinJuego = 0;
        for (int i = 0; i < carpetas.Count; i++)
        {
            var j = detectados[i];
            if (j == null) { Log(Nivel.Error, L.T("log.scanFail", Path.GetFileName(carpetas[i]), "?")); continue; }
            if (!j.PareceJuego) { sinJuego++; continue; } // carpetas sin archivos de juego: no se muestran
            revisados.Add(j);
            conteo[j.Estado] = conteo.GetValueOrDefault(j.Estado) + 1;
            if (j.Plataforma != null) plataformas[j.Plataforma] = plataformas.GetValueOrDefault(j.Plataforma) + 1;
            MostrarJuego(j);
            Log(NivelDe(j.Estado), $"{TextoEstado(j.Estado),-13} {j.Nombre} — {string.Join(" ", j.Mensajes)}");
        }

        string resumen = revisados.Count == 0 ? L.T("sum.noGames")
            : L.T("sum.games", revisados.Count, string.Join(" · ", conteo.OrderBy(k => k.Key).Select(k => $"{TextoEstado(k.Key)} {k.Value}")));
        Log(Nivel.Info, L.T("log.scanDone", resumen) + " " + L.T("log.scanTime", reloj.Elapsed.TotalSeconds.ToString("0.0")));
        if (sinJuego > 0) Log(Nivel.Detalle, L.T("log.notGames", sinJuego));
        if (conteo.GetValueOrDefault(Estado.Reparable) > 0)
            Log(Nivel.Aviso, L.T("log.hasFixable", L.T("ui.fixAll")));
        if (plataformas.Count > 0)
            Log(Nivel.Info, L.T("log.noSteamSummary", string.Join(" · ", plataformas.OrderByDescending(k => k.Value)
                .Select(k => $"{RutasHydra.Nombre(k.Key)} {k.Value}"))));
        Estado_(resumen);
        Invocar(Ordenar);
        ultimosJuegos = revisados;
        await Puentes.SincronizarTodo(ultimosJuegos, cfg, Log);
    }

    async Task RepararTodos()
    {
        Log(Nivel.Info, L.T("log.fixHeader"));
        var juegos = Escaner.DetectarVarios(Escaner.ListarCarpetasDeJuegos(cfg, Log), cfg).Where(j => j != null && j.Reparable).ToList();
        if (juegos.Count == 0) { Log(Nivel.Info, L.T("log.nothingToFix")); return; }
        int ok = 0, mal = 0;
        foreach (var j in juegos)
        {
            Estado_(L.T("st.fixing", j.Nombre));
            if (await Reparador.Reparar(j, cfg, Log)) ok++; else mal++;
            MostrarJuego(j);
        }
        string fin = L.T("log.fixDone", ok, mal);
        Log(mal == 0 ? Nivel.Exito : Nivel.Aviso, fin);
        Estado_(fin);
    }

    async Task RepararUno(Juego j)
    {
        var fresco = Escaner.Detectar(j.Carpeta, cfg);
        if (!fresco.Reparable) { Log(Nivel.Info, $"{fresco.Nombre}: {string.Join(" ", fresco.Mensajes)}"); MostrarJuego(fresco); return; }
        await Reparador.Reparar(fresco, cfg, Log);
        MostrarJuego(fresco);
    }

    async Task DeshacerCambios()
    {
        var cambios = Registro.Leer();
        if (cambios.Count == 0) { Log(Nivel.Info, L.T("log.noChanges")); return; }
        var r = MessageBox.Show(this, L.T("dlg.undoText", cambios.Count(c => c.Tipo == "archivo")),
            L.T("dlg.undoTitle"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (r != DialogResult.Yes) { Log(Nivel.Info, L.T("log.undoCancelled")); return; }
        await Ejecutar(() =>
        {
            Log(Nivel.Info, L.T("log.undoHeader"));
            var (q, c) = Registro.Deshacer(Log);
            Log(Nivel.Exito, L.T("log.undoDone", q, c));
            return Revisar();
        });
    }

    // ------------------------------------------------------------------ vigilancia
    void ActualizarVigilancia()
    {
        temporizador.Stop();
        bool activa = PintarVigilancia();
        if (activa)
        {
            temporizador.Interval = cfg.IntervaloSegundos * 1000;
            temporizador.Start();
            _ = TickVigilancia();
        }
    }

    bool PintarVigilancia()
    {
        bool activa = cfg.Vigilar && cfg.Bibliotecas.Count > 0;
        btnVigilancia.Text = L.T(activa ? "ui.watchOn" : "ui.watchOff", cfg.IntervaloSegundos).TrimStart('●', '○', ' ');
        btnVigilancia.Punto = activa ? Color.FromArgb(74, 222, 128) : Color.FromArgb(170, 175, 190);
        btnVigilancia.Invalidate();
        cabecera.PerformLayout();
        cabecera.Invalidate();
        return activa;
    }

    async Task TickVigilancia()
    {
        if (ocupado || IsDisposed) return;
        await Ejecutar(async () =>
        {
            var estado = Vigilancia.Cargar();
            bool primeraVez = !estado.ContainsKey("*");
            if (primeraVez)
            {
                estado["*"] = new EstadoCarpeta { Procesado = true };
                Log(Nivel.Info, L.T("log.watchStarted"));
            }

            foreach (var c in Escaner.ListarCarpetasDeJuegos(cfg, null))
            {
                if (!estado.TryGetValue(c, out var s)) estado[c] = s = new EstadoCarpeta { Procesado = primeraVez };
                if (s.Procesado) continue;

                // Huella rápida del primer nivel: si nada cambió desde la última vez que se vio sin juego,
                // no se vuelve a recorrer (p. ej. carpetas normales dentro de Descargas).
                string huella = Escaner.Huella(c);
                if (s.SinJuego && huella == s.Huella) continue;

                var j = Escaner.Detectar(c, cfg);
                if (j.Entradas != s.Conteo)
                {
                    if (s.Conteo == -1) Log(Nivel.Info, L.T("log.newFolder", j.Nombre));
                    s.Conteo = j.Entradas;
                    continue;
                }
                if (!j.PareceJuego) { s.SinJuego = true; s.Huella = huella; continue; } // aún no hay juego (o no es un juego)
                s.SinJuego = false;

                Log(Nivel.Info, L.T("log.installDone", j.Nombre));
                MostrarJuego(j);
                ultimosJuegos = ultimosJuegos.Where(x => !string.Equals(x.Carpeta, j.Carpeta, StringComparison.OrdinalIgnoreCase)).Append(j).ToList();
                if (j.Reparable && cfg.RepararAutomaticamente)
                {
                    bool ok = await Reparador.Reparar(j, cfg, Log);
                    MostrarJuego(j);
                    Notificar(L.T(ok ? "notif.ready" : "notif.failed", j.Nombre), ok);
                }
                else
                {
                    Log(NivelDe(j.Estado), $"{TextoEstado(j.Estado)} {j.Nombre} — {string.Join(" ", j.Mensajes)}");
                    Notificar($"{j.Nombre}: {TextoEstado(j.Estado)}", j.Estado is Estado.Ok or Estado.Listo or Estado.SteamOriginal);
                }
                s.Procesado = true;
            }

            await Puentes.SincronizarTodo(ultimosJuegos, cfg, Log);

            foreach (var k in estado.Keys.Where(k => k != "*" && !Directory.Exists(k)).ToList()) estado.Remove(k);
            try { Vigilancia.Guardar(estado); }
            catch (Exception e) { Log(Nivel.Error, L.T("log.stateSaveFail", Util.Explicar(e))); }
        });
    }

    // ------------------------------------------------------------------ bibliotecas
    void AnadirBiblioteca()
    {
        using var dlg = new FolderBrowserDialog { Description = L.T("dlg.pickLibrary"), UseDescriptionForTitle = true };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        if (cfg.Bibliotecas.Contains(dlg.SelectedPath, StringComparer.OrdinalIgnoreCase)) { Log(Nivel.Aviso, L.T("log.libExists")); return; }
        var problema = Seguridad.ProblemaBiblioteca(dlg.SelectedPath);
        if (problema != null) { Log(Nivel.Error, L.T(problema, dlg.SelectedPath)); return; }
        cfg.Bibliotecas.Add(dlg.SelectedPath);
        GuardarConfig(L.T("log.libAdded", dlg.SelectedPath));
        RefrescarBibliotecas();
        ActualizarVigilancia();
    }

    void QuitarBiblioteca()
    {
        if (lstBibliotecas.SelectedItem is not string ruta) { Log(Nivel.Aviso, L.T("log.selectLib")); return; }
        cfg.Bibliotecas.Remove(ruta);
        GuardarConfig(L.T("log.libRemoved", ruta));
        RefrescarBibliotecas();
        ActualizarVigilancia();
    }

    void DetectarHydra()
    {
        var encontradas = RutasHydra.DetectarBibliotecasDeHydra(Log);
        int nuevas = 0;
        foreach (var b in encontradas)
            if (Seguridad.ProblemaBiblioteca(b) is string p) Log(Nivel.Error, L.T(p, b));
            else if (!cfg.Bibliotecas.Contains(b, StringComparer.OrdinalIgnoreCase)) { cfg.Bibliotecas.Add(b); nuevas++; Log(Nivel.Exito, L.T("log.hydraLibFound", b)); }
        if (nuevas == 0 && encontradas.Count > 0) Log(Nivel.Info, L.T("log.hydraLibsKnown"));
        if (encontradas.Count == 0) Log(Nivel.Aviso, L.T("log.hydraNotDetected", L.T("ui.add")));
        if (nuevas > 0) { GuardarConfig(); RefrescarBibliotecas(); ActualizarVigilancia(); }
    }

    void RefrescarBibliotecas()
    {
        bibliotecasAusentes = cfg.Bibliotecas.Where(b => !Directory.Exists(b)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        lstBibliotecas.Items.Clear();
        foreach (var b in cfg.Bibliotecas) lstBibliotecas.Items.Add(b);
    }

    void AsignarAppId(Juego j)
    {
        string valor = Pedir(L.T("dlg.appId", j.Nombre), j.AppId ?? "");
        if (valor == null) return;
        valor = valor.Trim();
        if (valor == "") { cfg.AppIdsManuales.Remove(j.Nombre); GuardarConfig(L.T("log.appIdRemoved", j.Nombre)); }
        else if (!valor.All(char.IsDigit)) { Log(Nivel.Error, L.T("log.appIdInvalid", valor)); return; }
        else { cfg.AppIdsManuales[j.Nombre] = valor; GuardarConfig(L.T("log.appIdSet", valor, j.Nombre)); }
        var fresco = Escaner.Detectar(j.Carpeta, cfg);
        MostrarJuego(fresco);
        Log(NivelDe(fresco.Estado), $"{TextoEstado(fresco.Estado)} {fresco.Nombre} — {string.Join(" ", fresco.Mensajes)}");
    }

    void AbrirGuia(Juego j)
    {
        var fresco = Escaner.Detectar(j.Carpeta, cfg);
        if (fresco.Estado != Estado.NoSoportado) { Log(Nivel.Info, L.T("guide.onlyUnsupported")); return; }
        using var guia = new GuiaGbe(fresco, cfg, Log, detectado =>
        {
            MostrarJuego(detectado);
            if (detectado.Reparable) _ = Ejecutar(() => RepararUno(detectado));
        });
        guia.ShowDialog(this);
    }

    void MostrarDetalle(Juego j)
    {
        string origen = j.AppIdOrigen != null ? " (" + L.T("det.from", j.AppIdOrigen) + ")" : "";
        var txt = $"{L.T("col.game")}: {j.Nombre}\n{L.T("col.folder")}: {j.Carpeta}\n{L.T("col.emu")}: {j.EmuladoresTexto}\nAppID: {j.AppId ?? "—"}{origen}\n" +
                  $"{L.T("col.status")}: {TextoEstado(j.Estado)}\n\n{string.Join("\n", j.Mensajes)}";
        if (j.InfoDlls.Count > 0) txt += "\n\n" + L.T("det.dll") + "\n" + string.Join("\n", j.InfoDlls.Select(i => "• " + i.Resumen()));
        if (j.ArchivosHydra.Count > 0) txt += "\n\n" + L.T("det.hydraFiles") + "\n" + string.Join("\n", j.ArchivosHydra);
        MessageBox.Show(this, txt, j.Nombre, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    // ------------------------------------------------------------------ lista: filtro, orden y contadores
    bool PasaFiltro(Juego j) =>
        (filtroEstados == null || filtroEstados.Contains(j.Estado)) &&
        (filtroTexto == "" || j.Nombre.Contains(filtroTexto, StringComparison.OrdinalIgnoreCase)
                           || (j.AppId ?? "").Contains(filtroTexto, StringComparison.OrdinalIgnoreCase)
                           || (j.EmuladoresTexto ?? "").Contains(filtroTexto, StringComparison.OrdinalIgnoreCase));

    void Filtrar(TarjetaEstado t)
    {
        bool quitar = t.Estados == null || t.Activa;
        filtroEstados = quitar ? null : t.Estados;
        foreach (var x in tarjetas) x.Activa = quitar ? x.Estados == null : x == t;
        AplicarFiltro();
    }

    void AplicarFiltro()
    {
        var elegido = Seleccionado();
        lista.BeginUpdate();
        lista.Items.Clear();
        foreach (var it in filas.Values)
            if (PasaFiltro((Juego)it.Tag)) lista.Items.Add(it);
        Ordenar();
        lista.EndUpdate();
        if (elegido != null && filas.TryGetValue(elegido.Carpeta, out var sel) && sel.ListView == lista) { sel.Selected = true; sel.EnsureVisible(); }
        ActualizarVacio();
    }

    void Ordenar()
    {
        if (lista.ColumnaOrden < 0) return;
        int col = lista.ColumnaOrden, signo = lista.Ascendente ? 1 : -1;
        lista.ListViewItemSorter = Comparer<ListViewItem>.Create((a, b) =>
        {
            int r = col == 0 ? ((Juego)a.Tag).Estado.CompareTo(((Juego)b.Tag).Estado) : 0;
            if (r == 0) r = string.Compare(a.SubItems[col].Text, b.SubItems[col].Text, StringComparison.CurrentCultureIgnoreCase);
            return r * signo;
        }) as System.Collections.IComparer;
        lista.Sort();
    }

    void ActualizarConteos()
    {
        var juegos = filas.Values.Select(i => (Juego)i.Tag).ToList();
        foreach (var t in tarjetas) t.Valor = t.Estados == null ? juegos.Count : juegos.Count(j => t.Estados.Contains(j.Estado));
    }

    void ActualizarVacio()
    {
        bool vacio = lista.Items.Count == 0;
        lblVacio.Text = filas.Count > 0 ? L.T("ui.noMatch") : ocupado ? L.T("st.scanning") : L.T("sum.noGames");
        lblVacio.Visible = vacio;
        if (vacio) lblVacio.BringToFront();
    }

    // ------------------------------------------------------------------ helpers de UI
    void GuardarConfig(string mensaje = null)
    {
        try { cfg.Guardar(); if (mensaje != null) Log(Nivel.Exito, mensaje); }
        catch (Exception e) { Log(Nivel.Error, L.T("log.configSaveFail", Util.Explicar(e))); }
    }

    void MostrarJuego(Juego j) => Invocar(() =>
    {
        if (!filas.TryGetValue(j.Carpeta, out var it))
        {
            it = new ListViewItem(new string[5]);
            filas[j.Carpeta] = it;
        }
        it.Tag = j;
        it.SubItems[0].Text = TextoEstado(j.Estado);
        it.SubItems[1].Text = j.Nombre;
        it.SubItems[2].Text = j.EmuladoresTexto;
        it.SubItems[3].Text = j.AppId ?? "—";
        it.SubItems[4].Text = string.Join(" ", j.Mensajes);
        bool pasa = PasaFiltro(j);
        if (pasa && it.ListView == null) lista.Items.Add(it);
        else if (!pasa && it.ListView != null) lista.Items.Remove(it);
        else if (it.ListView != null) lista.Invalidate(it.Bounds);
        ActualizarConteos();
        ActualizarVacio();
    });

    Juego Seleccionado() => lista.SelectedItems.Count > 0 ? lista.SelectedItems[0].Tag as Juego : null;

    void Log(Nivel nivel, string mensaje)
    {
        mensaje = Privacidad.Limpiar(mensaje, cfg?.SteamApiKey);
        string linea = $"[{DateTime.Now:HH:mm:ss}] {Prefijo(nivel)}{mensaje}";
        lock (bloqueoLog)
        {
            try
            {
                // Rotación: el registro nunca crece sin límite (se conserva una copia anterior).
                var fi = new FileInfo(rutaLog);
                if (fi.Exists && fi.Length > Seguridad.MaxRegistroBytes) File.Move(rutaLog, rutaLog + ".anterior", true);
                File.AppendAllText(rutaLog, $"{DateTime.Now:yyyy-MM-dd} {linea}{Environment.NewLine}");
            }
            catch { }
        }
        Invocar(() =>
        {
            // la hora en gris y el mensaje en el color de su nivel
            log.SelectionStart = log.TextLength;
            log.SelectionColor = Color.FromArgb(95, 100, 120);
            log.AppendText($"{DateTime.Now:HH:mm:ss}  ");
            log.SelectionStart = log.TextLength;
            log.SelectionColor = nivel switch
            {
                Nivel.Exito => Color.FromArgb(110, 225, 140),
                Nivel.Error => Color.FromArgb(255, 115, 115),
                Nivel.Aviso => Color.FromArgb(255, 196, 90),
                Nivel.Detalle => Color.FromArgb(135, 140, 158),
                _ => Tema.ConsolaTexto,
            };
            log.AppendText(Prefijo(nivel) + mensaje + Environment.NewLine);
            log.ScrollToCaret();
        });
    }

    static string Prefijo(Nivel n) => n switch
    {
        Nivel.Exito => "✔ ",
        Nivel.Error => "✖ ",
        Nivel.Aviso => "⚠ ",
        Nivel.Detalle => "   · ",
        _ => "",
    };

    static Nivel NivelDe(Estado e) => e switch
    {
        Estado.Ok or Estado.Listo or Estado.SteamOriginal => Nivel.Exito,
        Estado.Reparable or Estado.SinAppId => Nivel.Aviso,
        Estado.NoSoportado or Estado.Error => Nivel.Error,
        _ => Nivel.Info,
    };

    static string TextoEstado(Estado e) => L.T(e switch
    {
        Estado.Ok => "st.ok",
        Estado.SteamOriginal => "st.steam",
        Estado.Listo => "st.ready",
        Estado.Reparable => "st.fixable",
        Estado.SinAppId => "st.noAppId",
        Estado.NoSoportado => "st.unsupported",
        Estado.SinSteam => "st.noSteam",
        _ => "st.error",
    });

    void Estado_(string t) => Invocar(() => lblEstado.Text = t);

    void Notificar(string texto, bool ok) => Invocar(() =>
    {
        if (!bandeja.Visible) return;
        bandeja.ShowBalloonTip(5000, Tema.Nombre, texto, ok ? ToolTipIcon.Info : ToolTipIcon.Warning);
    });

    void Invocar(Action a)
    {
        if (IsDisposed || !IsHandleCreated) return;
        if (InvokeRequired) BeginInvoke(a); else a();
    }

    void Restaurar()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
        bandeja.Visible = false;
    }

    // Nunca se "ejecuta" una ruta: las carpetas se abren con el Explorador y las webs solo si están
    // en la lista de confianza. Así un nombre de carpeta o archivo engañoso no puede lanzar un programa.
    void Abrir(string destino)
    {
        try
        {
            if (Uri.TryCreate(destino, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp))
            {
                if (!Seguridad.EsUrlDeConfianza(destino)) { Log(Nivel.Error, L.T("sec.urlBlocked")); return; }
                Process.Start(new ProcessStartInfo(destino) { UseShellExecute = true });
                return;
            }
            if (!Directory.Exists(destino)) { Log(Nivel.Error, L.T("err.dirGone")); return; }
            var psi = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe")) { UseShellExecute = false };
            psi.ArgumentList.Add(Seguridad.Norm(destino));
            Process.Start(psi);
        }
        catch (Exception e) { Log(Nivel.Error, L.T("log.openFail", destino, e.Message)); }
    }

    string Pedir(string pregunta, string valor)
    {
        using var f = new Form { Text = Tema.Nombre, Icon = Recursos.Icono, FormBorderStyle = FormBorderStyle.FixedDialog, StartPosition = FormStartPosition.CenterParent, MinimizeBox = false, MaximizeBox = false, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(S(18)), BackColor = Tema.Tarjeta, Font = Font };
        var p = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true };
        var txt = new TextBox { Text = valor, Width = S(380), Font = new Font(Tema.FuenteMono, 11f), Margin = new Padding(0, S(10), 0, S(14)) };
        var ok = new Boton { Text = L.T("ui.ok"), Estilo = Boton.Tipo.Primario, DialogResult = DialogResult.OK };
        var no = new Boton { Text = L.T("ui.cancel"), DialogResult = DialogResult.Cancel };
        var bots = new FlowLayoutPanel { AutoSize = true, Anchor = AnchorStyles.Right };
        bots.Controls.AddRange(new Control[] { ok, no });
        p.Controls.AddRange(new Control[] { new Label { Text = pregunta, AutoSize = true, MaximumSize = new Size(S(380), 0), ForeColor = Tema.Texto }, txt, bots });
        f.Controls.Add(p);
        f.AcceptButton = ok; f.CancelButton = no;
        return f.ShowDialog(this) == DialogResult.OK ? txt.Text : null;
    }
}
