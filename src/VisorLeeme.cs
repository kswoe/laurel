using System.Text.RegularExpressions;

namespace HydraLogros;

// Visor del LEEME dentro de la app.
// Seguridad: solo muestra las copias INCLUIDAS en el propio .exe (recursos incrustados al compilar).
// Nunca lee los .md de la carpeta, así que si alguien los reescribe, la app sigue mostrando el texto original.
// Es de solo lectura, no detecta URL y no abre ningún enlace del documento.
public class VisorLeeme : Form
{
    readonly RichTextBox texto = new()
    {
        ReadOnly = true, Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, BackColor = Tema.Tarjeta,
        DetectUrls = false, ShortcutsEnabled = true, ScrollBars = RichTextBoxScrollBars.Vertical,
    };
    readonly ComboBox idiomas = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    readonly Cabecera cabecera = new();

    int S(int px) => LogicalToDeviceUnits(px);

    public VisorLeeme()
    {
        Text = $"{Tema.Nombre} — {L.T("ui.readme")}";
        Icon = Recursos.Icono;
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 9.5f);
        Size = new Size(S(900), S(820));
        MinimumSize = new Size(S(560), S(420));
        BackColor = Tema.Fondo;
        ShowInTaskbar = false;

        cabecera.Lema = L.T("app.tagline");
        cabecera.Sub = L.T("ui.readme");
        foreach (var i in L.Todos) idiomas.Items.Add(i.Nativo);
        idiomas.Width = S(190);
        idiomas.SelectedIndexChanged += (_, _) => Mostrar(L.Todos[idiomas.SelectedIndex].Codigo);

        var lblIdioma = new Label { Text = L.T("ui.language"), AutoSize = true, ForeColor = Tema.Suave, Margin = new Padding(0, S(5), S(6), 0) };
        var lblNota = new Label { Text = L.T("readme.embedded"), AutoSize = true, ForeColor = Tema.Suave, Margin = new Padding(S(14), S(5), 0, 0) };
        var barra = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = false, BackColor = Tema.Fondo, Padding = new Padding(S(18), S(12), S(18), S(4)) };
        barra.Controls.AddRange(new Control[] { lblIdioma, idiomas, lblNota });

        var tarjeta = new Tarjeta { Padding = new Padding(S(22), S(16), S(10), S(16)), Margin = Padding.Empty };
        tarjeta.Controls.Add(texto);
        var contenedor = new Panel { Dock = DockStyle.Fill, Padding = new Padding(S(16), S(8), S(16), S(16)), BackColor = Tema.Fondo };
        contenedor.Controls.Add(tarjeta);

        Controls.Add(contenedor);
        Controls.Add(barra);
        Controls.Add(cabecera);

        int actual = Math.Max(0, L.Indice(L.Codigo));
        idiomas.SelectedIndex = actual;
        ActiveControl = texto; // el foco en el texto: la rueda y las teclas desplazan el documento, no cambian el idioma
        KeyPreview = true;
        KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) Close(); };
    }

    // ------------------------------------------------------------------ contenido incrustado
    public static string Leer(string codigo)
    {
        var asm = typeof(VisorLeeme).Assembly;
        using var s = asm.GetManifestResourceStream($"leeme.{codigo}.md") ?? asm.GetManifestResourceStream("leeme.en.md");
        if (s == null) return null;
        using var r = new StreamReader(s, System.Text.Encoding.UTF8);
        return r.ReadToEnd();
    }

    void Mostrar(string codigo)
    {
        string md = Leer(codigo);
        // fuente de Windows adecuada a cada escritura (Segoe UI no tiene caracteres chinos ni japoneses)
        familia = codigo switch { "ja" => "Yu Gothic UI", "zh" => "Microsoft YaHei UI", _ => "Segoe UI" };
        foreach (var f in new[] { fNormal, fNegrita, fCursiva, fH1, fH2, fNegCur }) f?.Dispose();
        fNormal = fNegrita = fCursiva = fH1 = fH2 = fNegCur = null;
        texto.SuspendLayout();
        texto.Clear();
        if (md == null) { Escribir(L.T("readme.missing"), Normal, Tema.Suave); texto.ResumeLayout(); return; }
        Pintar(md);
        texto.SelectionStart = 0;
        texto.ScrollToCaret();
        texto.ResumeLayout();
    }

    // ------------------------------------------------------------------ mini intérprete de Markdown
    // Solo lo que usan los LEEME: títulos, párrafos, listas, tablas, negrita, cursiva, código y enlaces (como texto).
    string familia = "Segoe UI";
    Font Normal => fNormal ??= new Font(familia, 10.5f);
    Font fNormal, fNegrita, fCursiva, fCodigo, fH1, fH2, fNegCur;

    void Pintar(string md)
    {
        fNegrita ??= new Font(familia, 10.5f, FontStyle.Bold);
        fCursiva ??= new Font(familia, 10.5f, FontStyle.Italic);
        fNegCur ??= new Font(familia, 11f, FontStyle.Bold | FontStyle.Italic);
        fCodigo ??= new Font(Tema.FuenteMono, 9.75f);
        fH1 ??= new Font(familia, 22f, FontStyle.Bold);
        fH2 ??= new Font(familia, 14f, FontStyle.Bold);

        var lineas = md.Replace("\r", "").Split('\n');
        var tabla = new List<string[]>();
        foreach (var bruta in lineas.Append(""))
        {
            string l = bruta.TrimEnd();
            if (l.StartsWith("|")) { tabla.Add(l.Trim('|').Split('|').Select(c => c.Trim()).ToArray()); continue; }
            if (tabla.Count > 0) { PintarTabla(tabla); tabla.Clear(); }

            // la línea de enlaces a otros idiomas no sirve dentro de la app (el selector la sustituye)
            if (Regex.Matches(l, @"\]\([^)]*\.md\)").Count >= 3) continue;

            if (l.StartsWith("# ")) { Escribir(l[2..], fH1, Tema.Indigo); Salto(); continue; }
            if (l.StartsWith("## ")) { Hueco(); Escribir(l[3..], fH2, Tema.Acento); Salto(); continue; }
            if (Regex.IsMatch(l, @"^\*\*\*.+\*\*\*$")) // banner: *** TEXTO ***
            {
                Hueco();
                texto.SelectionAlignment = HorizontalAlignment.Center;
                Escribir(l.Trim('*', ' '), fNegCur, Tema.Verde);
                Salto();
                texto.SelectionAlignment = HorizontalAlignment.Left;
                continue;
            }
            var m = Regex.Match(l, @"^(\s*)([-*]|\d+\.)\s+(.*)$");
            if (m.Success)
            {
                int nivel = m.Groups[1].Value.Length / 2;
                string marca = m.Groups[2].Value is "-" or "*" ? (nivel == 0 ? "•" : "◦") : m.Groups[2].Value;
                texto.SelectionIndent = S(14 + nivel * 22);
                texto.SelectionHangingIndent = S(18);
                Escribir(marca + "  ", fNegrita, Tema.Acento);
                Linea(m.Groups[3].Value);
                Salto();
                texto.SelectionIndent = 0; texto.SelectionHangingIndent = 0;
                continue;
            }
            if (l.Length == 0) { Hueco(); continue; }
            Linea(l);
            Salto();
        }
    }

    void PintarTabla(List<string[]> filas)
    {
        // cabecera | separador | filas → cada fila como "TÉRMINO — significado"
        foreach (var f in filas.Skip(1).Where(f => !f.All(c => Regex.IsMatch(c, @"^:?-+:?$"))))
        {
            texto.SelectionIndent = S(14);
            texto.SelectionHangingIndent = S(18);
            Escribir("▸  ", fNegrita, Tema.Acento);
            Escribir(f[0].Replace("`", ""), fNegrita, Tema.Texto);
            if (f.Length > 1) { Escribir(" — ", Normal, Tema.Suave); Linea(string.Join(" · ", f.Skip(1))); }
            Salto();
            texto.SelectionIndent = 0; texto.SelectionHangingIndent = 0;
        }
    }

    static readonly Regex Enlinea = new(@"(\*\*(?<b>.+?)\*\*)|(`(?<c>[^`]+)`)|(\[(?<t>[^\]]+)\]\([^)]*\))|(\*(?<i>[^*]+)\*)", RegexOptions.Compiled);

    void Linea(string l)
    {
        int pos = 0;
        foreach (Match m in Enlinea.Matches(l))
        {
            if (m.Index > pos) Escribir(l[pos..m.Index], Normal, Tema.Texto);
            if (m.Groups["b"].Success) Linea2(m.Groups["b"].Value, fNegrita);
            else if (m.Groups["c"].Success) Escribir(m.Groups["c"].Value, fCodigo, Color.FromArgb(150, 40, 110));
            else if (m.Groups["t"].Success) Escribir(m.Groups["t"].Value, fNegrita, Tema.Acento); // el enlace se muestra como texto, nunca se abre
            else if (m.Groups["i"].Success) Escribir(m.Groups["i"].Value, fCursiva, Tema.Suave);
            pos = m.Index + m.Length;
        }
        if (pos < l.Length) Escribir(l[pos..], Normal, Tema.Texto);
    }

    // negrita que puede contener código o cursiva dentro
    void Linea2(string l, Font f)
    {
        int pos = 0;
        foreach (Match m in Regex.Matches(l, @"`([^`]+)`|\*([^*]+)\*"))
        {
            if (m.Index > pos) Escribir(l[pos..m.Index], f, Tema.Texto);
            Escribir(m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value, m.Groups[1].Success ? fCodigo : f, m.Groups[1].Success ? Color.FromArgb(150, 40, 110) : Tema.Texto);
            pos = m.Index + m.Length;
        }
        if (pos < l.Length) Escribir(l[pos..], f, Tema.Texto);
    }

    void Escribir(string t, Font f, Color c)
    {
        texto.SelectionStart = texto.TextLength;
        texto.SelectionLength = 0;
        texto.SelectionFont = f;
        texto.SelectionColor = c;
        texto.AppendText(t);
    }

    void Salto() => Escribir("\n", Normal, Tema.Texto);

    // como mucho una línea en blanco seguida
    void Hueco() { if (texto.TextLength > 0 && !texto.Text.EndsWith("\n\n")) Salto(); }

    protected override void Dispose(bool disposing)
    {
        if (disposing) foreach (var f in new[] { fNormal, fNegrita, fCursiva, fCodigo, fH1, fH2, fNegCur }) f?.Dispose();
        base.Dispose(disposing);
    }
}
