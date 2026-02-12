using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public GameObject comingSoonPanel;
    public GameObject shopPanel;

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
    public void OpenShop()
    {
        if (shopPanel != null)
            shopPanel.SetActive(true);
    }

    public void CloseShop()
    {
        if (shopPanel != null)
            shopPanel.SetActive(false);
    }
    // =========================
    // BACK BUTTON (IN GAME SCENES)
    // =========================

    public void BackToMainMenu()
    {
        SceneManager.LoadScene(1);
    }
}
