using TMPro;
using UnityEngine;

public class PlayerNameTag : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerProfile profile;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Transform nameTagAnchor;

    private Camera _mainCamera;
    private string _lastName = "";

    private void LateUpdate()
    {
        UpdateCameraReference();
        UpdateBillboard();
        UpdateNameText();
    }

    private void UpdateCameraReference()
    {
        if (_mainCamera == null)
            _mainCamera = Camera.main;
    }

    private void UpdateBillboard()
    {
        if (_mainCamera == null || nameTagAnchor == null)
        {
            return;
        }
           

        Vector3 dir = nameTagAnchor.position - _mainCamera.transform.position;
        nameTagAnchor.forward = dir.normalized;
    }

    private void UpdateNameText()
    {
        if (profile == null || nameText == null)
        {
            return;
        }
            

        string targetName = profile.ProfileReady
            ? profile.PlayerName
            : "Loading...";

        if (_lastName == targetName)
        {
            return;
        }
            

        _lastName = targetName;
        nameText.text = targetName;
    }
}