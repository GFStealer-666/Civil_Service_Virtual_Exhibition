using System.Collections.Generic;
using UnityEngine;

public class ExhibitionAudioManager : MonoBehaviour
{
    public static ExhibitionAudioManager Instance { get; private set; }

    private readonly List<ExhibitionAudioSource> _registeredSources = new List<ExhibitionAudioSource>();

    public float BgmVolume
    {
        get
        {
            if (LocalPlayerData.Instance == null) return 1f;
            return LocalPlayerData.Instance.BgmVolume;
        }
    }

    public float EffectVolume
    {
        get
        {
            if (LocalPlayerData.Instance == null) return 1f;
            return LocalPlayerData.Instance.EffectVolume;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        ApplyAllVolumes();
    }

    public void RegisterSource(ExhibitionAudioSource source)
    {
        if (source == null) return;
        if (_registeredSources.Contains(source)) return;

        _registeredSources.Add(source);
        ApplyVolumeToSource(source);
    }

    public void UnregisterSource(ExhibitionAudioSource source)
    {
        if (source == null) return;
        _registeredSources.Remove(source);
    }

    public void SetBgmVolume(float value)
    {
        if (LocalPlayerData.Instance != null)
            LocalPlayerData.Instance.SetBgmVolume(value);

        ApplyAllVolumes();
    }

    public void SetEffectVolume(float value)
    {
        if (LocalPlayerData.Instance != null)
            LocalPlayerData.Instance.SetEffectVolume(value);

        ApplyAllVolumes();
    }

    public void ApplyAllVolumes()
    {
        for (int i = _registeredSources.Count - 1; i >= 0; i--)
        {
            if (_registeredSources[i] == null)
            {
                _registeredSources.RemoveAt(i);
                continue;
            }

            ApplyVolumeToSource(_registeredSources[i]);
        }
    }

    private void ApplyVolumeToSource(ExhibitionAudioSource source)
    {
        if (source == null || source.AudioSource == null) return;

        switch (source.Channel)
        {
            case ExhibitionAudioChannel.Bgm:
                source.AudioSource.volume = BgmVolume;
                break;

            case ExhibitionAudioChannel.Effect:
                source.AudioSource.volume = EffectVolume;
                break;
        }
    }
}