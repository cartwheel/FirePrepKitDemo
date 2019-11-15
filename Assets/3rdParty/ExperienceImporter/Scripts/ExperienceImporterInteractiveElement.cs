using UnityEngine;
using UnityEngine.EventSystems;

public class ExperienceImporterInteractiveElement : MonoBehaviour, IPointerClickHandler
{
    [HideInInspector]
    public string targetElementId;
    public GameObject mainBoard;

    public void OnPointerClick(PointerEventData eventData)
    {
        mainBoard.GetComponent<ExperienceImporterTransition>().AnimateTo(targetElementId);
    }
}
