using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class AE_CardUI : MonoBehaviour
{
    [SerializeField] private Button rootButton;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image logoImage;
    [SerializeField] private TMP_Text titleText;

    [Header("Fallback Colors")]
    [SerializeField] private Color backgroundFallbackColor = new Color(219f / 255f, 219f / 255f, 219f / 255f, 1f);
    [SerializeField] private Color logoFallbackColor = new Color(1f, 1f, 1f, 1f);

    private ExhibitionAgencyData _data;
    private Action<ExhibitionAgencyData> _onClicked;

    private Coroutine _backgroundLoadRoutine;
    private Coroutine _logoLoadRoutine;
    private int _bindVersion;

    private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

    private void Awake()
    {
        if (rootButton != null)
            rootButton.onClick.AddListener(HandleClicked);
    }

    private void OnDestroy()
    {
        if (rootButton != null)
            rootButton.onClick.RemoveListener(HandleClicked);

        if (_backgroundLoadRoutine != null)
            StopCoroutine(_backgroundLoadRoutine);

        if (_logoLoadRoutine != null)
            StopCoroutine(_logoLoadRoutine);
    }

    public void Bind(ExhibitionAgencyData data, Action<ExhibitionAgencyData> onClicked)
    {
        _bindVersion++;

        _data = data;
        _onClicked = onClicked;

        if (_backgroundLoadRoutine != null)
        {
            StopCoroutine(_backgroundLoadRoutine);
            _backgroundLoadRoutine = null;
        }

        if (_logoLoadRoutine != null)
        {
            StopCoroutine(_logoLoadRoutine);
            _logoLoadRoutine = null;
        }

        if (titleText != null)
            titleText.text = data != null ? data.Title : string.Empty;

        if (backgroundImage != null)
        {
           backgroundImage.sprite = data != null ? data.FallbackBackgroundSprite : null;
            
            backgroundImage.color = backgroundImage.sprite != null ? Color.white : backgroundFallbackColor;
        
            backgroundImage.preserveAspect = false;
        }

        if (logoImage != null)
        {
            logoImage.sprite = data != null ? data.FallbackLogoSprite : null;
            logoImage.color = logoImage.sprite != null ? Color.white : logoFallbackColor;
            logoImage.preserveAspect = true;
        }

        if (data == null)
            return;

        int versionAtBind = _bindVersion;

        if (backgroundImage != null && !string.IsNullOrWhiteSpace(data.BackgroundUrl))
            _backgroundLoadRoutine = StartCoroutine(LoadSpriteIntoImage(data.BackgroundUrl, backgroundImage, true, versionAtBind));

        if (logoImage != null && !string.IsNullOrWhiteSpace(data.LogoUrl))
            _logoLoadRoutine = StartCoroutine(LoadSpriteIntoImage(data.LogoUrl, logoImage, false, versionAtBind));
    }

    private void HandleClicked()
    {
        if (_data == null)
            return;

        _onClicked?.Invoke(_data);
    }

    private IEnumerator LoadSpriteIntoImage(string url, Image targetImage, bool isBackground, int bindVersion)
    {
        if (targetImage == null || string.IsNullOrWhiteSpace(url))
            yield break;

        if (SpriteCache.TryGetValue(url, out Sprite cachedSprite) && cachedSprite != null)
        {
            if (bindVersion == _bindVersion && targetImage != null)
            {
                targetImage.sprite = cachedSprite;
                targetImage.preserveAspect = !isBackground;
            }
            //_CheckImage();
            yield break;
        }

        using UnityWebRequest request = UnityWebRequest.Get(url);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Accept", "image/png,image/jpeg,image/*,*/*");

        yield return request.SendWebRequest();

        if (bindVersion != _bindVersion)
            yield break;

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[ExhibitionAgencyCardUI] Download failed: {url} | {request.error}");
            if (bindVersion == _bindVersion && targetImage != null)
                targetImage.color = isBackground ? backgroundFallbackColor : logoFallbackColor;
                //_CheckImage();
            yield break;
        }

        string contentType = request.GetResponseHeader("Content-Type");

        if (!string.IsNullOrWhiteSpace(contentType) &&
            contentType.IndexOf("image/webp", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            Debug.LogWarning($"[ExhibitionAgencyCardUI] WebP is not supported by current loader. Url={url}");
            if (bindVersion == _bindVersion && targetImage != null)
                targetImage.color = isBackground ? backgroundFallbackColor : logoFallbackColor;
                //_CheckImage();
            yield break;
        }

        byte[] bytes = request.downloadHandler.data;
        if (bytes == null || bytes.Length == 0)
        {
            Debug.LogWarning($"[ExhibitionAgencyCardUI] Empty image bytes. Url={url}");
            if (bindVersion == _bindVersion && targetImage != null)
                targetImage.color = isBackground ? backgroundFallbackColor : logoFallbackColor;
                //_CheckImage();
            yield break;
        }

        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        bool loaded = texture.LoadImage(bytes, false);

        if (!loaded)
        {
            Debug.LogWarning($"[ExhibitionAgencyCardUI] Texture decode failed. Url={url} | content-type={contentType}");
            Destroy(texture);
            if (bindVersion == _bindVersion && targetImage != null)
                targetImage.color = isBackground ? backgroundFallbackColor : logoFallbackColor;
                //_CheckImage();
            yield break;
        }

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f)
        );

        SpriteCache[url] = sprite;

        if (bindVersion != _bindVersion || targetImage == null)
            yield break;

        targetImage.sprite = sprite;
        targetImage.color = Color.white;
        targetImage.preserveAspect = !isBackground;

        DynamicCardImageFit fitter = targetImage.GetComponent<DynamicCardImageFit>();
        if (fitter != null)
            fitter.Refresh();
    }

    public void _CheckImage()
    {
        if (backgroundImage.color != Color.white)
        {
            backgroundImage.gameObject.SetActive(false);
        }
        else
        {
            backgroundImage.gameObject.SetActive(true);
        }

    }
}