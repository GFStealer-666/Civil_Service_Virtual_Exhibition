using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class MobileCameraGestureArea : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private float lookMultiplier = 1f;
    [SerializeField] private float pinchZoomMultiplier = 0.01f;

    private readonly Dictionary<int, Vector2> _positions = new Dictionary<int, Vector2>();

    public void OnPointerDown(PointerEventData eventData)
    {
        _positions[eventData.pointerId] = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        _positions[eventData.pointerId] = eventData.position;

        if (_positions.Count == 1)
        {
            MobileInputState.Instance?.AddLookDelta(eventData.delta * lookMultiplier);
            return;
        }

        if (_positions.Count >= 2)
        {
            int count = 0;
            Vector2[] current = new Vector2[2];
            Vector2[] previous = new Vector2[2];

            foreach (var kvp in _positions)
            {
                current[count] = kvp.Value;

                if (kvp.Key == eventData.pointerId)
                    previous[count] = kvp.Value - eventData.delta;
                else
                    previous[count] = kvp.Value;

                count++;
                if (count >= 2)
                    break;
            }

            float prevDistance = Vector2.Distance(previous[0], previous[1]);
            float currentDistance = Vector2.Distance(current[0], current[1]);
            float pinchDelta = currentDistance - prevDistance;

            MobileInputState.Instance?.AddZoomDelta(-pinchDelta * pinchZoomMultiplier);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _positions.Remove(eventData.pointerId);
    }
}