using System;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicController : MonoBehaviour
{
    [Header("Background Music")]
    public AudioClip backgroundMusic;
    [Range(0f, 1f)] public float volume = 0.7f;

    [Header("Dynamic Volume")]
    [Tooltip("Mức âm lượng tham chiếu tính bằng dB. Âm thanh nhỏ hơn sẽ được nâng nhẹ, âm thanh lớn hơn sẽ được giảm.")]
    [Range(-36f, -6f)] public float targetLevelDb = -18f;
    [Range(1f, 8f)] public float compressionRatio = 3f;
    [Range(0f, 18f)] public float maxBoostDb = 9f;
    [Range(0f, 24f)] public float maxReductionDb = 12f;
    [Range(0.001f, 0.2f)] public float attackTime = 0.03f;
    [Range(0.02f, 1f)] public float releaseTime = 0.25f;

    private AudioSource audioSource;
    private float levelEnvelope;
    private float currentGain = 1f;
    private int outputSampleRate;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.spatialBlend = 0f;
        audioSource.volume = volume;
        audioSource.clip = backgroundMusic;
        outputSampleRate = AudioSettings.outputSampleRate;
    }

    void Start()
    {
        if (backgroundMusic == null)
        {
            Debug.LogError("Assign a background music clip in the MusicController Inspector.", this);
            return;
        }

        audioSource.Play();
    }

    void Update()
    {
        audioSource.volume = volume;
    }

    public void StopMusic()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        if (channels <= 0 || data.Length == 0)
        {
            return;
        }

        double sumSquares = 0d;
        for (int i = 0; i < data.Length; i++)
        {
            sumSquares += data[i] * data[i];
        }

        float rms = (float)Math.Sqrt(sumSquares / data.Length);
        float sampleRate = outputSampleRate;
        if (sampleRate <= 0f)
        {
            return;
        }

        float blockDuration = data.Length / (sampleRate * channels);
        float attack = Math.Max(0.001f, attackTime);
        float release = Math.Max(0.02f, releaseTime);
        float envelopeTime = rms > levelEnvelope ? attack : release;
        float envelopeCoefficient = (float)Math.Exp(-blockDuration / envelopeTime);
        levelEnvelope = envelopeCoefficient * levelEnvelope + (1f - envelopeCoefficient) * rms;

        float levelDb = 20f * (float)Math.Log10(Math.Max(levelEnvelope, 0.000001f));
        float ratio = Math.Max(1f, compressionRatio);
        float outputLevelDb = targetLevelDb + (levelDb - targetLevelDb) / ratio;
        float gainDb = outputLevelDb - levelDb;
        gainDb = Math.Max(-Math.Max(0f, maxReductionDb), Math.Min(Math.Max(0f, maxBoostDb), gainDb));

        float targetGain = (float)Math.Pow(10f, gainDb / 20f);
        float gainTime = targetGain < currentGain ? attack : release;
        float gainCoefficient = (float)Math.Exp(-1f / (sampleRate * Math.Max(0.001f, gainTime)));

        for (int i = 0; i < data.Length; i++)
        {
            currentGain = targetGain + gainCoefficient * (currentGain - targetGain);
            data[i] *= currentGain;
        }
    }
}
