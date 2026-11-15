namespace HydraLogros;

// Ventana de selección de idioma (se muestra antes de la ventana principal y desde el botón "Idioma").
// Mientras eliges, la propia ventana se muestra en el idioma marcado.
public class SelectorIdioma : Form
{
    readonly ListBox lista = new() { IntegralHeight = false, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 12.5f), DrawMode = DrawMode.OwnerDrawFixed, BorderStyle = BorderStyle.None };
    readonly Label lblPregunta = new() { AutoSize = true, Font = new Font("Segoe UI Semibold", 12f), ForeColor = Tema.Texto, Margin = new Padding(0, 0, 0, 10) };
    readonly CheckBox chkNoPreguntar = new() { AutoSize = true, Margin = new Padding(0, 12, 0, 0), ForeColor = Tema.Texto };
    readonly Label lblPista = new() { AutoSize = true, ForeColor = Tema.Suave, Margin = new Padding(0, 4, 0, 0) };
    readonly Boton btnContinuar = new() { Estilo = Boton.Tipo.Primario, DialogResult = DialogResult.OK, Icono = "" };
    readonly Cabecera cabecera = new();
    int encima = -1;

    public string Codigo => L.Todos[Math.Max(0, lista.SelectedIndex)].Codigo;
    public bool NoPreguntar => chkNoPreguntar.Checked;

    int S(int px) => LogicalToDeviceUnits(px);

    public SelectorIdioma(string codigoInicial, bool noPreguntar)
    {
        Text = Tema.Nombre;
        Icon = Recursos.Icono;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9.5f);
        ClientSize = new Size(S(460), S(620));
        ShowInTaskbar = true;
        BackColor = Tema.Tarjeta;

        var raiz = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, Padding = new Padding(S(22), S(18), S(22), S(18)), BackColor = Tema.Tarjeta };
        raiz.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        raiz.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        raiz.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        raiz.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        lista.ItemHeight = S(40);
        foreach (var i in L.Todos) lista.Items.Add(i.Nativo);
        lista.DrawItem += DibujarItem;
        lista.SelectedIndexChanged += (_, _) => { Traducir(); lista.Invalidate(); };
        lista.MouseMove += (_, e) => { int i = lista.IndexFromPoint(e.Location); if (i != encima) { encima = i; lista.Invalidate(); } };
        lista.MouseLeave += (_, _) => { encima = -1; lista.Invalidate(); };
        lista.DoubleClick += (_, _) => { if (lista.SelectedIndex >= 0) { DialogResult = DialogResult.OK; Close(); } };
        var marco = new Panel { Dock = DockStyle.Fill, Padding = new Padding(1), BackColor = Tema.Borde, Margin = Padding.Empty };
        marco.Controls.Add(lista);

        chkNoPreguntar.Checked = noPreguntar;
        lblPista.MaximumSize = new Size(S(410), 0);

        var pie = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Margin = new Padding(0, S(14), 0, 0) };
        btnContinuar.Margin = Padding.Empty;
        pie.Controls.Add(btnContinuar);

        raiz.Controls.Add(lblPregunta, 0, 0);
        raiz.Controls.Add(marco, 0, 1);
        raiz.Controls.Add(chkNoPreguntar, 0, 2);
        raiz.Controls.Add(lblPista, 0, 3);
        raiz.Controls.Add(pie, 0, 4);
        Controls.Add(raiz);
        Controls.Add(cabecera);

        AcceptButton = btnContinuar;
        int inicial = L.Indice(codigoInicial);
        lista.SelectedIndex = inicial >= 0 ? inicial : 1;
        Traducir();
    }

    void Traducir()
    {
        int i = Math.Max(0, lista.SelectedIndex);
        lblPregunta.Text = L.En(i, "sel.prompt");
        chkNoPreguntar.Text = L.En(i, "sel.dontAsk");
        lblPista.Text = L.En(i, "sel.hint", L.En(i, "ui.language"));
        btnContinuar.Text = L.En(i, "sel.continue");
        cabecera.Lema = L.En(i, "app.tagline");
        cabecera.Sub = L.En(i, "app.for");
    }

    void DibujarItem(object sender, DrawItemEventArgs e)
    {
        if (e.Index < 0) return;
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        bool sel = (e.State & DrawItemState.Selected) != 0;
        using (var b = new SolidBrush(Tema.Tarjeta)) g.FillRectangle(b, e.Bounds);
        if (sel || e.Index == encima)
        {
            var r = new RectangleF(e.Bounds.X + S(4), e.Bounds.Y + S(2), e.Bounds.Width - S(8), e.Bounds.Height - S(4));
            using var forma = Tema.Redondeado(r, S(8));
            using var b = new SolidBrush(sel ? Tema.Acento : Tema.Tenue(Tema.Acento, 0.92f));
            g.FillPath(b, forma);
        }
        var rt = new Rectangle(e.Bounds.X + S(18), e.Bounds.Y, e.Bounds.Width - S(60), e.Bounds.Height);
        TextRenderer.DrawText(g, lista.Items[e.Index].ToString(), lista.Font, rt,
            sel ? Color.White : Tema.Texto, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        if (sel)
        {
            using var fi = new Font(Tema.FuenteIconos, 11f);
            TextRenderer.DrawText(g, "", fi, new Rectangle(e.Bounds.Right - S(44), e.Bounds.Y, S(30), e.Bounds.Height), Color.White,
                TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
        }
    }
}
