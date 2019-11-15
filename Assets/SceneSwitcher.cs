using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void GoToLogin()
    {
        SceneManager.LoadScene("Login");
    }

    public void GoToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void GoToQuests()
    {
        SceneManager.LoadScene("Quests");
    }

    public void GoToQuestSmokeDetectors()
    {
        SceneManager.LoadScene("QuestSmokeDetectors");
    }

    public void GoToQuestSmokeDetectorsLivingRoom()
    {
        SceneManager.LoadScene("QuestSmokeDetectorsLivingRoom");
    }

}
