using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PS_ServiceRepository : MonoBehaviour
{
    public static PS_ServiceRepository Instance { get; private set; }

    [Header("Load")]
    [SerializeField] private bool preloadOnStart = true;
    [SerializeField] private bool dontDestroyOnLoad = true;
    public bool IsLoading { get; private set; }
    public bool HasData => _cachedResponse != null && _cachedResponse.data != null;
    public string LastError { get; private set; }

    public event Action OnDataLoaded;
    public event Action<string> OnDataLoadFailed;

    private PS_ServiceResponseDto _cachedResponse;
    private Coroutine _loadRoutine;

    private readonly List<PS_ServiceActivityDto> _resultBuffer = new List<PS_ServiceActivityDto>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (preloadOnStart)
            Initialize();
    }

    public void Initialize(bool forceRefresh = false)
    {
        if (IsLoading)
            return;

        if (!forceRefresh && HasData)
        {
            OnDataLoaded?.Invoke();
            return;
        }

        if (_loadRoutine != null)
            StopCoroutine(_loadRoutine);

        _loadRoutine = StartCoroutine(DownloadRoutine());
    }

    public void Refresh()
    {
        Initialize(true);
    }

    public PS_ServiceResponseDto GetRawResponse()
    {
        return _cachedResponse;
    }

    public string GetCategoryDisplayName(string categoryKey, bool useEnglish)
    {
        PS_ServiceCategoryDto category = FindCategory(categoryKey);
        if (category == null)
            return string.IsNullOrWhiteSpace(categoryKey) ? "-" : categoryKey;

        string displayName = useEnglish ? category.categoryEn : category.category;
        return string.IsNullOrWhiteSpace(displayName) ? categoryKey : displayName;
    }

    public int GetCategoryTotalCount(string categoryKey)
    {
        PS_ServiceCategoryDto category = FindCategory(categoryKey);
        if (category == null || category.activities == null)
            return 0;

        return category.activities.Length;
    }

    public List<PS_ServiceActivityDto> GetActivitiesByCategory(string categoryKey, string keyword = null)
    {
        _resultBuffer.Clear();

        PS_ServiceCategoryDto category = FindCategory(categoryKey);
        if (category == null || category.activities == null || category.activities.Length == 0)
            return new List<PS_ServiceActivityDto>();

        for (int i = 0; i < category.activities.Length; i++)
        {
            PS_ServiceActivityDto item = category.activities[i];
            if (item == null)
                continue;

            if (!PassSearch(item, keyword))
                continue;

            _resultBuffer.Add(item);
        }

        return new List<PS_ServiceActivityDto>(_resultBuffer);
    }

    private IEnumerator DownloadRoutine()
    {
        IsLoading = true;
        LastError = string.Empty;

        if (ApiService.Instance == null)
        {
            HandleFailed("ApiService.Instance is missing.");
            yield break;
        }

        if (string.IsNullOrWhiteSpace(ApiService.Instance.PublicServiceUrl))
        {
            HandleFailed("PublicServiceUrl is missing in ApiConfig.");
            yield break;
        }

        UnityWebRequest request = ApiService.Instance.Get(ApiService.Instance.PublicServiceUrl);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            HandleFailed(request.error);
            request.Dispose();
            yield break;
        }

        string json = request.downloadHandler.text;
        request.Dispose();

        PS_ServiceResponseDto dto = null;

        try
        {
            dto = JsonUtility.FromJson<PS_ServiceResponseDto>(json);
        }
        catch (Exception e)
        {
            HandleFailed($"JSON parse failed: {e.Message}");
            yield break;
        }

        HandleSuccess(dto);
        Debug.Log($"[PS_Repository] Downloaded success");
        IsLoading = false;
        _loadRoutine = null;
    }

    private void HandleSuccess(PS_ServiceResponseDto response)
    {
        if (response == null)
        {
            HandleFailed("Public service response is null.");
            return;
        }

        if (!response.success)
        {
            HandleFailed("Public service API returned success = false.");
            return;
        }

        _cachedResponse = response;
        LastError = string.Empty;
        OnDataLoaded?.Invoke();
    }

    private void HandleFailed(string error)
    {
        IsLoading = false;
        _cachedResponse = null;
        LastError = string.IsNullOrWhiteSpace(error) ? "Load failed." : error;
        OnDataLoadFailed?.Invoke(LastError);
    }

    private PS_ServiceCategoryDto FindCategory(string categoryKey)
    {
        if (_cachedResponse == null || _cachedResponse.data == null || _cachedResponse.data.Length == 0)
            return null;

        if (string.IsNullOrWhiteSpace(categoryKey))
            return null;

        for (int i = 0; i < _cachedResponse.data.Length; i++)
        {
            PS_ServiceCategoryDto category = _cachedResponse.data[i];
            if (category == null)
                continue;

            if (IsSameCategory(category.category, categoryKey) || IsSameCategory(category.categoryEn, categoryKey))
                return category;
        }

        return null;
    }

    private bool IsSameCategory(string left, string right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
            return false;

        return string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private bool PassSearch(PS_ServiceActivityDto item, string keyword)
    {
        if (item == null)
            return false;

        if (string.IsNullOrWhiteSpace(keyword))
            return true;

        string text = keyword.Trim();

        return Contains(item.activityName, text)
            || Contains(item.activityNameEn, text)
            || Contains(item.department, text)
            || Contains(item.departmentEn, text)
            || Contains(item.activityDate, text)
            || Contains(item.activityDateEn, text)
            || Contains(item.category, text)
            || Contains(item.categoryEn, text);
    }

    private bool Contains(string source, string keyword)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(keyword))
            return false;

        return source.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}