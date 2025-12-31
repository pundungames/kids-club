using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    [SerializeField] private Transform popup;
    [SerializeField] Image dimed;
    public void SetPopup()
    {
        Taptic.Medium();
        var popupActive = popup.gameObject.activeSelf;
        int popupTargetScale = popupActive ? 0 : 1;
        Time.timeScale = popupActive ? 1 : 0;
        var ease = popupActive ? Ease.InBack : Ease.OutBack;
        // Color color = dimed.color; color.a = popupTargetScale * 250; dimed.color = color;
        if (!popupActive) popup.gameObject.SetActive(true);
        if (!popupActive) dimed.gameObject.SetActive(true);
        dimed.DOFade(popupTargetScale * (250f / 255f), 0.3f).SetUpdate(true);

        popup.DOScale(popupTargetScale, 0.5f).SetUpdate(true).SetUpdate(true).SetEase(ease).OnComplete(() =>
        {
            if (popupActive) popup.gameObject.SetActive(false);
            if (popupActive) dimed.gameObject.SetActive(false);

        });
    }
}
