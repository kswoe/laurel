// Genera src/app.ico (diseño original de Laurel): trofeo dorado dentro de una corona de laurel,
// sobre fondo índigo→turquesa. Cada tamaño se dibuja por separado para que sea nítido.
// Uso: dotnet run --project tools/IconGen -- <ruta app.ico> [carpeta de vistas previas]
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

int[] tamanos = { 16, 20, 24, 32, 40, 48, 64, 128, 256 };
string salida = args.Length > 0 ? args[0] : "app.ico";
string vistas = args.Length > 1 ? args[1] : null;

var pngs = new List<(int, byte[])>();
foreach (var t in tamanos)
{
    using var bmp = Dibujar(t);
    using var ms = new MemoryStream();
    bmp.Save(ms, ImageFormat.Png);
    pngs.Add((t, ms.ToArray()));
    if (vistas != null) { Directory.CreateDirectory(vistas); bmp.Save(Path.Combine(vistas, $"icono-{t}.png"), ImageFormat.Png); }
}

using (var fs = File.Create(salida))
using (var w = new BinaryWriter(fs))
{
    w.Write((short)0); w.Write((short)1); w.Write((short)pngs.Count);
    int offset = 6 + 16 * pngs.Count;
    foreach (var (t, data) in pngs)
    {
        w.Write((byte)(t >= 256 ? 0 : t)); w.Write((byte)(t >= 256 ? 0 : t));
        w.Write((byte)0); w.Write((byte)0);
        w.Write((short)1); w.Write((short)32);
        w.Write(data.Length); w.Write(offset);
        offset += data.Length;
    }
    foreach (var (_, data) in pngs) w.Write(data);
}
Console.WriteLine($"Icono generado: {salida}");

static Bitmap Dibujar(int t)
{
    var bmp = new Bitmap(t, t, PixelFormat.Format32bppArgb);
    using var g = Graphics.FromImage(bmp);
    g.SmoothingMode = SmoothingMode.AntiAlias;
    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
    g.Clear(Color.Transparent);
    float s = t / 256f;
    g.ScaleTransform(s, s);
    bool pequeno = t <= 24;
    bool mediano = t <= 48;

    // Fondo: cuadrado redondeado con degradado
    var fondo = new RectangleF(pequeno ? 4 : 10, pequeno ? 4 : 10, pequeno ? 248 : 236, pequeno ? 248 : 236);
    using (var p = Redondeado(fondo, pequeno ? 44 : 54))
    {
        using var b = new LinearGradientBrush(new PointF(0, 0), new PointF(256, 256), Color.FromArgb(58, 36, 140), Color.FromArgb(10, 150, 160));
        g.FillPath(b, p);
        using var brillo = new LinearGradientBrush(new PointF(0, 10), new PointF(0, 130), Color.FromArgb(60, 255, 255, 255), Color.FromArgb(0, 255, 255, 255));
        g.SetClip(p);
        g.FillEllipse(brillo, -40, -120, 336, 250);
        g.ResetClip();
    }

    var oro1 = Color.FromArgb(255, 222, 110);
    var oro2 = Color.FromArgb(232, 150, 20);

    // Corona de laurel (no en los tamaños más pequeños, donde solo se vería ruido)
    if (!pequeno)
    {
        var c = new PointF(128, 134);
        float radio = mediano ? 92 : 96, hoja = mediano ? 40 : 34;
        int hojas = mediano ? 4 : 6;
        Corona(g, c, radio, hoja, Color.FromArgb(70, 0, 0, 30), hojas, 3); // sombra
        Corona(g, c, radio, hoja, oro1, hojas, 0);
        using var lazo = new SolidBrush(oro2);
        g.FillEllipse(lazo, 114, 220, 28, 18);
    }

    // Trofeo (más pequeño cuando va dentro de la corona)
    float k = pequeno ? 1f : 0.6f;
    g.TranslateTransform(128, pequeno ? 133 : 130);
    g.ScaleTransform(k, k);
    g.TranslateTransform(-128, -129);

    using (var asa = new Pen(oro2, pequeno ? 20 : 16) { StartCap = LineCap.Round, EndCap = LineCap.Round })
    {
        g.DrawArc(asa, 46, 66, 50, 56, 100, 160);
        g.DrawArc(asa, 160, 66, 50, 56, 280, 160);
    }
    using (var copa = new GraphicsPath())
    {
        copa.AddLine(68, 52, 188, 52);
        copa.AddBezier(188, 52, 188, 124, 164, 154, 128, 154);
        copa.AddBezier(128, 154, 92, 154, 68, 124, 68, 52);
        copa.CloseFigure();
        using var b = new LinearGradientBrush(new PointF(68, 0), new PointF(188, 0), oro1, oro2);
        g.FillPath(b, copa);
    }
    using (var b = new SolidBrush(oro2))
    {
        g.FillRectangle(b, 114, 150, 28, 32);
        using var baseP = Redondeado(new RectangleF(82, 178, 92, 28), 8);
        using var bb = new LinearGradientBrush(new PointF(82, 0), new PointF(174, 0), oro1, oro2);
        g.FillPath(bb, baseP);
    }
    if (!mediano)
    {
        using var reflejo = new SolidBrush(Color.FromArgb(110, 255, 255, 255));
        g.FillEllipse(reflejo, 84, 62, 16, 52);
        using var estrella = Estrella(128, 96, 24, 10);
        using var be = new SolidBrush(Color.FromArgb(255, 250, 225));
        g.FillPath(be, estrella);
    }
    g.ResetTransform();
    g.ScaleTransform(s, s);

    // Estrella en el hueco superior de la corona
    if (!pequeno)
    {
        using var e = Estrella(128, 40, mediano ? 20 : 16, mediano ? 8 : 6.5f);
        using var b = new SolidBrush(Color.FromArgb(255, 245, 200));
        g.FillPath(b, e);
    }
    return bmp;
}

static void Corona(Graphics g, PointF c, float radio, float hoja, Color color, int hojasPorRama, float desplaza)
{
    using var b = new SolidBrush(color);
    using var tallo = new Pen(color, Math.Max(1, hoja * 0.18f)) { StartCap = LineCap.Round, EndCap = LineCap.Round };
    var st0 = g.Save();
    g.TranslateTransform(0, desplaza);
    foreach (int lado in new[] { -1, 1 })
    {
        float ini = lado < 0 ? 97 : 83, fin = lado < 0 ? 246 : -66;
        using (var arco = new GraphicsPath())
        {
            arco.AddArc(c.X - radio, c.Y - radio, radio * 2, radio * 2, Math.Min(ini, fin), Math.Abs(fin - ini));
            g.DrawPath(tallo, arco);
        }
        for (int i = 0; i <= hojasPorRama; i++)
        {
            bool punta = i == hojasPorRama;
            float t = punta ? 1f : (i + 0.5f) / hojasPorRama;
            float a = ini + (fin - ini) * t;
            double rad = a * Math.PI / 180;
            double dx = -Math.Sin(rad) * Math.Sign(fin - ini), dy = Math.Cos(rad) * Math.Sign(fin - ini);
            float baseAng = (float)(Math.Atan2(dy, dx) * 180 / Math.PI);
            float tam = punta ? hoja * 0.75f : hoja * (1.05f - 0.3f * t);
            foreach (int cara in punta ? new[] { 0 } : new[] { -1, 1 })
            {
                var st = g.Save();
                g.TranslateTransform(c.X + (float)Math.Cos(rad) * radio, c.Y + (float)Math.Sin(rad) * radio);
                g.RotateTransform(baseAng + cara * 36);
                using var p = Hoja(tam);
                g.FillPath(b, p);
                g.Restore(st);
            }
        }
    }
    g.Restore(st0);
}

static GraphicsPath Hoja(float l)
{
    var p = new GraphicsPath();
    float w = l * 0.44f;
    p.AddBezier(0, 0, l * 0.3f, -w, l * 0.75f, -w * 0.8f, l, 0);
    p.AddBezier(l, 0, l * 0.75f, w * 0.8f, l * 0.3f, w, 0, 0);
    p.CloseFigure();
    return p;
}

static GraphicsPath Redondeado(RectangleF r, float radio)
{
    var p = new GraphicsPath();
    float d = radio * 2;
    p.AddArc(r.X, r.Y, d, d, 180, 90);
    p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
    p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
    p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
    p.CloseFigure();
    return p;
}

static GraphicsPath Estrella(float cx, float cy, float rExt, float rInt)
{
    var pts = new PointF[10];
    for (int i = 0; i < 10; i++)
    {
        double ang = -Math.PI / 2 + i * Math.PI / 5;
        float r = i % 2 == 0 ? rExt : rInt;
        pts[i] = new PointF(cx + (float)(Math.Cos(ang) * r), cy + (float)(Math.Sin(ang) * r));
    }
    var p = new GraphicsPath();
    p.AddPolygon(pts);
    return p;
}
