using System.Collections;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Dropdown))]
public class DropdownHeightFix : MonoBehaviour
{
    [SerializeField] private float itemHeight = 57f;
    private TMP_Dropdown dropdown;

    private void Awake()
    {
        dropdown = GetComponent<TMP_Dropdown>();
        dropdown.onValueChanged.AddListener(_ => StartCoroutine(FixHeight()));
    }

    // Call this from the dropdown's Button OnClick in Inspector
    public void OnDropdownOpened()
    {
        StartCoroutine(FixHeight());
    }

    private IEnumerator FixHeight()
    {
        yield return null; // wait one frame for Dropdown List to spawn

        Transform list = dropdown.transform.Find("Dropdown List");
        if (list == null) yield break;

        // Force correct height based on real option count (minus placeholder)
        int realCount = dropdown.options.Count - 1;
        float targetHeight = itemHeight * realCount;

        RectTransform listRT = list.GetComponent<RectTransform>();
        listRT.sizeDelta = new Vector2(listRT.sizeDelta.x, targetHeight);
    }
}