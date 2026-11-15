// Aspecto visual de Laurel: colores, iconos de texto y controles dibujados a mano.
// Solo dibuja; no contiene lógica de la app.
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace HydraLogros;

static class Tema
{
    public const string Nombre = "Laurel";

    public static readonly Color Fondo = Color.FromArgb(243, 244, 248);
    public static readonly Color Tarjeta = Color.White;
    public static readonly Color Borde = Color.FromArgb(225, 228, 237);
    public static readonly Color Texto = Color.FromArgb(28, 30, 42);
    public static readonly Color Suave = Color.FromArgb(108, 113, 132);
    public static readonly Color Indigo = Color.FromArgb(58, 36, 140);
    public static readonly Color Turquesa = Color.FromArgb(10, 150, 160);
    public static readonly Color Acento = Color.FromArgb(84, 60, 200);
    public static readonly Color Verde = Color.FromArgb(22, 150, 90);
    public static readonly Color Oro = Color.FromArgb(244, 190, 60);
    public static readonly Color Consola = Color.FromArgb(21, 22, 30);
    public static readonly Color ConsolaTexto = Color.FromArgb(220, 222, 232);

    public static Color DeEstado(Estado e) => e switch
    {
        Estado.Ok or Estado.Listo => Color.FromArgb(22, 163, 74),
        Estado.SteamOriginal => Color.FromArgb(37, 99, 235),
        Estado.Reparable => Color.FromArgb(234, 128, 12),
        Estado.SinAppId => Color.FromArgb(190, 140, 0),
        Estado.NoSoportado or Estado.Error => Color.FromArgb(220, 38, 38),
        _ => Color.FromArgb(100, 116, 139),
    };

    public static Color Mezcla(Color a, Color b, float t) => Color.FromArgb(
        (int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));
    public static Color Tenue(Color c, float t = 0.88f) => Mezcla(c, Color.White, t);
    public static Color Oscuro(Color c, float t = 0.25f) => Mezcla(c, Color.Black, t);

    public static readonly string FuenteIconos = Elegir("Segoe Fluent Icons", "Segoe MDL2 Assets");
    public static readonly string FuenteMono = Elegir("Cascadia Mono", "Consolas");

    static string Elegir(params string[] nombres)
    {
        try
        {
            using var col = new InstalledFontCollection();
            foreach (var n in nombres)
                if (col.Families.Any(f => f.Name.Equals(n, StringComparison.OrdinalIgnoreCase))) return n;
        }
        catch { }
        return nombres[^1];
    }

    public static GraphicsPath Redondeado(RectangleF r, float radio)
    {
        var p = new GraphicsPath();
        float d = Math.Max(1, Math.Min(radio * 2, Math.Min(r.Width, r.Height)));
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    // Glifo de la fuente de iconos de Windows convertido en imagen (para menús).
    public static Bitmap Imagen(string glifo, Color color, int tam)
    {
        var bmp = new Bitmap(tam, tam);
        using var g = Graphics.FromImage(bmp);
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        using var f = new Font(FuenteIconos, tam * 0.62f, GraphicsUnit.Pixel);
        TextRenderer.DrawText(g, glifo, f, new Rectangle(0, 0, tam, tam), color, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        return bmp;
    }

    // Corona de laurel: dos ramas de hojas que suben desde abajo y dejan un hueco arriba.
    public static void Corona(Graphics g, PointF c, float radio, float hoja, Color color, int hojasPorRama = 7)
    {
        using var b = new SolidBrush(color);
        using var tallo = new Pen(color, Math.Max(1, hoja * 0.16f));
        foreach (int lado in new[] { -1, 1 })
        {
            // ángulos GDI: 90° = abajo, 270° = arriba. Rama izquierda 108°→248°, derecha 72°→-68°.
            float ini = lado < 0 ? 108 : 72, fin = lado < 0 ? 248 : -68;
            var arco = new GraphicsPath();
            arco.AddArc(c.X - radio, c.Y - radio, radio * 2, radio * 2, Math.Min(ini, fin), Math.Abs(fin - ini));
            g.DrawPath(tallo, arco);
            arco.Dispose();
            for (int i = 0; i < hojasPorRama; i++)
            {
                float t = (i + 0.5f) / hojasPorRama;
                float a = ini + (fin - ini) * t;
                double rad = a * Math.PI / 180;
                // dirección de avance (de abajo hacia arriba)
                double dx = -Math.Sin(rad) * Math.Sign(fin - ini), dy = Math.Cos(rad) * Math.Sign(fin - ini);
                float baseAng = (float)(Math.Atan2(dy, dx) * 180 / Math.PI);
                float tam = hoja * (1.05f - 0.35f * t);
                foreach (int cara in new[] { -1, 1 })
                {
                    var st = g.Save();
                    g.TranslateTransform(c.X + (float)Math.Cos(rad) * radio, c.Y + (float)Math.Sin(rad) * radio);
                    g.RotateTransform(baseAng + cara * 38);
                    using var p = Hoja(tam);
                    g.FillPath(b, p);
                    g.Restore(st);
                }
            }
        }
    }

    static GraphicsPath Hoja(float l)
    {
        var p = new GraphicsPath();
        float w = l * 0.42f;
        p.AddBezier(0, 0, l * 0.3f, -w, l * 0.75f, -w * 0.8f, l, 0);
        p.AddBezier(l, 0, l * 0.75f, w * 0.8f, l * 0.3f, w, 0, 0);
        p.CloseFigure();
        return p;
    }
}

static class Glifo
{
    public const string Revisar = "", Reparar = "", Deshacer = "", Carpeta = "",
        Idioma = "", Buscar = "", Anadir = "", Quitar = "", Detectar = "",
        Copiar = "", Limpiar = "", Llave = "", Ajustes = "", Biblioteca = "",
        Actividad = "", Juegos = "", Ver = "", Info = "", Enlace = "",
        Guia = "", Abajo = "", Arriba = "", Salir = "", Abrir = "";
}

// Botón plano con icono, esquinas redondeadas y estados de ratón.
class Boton : Control, IButtonControl
{
    public enum Tipo { Primario, Secundario, Fantasma, Cabecera }
    public Tipo Estilo { get; set; } = Tipo.Secundario;
    public string Icono { get; set; }
    public Color Color { get; set; } = Tema.Acento;
    public Color? ColorTexto { get; set; }
    public Color? Punto { get; set; }
    public DialogResult DialogResult { get; set; }
    bool encima, pulsado, porDefecto;

    public Boton()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor | ControlStyles.Selectable | ControlStyles.StandardClick, true);
        BackColor = Color.Transparent;
        Cursor = Cursors.Hand;
        Font = new Font("Segoe UI Semibold", 9.5f);
        AutoSize = true;
        TabStop = true;
        AccessibleRole = AccessibleRole.PushButton;
        Margin = new Padding(0, 0, 8, 0);
    }

    int S(int v) => LogicalToDeviceUnits(v);

    public override Size GetPreferredSize(Size proposed)
    {
        int alto = S(Estilo == Tipo.Fantasma ? 30 : 36);
        if (string.IsNullOrEmpty(Text)) return new Size(alto, alto);
        int texto = TextRenderer.MeasureText(Text, Font, Size.Empty, TextFormatFlags.NoPadding).Width;
        int extra = Icono != null || Punto != null ? S(26) : 0;
        return new Size(texto + extra + S(Estilo == Tipo.Fantasma ? 18 : 28), alto);
    }

    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        AccessibleName = Text;
        Parent?.PerformLayout(this, nameof(Text));
        Invalidate();
    }

    protected override void OnMouseEnter(EventArgs e) { encima = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { encima = false; pulsado = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) { pulsado = true; Invalidate(); } base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { pulsado = false; Invalidate(); base.OnMouseUp(e); }
    protected override void OnEnabledChanged(EventArgs e) { Cursor = Enabled ? Cursors.Hand : Cursors.Default; Invalidate(); base.OnEnabledChanged(e); }
    protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
    protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
    protected override void OnKeyUp(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter) { PerformClick(); e.Handled = true; }
        base.OnKeyUp(e);
    }

    protected override void OnClick(EventArgs e)
    {
        if (DialogResult != DialogResult.None && FindForm() is Form f) f.DialogResult = DialogResult;
        base.OnClick(e);
    }

    public void NotifyDefault(bool value) { porDefecto = value; Invalidate(); }
    public void PerformClick() { if (Enabled && Visible) OnClick(EventArgs.Empty); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var r = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
        using var forma = Tema.Redondeado(r, S(Estilo == Tipo.Cabecera ? 18 : 7));

        Color fondo = Color.Transparent, borde = Color.Transparent, texto = Tema.Texto, icono = Color;
        switch (Estilo)
        {
            case Tipo.Primario:
                fondo = !Enabled ? Tema.Mezcla(Color, Color.White, 0.55f) : pulsado ? Tema.Oscuro(Color, 0.2f) : encima ? Tema.Mezcla(Color, Color.White, 0.12f) : Color;
                texto = icono = Color.White;
                break;
            case Tipo.Secundario:
                fondo = pulsado ? Tema.Tenue(Color, 0.8f) : encima ? Tema.Tenue(Color, 0.92f) : Color.White;
                borde = encima ? Tema.Tenue(Color, 0.55f) : Tema.Borde;
                if (!Enabled) { texto = icono = Tema.Mezcla(Tema.Suave, Color.White, 0.4f); }
                break;
            case Tipo.Fantasma:
                fondo = pulsado ? Color.FromArgb(50, 128, 128, 140) : encima ? Color.FromArgb(28, 128, 128, 140) : Color.Transparent;
                texto = ColorTexto ?? Color; icono = ColorTexto ?? Color;
                break;
            case Tipo.Cabecera:
                fondo = Color.FromArgb(pulsado ? 70 : encima ? 52 : 34, 255, 255, 255);
                borde = Color.FromArgb(70, 255, 255, 255);
                texto = icono = Enabled ? Color.White : Color.FromArgb(150, 255, 255, 255);
                break;
        }
        if (ColorTexto is Color ct && Estilo != Tipo.Primario) texto = ct;

        if (fondo.A > 0) { using var b = new SolidBrush(fondo); g.FillPath(b, forma); }
        if (borde.A > 0) { using var p = new Pen(borde); g.DrawPath(p, forma); }
        if (Focused && ShowFocusCues)
        {
            using var p = new Pen(Tema.Mezcla(Color, Color.White, 0.3f), S(2));
            using var foco = Tema.Redondeado(new RectangleF(1.5f, 1.5f, Width - 3.5f, Height - 3.5f), S(6));
            g.DrawPath(p, foco);
        }

        int x = S(Estilo == Tipo.Fantasma ? 9 : 14);
        bool soloIcono = string.IsNullOrEmpty(Text);
        if (Punto is Color punto)
        {
            int d = S(9);
            using var b = new SolidBrush(punto);
            g.FillEllipse(b, x + S(2), (Height - d) / 2f, d, d);
            using var halo = new Pen(Color.FromArgb(90, punto), S(3));
            g.DrawEllipse(halo, x + S(2) - 1.5f, (Height - d) / 2f - 1.5f, d + 3, d + 3);
            x += S(22);
        }
        else if (Icono != null)
        {
            using var fi = new Font(Tema.FuenteIconos, 10.5f);
            var ri = soloIcono ? ClientRectangle : new Rectangle(x, 0, S(18), Height);
            TextRenderer.DrawText(g, Icono, fi, ri, icono, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            x += S(26);
        }
        if (!soloIcono)
            TextRenderer.DrawText(g, Text, Font, new Rectangle(x, 0, Width - x, Height), texto,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
    }
}

// Panel blanco con esquinas redondeadas. El contenido va dentro del margen (Padding).
class Tarjeta : Panel
{
    public Color Relleno { get; set; } = Tema.Tarjeta;

    public Tarjeta()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        BackColor = Tema.Fondo;
        Dock = DockStyle.Fill;
        Margin = new Padding(0, 0, 0, LogicalToDeviceUnits(12));
        Padding = new Padding(LogicalToDeviceUnits(14), LogicalToDeviceUnits(10), LogicalToDeviceUnits(14), LogicalToDeviceUnits(14));
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var forma = Tema.Redondeado(new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), LogicalToDeviceUnits(10));
        using (var sombra = Tema.Redondeado(new RectangleF(0.5f, 1.5f, Width - 1.5f, Height - 1.5f), LogicalToDeviceUnits(10)))
        using (var ps = new Pen(Color.FromArgb(18, 20, 20, 60)))
            e.Graphics.DrawPath(ps, sombra);
        using var b = new SolidBrush(Relleno);
        e.Graphics.FillPath(b, forma);
        using var p = new Pen(Relleno == Tema.Tarjeta ? Tema.Borde : Tema.Mezcla(Relleno, Color.White, 0.08f));
        e.Graphics.DrawPath(p, forma);
    }

    // Tarjeta con fila de título (icono + título + pista) y controles extra a la derecha.
    public static (Tarjeta tarjeta, TableLayoutPanel cuerpo) Crear(string titulo, string icono, string pista, Color? relleno = null, params Control[] extras)
    {
        var t = new Tarjeta();
        if (relleno is Color r) t.Relleno = r;
        bool oscuro = t.Relleno.GetBrightness() < 0.4f;
        var cuerpo = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = t.Relleno, Margin = Padding.Empty };
        cuerpo.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        cuerpo.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var cab = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 4, RowCount = 1, BackColor = t.Relleno, Margin = new Padding(0, 0, 0, t.LogicalToDeviceUnits(8)) };
        cab.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        cab.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        cab.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        cab.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        var lblIcono = new Label { Text = icono, AutoSize = true, Font = new Font(Tema.FuenteIconos, 12f), ForeColor = oscuro ? Tema.Oro : Tema.Acento, Anchor = AnchorStyles.Left, Margin = new Padding(0, 0, t.LogicalToDeviceUnits(4), 0) };
        var lblTitulo = new Label { Text = titulo, AutoSize = true, Font = new Font("Segoe UI Semibold", 11f), ForeColor = oscuro ? Tema.ConsolaTexto : Tema.Texto, Anchor = AnchorStyles.Left, Margin = Padding.Empty };
        var lblPista = new Label { Text = pista ?? "", AutoSize = true, ForeColor = oscuro ? Color.FromArgb(140, 144, 160) : Tema.Suave, Anchor = AnchorStyles.Left, Margin = new Padding(t.LogicalToDeviceUnits(6), t.LogicalToDeviceUnits(2), 0, 0), AutoEllipsis = true };
        cab.Controls.Add(lblIcono, 0, 0);
        cab.Controls.Add(lblTitulo, 1, 0);
        cab.Controls.Add(lblPista, 2, 0);
        lblPista.Dock = DockStyle.Fill; lblPista.AutoSize = false; lblPista.TextAlign = ContentAlignment.MiddleLeft;
        var der = new FlowLayoutPanel { AutoSize = true, WrapContents = false, BackColor = t.Relleno, Margin = Padding.Empty, Anchor = AnchorStyles.Right };
        foreach (var x in extras) { x.Margin = new Padding(t.LogicalToDeviceUnits(6), 0, 0, 0); der.Controls.Add(x); }
        cab.Controls.Add(der, 3, 0);

        cuerpo.Controls.Add(cab, 0, 0);
        t.Controls.Add(cuerpo);
        return (t, cuerpo);
    }
}

// Cabecera con degradado índigo→turquesa, logo, nombre y lema. Los controles hijos se alinean a la derecha.
class Cabecera : Control
{
    public string Lema { get => lema; set { lema = value; Invalidate(); } }
    public string Sub { get => sub; set { sub = value; Invalidate(); } }
    string lema, sub;
    readonly Bitmap logo = new Icon(Recursos.Icono, 256, 256).ToBitmap();

    public Cabecera()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        Dock = DockStyle.Top;
        Height = LogicalToDeviceUnits(84);
    }

    int S(int v) => LogicalToDeviceUnits(v);

    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        int x = Width - S(20);
        foreach (Control c in Controls.Cast<Control>().Reverse())
        {
            if (!c.Visible) continue;
            var t = c.GetPreferredSize(Size.Empty);
            x -= t.Width;
            c.Bounds = new Rectangle(x, (Height - t.Height) / 2, t.Width, t.Height);
            x -= S(10);
        }
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        var g = e.Graphics;
        using (var b = new LinearGradientBrush(new Rectangle(0, 0, Math.Max(1, Width), Math.Max(1, Height)), Tema.Indigo, Tema.Turquesa, LinearGradientMode.Horizontal))
        {
            var mezcla = new ColorBlend { Colors = new[] { Tema.Indigo, Color.FromArgb(70, 50, 170), Tema.Turquesa }, Positions = new[] { 0f, 0.55f, 1f } };
            b.InterpolationColors = mezcla;
            g.FillRectangle(b, ClientRectangle);
        }
        // marca de agua: corona de laurel tenue a la derecha
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var st = g.Save();
        g.SetClip(ClientRectangle);
        Tema.Corona(g, new PointF(Width * 0.62f, Height * 0.62f), S(70), S(22), Color.FromArgb(20, 255, 255, 255), 8);
        g.Restore(st);
        using var linea = new Pen(Color.FromArgb(60, 255, 255, 255));
        g.DrawLine(linea, 0, Height - 1, Width, Height - 1);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        int lado = S(54);
        var rl = new Rectangle(S(20), (Height - lado) / 2, lado, lado);
        using (var sombra = new SolidBrush(Color.FromArgb(40, 0, 0, 30)))
        using (var ps = Tema.Redondeado(new RectangleF(rl.X + 1, rl.Y + S(3), lado - 2, lado - 2), S(12)))
            g.FillPath(sombra, ps);
        g.DrawImage(logo, rl);

        int x = rl.Right + S(14);
        using var fTitulo = new Font("Segoe UI Semibold", 19f);
        using var fLema = new Font("Segoe UI", 10f);
        var tTitulo = TextRenderer.MeasureText(Tema.Nombre, fTitulo, Size.Empty, TextFormatFlags.NoPadding);
        int bloque = tTitulo.Height + S(2) + fLema.Height;
        int y = (Height - bloque) / 2 - S(1);
        TextRenderer.DrawText(g, Tema.Nombre, fTitulo, new Point(x, y), Color.White, TextFormatFlags.NoPadding);
        if (!string.IsNullOrEmpty(sub))
        {
            using var fSub = new Font("Segoe UI Semibold", 8.5f);
            var ts = TextRenderer.MeasureText(sub, fSub, Size.Empty, TextFormatFlags.NoPadding);
            var rp = new RectangleF(x + tTitulo.Width + S(10), y + (tTitulo.Height - ts.Height - S(6)) / 2f + S(2), ts.Width + S(14), ts.Height + S(6));
            using var pp = Tema.Redondeado(rp, rp.Height / 2);
            using var bp = new SolidBrush(Color.FromArgb(40, 255, 255, 255));
            g.FillPath(bp, pp);
            TextRenderer.DrawText(g, sub, fSub, Rectangle.Round(rp), Color.FromArgb(235, 255, 255, 255), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
        int limite = Controls.Cast<Control>().Where(c => c.Visible).Select(c => c.Left).DefaultIfEmpty(Width).Min() - S(12);
        if (!string.IsNullOrEmpty(lema))
            TextRenderer.DrawText(g, lema, fLema, new Rectangle(x, y + tTitulo.Height + S(2), Math.Max(0, limite - x), fLema.Height + S(4)),
                Color.FromArgb(225, 236, 240, 255), TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
    }

    protected override void Dispose(bool disposing) { if (disposing) logo.Dispose(); base.Dispose(disposing); }
}

// Tarjeta de contador por estado. Al hacer clic filtra la lista.
class TarjetaEstado : Control
{
    public Color Color { get; set; }
    public string Etiqueta { get => etiqueta; set { etiqueta = value; AccessibleName = value; Invalidate(); } }
    public int Valor { get => valor; set { if (valor != value) { valor = value; Invalidate(); } } }
    public bool Activa { get => activa; set { activa = value; Invalidate(); } }
    public HashSet<Estado> Estados { get; init; }
    string etiqueta; int valor; bool activa, encima;

    public TarjetaEstado()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        BackColor = Tema.Fondo;
        Cursor = Cursors.Hand;
        Dock = DockStyle.Fill;
        AccessibleRole = AccessibleRole.PushButton;
    }

    int S(int v) => LogicalToDeviceUnits(v);
    protected override void OnMouseEnter(EventArgs e) { encima = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { encima = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var r = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
        using var forma = Tema.Redondeado(r, S(10));
        using (var b = new SolidBrush(activa ? Tema.Tenue(Color, 0.86f) : encima ? Tema.Tenue(Color, 0.95f) : Tema.Tarjeta)) g.FillPath(b, forma);
        using (var p = new Pen(activa ? Color : encima ? Tema.Tenue(Color, 0.5f) : Tema.Borde, activa ? S(2) : 1)) g.DrawPath(p, forma);

        // barra de color a la izquierda
        var st = g.Save();
        g.SetClip(forma);
        using (var b = new SolidBrush(Color)) g.FillRectangle(b, 0, 0, S(5), Height);
        g.Restore(st);

        int x = S(16);
        using var fNum = new Font("Segoe UI Semibold", 17f);
        using var fEt = new Font("Segoe UI Semibold", 7.75f);
        var tn = TextRenderer.MeasureText(valor.ToString(), fNum, Size.Empty, TextFormatFlags.NoPadding);
        int bloque = tn.Height + S(2) + fEt.Height;
        int y = (Height - bloque) / 2;
        TextRenderer.DrawText(g, valor.ToString(), fNum, new Point(x, y), valor == 0 ? Tema.Mezcla(Tema.Suave, Color.White, 0.3f) : Tema.Texto, TextFormatFlags.NoPadding);
        TextRenderer.DrawText(g, (etiqueta ?? "").ToUpper(), fEt, new Rectangle(x, y + tn.Height + S(2), Width - x - S(6), fEt.Height + S(2)),
            activa ? Tema.Oscuro(Color, 0.2f) : Tema.Suave, TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
    }
}

// Línea fina animada que indica que la app está trabajando.
class BarraActividad : Control
{
    readonly System.Windows.Forms.Timer reloj = new() { Interval = 16 };
    float pos;
    bool activa;

    public bool Activa
    {
        get => activa;
        set { activa = value; if (value) reloj.Start(); else { reloj.Stop(); pos = 0; } Invalidate(); }
    }

    public BarraActividad()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        Dock = DockStyle.Top;
        Height = LogicalToDeviceUnits(3);
        BackColor = Tema.Fondo;
        reloj.Tick += (_, _) => { pos = (pos + 0.012f) % 1.4f; Invalidate(); };
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);
        if (!activa) return;
        float ancho = Width * 0.3f, x = (pos - 0.2f) * Width;
        var r = new RectangleF(x, 0, ancho, Height);
        using var b = new LinearGradientBrush(new RectangleF(x - 1, 0, ancho + 2, Height), Color.FromArgb(0, Tema.Acento), Tema.Turquesa, LinearGradientMode.Horizontal);
        b.SetBlendTriangularShape(0.5f);
        e.Graphics.FillRectangle(b, r);
    }

    protected override void Dispose(bool disposing) { if (disposing) reloj.Dispose(); base.Dispose(disposing); }
}

// ListView de juegos dibujada a mano: insignias de estado, filas alternas y cabecera plana.
class ListaJuegos : ListView
{
    public int ColumnaOrden { get; set; } = -1;
    public bool Ascendente { get; set; } = true;
    public Func<Estado, string> TextoEstado { get; set; } = e => e.ToString();

    public ListaJuegos()
    {
        DoubleBuffered = true;
        OwnerDraw = true;
        View = View.Details;
        FullRowSelect = true;
        HideSelection = false;
        MultiSelect = false;
        BorderStyle = BorderStyle.None;
        BackColor = Tema.Tarjeta;
        SmallImageList = new ImageList { ImageSize = new Size(1, LogicalToDeviceUnits(34)) };
        ItemSelectionChanged += (_, e) => { if (e.Item?.ListView == this) Invalidate(e.Item.Bounds); };
        Resize += (_, _) => AjustarUltima();
        ColumnWidthChanged += (_, e) => { if (e.ColumnIndex != Columns.Count - 1) AjustarUltima(); };
    }

    int S(int v) => LogicalToDeviceUnits(v);
    bool ajustando;

    void AjustarUltima()
    {
        if (ajustando || Columns.Count == 0) return;
        ajustando = true;
        int resto = ClientSize.Width - Columns.Cast<ColumnHeader>().Take(Columns.Count - 1).Sum(c => c.Width);
        // se reserva siempre el hueco de la barra vertical para que nunca aparezca la horizontal
        Columns[^1].Width = Math.Max(S(80), resto - SystemInformation.VerticalScrollBarWidth - 2);
        ajustando = false;
    }

    protected override void OnDrawColumnHeader(DrawListViewColumnHeaderEventArgs e)
    {
        var g = e.Graphics;
        using (var b = new SolidBrush(Color.FromArgb(248, 249, 252))) g.FillRectangle(b, e.Bounds);
        using (var p = new Pen(Tema.Borde)) g.DrawLine(p, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
        using var f = new Font("Segoe UI Semibold", 8.25f);
        var r = new Rectangle(e.Bounds.X + S(10), e.Bounds.Y, e.Bounds.Width - S(28), e.Bounds.Height);
        TextRenderer.DrawText(g, e.Header.Text.ToUpper(), f, r, Tema.Suave, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        if (e.ColumnIndex == ColumnaOrden)
        {
            using var fi = new Font(Tema.FuenteIconos, 7.5f);
            TextRenderer.DrawText(g, Ascendente ? Glifo.Arriba : Glifo.Abajo, fi, new Rectangle(e.Bounds.Right - S(22), e.Bounds.Y, S(16), e.Bounds.Height), Tema.Acento, TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
        }
    }

    protected override void OnDrawItem(DrawListViewItemEventArgs e) { }

    protected override void OnDrawSubItem(DrawListViewSubItemEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        bool sel = e.Item.Selected;
        var fondo = sel ? Tema.Tenue(Tema.Acento, 0.88f) : e.ItemIndex % 2 == 1 ? Color.FromArgb(250, 251, 253) : Tema.Tarjeta;
        using (var b = new SolidBrush(fondo)) g.FillRectangle(b, e.Bounds);
        using (var p = new Pen(Color.FromArgb(240, 241, 246))) g.DrawLine(p, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
        var j = e.Item.Tag as Juego;

        if (e.ColumnIndex == 0)
        {
            if (sel) using (var b = new SolidBrush(Tema.Acento)) g.FillRectangle(b, e.Bounds.X, e.Bounds.Y, S(3), e.Bounds.Height);
            if (j == null) return;
            var c = Tema.DeEstado(j.Estado);
            using var f = new Font("Segoe UI Semibold", 8f);
            string t = TextoEstado(j.Estado);
            var tt = TextRenderer.MeasureText(t, f, Size.Empty, TextFormatFlags.NoPadding);
            int alto = S(22), ancho = Math.Min(tt.Width + S(26), e.Bounds.Width - S(16));
            var rp = new RectangleF(e.Bounds.X + S(10), e.Bounds.Y + (e.Bounds.Height - alto) / 2f, ancho, alto);
            using var forma = Tema.Redondeado(rp, alto / 2f);
            using (var b = new SolidBrush(Tema.Tenue(c, 0.86f))) g.FillPath(b, forma);
            int d = S(6);
            using (var b = new SolidBrush(c)) g.FillEllipse(b, rp.X + S(8), rp.Y + (alto - d) / 2f, d, d);
            TextRenderer.DrawText(g, t, f, new Rectangle((int)rp.X + S(18), (int)rp.Y, ancho - S(20), alto), Tema.Oscuro(c, 0.25f),
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
            return;
        }

        Font fuente = Font; Color color = Tema.Texto; bool propia = false;
        if (e.ColumnIndex == 1) { fuente = new Font("Segoe UI Semibold", 9.75f); propia = true; }
        else if (e.ColumnIndex == 4) color = Tema.Suave;
        else if (e.ColumnIndex == 3) { fuente = new Font(Tema.FuenteMono, 9f); propia = true; color = e.SubItem.Text == "—" ? Tema.Suave : Tema.Texto; }
        var rt = new Rectangle(e.Bounds.X + S(10), e.Bounds.Y, e.Bounds.Width - S(16), e.Bounds.Height);
        TextRenderer.DrawText(g, e.SubItem.Text, fuente, rt, color, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
        if (propia) fuente.Dispose();
    }
}

// Caja de búsqueda con lupa y borde redondeado.
class CajaBusqueda : Control
{
    public readonly TextBox Caja = new() { BorderStyle = BorderStyle.None, BackColor = Tema.Tarjeta, ForeColor = Tema.Texto };

    public CajaBusqueda()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        BackColor = Tema.Tarjeta;
        Controls.Add(Caja);
        Caja.GotFocus += (_, _) => Invalidate();
        Caja.LostFocus += (_, _) => Invalidate();
        Cursor = Cursors.IBeam;
        Click += (_, _) => Caja.Focus();
    }

    int S(int v) => LogicalToDeviceUnits(v);
    public override Size GetPreferredSize(Size p) => new(S(280), S(32));

    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        int x = S(30);
        Caja.Bounds = new Rectangle(x, (Height - Caja.PreferredHeight) / 2, Math.Max(10, Width - x - S(10)), Caja.PreferredHeight);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var forma = Tema.Redondeado(new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), S(16));
        using (var b = new SolidBrush(Color.FromArgb(247, 248, 251))) g.FillPath(b, forma);
        using (var p = new Pen(Caja.Focused ? Tema.Acento : Tema.Borde, Caja.Focused ? S(2) : 1)) g.DrawPath(p, forma);
        Caja.BackColor = Color.FromArgb(247, 248, 251);
        using var fi = new Font(Tema.FuenteIconos, 9.5f);
        TextRenderer.DrawText(g, Glifo.Buscar, fi, new Rectangle(S(8), 0, S(18), Height), Tema.Suave, TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
    }
}

// Colores del menú contextual.
class ColoresMenu : ProfessionalColorTable
{
    public override Color MenuItemSelected => Tema.Tenue(Tema.Acento, 0.88f);
    public override Color MenuItemBorder => Tema.Tenue(Tema.Acento, 0.6f);
    public override Color MenuBorder => Tema.Borde;
    public override Color ToolStripDropDownBackground => Color.White;
    public override Color ImageMarginGradientBegin => Color.White;
    public override Color ImageMarginGradientMiddle => Color.White;
    public override Color ImageMarginGradientEnd => Color.White;
    public override Color SeparatorDark => Tema.Borde;
}
