using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class PSC_FilterToggleBinding
{
    public PSC_MinistryCategory option;
    public Toggle toggle;
}

public class PSC_FilterGroup : MonoBehaviour
{
    [SerializeField] private PSC_FilterToggleBinding[] toggleBindings;

    public event Action<IReadOnlyCollection<PSC_MinistryCategory>> FiltersChanged;

    private readonly HashSet<PSC_MinistryCategory> _currentFilters = new();
    private bool _initialized;
    private bool _isApplying;

    public IReadOnlyCollection<PSC_MinistryCategory> CurrentFilters => _currentFilters;
    public bool IsAllSelected => _currentFilters.Count == 0;

    private void Awake()
    {
        Initialize();
    }

    public void Initialize()
    {
        if (_initialized)
            return;

        _initialized = true;

        if (toggleBindings == null || toggleBindings.Length == 0)
        {
            SelectAll(false);
            return;
        }

        for (int i = 0; i < toggleBindings.Length; i++)
        {
            PSC_FilterToggleBinding binding = toggleBindings[i];
            if (binding == null || binding.toggle == null)
                continue;

            PSC_FilterToggleBinding captured = binding;
            binding.toggle.onValueChanged.AddListener(isOn => HandleToggleChanged(captured, isOn));
        }

        RefreshFromToggles(false);
    }

    public bool IsSelected(PSC_MinistryCategory option)
    {
        if (option == PSC_MinistryCategory.All)
            return IsAllSelected;

        return _currentFilters.Contains(option);
    }

    public void SelectAll(bool notify = true)
    {
        _isApplying = true;
        _currentFilters.Clear();

        if (toggleBindings != null)
        {
            for (int i = 0; i < toggleBindings.Length; i++)
            {
                PSC_FilterToggleBinding binding = toggleBindings[i];
                if (binding == null || binding.toggle == null)
                    continue;

                bool shouldBeOn = binding.option == PSC_MinistryCategory.All;
                binding.toggle.SetIsOnWithoutNotify(shouldBeOn);
            }
        }

        _isApplying = false;

        if (notify)
            RaiseChanged();
    }

    public void RefreshFromToggles(bool notify = true)
    {
        _currentFilters.Clear();

        if (toggleBindings == null || toggleBindings.Length == 0)
        {
            if (notify)
                RaiseChanged();

            return;
        }

        bool allIsOn = false;

        for (int i = 0; i < toggleBindings.Length; i++)
        {
            PSC_FilterToggleBinding binding = toggleBindings[i];
            if (binding == null || binding.toggle == null || !binding.toggle.isOn)
                continue;

            if (binding.option == PSC_MinistryCategory.All)
            {
                allIsOn = true;
            }
            else
            {
                _currentFilters.Add(binding.option);
            }
        }

        if (allIsOn || _currentFilters.Count == 0)
        {
            SelectAll(notify);
            return;
        }

        SetToggleWithoutNotify(PSC_MinistryCategory.All, false);

        if (notify)
            RaiseChanged();
    }

    private void HandleToggleChanged(PSC_FilterToggleBinding binding, bool isOn)
    {
        if (_isApplying || binding == null || binding.toggle == null)
            return;

        if (binding.option == PSC_MinistryCategory.All)
        {
            if (isOn)
            {
                SelectAll(true);
            }
            else if (_currentFilters.Count == 0)
            {
                SetToggleWithoutNotify(PSC_MinistryCategory.All, true);
            }

            return;
        }

        if (isOn)
        {
            _currentFilters.Add(binding.option);
            SetToggleWithoutNotify(PSC_MinistryCategory.All, false);
        }
        else
        {
            _currentFilters.Remove(binding.option);

            if (_currentFilters.Count == 0)
            {
                SelectAll(true);
                return;
            }
        }

        RaiseChanged();
    }

    private void SetToggleWithoutNotify(PSC_MinistryCategory option, bool value)
    {
        Toggle toggle = GetToggle(option);
        if (toggle == null)
            return;

        _isApplying = true;
        toggle.SetIsOnWithoutNotify(value);
        _isApplying = false;
    }

    private Toggle GetToggle(PSC_MinistryCategory option)
    {
        if (toggleBindings == null)
            return null;

        for (int i = 0; i < toggleBindings.Length; i++)
        {
            PSC_FilterToggleBinding binding = toggleBindings[i];
            if (binding == null || binding.toggle == null)
                continue;

            if (binding.option == option)
                return binding.toggle;
        }

        return null;
    }

    private void RaiseChanged()
    {
        FiltersChanged?.Invoke(new List<PSC_MinistryCategory>(_currentFilters));
    }
}