using System.Threading.Tasks;
using UnityEngine;

public class HOH_ZoneInteractable : WorldInteractable
{
    [Header("NameTag")]
    [SerializeField] private GameObject canvas;

    [Header("HOH")]
    [SerializeField] private HOH_PanelController panelController;
    [SerializeField] private HOH_CategoryKind categoryToOpen = HOH_CategoryKind.Ministry;

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
        return panelController != null;
    }

    public override Task InteractAsync(GameObject interactor)
    {
        panelController.Open(categoryToOpen);
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