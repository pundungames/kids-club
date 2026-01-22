using UnityEngine;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Zenject;

/// <summary>
/// CurrencyManager - SADECE UI
/// Para logic'i MoneyManager'da
/// </summary>
public class CurrencyManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI[] cashTexts; // Para UI'larý
    [SerializeField] private TextMeshProUGUI[] gemTexts;  // Gem UI'larý

    [Header("Animation")]
    [SerializeField] private bool animateOnChange = true;

    [Inject] private MilkFarm.IAPManager iapManager;

    private Dictionary<GameObject, Coroutine> activeAnimations = new Dictionary<GameObject, Coroutine>();

    private void Start()
    {
        // Ýlk güncelleme (load'dan sonra MoneyManager UpdateCashUI çaðýracak)
        UpdateGemUI();
    }

    private void OnEnable()
    {
        MilkFarm.MilkFarmEvents.OnGemChanged += UpdateGemUI;
    }

    private void OnDisable()
    {
        MilkFarm.MilkFarmEvents.OnGemChanged -= UpdateGemUI;
    }

    // === CASH UI (MoneyManager'dan çaðrýlýr) ===

    /// <summary>
    /// Para UI'ýný güncelle
    /// MoneyManager'dan çaðrýlýr
    /// </summary>
    public void UpdateCashUI(float amount)
    {
        if (cashTexts == null) return;

        string formatted = FormatNumberShort(amount);

        foreach (var text in cashTexts)
        {
            if (text != null)
            {
                text.text = formatted;
                if (animateOnChange) TriggerAnimation(text.gameObject);
            }
        }
    }
    public void ScaleEffect(GameObject target)
    {
        StartCoroutine(ScalePunchRoutine(target));
    }
    // === GEM UI (IAPManager event'inden) ===

    private void UpdateGemUI()
    {
        if (gemTexts == null || iapManager == null) return;

        int currentGems = iapManager.GetCurrentGems();
        string formatted = FormatNumberShort(currentGems);

        foreach (var text in gemTexts)
        {
            if (text != null)
            {
                text.text = formatted;
                if (animateOnChange) TriggerAnimation(text.gameObject);
            }
        }
    }

    // === ANIMATION ===

    private void TriggerAnimation(GameObject target)
    {
        if (activeAnimations.ContainsKey(target) && activeAnimations[target] != null)
        {
            StopCoroutine(activeAnimations[target]);
        }

        activeAnimations[target] = StartCoroutine(ScalePunchRoutine(target));
    }

    private IEnumerator ScalePunchRoutine(GameObject target)
    {
        float duration = 0.2f;
        float elapsed = 0f;
        Vector3 startScale = Vector3.one * 1.2f;
        Vector3 endScale = Vector3.one;

        target.transform.localScale = startScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            target.transform.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }

        target.transform.localScale = endScale;
        activeAnimations[target] = null;
    }

    // === FORMATTING ===

    public string FormatNumberShort(float number)
    {
        CultureInfo culture = CultureInfo.InvariantCulture;

        if (number >= 1_000_000_000)
            return (number / 1_000_000_000f).ToString("0.##", culture) + "B";
        else if (number >= 1_000_000)
            return (number / 1_000_000f).ToString("0.##", culture) + "M";
        else if (number >= 1_000)
            return (number / 1_000f).ToString("0.##", culture) + "K";
        else
            return number.ToString("0", culture);
    }

    // === DEBUG ===

    [ContextMenu("Debug: Test Cash Update")]
    public void DebugTestCashUpdate()
    {
        UpdateCashUI(12345f);
    }

    [ContextMenu("Debug: Test Gem Update")]
    public void DebugTestGemUpdate()
    {
        UpdateGemUI();
    }
}