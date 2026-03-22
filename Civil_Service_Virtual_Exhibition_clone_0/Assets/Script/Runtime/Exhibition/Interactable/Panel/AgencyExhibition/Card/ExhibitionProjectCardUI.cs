using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ExhibitionProjectCardUI : MonoBehaviour
{
    [SerializeField] private Button rootButton;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMP_Text titleText;

    private ExhibitionProjectData _data;
    private Action<ExhibitionProjectData> _onClicked;

    private Coroutine _backgroundLoadRoutine;
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
    }

    public void Bind(ExhibitionProjectData data, Action<ExhibitionProjectData> onClicked)
    {
        _bindVersion++;

        _data = data;
        _onClicked = onClicked;

        if (_backgroundLoadRoutine != null)
        {
            StopCoroutine(_backgroundLoadRoutine);
            _backgroundLoadRoutine = null;
        }

        if (titleText != null)
            titleText.text = data != null ? data.Title : string.Empty;

        if (backgroundImage != null)
        {
            backgroundImage.sprite = data != null ? data.FallbackBackgroundSprite : null;
            backgroundImage.preserveAspect = false;
        }

        if (data == null)
            return;

        int versionAtBind = _bindVersion;

        if (backgroundImage != null && !string.IsNullOrWhiteSpace(data.BackgroundUrl))
            _backgroundLoadRoutine = StartCoroutine(
                LoadSpriteIntoImage(data.BackgroundUrl, backgroundImage, versionAtBind)
            );
    }

    private void HandleClicked()
    {
        if (_data == null)
            return;

        _onClicked?.Invoke(_data);
    }

    private IEnumerator LoadSpriteIntoImage(string url, Image targetImage, int bindVersion)
    {
        if (targetImage == null || string.IsNullOrWhiteSpace(url))
            yield break;

        if (SpriteCache.TryGetValue(url, out Sprite cachedSprite) && cachedSprite != null)
        {
            if (bindVersion == _bindVersion && targetImage != null)
                targetImage.sprite = cachedSprite;

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
            Debug.LogWarning($"[ExhibitionProjectCardUI] Download failed: {url} | {request.error}");
            yield break;
        }

        string contentType = request.GetResponseHeader("Content-Type");

        if (!string.IsNullOrWhiteSpace(contentType) &&
            contentType.IndexOf("image/webp", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            Debug.LogWarning($"[ExhibitionProjectCardUI] WebP is not supported by current loader. Url={url}");
            yield break;
        }

        byte[] bytes = request.downloadHandler.data;
        if (bytes == null || bytes.Length == 0)
        {
            Debug.LogWarning($"[ExhibitionProjectCardUI] Empty image bytes. Url={url}");
            yield break;
        }

        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        bool loaded = texture.LoadImage(bytes, false);

        if (!loaded)
        {
            Debug.LogWarning($"[ExhibitionProjectCardUI] Texture decode failed. Url={url} | content-type={contentType}");
            Destroy(texture);
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
    }
}