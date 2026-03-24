using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class QuizSubmitMockSender : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private QuizRepository repository;

    [Header("Mock Data")]
    [SerializeField] private int mockScore = 85;

    [Header("Token")]
    [SerializeField] private bool useStoredToken = true;
    [TextArea]
    [SerializeField] private string manualToken = "";

    [Header("Debug")]
    [SerializeField] private bool autoSubmitOnStart = false;
    [SerializeField] private bool logRawResponse = true;

    private void Start()
    {
        if (autoSubmitOnStart)
            SubmitMockScore();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
            SubmitMockScore();
    }

    [ContextMenu("Submit Mock Score")]
    public void SubmitMockScore()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[QuizSubmitMockSender] Enter Play Mode first.");
            return;
        }

        if (repository == null)
        {
            Debug.LogError("[QuizSubmitMockSender] Repository is not assigned.");
            return;
        }

        StartCoroutine(SubmitRoutine());
    }

    private IEnumerator SubmitRoutine()
    {
        string token = useStoredToken
            ? (LocalPlayerData.Instance != null ? LocalPlayerData.Instance.PlayerToken : null)
            : manualToken;

        Debug.Log(
            $"[QuizSubmitMockSender] Sending mock score | score={mockScore} | hasToken={!string.IsNullOrWhiteSpace(token)}"
        );

        yield return repository.SubmitResult(
            mockScore,
            HandleSubmitCompleted,
            token,
            true
        );
    }

    private void HandleSubmitCompleted(QuizSubmitOperationResult result)
    {
        if (result == null)
        {
            Debug.LogError("[QuizSubmitMockSender] Result is null.");
            return;
        }

        if (result.success)
        {
            Debug.Log(
                $"[QuizSubmitMockSender] Submit success | code={result.statusCode} | message={result.message}"
            );
        }
        else
        {
            Debug.LogError(
                $"[QuizSubmitMockSender] Submit failed | type={result.errorType} | code={result.statusCode} | message={result.message}"
            );
        }

        if (logRawResponse && !string.IsNullOrWhiteSpace(result.rawBody))
        {
            Debug.Log($"[QuizSubmitMockSender] Raw response: {result.rawBody}");
        }
    }
}