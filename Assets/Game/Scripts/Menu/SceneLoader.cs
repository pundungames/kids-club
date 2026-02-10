using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [Header("Coming Soon Panel")]
    public GameObject comingSoonPanel;

    // =========================
    // MAIN MENU BUTTONS
    // =========================

    public void OpenCowFarm()
    {
        SceneManager.LoadScene(2);
    }

    public void OpenChickenFarm()
    {
        SceneManager.LoadScene(3);
    }

    public void OpenFarmComingSoon()
    {
        if (comingSoonPanel != null)
            comingSoonPanel.SetActive(true);
    }

    public void CloseComingSoon()
    {
        if (comingSoonPanel != null)
            comingSoonPanel.SetActive(false);
    }

    // =========================
    // BACK BUTTON (IN GAME SCENES)
    // =========================

    public void BackToMainMenu()
    {
        SceneManager.LoadScene(1);
    }
}
