using UnityEngine;

/// <summary>
/// Gestor de audio ambiental.
/// Genera sonidos de naturaleza proceduralmente (viento, pajaros, agua)
/// sin necesitar AudioClips externos.
/// </summary>
public class AmbientAudioManager : MonoBehaviour
{
    public static AmbientAudioManager Instance { get; private set; }

    [Header("Configuracion de Volumen")]
    [Range(0f, 1f)] public float masterVolume = 0.5f;
    [Range(0f, 1f)] public float ambientVolume = 0.35f;
    [Range(0f, 1f)] public float musicVolume = 0.25f;

    [Header("Audio Sources (se crean automaticamente)")]
    private AudioSource ambientSource;
    private AudioSource musicSource;
    private AudioSource birdSource;

    // Clips procedurales
    private AudioClip windClip;
    private AudioClip birdsClip;
    private AudioClip musicClip;

    // Temporizador de pajaros
    private float nextBirdTime = 0f;
    private float birdInterval = 8f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // Crear AudioSources
        ambientSource = CreateAudioSource("AmbientSource", ambientVolume, true, 0f);
        musicSource   = CreateAudioSource("MusicSource",   musicVolume,   true, 0f);
        birdSource    = CreateAudioSource("BirdSource",    ambientVolume * 0.7f, false, 0.4f);

        // Generar y reproducir clips
        windClip  = GenerateWindClip();
        birdsClip = GenerateBirdsClip();
        musicClip = GenerateNatureMusic();

        ambientSource.clip = windClip;
        ambientSource.Play();

        musicSource.clip = musicClip;
        musicSource.Play();

        nextBirdTime = Time.time + Random.Range(3f, 8f);
    }

    private void Update()
    {
        // Pajaros periodicos
        if (Time.time >= nextBirdTime)
        {
            birdSource.clip = birdsClip;
            birdSource.pitch = Random.Range(0.85f, 1.25f);
            birdSource.Play();
            nextBirdTime = Time.time + Random.Range(birdInterval * 0.6f, birdInterval * 1.5f);
        }

        // Ajustar volumenes segun estado del juego
        float gameTimeScale = Time.timeScale;
        ambientSource.mute = (gameTimeScale == 0f);
        musicSource.mute   = (gameTimeScale == 0f);
    }

    private AudioSource CreateAudioSource(string name, float vol, bool loop, float spatialBlend)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform);
        AudioSource src = go.AddComponent<AudioSource>();
        src.volume = vol * masterVolume;
        src.loop = loop;
        src.spatialBlend = spatialBlend;
        src.playOnAwake = false;
        src.priority = 128;
        return src;
    }

    // --- Generadores Procedurales ---

    /// <summary>Ruido de viento suave (ruido rosa filtrado)</summary>
    private AudioClip GenerateWindClip()
    {
        int sampleRate = 22050;
        float duration = 6f;
        int samples = (int)(sampleRate * duration);
        float[] data = new float[samples];

        System.Random rng = new System.Random(42);
        float lowPassPrev = 0f;

        for (int i = 0; i < samples; i++)
        {
            float white = (float)(rng.NextDouble() * 2.0 - 1.0);
            // Low-pass simple para simular viento (suaviza el ruido blanco)
            lowPassPrev = lowPassPrev * 0.98f + white * 0.02f;
            // Modulacion de amplitud lenta (viento que va y viene)
            float t = (float)i / sampleRate;
            float amp = 0.4f + 0.25f * Mathf.Sin(t * 0.3f) + 0.15f * Mathf.Sin(t * 0.7f);
            data[i] = lowPassPrev * amp * 0.6f;
        }

        AudioClip clip = AudioClip.Create("Wind", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    /// <summary>Gorjeo de pajaros (trinos cortos) </summary>
    private AudioClip GenerateBirdsClip()
    {
        int sampleRate = 44100;
        float duration = 1.2f;
        int samples = (int)(sampleRate * duration);
        float[] data = new float[samples];

        // Dos trinos ascendentes solapados
        float[] tweetFreqs = { 2800f, 3400f, 3100f, 3800f };
        int tweetLen = samples / 4;

        for (int i = 0; i < samples; i++)
        {
            int seg = Mathf.Clamp(i / tweetLen, 0, 3);
            float baseFreq = tweetFreqs[seg];
            float t = (float)i / sampleRate;
            float localT = (float)(i % tweetLen) / tweetLen;

            // Glissando (frecuencia sube dentro de cada trino)
            float freq = baseFreq * (1f + localT * 0.15f);
            float env  = Mathf.Sin(localT * Mathf.PI); // Envelope suave

            // Vibrato sutil
            float vibrato = 1f + 0.03f * Mathf.Sin(t * 28f);

            data[i] = Mathf.Sin(2f * Mathf.PI * freq * vibrato * t) * env * 0.3f;
        }

        AudioClip clip = AudioClip.Create("Birds", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    /// <summary>Musica ambiental suave de naturaleza (acordes armonicos)</summary>
    private AudioClip GenerateNatureMusic()
    {
        int sampleRate = 44100;
        float duration = 16f;
        int samples = (int)(sampleRate * duration);
        float[] data = new float[samples];

        // Progresion de acordes: Am - F - C - G
        float[][] chords = {
            new float[] { 220f, 261.63f, 329.63f },  // Am
            new float[] { 174.61f, 220f, 261.63f },   // F
            new float[] { 261.63f, 329.63f, 392f },   // C
            new float[] { 196f, 246.94f, 293.66f }    // G
        };

        float chordDuration = duration / chords.Length;

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            int chordIdx = Mathf.Clamp((int)(t / chordDuration), 0, chords.Length - 1);
            float[] chord = chords[chordIdx];

            float localT = t - chordIdx * chordDuration;
            // Envelope de cada acorde: fade in y fade out suave
            float env = Mathf.Clamp01(localT / 0.6f) * Mathf.Clamp01((chordDuration - localT) / 0.8f);

            float sample = 0f;
            foreach (float freq in chord)
            {
                // Onda senoidal con armónico suave
                sample += Mathf.Sin(2f * Mathf.PI * freq * t) * 0.6f;
                sample += Mathf.Sin(2f * Mathf.PI * freq * 2f * t) * 0.12f;
            }

            // Tremolo muy suave
            float tremolo = 1f + 0.04f * Mathf.Sin(t * 5f);
            data[i] = (sample / chord.Length) * env * tremolo * 0.18f;
        }

        AudioClip clip = AudioClip.Create("NatureMusic", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    public void SetMasterVolume(float vol)
    {
        masterVolume = Mathf.Clamp01(vol);
        if (ambientSource) ambientSource.volume = ambientVolume * masterVolume;
        if (musicSource)   musicSource.volume   = musicVolume   * masterVolume;
        if (birdSource)    birdSource.volume     = ambientVolume * 0.7f * masterVolume;
    }
}
