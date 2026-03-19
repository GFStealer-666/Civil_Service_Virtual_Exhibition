using UnityEngine;

public static class InputModeResolver
{
    public static bool UseMobileInput(bool forceMobileInEditor = false)
    {
        if (forceMobileInEditor)
            return true;

        if (MobileInputState.Instance != null)
            return MobileInputState.Instance.UseMobileInput;

        return Application.isMobilePlatform;
    }

    public static bool UseDesktopInput(bool forceMobileInEditor = false)
    {
        return !UseMobileInput(forceMobileInEditor);
    }
}