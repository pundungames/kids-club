using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using TMPro;
using System.Globalization;

public class CurrencyManager : MonoBehaviour
{
    [SerializeField] internal float baseCash;
    [SerializeField] TextMeshProUGUI[] cashText;
    [SerializeField] GameObject statsGold;

    private void Awake()
    {
        baseCash = PlayerPrefs.GetFloat("_baseCash", 0);
        CashTextUpdate();
    }
    private void OnEnable()
    {
        EventManager.levelComplete += LevelComplete;
    }
    private void OnDisable()
    {
        EventManager.levelComplete -= LevelComplete;
    }

    #region Cash
    void LevelComplete()
    {
        PlayerPrefs.SetFloat("_baseCash", baseCash);
    }
    public void UpdateCash(float cashValue)
    {
        baseCash += cashValue;
        CashTextUpdate();
    }
    public void UpdateCashAndSave(float cashValue)
    {
        UpdateCash(cashValue);
        PlayerPrefs.SetFloat("_baseCash", baseCash);

    }
    public void Payment(float cashValue)
    {
        baseCash -= cashValue;
        PlayerPrefs.SetFloat("_baseCash", baseCash);
        CashTextUpdate();
    }
    public void CashTextUpdate()
    {
        if (cashText.Length > 0)
            foreach (var item in cashText)
            {
                if (item != null)
                    item.text = FormatNumberShort(baseCash);
            }
    }
    #endregion
    public string FormatNumberWithCommas(float number)
    {
        // Sayýyý ters çevirerek iþlemi daha kolay hale getiriyoruz.
        string reversedNumber = number.ToString();
        char[] charArray = reversedNumber.ToCharArray();
        Array.Reverse(charArray);
        reversedNumber = new string(charArray);

        // Her 3 basamakta bir virgül ekliyoruz.
        string formattedNumber = "";
        for (int i = 0; i < reversedNumber.Length; i++)
        {
            if (i > 0 && i % 3 == 0)
                formattedNumber += ",";
            formattedNumber += reversedNumber[i];
        }

        // Son olarak tekrar ters çevirip döndürüyoruz.
        charArray = formattedNumber.ToCharArray();
        Array.Reverse(charArray);
        return new string(charArray);
    }
    public string FormatNumberShort(float number)
    {
        CultureInfo culture = CultureInfo.InvariantCulture; // veya yeni CultureInfo("tr-TR") yapabilirsin

        if (number >= 1_000_000_000)
            return (number / 1_000_000_000f).ToString("0.##", culture) + "B";
        else if (number >= 1_000_000)
            return (number / 1_000_000f).ToString("0.##", culture) + "M";
        else if (number >= 1_000)
            return (number / 1_000f).ToString("0.##", culture) + "K";
        else
            return number.ToString("0", culture);
    }
}