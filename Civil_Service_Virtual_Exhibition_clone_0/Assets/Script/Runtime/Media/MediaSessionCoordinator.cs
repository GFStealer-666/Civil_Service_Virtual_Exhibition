using System.Collections.Generic;
using UnityEngine;

public class MediaSessionCoordinator : MonoBehaviour
{
    private readonly List<IMediaControllable> _controllers = new();

    public void Register(IMediaControllable controller)
    {
        if (controller == null || _controllers.Contains(controller))
            return;

        _controllers.Add(controller);
    }

    public void Unregister(IMediaControllable controller)
    {
        if (controller == null)
            return;

        _controllers.Remove(controller);
    }

    public void TakeFocus(IMediaControllable owner)
    {
        for (int i = 0; i < _controllers.Count; i++)
        {
            IMediaControllable controller = _controllers[i];

            if (controller == null || controller == owner)
                continue;

            if (controller.State == MediaPlaybackState.Loading)
            {
                controller.CancelMediaLoading();
                continue;
            }

            if (controller.State != MediaPlaybackState.Idle)
                controller.StopMedia();
        }
    }

    public void StopAll()
    {
        for (int i = 0; i < _controllers.Count; i++)
        {
            IMediaControllable controller = _controllers[i];

            if (controller == null)
                continue;

            if (controller.State == MediaPlaybackState.Loading)
            {
                controller.CancelMediaLoading();
                continue;
            }

            if (controller.State != MediaPlaybackState.Idle)
                controller.StopMedia();
        }
    }
}