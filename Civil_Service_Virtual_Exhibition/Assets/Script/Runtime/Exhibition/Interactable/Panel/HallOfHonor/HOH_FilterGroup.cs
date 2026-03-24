using System;
using UnityEngine;
using UnityEngine.UI;

public enum HOH_FilterOption
{
    All = 0,

    Economy = 1,
    Social = 2,
    Security = 3,
    Infrastructure = 4,

    North = 5,
    Northeast = 6,
    Central = 7,
    South = 8
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

    public event Action<HOH_FilterOption> FilterChanged;

    public HOH_FilterOption CurrentFilter { get; private set; } = HOH_FilterOption.All;

    private bool _initialized;

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
            CurrentFilter = HOH_FilterOption.All;
            return;
        }

        for (int i = 0; i < toggleBindings.Length; i++)
        {
            HOH_FilterToggleBinding binding = toggleBindings[i];
            if (binding == null || binding.toggle == null)
                continue;

            HOH_FilterToggleBinding capturedBinding = binding;
            binding.toggle.onValueChanged.AddListener(isOn => HandleToggleChanged(capturedBinding, isOn));
        }

        RefreshCurrentFilterFromToggles();
    }

    public void Select(HOH_FilterOption option, bool notify = true)
    {
        CurrentFilter = option;

        if (toggleBindings != null)
        {
            for (int i = 0; i < toggleBindings.Length; i++)
            {
                HOH_FilterToggleBinding binding = toggleBindings[i];
                if (binding == null || binding.toggle == null)
                    continue;

                bool isTarget = binding.option == option;
                binding.toggle.SetIsOnWithoutNotify(isTarget);
            }
        }

        if (notify)
            FilterChanged?.Invoke(CurrentFilter);
    }

    public void RefreshCurrentFilterFromToggles()
    {
        if (toggleBindings == null || toggleBindings.Length == 0)
        {
            CurrentFilter = HOH_FilterOption.All;
            return;
        }

        for (int i = 0; i < toggleBindings.Length; i++)
        {
            HOH_FilterToggleBinding binding = toggleBindings[i];
            if (binding == null || binding.toggle == null)
                continue;

            if (binding.toggle.isOn)
            {
                CurrentFilter = binding.option;
                return;
            }
        }

        CurrentFilter = HOH_FilterOption.All;
    }

    private void HandleToggleChanged(HOH_FilterToggleBinding binding, bool isOn)
    {
        if (!isOn || binding == null)
            return;

        CurrentFilter = binding.option;
        FilterChanged?.Invoke(CurrentFilter);
    }
}