using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class HOH_RemoteImageLoader : MonoBehaviour
{
    [SerializeField] private Image targetImage;
    [SerializeField] private Sprite fallbackSprite;

    private static readonly Dictionary<string, Sprite> SpriteCache = new();

    private Coroutine _loadRoutine;
    private string _pendingUrl;

    private void Awake()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();
    }

    private void OnEnable()
    {
        TryStartLoad();
    }

    private void OnDisable()
    {
        if (_loadRoutine != null)
        {
            StopCoroutine(_loadRoutine);
            _loadRoutine = null;
        }
    }

    public void Load(string url)
    {
        _pendingUrl = url;

        if (targetImage == null)
            return;

        if (string.IsNullOrWhiteSpace(url))
        {
            ApplyFallback();
            return;
        }

        if (SpriteCache.TryGetValue(url, out Sprite cachedSprite) && cachedSprite != null)
        {
            targetImage.sprite = cachedSprite;
            targetImage.preserveAspect = true;
            _pendingUrl = null;
            return;
        }

        ApplyFallback();
        TryStartLoad();
    }

    private void TryStartLoad()
    {
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
            return;

        if (string.IsNullOrWhiteSpace(_pendingUrl))
            return;

        if (_loadRoutine != null)
        {
            StopCoroutine(_loadRoutine);
            _loadRoutine = null;
        }

        _loadRoutine = StartCoroutine(LoadRoutine(_pendingUrl));
    }

    private IEnumerator LoadRoutine(string url)
    {
        using UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        _loadRoutine = null;

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[HOH_RemoteImageLoader] Failed to load image: {url} | {request.error}");
            yield break;
        }

        Texture2D texture = DownloadHandlerTexture.GetContent(request);
        if (texture == null)
            yield break;

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f)
        );

        if (!SpriteCache.ContainsKey(url))
            SpriteCache.Add(url, sprite);

        if (targetImage != null)
        {
            targetImage.sprite = sprite;
            targetImage.preserveAspect = true;
        }

        if (_pendingUrl == url)
            _pendingUrl = null;
    }

    private void ApplyFallback()
    {
        if (targetImage == null)
            return;

        if (fallbackSprite != null)
        {
            targetImage.sprite = fallbackSprite;
            targetImage.preserveAspect = true;
        }
    }
}