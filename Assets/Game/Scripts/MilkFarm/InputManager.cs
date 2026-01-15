using MilkFarm;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    [Header("Ayarlar")]
    public LayerMask clickableLayers; // Sadece týklanabilir objelere (Ýnek gibi) çarpsýn

    void Update()
    {
        // Hem PC Sol Týk hem de Mobil Dokunuþ algýlar
        if (Input.GetMouseButtonDown(0))
        {
            CheckClick();
        }
    }

    void CheckClick()
    {
        // Kameradan týkladýðýmýz yere ýþýn yolluyoruz
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f, clickableLayers))
        {
            // Eðer týkladýðýmýz objenin bir "CowController" scripti varsa çalýþtýr
            CowController cow = hit.collider.GetComponent<CowController>();
            if (cow != null)
            {
                cow.OnClicked();
            }
        }
    }
}