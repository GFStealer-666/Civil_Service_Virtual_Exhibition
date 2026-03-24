// using System;
// using System.Collections;
// using UnityEngine;
// using UnityEngine.Networking;

// public class GovernmentCatalogDownloader : MonoBehaviour
// {
//     public static GovernmentCatalogDownloader Instance { get; private set; }

//     public bool IsDownloading { get; private set; }
//     public string LastError { get; private set; }

//     private Coroutine _downloadRoutine;

//     private ApiService Api => ApiService.Instance;

//     private void Awake()
//     {
//         if (Instance != null && Instance != this)
//         {
//             Destroy(gameObject);
//             return;
//         }

//         Instance = this;
//         DontDestroyOnLoad(gameObject);
//     }

//     private void Start()
//     {
//         Download(
//             dto =>
//             {
//                 GovernmentCatalogStore.EnsureExists().SetData(dto);
//             },
//             error =>
//             {
//                 Debug.LogError($"[GovernmentCatalogDownloader] {error}");
//             }
//         );
//     }

//     public static GovernmentCatalogDownloader EnsureExists()
//     {
//         if (Instance != null)
//             return Instance;

//         GameObject go = new GameObject(nameof(GovernmentCatalogDownloader));
//         return go.AddComponent<GovernmentCatalogDownloader>();
//     }

//     public void Download(
//         Action<GovernmentCatalogResponseDto> onSuccess,
//         Action<string> onFail,
//         string accessToken = null)
//     {
//         if (Api == null)
//         {
//             onFail?.Invoke("ApiService.Instance is null.");
//             return;
//         }

//         if (string.IsNullOrWhiteSpace(Api.GovernmentCatalogUrl))
//         {
//             onFail?.Invoke("GovernmentCatalogUrl is empty.");
//             return;
//         }

//         Debug.Log($"[GovernmentCatalogDownloader] Downloading from: {Api.GovernmentCatalogUrl}");

//         if (_downloadRoutine != null)
//             StopCoroutine(_downloadRoutine);

//         _downloadRoutine = StartCoroutine(
//             DownloadRoutine(Api.GovernmentCatalogUrl, accessToken, onSuccess, onFail)
//         );
//     }

//     private IEnumerator DownloadRoutine(
//         string url,
//         string accessToken,
//         Action<GovernmentCatalogResponseDto> onSuccess,
//         Action<string> onFail)
//     {
//         Debug.Log("[GovernmentCatalogDownloader] Catalog download started.");

//         IsDownloading = true;
//         LastError = null;

//         using UnityWebRequest request = Api.Get(url, accessToken);
//         yield return request.SendWebRequest();

//         IsDownloading = false;
//         _downloadRoutine = null;

//         if (request.result != UnityWebRequest.Result.Success)
//         {
//             LastError = $"Download failed: {request.error}";
//             onFail?.Invoke(LastError);
//             yield break;
//         }

//         GovernmentCatalogResponseDto dto;

//         try
//         {
//             dto = JsonUtility.FromJson<GovernmentCatalogResponseDto>(request.downloadHandler.text);
//         }
//         catch (Exception ex)
//         {
//             LastError = $"Parse failed: {ex.Message}";
//             onFail?.Invoke(LastError);
//             yield break;
//         }

//         if (dto == null)
//         {
//             LastError = "Parsed dto is null.";
//             onFail?.Invoke(LastError);
//             yield break;
//         }

//         onSuccess?.Invoke(dto);
//     }
// }