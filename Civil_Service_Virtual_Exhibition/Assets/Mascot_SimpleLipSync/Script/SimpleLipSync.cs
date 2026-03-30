using UnityEngine;
using System.Collections;

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

    [Header("Lip Sync Mode")]
    [Tooltip("ถ้าเปิด จะใช้โหมดขยับปากแบบ fake flap สำหรับเสียงจาก API / WebGL")]
    public bool useFakeLipSync = true;

    [Header("Fake Lip Sync Settings")]
    [Tooltip("จำนวนครั้งต่อวินาทีที่ปากจะเปิด/ปิด")]
    float flapSpeed = 5f;

    [Tooltip("สุ่มจังหวะเพิ่มขึ้นเล็กน้อยให้ดูไม่แข็ง")]
    public float flapRandomness = 0.15f;

    [Tooltip("ถ้าเสียงเบากว่านี้ จะไม่ขยับปาก")]
    [Range(0f, 1f)]
    public float volumeThreshold = 0.01f;

    [Tooltip("หน่วงก่อนปิดปากหลังเสียงหยุดเล็กน้อย")]
    public float mouthCloseDelay = 0.05f;

    [Header("Blink Settings")]
    public float blinkIntervalMin = 2.0f;
    public float blinkIntervalMax = 5.0f;
    public float blinkDuration = 0.08f;
    [Range(0f, 1f)]
    public float doubleBlinkChance = 0.2f;

    private bool isMouthOpen;
    private bool isTalking;
    private bool isBlinking;

    private Material[] mouthMats;
    private Material[] eyeMats;

    private Coroutine blinkRoutine;
    private Coroutine mouthRoutine;
    private float flapSeed;

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

        flapSeed = Random.Range(0f, 1000f);

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

        if (mouthRoutine != null)
        {
            StopCoroutine(mouthRoutine);
            mouthRoutine = null;
        }

        SetMouth(false);
        SetTalking(false, true);
    }

    void Update()
    {
        if (audioSource == null) return;

        bool playing = audioSource.isPlaying && audioSource.clip != null;
        SetTalking(playing);

        if (!playing)
        {
            if (mouthRoutine == null)
            {
                SetMouth(false);
            }
            return;
        }

        if (useFakeLipSync)
        {
            UpdateFakeLipSync();
        }
    }

    void UpdateFakeLipSync()
    {
        float volume = Mathf.Abs(audioSource.volume);

        if (audioSource.mute || volume <= volumeThreshold)
        {
            SetMouth(false);
            return;
        }

        // ใช้เวลาเป็นฐานในการ flap ปาก
        // audioSource.time อาจไม่สมูททุก platform เลยผสมกับ Time.time ให้เสถียรขึ้น
        float t = (audioSource.time > 0f ? audioSource.time : Time.time) * flapSpeed;

        // เพิ่ม randomness เบา ๆ
        float noise = Mathf.PerlinNoise(flapSeed, Time.time * (flapSpeed * 0.5f)) - 0.5f;
        t += noise * flapRandomness * flapSpeed;

        // สลับ open / close
        float wave = Mathf.PingPong(t, 1f);

        // ถ้า volume สูง จะเปิดปากได้นานขึ้นนิดหน่อย
        float dynamicThreshold = Mathf.Lerp(0.6f, 0.35f, Mathf.Clamp01(volume));

        SetMouth(wave > dynamicThreshold);
    }

    IEnumerator BlinkLoop()
    {
        while (true)
        {
            float wait = Random.Range(blinkIntervalMin, blinkIntervalMax);
            yield return new WaitForSeconds(wait);

            yield return StartCoroutine(BlinkOnce());

            if (Random.value < doubleBlinkChance)
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

    IEnumerator CloseMouthDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        mouthRoutine = null;

        if (audioSource == null || !audioSource.isPlaying)
        {
            SetMouth(false);
            SetTalking(false, true);
        }
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
            return;
        }

        if (!force && eyeMats[eyeMaterialIndex] == mat) return;

        eyeMats[eyeMaterialIndex] = mat;
        eyeRenderer.materials = eyeMats;
    }

    public void PlayVoice(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;

        if (mouthRoutine != null)
        {
            StopCoroutine(mouthRoutine);
            mouthRoutine = null;
        }

        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();

        SetTalking(true, true);
        SetMouth(true);
    }

    public void StopVoice()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        if (mouthRoutine != null)
        {
            StopCoroutine(mouthRoutine);
            mouthRoutine = null;
        }

        SetMouth(false);
        SetTalking(false, true);
    }

    public void OnVoicePlaybackEnded()
    {
        if (mouthRoutine != null)
        {
            StopCoroutine(mouthRoutine);
        }

        mouthRoutine = StartCoroutine(CloseMouthDelayed(mouthCloseDelay));
    }

    public void BlinkNow()
    {
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(BlinkOnce());
        }
    }
}