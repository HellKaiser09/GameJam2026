// "Partitura" de cancion_tutorial.mp3 (120 BPM, 64 compases, 2:08).
// La saque analizando el audio: en que corcheas suena el bombo (K) y la caja (S).
// Cada texto es un compas de 8 corcheas:  1 y 2 y 3 y 4 y
// El jefe dispara exactamente en esos golpes, asi las balas van con el "tun tun".
public static class SongChart
{
    public const int Bars = 64;
    public const int StepsPerBar = 8;

    // 0 = intro (no dispara), 1 = tranquila, 2 = intensa
    public static int Level(int bar)
    {
        bar %= Bars;
        if (bar < 4) return 0;
        if (bar < 8) return 1;
        if (bar < 32) return 2;
        if (bar < 40) return 1;
        return 2;
    }

    public static bool Kick(int bar, int step) { return kick[bar % Bars][step] == 'K'; }
    public static bool Snare(int bar, int step) { return step != 0 && snare[bar % Bars][step] == 'S'; }

    static readonly string[] kick =
    {
        "........", "........", "........", "........",
        ".K.K.K..", "KK.KK...", "K...KK..", "KK.KKK..",
        "KK.KKK..", "K..KK...", "KK.KKK..", ".K.K.K..",
        "KK.KKK..", "K..KK...", "KK.KKK..", ".K.K.K..",
        "KK.KK...", "K...K...", "KK.K.K..", ".K.K.K..",
        "KK..K...", "K...K...", "KK.K.K..", "KK.K.K..",
        "KK..KK..", "KK..K.K.", "KK.K.K..", ".K.KKK..",
        "K...KK..", "KK.K.KK.", "KK.K.K..", ".K.KK...",
        "K...K...", "KK.KKK..", "KK.K.K..", "KK.KK...",
        "K...K...", "KK.KKK..", "KK.K.K..", "K..KK...",
        "KK..K...", ".K.K.K..", "KK.KKK..", "K...K...",
        "KK.KKK..", ".K.K.K..", "KK.KK...", "K...K...",
        "KK.KKK..", "KK.K.K..", "KK.KK...", "KK..KK..",
        "KK.K.K..", "KK.K.K..", "K..KK...", "KK..KK..",
        ".K.K.K..", "KK.KKKK.", "K...KK..", ".K.KKK..",
        ".K.K.K..", "KK..K.K.", "K...KK..", "KK.K.K..",
    };

    static readonly string[] snare =
    {
        "........", "........", "........", "........",
        "..S...S.", "..S...S.", "..S...S.", "..S...S.",
        "..S...S.", "..S...S.", "..S...S.", "..S...S.",
        "..S...S.", "..S...S.", "..S...S.", "..S...S.",
        "..S...S.", "..S...S.", "..S...S.", "..S...S.",
        "..S...S.", "..S...S.", "..S...S.", "..S..SS.",
        "..S...S.", "..S...S.", "..S...S.", "..S...S.",
        "..S...S.", "..S...S.", "..S...S.", "..S...S.",
        "..S...S.", "..S...S.", "..S...S.", "..S...S.",
        "..S...S.", "..S...S.", "..S...S.", "..S...S.",
        "..S...S.", "..S...S.", "..S...S.", "..S...S.",
        "..S...S.", "..S...S.", "..S...S.", "..S...S.",
        "..S...S.", "..S...S.", "..S...S.", "..S...S.",
        "..S...S.", "..S...S.", "..S...S.", "..S...S.",
        "..S...S.", "..S...S.", "..S...S.", "..S...S.",
        "..S...S.", "..S...S.", "..S...S.", "..S...S.",
    };
}
