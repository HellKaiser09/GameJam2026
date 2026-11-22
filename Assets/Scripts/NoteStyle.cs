using UnityEngine;

// Forma y color de cada nota. Los opuestos comparten forma:
// Do/Fa circulo, Re/Mi triangulo, Sol/La cuadrado, Si/Do2 rombo.
// Las formas se dibujan por codigo (blancas con borde oscuro), asi no dependen de importar imagenes.
public static class NoteStyle
{
    static readonly Color[] colors =
    {
        new Color(1.00f, 0.30f, 0.37f), // Do  rojo
        new Color(1.00f, 0.62f, 0.11f), // Re  naranja
        new Color(0.30f, 0.53f, 1.00f), // Mi  azul
        new Color(0.25f, 0.88f, 0.94f), // Fa  cyan
        new Color(1.00f, 0.88f, 0.30f), // Sol amarillo
        new Color(0.96f, 0.95f, 1.00f), // La  blanco
        new Color(1.00f, 0.37f, 0.82f), // Si  rosa
        new Color(0.36f, 0.89f, 0.42f), // Do2 verde
    };
    static readonly int[] shapeOf = { 0, 1, 1, 0, 2, 2, 3, 3 };

    static Sprite[] shapes;
    static Sprite halo;

    // La nota que el jugador necesita ahora (la pone FightUI)
    public static bool HighlightOn = true;
    public static bool HasNeeded { get; private set; }
    public static NoteType Needed { get; private set; }
    public static void SetNeeded(bool has, NoteType n) { HasNeeded = has; Needed = n; }
    public static bool IsNeeded(NoteType n) { return HighlightOn && HasNeeded && n == Needed; }

    public static Color GetColor(NoteType n) { return colors[(int)n]; }

    // El proyecto no recarga el codigo al dar Play (Enter Play Mode Options),
    // asi que las variables static sobreviven entre partidas. Aqui se limpian.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        shapes = null;
        halo = null;
        HighlightOn = true;
        HasNeeded = false;
    }

    public static Sprite GetSprite(NoteType n)
    {
        if (shapes == null || shapes[0] == null) // == null tambien detecta sprites destruidos al parar
        {
            shapes = new Sprite[4];
            for (int i = 0; i < 4; i++) shapes[i] = MakeShape(i);
        }
        return shapes[shapeOf[(int)n]];
    }

    // Brillo suave para marcar la nota que necesitas
    public static Sprite Halo
    {
        get
        {
            if (halo == null)
            {
                const int N = 128;
                Texture2D t = NewTex(N);
                for (int y = 0; y < N; y++)
                    for (int x = 0; x < N; x++)
                    {
                        float d = new Vector2((x + 0.5f) / N - 0.5f, (y + 0.5f) / N - 0.5f).magnitude * 2f;
                        float a = Mathf.Clamp01(1f - d);
                        t.SetPixel(x, y, new Color(1, 1, 1, a * a));
                    }
                t.Apply();
                halo = Sprite.Create(t, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N);
            }
            return halo;
        }
    }

    // ---------- Dibujo de formas ----------

    static Texture2D NewTex(int n)
    {
        Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Clamp;
        t.filterMode = FilterMode.Bilinear;
        return t;
    }

    static Sprite MakeShape(int kind)
    {
        const int N = 128;
        const float outline = 0.09f;
        Color dark = new Color(0.07f, 0.04f, 0.18f, 1f);
        float px = 1f / N;
        Texture2D t = NewTex(N);
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                Vector2 p = new Vector2((x + 0.5f) / N - 0.5f, (y + 0.5f) / N - 0.5f);
                float d = Dist(kind, p);
                float shapeA = Mathf.Clamp01(0.5f - d / px);              // dentro de la forma
                float fillA = Mathf.Clamp01(0.5f - (d + outline) / px);   // dentro del relleno
                Color c = Color.Lerp(dark, Color.white, fillA);
                c.a = shapeA;
                t.SetPixel(x, y, c);
            }
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N);
    }

    // Distancia con signo: negativa adentro
    static float Dist(int kind, Vector2 p)
    {
        switch (kind)
        {
            case 0: return p.magnitude - 0.46f; // circulo
            case 1:                             // triangulo
            {
                const float r = 0.42f;
                float k = Mathf.Sqrt(3f);
                p.y += 0.07f;
                p.x = Mathf.Abs(p.x) - r;
                p.y = p.y + r / k;
                if (p.x + k * p.y > 0f) p = new Vector2(p.x - k * p.y, -k * p.x - p.y) / 2f;
                p.x -= Mathf.Clamp(p.x, -2f * r, 0f);
                return -p.magnitude * Mathf.Sign(p.y);
            }
            case 2:                             // cuadrado redondeado
            {
                const float b = 0.40f, rad = 0.08f;
                Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - new Vector2(b - rad, b - rad);
                return Vector2.Max(q, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - rad;
            }
            default:                            // rombo
                return (Mathf.Abs(p.x) + Mathf.Abs(p.y) - 0.48f) / Mathf.Sqrt(2f);
        }
    }
}
