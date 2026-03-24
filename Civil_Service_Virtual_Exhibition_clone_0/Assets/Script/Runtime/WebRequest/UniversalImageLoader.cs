using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class UniversalImageLoader : MonoBehaviour
{
    [SerializeField] private Image targetImage;
    [SerializeField] private Sprite fallbackSprite;
    [SerializeField] private bool preserveAspect = true;
    [SerializeField] private bool useApiService = true;
    [SerializeField] private bool useCache = true;

    private static readonly Dictionary<string, Sprite> SpriteCache = new();

    private Coroutine _loadRoutine;
    private string _pendingUrl;
    private string _pendingBearerToken;

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

    public void Load(string url, string bearerToken = null)
    {
        _pendingUrl = url;
        _pendingBearerToken = bearerToken;

        if (targetImage == null)
        {
            Debug.LogWarning("[UniversalImageLoader] Target Image is null.");
            return;
        }

        if (_loadRoutine != null)
        {
            StopCoroutine(_loadRoutine);
            _loadRoutine = null;
        }

        if (string.IsNullOrWhiteSpace(url))
        {
            ApplyFallback();
            return;
        }

        if (useCache && SpriteCache.TryGetValue(url, out Sprite cachedSprite) && cachedSprite != null)
        {
            ApplySprite(cachedSprite);
            _pendingUrl = null;
            _pendingBearerToken = null;
            return;
        }

        ApplyFallback();
        TryStartLoad();
    }

    public void ClearImage()
    {
        _pendingUrl = null;
        _pendingBearerToken = null;

        if (_loadRoutine != null)
        {
            StopCoroutine(_loadRoutine);
            _loadRoutine = null;
        }

        ApplyFallback();
    }

    public static void ClearCache()
    {
        SpriteCache.Clear();
    }

    private void TryStartLoad()
    {
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
            return;

        if (targetImage == null)
        {
            Debug.LogWarning("[UniversalImageLoader] Cannot start load because targetImage is null.");
            return;
        }

        if (string.IsNullOrWhiteSpace(_pendingUrl))
            return;

        if (useCache && SpriteCache.TryGetValue(_pendingUrl, out Sprite cachedSprite) && cachedSprite != null)
        {
            ApplySprite(cachedSprite);
            _pendingUrl = null;
            _pendingBearerToken = null;
            return;
        }

        if (_loadRoutine != null)
        {
            StopCoroutine(_loadRoutine);
            _loadRoutine = null;
        }

        _loadRoutine = StartCoroutine(LoadRoutine(_pendingUrl, _pendingBearerToken));
    }

    private IEnumerator LoadRoutine(string url, string bearerToken)
    {
        UnityWebRequest request = null;

        if (useApiService && ApiService.Instance != null)
            request = ApiService.Instance.GetTexture(url, bearerToken);
        else
            request = UnityWebRequestTexture.GetTexture(url);

        yield return request.SendWebRequest();

        _loadRoutine = null;

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[UniversalImageLoader] Failed to load image: {url} | {request.error}");

            if (request.downloadHandler != null && !string.IsNullOrEmpty(request.downloadHandler.error))
                Debug.LogWarning($"[UniversalImageLoader] DownloadHandler error: {request.downloadHandler.error}");

            request.Dispose();
            yield break;
        }

        Texture2D texture = null;

        try
        {
            texture = DownloadHandlerTexture.GetContent(request);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[UniversalImageLoader] Texture decode failed: {url} | {e.Message}");
        }

        request.Dispose();

        if (texture == null)
        {
            Debug.LogWarning($"[UniversalImageLoader] Texture is null: {url}");
            yield break;
        }

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f)
        );

        if (useCache && !SpriteCache.ContainsKey(url))
            SpriteCache.Add(url, sprite);

        // กันกรณีระหว่างโหลดมีการสั่งโหลด URL ใหม่
        if (_pendingUrl != url)
            yield break;

        ApplySprite(sprite);
        _pendingUrl = null;
        _pendingBearerToken = null;
    }

    private void ApplyFallback()
    {
        if (targetImage == null)
            return;

        if (fallbackSprite != null)
            targetImage.sprite = fallbackSprite;
        else
            targetImage.sprite = null;

        targetImage.preserveAspect = preserveAspect;
    }

    private void ApplySprite(Sprite sprite)
    {
        if (targetImage == null || sprite == null)
            return;

        targetImage.sprite = sprite;
        targetImage.preserveAspect = preserveAspect;
    }
}