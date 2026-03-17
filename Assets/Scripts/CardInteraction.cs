using UnityEngine;
using UnityEngine.EventSystems;

public class CardInteraction : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [HideInInspector] public int cardIndex;
    [HideInInspector] public HandDisplay handDisplay;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (handDisplay != null)
            handDisplay.OnCardHover(cardIndex);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (handDisplay != null)
            handDisplay.OnCardUnhover(cardIndex);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && handDisplay != null)
            handDisplay.OnCardClick(cardIndex);
    }
}
