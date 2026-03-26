using UnityEngine;
using UnityEngine.Video;

public class VideoPlayerDisableWatcher : MonoBehaviour
{
    [SerializeField] private VideoPlayer target;

    private void Reset()
    {
        if (target == null)
            target = GetComponent<VideoPlayer>();
    }

    private void Awake()
    {
        LogState("Awake");
    }

    private void OnEnable()
    {
        LogState("OnEnable");
    }

    private void Start()
    {
        LogState("Start");
    }

    private void OnDisable()
    {
        LogState("OnDisable");
        Debug.LogError("[VideoPlayerDisableWatcher] Something disabled this component or its GameObject.\n" +
                       UnityEngine.StackTraceUtility.ExtractStackTrace());
    }

    private void Update()
    {
        if (target == null)
            return;

        if (!target.enabled)
            Debug.LogError("[VideoPlayerDisableWatcher] VideoPlayer component checkbox is OFF at runtime.");

        if (!gameObject.activeInHierarchy)
            Debug.LogError("[VideoPlayerDisableWatcher] GameObject is inactive in hierarchy.");
    }

    private void LogState(string phase)
    {
        Debug.Log(
            $"[VideoPlayerDisableWatcher] {phase} | " +
            $"go={gameObject.name} | activeSelf={gameObject.activeSelf} | activeInHierarchy={gameObject.activeInHierarchy} | " +
            $"componentEnabled={(target != null && target.enabled)}"
        );
    }
}