using System.Threading.Tasks;
using UnityEngine;

public class SimpleInteractable : WorldInteractable
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

    private void Start()
    {
        if (canvas != null)
            _canvasStartLocalPos = canvas.transform.localPosition;
    }

    public override bool CanInteract(GameObject interactor)
    {
        if (objectToShow == null || interactor == null)
            return false;

        if (!LocalPlayerResolver.IsLocalPlayer(interactor))
            return false;

        return !objectToShow.activeSelf;
    }

    public override Task InteractAsync(GameObject interactor)
    {
        if (!CanInteract(interactor))
            return Task.CompletedTask;

        objectToShow.SetActive(true);
        return Task.CompletedTask;
    }

    private void Update()
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