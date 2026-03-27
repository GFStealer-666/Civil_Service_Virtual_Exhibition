using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum HOH_FilterOption
{
    All = 0,

    Economy = 1,
    Social = 2,
    Security = 3,
    Infrastructure = 4,
    Service = 5,

    North = 6,
    Northeast = 7,
    Central = 8,
    East = 9,
    West = 10,
    South = 11
}

[Serializable]
public class HOH_FilterToggleBinding
{
    public HOH_FilterOption option;
    public Toggle toggle;
}

public class HOH_FilterGroup : MonoBehaviour
{
    [SerializeField] private HOH_FilterToggleBinding[] toggleBindings;

    public event Action<IReadOnlyCollection<HOH_FilterOption>> FiltersChanged;

    private readonly HashSet<HOH_FilterOption> _currentFilters = new();
    private bool _initialized;
    private bool _isApplying;

    public IReadOnlyCollection<HOH_FilterOption> CurrentFilters => _currentFilters;
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
            HOH_FilterToggleBinding binding = toggleBindings[i];
            if (binding == null || binding.toggle == null)
                continue;

            HOH_FilterToggleBinding captured = binding;
            binding.toggle.onValueChanged.AddListener(isOn => HandleToggleChanged(captured, isOn));
        }

        RefreshFromToggles(false);
    }

    public bool IsSelected(HOH_FilterOption option)
    {
        if (option == HOH_FilterOption.All)
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
                HOH_FilterToggleBinding binding = toggleBindings[i];
                if (binding == null || binding.toggle == null)
                    continue;

                bool shouldBeOn = binding.option == HOH_FilterOption.All;
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
            HOH_FilterToggleBinding binding = toggleBindings[i];
            if (binding == null || binding.toggle == null || !binding.toggle.isOn)
                continue;

            if (binding.option == HOH_FilterOption.All)
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

        SetToggleWithoutNotify(HOH_FilterOption.All, false);

        if (notify)
            RaiseChanged();
    }

    private void HandleToggleChanged(HOH_FilterToggleBinding binding, bool isOn)
    {
        if (_isApplying || binding == null || binding.toggle == null)
            return;

        if (binding.option == HOH_FilterOption.All)
        {
            if (isOn)
            {
                SelectAll(true);
            }
            else if (_currentFilters.Count == 0)
            {
                SetToggleWithoutNotify(HOH_FilterOption.All, true);
            }

            return;
        }

        if (isOn)
        {
            _currentFilters.Add(binding.option);
            SetToggleWithoutNotify(HOH_FilterOption.All, false);
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

    private void SetToggleWithoutNotify(HOH_FilterOption option, bool value)
    {
        Toggle toggle = GetToggle(option);
        if (toggle == null)
            return;

        _isApplying = true;
        toggle.SetIsOnWithoutNotify(value);
        _isApplying = false;
    }

    private Toggle GetToggle(HOH_FilterOption option)
    {
        if (toggleBindings == null)
            return null;

        for (int i = 0; i < toggleBindings.Length; i++)
        {
            HOH_FilterToggleBinding binding = toggleBindings[i];
            if (binding == null || binding.toggle == null)
                continue;

            if (binding.option == option)
                return binding.toggle;
        }

        return null;
    }

    private void RaiseChanged()
    {
        FiltersChanged?.Invoke(new List<HOH_FilterOption>(_currentFilters));
    }
}