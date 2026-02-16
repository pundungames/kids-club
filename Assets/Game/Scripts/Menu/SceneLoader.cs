using MilkFarm;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

public class SceneLoader : MonoBehaviour
{
    public GameObject comingSoonPanel;
    public GameObject shopPanel;
    [Inject] AudioManager audioManager;
    // =========================
    // MAIN MENU BUTTONS
    // =========================
    private void Start()
    {
        AudioManager audioManager = FindObjectOfType<AudioManager>();
        if (!audioManager.IsPlaying("Music"))
            audioManager.Play("Music");
    }
    public void OpenCowFarm()
    {
        audioManager.Play("Tap");
        MilkFarmEvents.SaveRequested(); // ✅
        SceneManager.LoadScene(2);
    }

    public void OpenChickenFarm()
    {
        audioManager.Play("Tap");
        MilkFarmEvents.SaveRequested(); // ✅
        SceneManager.LoadScene(3);
    }
    public void OpenFarm()
    {
        audioManager.Play("Tap");

        SceneManager.LoadScene(4);
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
        MilkFarmEvents.SaveRequested(); // ✅ Save fire et

        SceneManager.LoadScene(1);
    }
}
