using UnityEngine;
using UnityEngine.UI;

public class CheckboxButtonLinker : MonoBehaviour
{
    [SerializeField] private Toggle checkbox;
    [SerializeField] private Button targetButton;

    private void Awake()
    {
        // Set initial state
        targetButton.interactable = checkbox.isOn;
        
        // Listen for changes
        checkbox.onValueChanged.AddListener(OnCheckboxChanged);
    }

    private void OnCheckboxChanged(bool isChecked)
    {
        targetButton.interactable = isChecked;
    }

    private void OnDestroy()
    {
        checkbox.onValueChanged.RemoveListener(OnCheckboxChanged);
    }
}