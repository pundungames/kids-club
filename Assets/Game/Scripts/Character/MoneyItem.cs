using MilkFarm;
using UnityEngine;

public class MoneyItem : MonoBehaviour
{
    public MoneyManager moneyManager;

    void OnMouseDown()
    {
        if (moneyManager != null)
        {
            moneyManager.OnTableClicked();
        }
    }
}