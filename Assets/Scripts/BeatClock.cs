using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(AudioSource))]
public class BeatClock : MonoBehaviour
{
    [SerializeField] float bpm = 120f;
    [SerializeField] float firstBeatOffset = 0f;
    [SerializeField] AudioClip clip;

    public event Action<int> OnBeat;
    public event Action<int> OnStep;   // cada corchea (2 por beat)

    AudioSource source;
    AudioDistortionFilter distortion;
    bool frozen;            // musica parada: el reloj no avanza
    double frozenSongTime;
    float distortLeft, distortTotal, distortLevel;

    public bool IsStopped => frozen;

    // Filtros: foco (ahogado + eco), golpe del parry, victoria (ahogado + distorsion)
    AudioLowPassFilter lowPass;
    AudioReverbFilter reverb;
    float focusAmount, thump, muffle;
    double songStartDsp;
    int lastBeat = -1;
    int lastStep = -1;
    double SecondsPerBeat => 60.0 / bpm;

    // Duracion de un beat en segundos (0.5 s a 120 BPM)
    public float BeatLength => (float)SecondsPerBeat;

    // 1 justo en el beat y baja a 0 en el primer cuarto del beat. Para que todo "lata".
    public static float Pulse { get; private set; }

    // Que tan avanzado va el beat actual: 0 justo en el beat, casi 1 justo antes del siguiente
    public float BeatPhase { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Pulse = 0f; }

    // Numero del ultimo beat que ya sono (-1 si aun no empieza)
    public int CurrentBeat => lastBeat;

    // Segundos que faltan (o pasaron) respecto al beat mas cercano
    public float DistanceToNearestBeat()
    {
        double t = AudioSettings.dspTime - songStartDsp - firstBeatOffset;
        if (t < 0) return float.MaxValue;
        double frac = t % SecondsPerBeat;
        return (float)Math.Min(frac, SecondsPerBeat - frac);
    }

    void Awake()
    {
        source = GetComponent<AudioSource>();
        if (clip != null) source.clip = clip;
        source.playOnAwake = false;
        distortion = GetComponent<AudioDistortionFilter>();
        if (distortion == null) distortion = gameObject.AddComponent<AudioDistortionFilter>();
        distortion.enabled = false;

        lowPass = GetComponent<AudioLowPassFilter>();
        if (lowPass == null) lowPass = gameObject.AddComponent<AudioLowPassFilter>();
        lowPass.enabled = false;
        reverb = GetComponent<AudioReverbFilter>();
        if (reverb == null) reverb = gameObject.AddComponent<AudioReverbFilter>();
        reverb.reverbPreset = AudioReverbPreset.User;
        reverb.dryLevel = 0f; reverb.decayTime = 2.8f; reverb.density = 100f; reverb.diffusion = 100f;
        reverb.reflectionsLevel = -800f; reverb.reverbLevel = 200f; reverb.room = -10000f;
        reverb.enabled = false;
    }

    // 0 = normal, 1 = foco completo: se escucha lejano y con eco (sin cambiar el ritmo)
    public void SetFocus(float amount) { focusAmount = Mathf.Clamp01(amount); }

    // Golpe sordo cuando una nota devuelta le pega al jefe
    public void Thump(float strength) { thump = Mathf.Max(thump, Mathf.Clamp01(strength)); }

    // Victoria: la musica del jefe "muere" ahogada y distorsionada
    public void SetMuffle(bool on) { muffle = on ? 1f : 0f; }

    // Corta la musica en seco
    public void Cut()
    {
        StopAllCoroutines();
        frozen = true;
        source.Pause();
    }

    // Vuelve a tocar limpia desde un segundo de la cancion (por ejemplo el coro)
    public void PlayCleanFrom(float seconds)
    {
        StopAllCoroutines();
        breaking = false;
        muffle = 0f; thump = 0f; focusAmount = 0f; distortLeft = 0f;
        distortion.enabled = false;
        source.pitch = 1f;
        source.time = seconds;
        source.UnPause();
        if (!source.isPlaying) source.Play();
        songStartDsp = AudioSettings.dspTime - seconds;
        frozen = false;
    }

    void UpdateFilters(float dt)
    {
        if (breaking) return; // la cinta rota controla sus propios filtros
        thump = Mathf.MoveTowards(thump, 0f, dt * 4f);
        float cutoff = 22000f;
        cutoff = Mathf.Min(cutoff, Mathf.Lerp(22000f, 2600f, focusAmount));
        cutoff = Mathf.Min(cutoff, Mathf.Lerp(22000f, 1100f, thump));
        if (muffle > 0f) cutoff = Mathf.Min(cutoff, 900f);
        lowPass.enabled = cutoff < 21500f;
        lowPass.cutoffFrequency = cutoff;

        reverb.enabled = focusAmount > 0.01f;
        if (reverb.enabled) reverb.room = Mathf.Lerp(-3000f, -300f, focusAmount);

        float d = distortLeft > 0f ? distortLevel * Mathf.Clamp01(distortLeft / distortTotal) : 0f;
        d = Mathf.Max(d, muffle * 0.5f);
        distortion.enabled = d > 0.01f;
        distortion.distortionLevel = d;
    }

    double SongTimeNow() { return AudioSettings.dspTime - songStartDsp - firstBeatOffset; }

    // Para la musica como una cinta que se frena (la cancion baja de tono y se detiene)
    public void TapeStop(float time = 0.6f)
    {
        if (frozen) return;
        frozen = true;
        frozenSongTime = SongTimeNow();
        StopAllCoroutines();
        StartCoroutine(PitchDown(time));
    }

    IEnumerator PitchDown(float time)
    {
        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            source.pitch = Mathf.Lerp(1f, 0.15f, t / time);
            yield return null;
        }
        source.Pause();
        source.pitch = 1f;
    }

    // Cinta que se descompone: tiembla el tono, se traba, se ahoga y muere (game over)
    public void TapeBreak(float time = 1.8f)
    {
        if (frozen && !source.isPlaying) return;
        frozen = true;
        frozenSongTime = SongTimeNow();
        StopAllCoroutines();
        StartCoroutine(BreakRoutine(time));
    }

    bool breaking;

    IEnumerator BreakRoutine(float time)
    {
        breaking = true;
        AudioLowPassFilter lp = lowPass;
        lp.enabled = true;
        reverb.enabled = false;
        distortion.enabled = true;

        int freq = source.clip != null ? source.clip.frequency : 44100;
        float t = 0f, nextStutter = 0.25f;
        while (t < time)
        {
            float dt = Time.unscaledDeltaTime;
            t += dt;
            float k = t / time;
            // "Wow & flutter": el tono tiembla cada vez mas y se va cayendo
            float wobble = Mathf.Sin(t * 9f) * 0.12f + Mathf.Sin(t * 31f) * 0.05f;
            source.pitch = Mathf.Max(0.05f, (1f - 0.8f * k * k) * (1f + wobble * (0.5f + k)));
            lp.cutoffFrequency = Mathf.Lerp(22000f, 700f, k);
            distortion.distortionLevel = Mathf.Lerp(0.3f, 0.85f, k);

            // Se "traba": regresa un pedacito y lo repite
            if (t >= nextStutter && source.clip != null && k < 0.75f)
            {
                int back = (int)(freq * Random.Range(0.06f, 0.14f));
                source.timeSamples = Mathf.Max(0, source.timeSamples - back);
                nextStutter = t + Random.Range(0.18f, 0.35f);
            }
            yield return null;
        }
        source.Pause();
        source.pitch = 1f;
        lp.enabled = false;
        distortion.enabled = false;
        breaking = false;
    }

    // Pausa inmediata (menu de pausa); Resume() la continua en sincronia
    public void PauseNow()
    {
        if (frozen || !Started) return;
        frozen = true;
        frozenSongTime = SongTimeNow();
        StopAllCoroutines();
        source.pitch = 1f;
        source.Pause();
    }

    // Vuelve a tocar desde donde se quedo y re-sincroniza el reloj con el audio
    public void Resume()
    {
        if (!frozen || !Started) return;
        StopAllCoroutines();
        source.pitch = 1f;
        double total = frozenSongTime + firstBeatOffset; // posicion aproximada de la cancion
        if (source.clip != null)
        {
            double len = source.clip.length;
            double audioPos = source.timeSamples / (double)source.clip.frequency;
            int loops = Mathf.Max(0, Mathf.RoundToInt((float)((total - audioPos) / len)));
            total = loops * len + audioPos;
        }
        songStartDsp = AudioSettings.dspTime - total;
        source.UnPause();
        frozen = false;
    }

    // Distorsion de audio por un momento (al recibir golpe o pasar de ronda)
    public void Distort(float level, float time)
    {
        distortLevel = Mathf.Max(distortLevel * (distortLeft / Mathf.Max(0.01f, distortTotal)), level);
        distortLeft = distortTotal = time;
    }

    public bool Started { get; private set; }

    void Start()
    {
        if (Tutorial.Active) return; // el tutorial arranca la cancion al terminar
        StartSong();
    }

    public void StartSong()
    {
        if (Started) return;
        Started = true;
        songStartDsp = AudioSettings.dspTime + 0.5;
        source.PlayScheduled(songStartDsp);
    }

    void Update()
    {
        if (distortLeft > 0f) distortLeft -= Time.unscaledDeltaTime;
        UpdateFilters(Time.unscaledDeltaTime);

        if (frozen) return;
        double songTime = SongTimeNow();
        if (songTime < 0) return;

        int beat = (int)(songTime / SecondsPerBeat);
        float frac = (float)(songTime / SecondsPerBeat - beat);
        Pulse = Mathf.Clamp01(1f - frac * 4f);
        BeatPhase = frac;
        while (lastBeat < beat)
        {
            lastBeat++;
            OnBeat?.Invoke(lastBeat);
        }

        int step = (int)(songTime / (SecondsPerBeat / 2.0));
        while (lastStep < step)
        {
            lastStep++;
            OnStep?.Invoke(lastStep);
        }
    }
}
