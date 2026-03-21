using UnityEngine;
using UnityEngine.EventSystems;

public class SliderDragForwarder : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    [SerializeField] private VideoPlayerUIController controller;
    void Awake()
    {
        if(controller == null)
        {
            FindAnyObjectByType<VideoPlayerUIController>();
        }       
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (controller != null)
            controller.BeginTimelineDrag();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (controller != null)
            controller.EndTimelineDrag();
    }
}