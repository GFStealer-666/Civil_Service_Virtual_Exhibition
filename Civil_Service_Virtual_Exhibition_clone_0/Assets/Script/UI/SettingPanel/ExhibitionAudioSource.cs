using UnityEngine;

public enum ExhibitionAudioChannel
{
    Bgm,
    Effect
}

[RequireComponent(typeof(AudioSource))]
public class ExhibitionAudioSource : MonoBehaviour
{
    [SerializeField] private ExhibitionAudioChannel channel = ExhibitionAudioChannel.Bgm;
    [SerializeField] private AudioSource audioSource;

    public ExhibitionAudioChannel Channel => channel;
    public AudioSource AudioSource => audioSource;

    private void Reset()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        if (ExhibitionAudioManager.Instance != null)
            ExhibitionAudioManager.Instance.RegisterSource(this);
    }

    private void Start()
    {
        if (ExhibitionAudioManager.Instance != null)
            ExhibitionAudioManager.Instance.RegisterSource(this);
    }

    private void OnDisable()
    {
        if (ExhibitionAudioManager.Instance != null)
            ExhibitionAudioManager.Instance.UnregisterSource(this);
    }
}