using System.Collections.Generic;
using UnityEngine;

public class ExperienceImporterTransition : MonoBehaviour
{
     public List<ExperienceImporterArtboard> targets;
     public string homeboard;
     public GameObject Artboards;
    private void Start()
    {
        targets.AddRange(FindObjectsOfType<ExperienceImporterArtboard>());
        AnimateTo(homeboard);
    }
    public void AnimateTo(string targetId)
    {
        int targetIndex = targets.FindIndex(i => i.GetComponent<ExperienceImporterArtboard>().elementId == targetId);
        if (targetIndex != -1)
            Artboards.GetComponent<RectTransform>().anchoredPosition = -targets[targetIndex].gameObject.GetComponent<RectTransform>().anchoredPosition;
    }
}
