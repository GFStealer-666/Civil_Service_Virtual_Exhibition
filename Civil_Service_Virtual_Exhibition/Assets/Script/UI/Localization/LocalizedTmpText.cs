using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Text))]
public class LocalizedTmpText : MonoBehaviour
{
    [Header("Localization")]
    [SerializeField] private LocalizedString localizedString;

    private TMP_Text _tmpText;

    public LocalizedString LocalizedString => localizedString;

    private void Awake()
    {
        CacheReferences();
    }

    private void OnEnable()
    {
        CacheReferences();
        localizedString.StringChanged += HandleStringChanged;
        RefreshNow();
    }

    private void OnDisable()
    {
        localizedString.StringChanged -= HandleStringChanged;
    }

    private void Reset()
    {
        CacheReferences();
    }

    private void OnValidate()
    {
        CacheReferences();
    }

    private void CacheReferences()
    {
        if (_tmpText == null)
            _tmpText = GetComponent<TMP_Text>();
    }

    private void HandleStringChanged(string value)
    {
        if (_tmpText != null)
            _tmpText.text = value;
    }

    public void RefreshNow()
    {
        if (_tmpText == null)
            return;

        localizedString.RefreshString();
    }

    public void SetReference(string tableCollection, string entryKey)
    {
        localizedString.TableReference = tableCollection;
        localizedString.TableEntryReference = entryKey;
        RefreshNow();
    }
}