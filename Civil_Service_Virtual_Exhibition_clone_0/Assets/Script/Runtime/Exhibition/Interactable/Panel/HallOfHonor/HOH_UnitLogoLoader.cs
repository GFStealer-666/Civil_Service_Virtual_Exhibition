using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class HOH_UnitLogoLoader : MonoBehaviour
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
            SetFallback();
            return;
        }

        if (SpriteCache.TryGetValue(url, out Sprite cachedSprite) && cachedSprite != null)
        {
            targetImage.sprite = cachedSprite;
            _pendingUrl = null;
            return;
        }

        SetFallback();
        TryStartLoad();
    }

    private void TryStartLoad()
    {
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
            return;

        if (string.IsNullOrWhiteSpace(_pendingUrl))
            return;

        _loadRoutine = StartCoroutine(LoadRoutine(_pendingUrl));
    }

    private IEnumerator LoadRoutine(string url)
    {
        using UnityWebRequest req = UnityWebRequestTexture.GetTexture(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[HOH_UnitLogoLoader] Failed to load logo: {url} | {req.error}");
            _loadRoutine = null;
            yield break;
        }

        Texture2D texture = DownloadHandlerTexture.GetContent(req);
        if (texture == null)
        {
            _loadRoutine = null;
            yield break;
        }

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f)
        );

        if (!SpriteCache.ContainsKey(url))
            SpriteCache.Add(url, sprite);

        if (targetImage != null)
            targetImage.sprite = sprite;

        if (_pendingUrl == url)
            _pendingUrl = null;

        _loadRoutine = null;
    }

    private void SetFallback()
    {
        if (targetImage != null && fallbackSprite != null)
            targetImage.sprite = fallbackSprite;
    }
}