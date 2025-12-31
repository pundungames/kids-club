using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class NewTowerUnlock : MonoBehaviour
{
    [Inject] UIManager manager;
    [Inject] AudioManager audioManager;
    [SerializeField] List<CurUnlockedTower> unlockedTowers;

    [SerializeField] Transform content;
    [SerializeField] Image towerImage;
    [SerializeField] TMP_Text towerNameText;
    [SerializeField] TMP_Text towerInfoText;
    CurUnlockedTower unlockedTower;
    int currentIndex;
    bool opened;
    public bool CheckTowers()
    {
        if (opened) return false;
        int level = PlayerPrefs.GetInt("_totalLevel", 0);
        if (level <= unlockedTowers.Count)
        {
            unlockedTower = unlockedTowers[level - 1];
            OpenTower();
            gameObject.SetActive(true);
            opened = true;
            return true;
        }
        return false;
    }

    bool OpenTower()
    {
      //  if (currentIndex >= unlockedTower.unlockedTowers.Count) return false;
        Taptic.Medium();
        audioManager.Play("Win");
      /*  TowerStatSo stat = unlockedTower.unlockedTowers[currentIndex];
        PlayerPrefs.SetInt(stat.towerName, 1);
        towerImage.sprite = stat.towerSprite;
        towerNameText.text = stat.towerName;
        towerInfoText.text = stat.towerInfo;*/
        currentIndex++;

        content.DOScale(Vector3.one * 1.2f, .2f).SetEase(Ease.OutBack).OnComplete(() => content.DOScale(Vector3.one, .2f));
        if (PlayerPrefs.GetInt("_totalLevel") != 1)
            PlayerPrefs.SetInt("OpenCollection", 1);
        return true;
    }
    public void Claim()
    {
        if (OpenTower()) return;
       // manager.MainMenu();
    }
}
[Serializable]
public class CurUnlockedTower
{
   // public List<TowerStatSo> unlockedTowers;
}
