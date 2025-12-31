using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingScene : MonoBehaviour
{
    [SerializeField] Slider slider;
    [SerializeField] TextMeshProUGUI text;
    [SerializeField] LevelManager levelManager;
    private void Awake()
    {
        DOTween.To(() => slider.value, x => slider.value = x, 100, 2).OnUpdate(() =>
        {
            int roundedValue = Mathf.RoundToInt(slider.value);
            text.text = $"Loading... {roundedValue}%";
        }).OnComplete(() => levelManager.Load());
    }
}
