using System.Threading.Tasks;
using UnityEngine;

public class QuizInteractable : WorldInteractable
{
    [Header("NameTag")]
    [SerializeField] private GameObject canvas;

    [Header("UI")]
    [SerializeField] private GameObject objectToShow;

    [Header("Floating")]
    [SerializeField] private float floatMinY = 0.2f;
    [SerializeField] private float floatMaxY = 0.4f;
    [SerializeField] private float floatSpeed = 1.2f;

    private Vector3 _canvasStartLocalPos;
    private bool _isQuizActive = false;

    void Start()
    {
        
        if (canvas != null)
        {
            _canvasStartLocalPos = canvas.transform.localPosition;
        }
    }

    public override bool CanInteract(GameObject interactor)
    {
        return objectToShow != null && !objectToShow.activeSelf && !_isQuizActive;
    }

    public override Task InteractAsync(GameObject interactor)
    {
        _isQuizActive = true;
        objectToShow.SetActive(true);
        return Task.CompletedTask;
    }

    public void OnQuizFinished()
    {
        _isQuizActive = false;
    }

    void Update()
    {
                if (canvas == null)
            return;

        float centerY = (floatMinY + floatMaxY) * 0.5f;
        float amplitude = (floatMaxY - floatMinY) * 0.5f;
        float yOffset = Mathf.Sin(Time.time * floatSpeed) * amplitude;

        Vector3 pos = _canvasStartLocalPos;
        pos.y = centerY + yOffset;
        canvas.transform.localPosition = pos;
    }
}
