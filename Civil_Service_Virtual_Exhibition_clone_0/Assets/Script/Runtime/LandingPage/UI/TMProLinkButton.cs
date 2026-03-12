using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class TMProLinkButton : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private TMP_Text tmpText;
    [SerializeField] private string linkID;
    [Space]
    public UnityEvent onLinkClicked;

    public void OnPointerClick(PointerEventData eventData)
    {
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(
            tmpText,
            eventData.position,
            null
        );

        if (linkIndex != -1)
        {
            TMP_LinkInfo linkInfo = tmpText.textInfo.linkInfo[linkIndex];

            if (linkInfo.GetLinkID() == linkID)
            {
                onLinkClicked?.Invoke();
            }
        }
    }
}
