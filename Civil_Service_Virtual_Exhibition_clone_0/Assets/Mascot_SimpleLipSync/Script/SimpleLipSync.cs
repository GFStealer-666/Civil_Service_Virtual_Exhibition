using UnityEngine;
using System.Collections;
using System.Diagnostics;
using System;

[RequireComponent(typeof(AudioSource))]
public class CharacterMaterialFaceController : MonoBehaviour
{
    [Header("References")]
    public AudioSource audioSource;
    public Animator animator;

    [Header("Mouth Renderer")]
    public Renderer mouthRenderer;
    public Material mouthClosed;
    public Material mouthOpen;
    [Tooltip("Material index ของปากบน mouthRenderer")]
    public int mouthMaterialIndex = 0;

    [Header("Eye Renderer")]
    public Renderer eyeRenderer;
    public Material eyeOpen;
    public Material eyeClosed;
    [Tooltip("Material index ของตาบน eyeRenderer")]
    public int eyeMaterialIndex = 0;

    [Header("Lip Sync Settings")]
    public int sampleWindowSize = 128;
    public float openThreshold = 0.012f;
    public float closeThreshold = 0.009f;
    public float sensitivityBoost = 1.2f;

    [Header("Blink Settings")]
    public float blinkIntervalMin = 2.0f;
    public float blinkIntervalMax = 5.0f;
    public float blinkDuration = 0.08f;
    [Range(0f, 1f)]
    public float doubleBlinkChance = 0.2f;

    private AudioClip currentClip;
    private float[] clipSamples;
    private int channels;

    private bool isMouthOpen;
    private bool isTalking;
    private bool isBlinking;

    private Material[] mouthMats;
    private Material[] eyeMats;

    private Coroutine blinkRoutine;

    void Reset()
    {
        audioSource = GetComponent<AudioSource>();
        animator = GetComponentInChildren<Animator>();
    }

    void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        if (mouthRenderer != null) mouthMats = mouthRenderer.materials;
        if (eyeRenderer != null) eyeMats = eyeRenderer.materials;

        SetMouthMaterial(mouthClosed, true);
        SetEyeMaterial(eyeOpen, true);
        SetTalking(false, true);
    }

    void OnEnable()
    {
        if (blinkRoutine == null)
        {
            blinkRoutine = StartCoroutine(BlinkLoop());
        }
    }

    void OnDisable()
    {
        if (blinkRoutine != null)
        {
            StopCoroutine(blinkRoutine);
            blinkRoutine = null;
        }
    }

    void Update()
    {
        if (audioSource == null) return;

        SetTalking(audioSource.isPlaying);

        if (!audioSource.isPlaying || audioSource.clip == null)
        {
            SetMouth(false);
            return;
        }

        if (currentClip != audioSource.clip || clipSamples == null)
        {
            CacheClip(audioSource.clip);
        }

        float level = GetLevel(audioSource.timeSamples) * sensitivityBoost;

        if (level >= openThreshold)
        {
            SetMouth(true);
        }
        else if (level <= closeThreshold)
        {
            SetMouth(false);
        }
    }

    IEnumerator BlinkLoop()
    {
        while (true)
        {
            float wait = UnityEngine.Random.Range(blinkIntervalMin, blinkIntervalMax);
            yield return new WaitForSeconds(wait);

            yield return StartCoroutine(BlinkOnce());

            if (UnityEngine.Random.value < doubleBlinkChance)
            {
                yield return new WaitForSeconds(0.06f);
                yield return StartCoroutine(BlinkOnce());
            }
        }
    }

    IEnumerator BlinkOnce()
    {
        if (isBlinking) yield break;
        isBlinking = true;

        SetEyesClosed(true);
        yield return new WaitForSeconds(blinkDuration);
        SetEyesClosed(false);

        isBlinking = false;
    }

    void CacheClip(AudioClip clip)
    {
        currentClip = clip;
        channels = clip.channels;
        clipSamples = new float[clip.samples * channels];
        clip.GetData(clipSamples, 0);
    }

    float GetLevel(int timeSamples)
    {
        if (clipSamples == null || channels == 0) return 0f;

        int start = timeSamples * channels;
        if (start >= clipSamples.Length) return 0f;

        int end = Mathf.Min(start + sampleWindowSize * channels, clipSamples.Length);

        float sum = 0f;
        int count = 0;

        for (int i = start; i < end; i += channels)
        {
            float v = 0f;

            for (int ch = 0; ch < channels; ch++)
            {
                v += Mathf.Abs(clipSamples[i + ch]);
            }

            v /= channels;
            sum += v;
            count++;
        }

        return count > 0 ? sum / count : 0f;
    }

    void SetMouth(bool open)
    {
        if (isMouthOpen == open) return;

        isMouthOpen = open;
        SetMouthMaterial(open ? mouthOpen : mouthClosed);
    }

    void SetEyesClosed(bool closed)
    {
        SetEyeMaterial(closed ? eyeClosed : eyeOpen);
    }

    void SetTalking(bool talking, bool force = false)
    {
        if (!force && isTalking == talking) return;

        isTalking = talking;

        if (animator != null)
        {
            animator.SetBool("isTalk", talking);
        }
    }

    void SetMouthMaterial(Material mat, bool force = false)
    {
        if (mouthRenderer == null || mat == null) return;

        if (mouthMats == null || mouthMats.Length == 0)
        {
            mouthMats = mouthRenderer.materials;
        }

        if (mouthMaterialIndex < 0 || mouthMaterialIndex >= mouthMats.Length)
        {
            //Debug.LogError($"Mouth material index {mouthMaterialIndex} ผิดบน {mouthRenderer.name}");
            return;
        }

        if (!force && mouthMats[mouthMaterialIndex] == mat) return;

        mouthMats[mouthMaterialIndex] = mat;
        mouthRenderer.materials = mouthMats;
    }

    void SetEyeMaterial(Material mat, bool force = false)
    {
        if (eyeRenderer == null || mat == null) return;

        if (eyeMats == null || eyeMats.Length == 0)
        {
            eyeMats = eyeRenderer.materials;
        }

        if (eyeMaterialIndex < 0 || eyeMaterialIndex >= eyeMats.Length)
        {
            //Debug.LogError($"Eye material index {eyeMaterialIndex} ผิดบน {eyeRenderer.name}");
            return;
        }

        if (!force && eyeMats[eyeMaterialIndex] == mat) return;

        eyeMats[eyeMaterialIndex] = mat;
        eyeRenderer.materials = eyeMats;
    }

    public void PlayVoice(AudioClip clip)
    {
        if (clip == null) return;

        CacheClip(clip);
        SetMouth(false);

        audioSource.clip = clip;
        audioSource.Play();
    }

    public void StopVoice()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        SetMouth(false);
        SetTalking(false, true);
    }

    public void BlinkNow()
    {
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(BlinkOnce());
        }
    }
}