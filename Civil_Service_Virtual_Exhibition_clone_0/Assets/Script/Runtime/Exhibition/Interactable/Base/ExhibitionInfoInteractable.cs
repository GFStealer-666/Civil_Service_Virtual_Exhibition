using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class ExhibitionInfoInteractable : WorldInteractable
{
    [Header("Ministry")]
    [SerializeField] private string ministryKey;

    [Header("NameTag")]
    [SerializeField] private GameObject canvas;

    [Header("UI")]
    [SerializeField] private AE_MainPanelController panelController;
    [SerializeField] private TextMeshProUGUI ministryName;

    [Header("Floating")]
    [SerializeField] private float floatMinY = 0.2f;
    [SerializeField] private float floatMaxY = 0.4f;
    [SerializeField] private float floatSpeed = 1.2f;

    private Vector3 _canvasStartLocalPos;
    private bool _ministryNameInitialized;

    private void Start()
    {
        if (canvas != null)
        {
            _canvasStartLocalPos = canvas.transform.localPosition;
        }

        TryInitializeMinistryName();
    }

    public override bool CanInteract(GameObject interactor)
    {
        return panelController != null && !panelController.IsOpen;
    }

    public override Task InteractAsync(GameObject interactor)
    {
        panelController.Open(ministryKey);
        return Task.CompletedTask;
    }

    private void Update()
    {
        if (!_ministryNameInitialized)
        {
            TryInitializeMinistryName();
        }

        if (canvas == null)
            return;

        float centerY = (floatMinY + floatMaxY) * 0.5f;
        float amplitude = (floatMaxY - floatMinY) * 0.5f;
        float yOffset = Mathf.Sin(Time.time * floatSpeed) * amplitude;

        Vector3 pos = _canvasStartLocalPos;
        pos.y = centerY + yOffset;
        canvas.transform.localPosition = pos;
    }

    private void TryInitializeMinistryName()
    {
        if (_ministryNameInitialized)
            return;

        if (ministryName == null)
            return;

        if (string.IsNullOrWhiteSpace(ministryKey))
            return;

        AE_CatalogRepository store = AE_CatalogRepository.Instance;
        if (store == null || !store.HasData)
            return;

        GovernmentMinistryDto ministry = store.GetMinistryById(ministryKey);

        // If ministryKey is not runtimeId, use this instead:
        // GovernmentMinistryDto ministry = store.FindMinistry(ministryKey);

        if (ministry == null)
        {
            Debug.LogWarning($"[ExhibitionInfoInteractable] Ministry not found for key: {ministryKey}");
            return;
        }

        ministryName.text = ministry.ministry;
        _ministryNameInitialized = true;
    }
}