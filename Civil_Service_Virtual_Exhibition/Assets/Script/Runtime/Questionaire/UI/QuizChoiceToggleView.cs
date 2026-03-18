using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuizChoiceToggleView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Toggle toggle;
    [SerializeField] private TMP_Text choiceText;

    private int _choiceIndex;
    private Action<int, bool> _onToggleValueChanged;

    public int ChoiceIndex => _choiceIndex;
    public bool IsOn => toggle != null && toggle.isOn;

    private void Awake()
    {
        if (toggle != null)
        {
            toggle.onValueChanged.AddListener(HandleToggleChanged);
        }
    }

    private void OnDestroy()
    {
        if (toggle != null)
        {
            toggle.onValueChanged.RemoveListener(HandleToggleChanged);
        }
    }

    public void Bind(int choiceIndex, string text, ToggleGroup group, Action<int, bool> onToggleValueChanged)
    {
        _choiceIndex = choiceIndex;
        _onToggleValueChanged = onToggleValueChanged;

        if (choiceText != null)
        {
            choiceText.text = text;
        }

        if (toggle != null)
        {
            toggle.group = group;
            toggle.SetIsOnWithoutNotify(false);
            toggle.interactable = true;
        }
    }

    public void SetInteractable(bool value)
    {
        if (toggle != null)
        {
            toggle.interactable = value;
        }
    }

    public void SetOnWithoutNotify(bool value)
    {
        if (toggle != null)
        {
            toggle.SetIsOnWithoutNotify(value);
        }
    }

    private void HandleToggleChanged(bool isOn)
    {
        _onToggleValueChanged?.Invoke(_choiceIndex, isOn);
    }
}