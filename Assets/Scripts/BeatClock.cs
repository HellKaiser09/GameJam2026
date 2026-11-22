using System;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BeatClock : MonoBehaviour
{
    [SerializeField] float bpm = 120f;
    [SerializeField] float firstBeatOffset = 0f;
    [SerializeField] AudioClip clip;

    public event Action<int> OnBeat;

    AudioSource source;
    double songStartDsp;
    int lastBeat = -1;
    double SecondsPerBeat => 60.0 / bpm;

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
    }

    void Start()
    {
        songStartDsp = AudioSettings.dspTime + 0.5;
        source.PlayScheduled(songStartDsp);
    }

    void Update()
    {
        double songTime = AudioSettings.dspTime - songStartDsp - firstBeatOffset;
        if (songTime < 0) return;

        int beat = (int)(songTime / SecondsPerBeat);
        while (lastBeat < beat)
        {
            lastBeat++;
            OnBeat?.Invoke(lastBeat);
        }
    }
}
