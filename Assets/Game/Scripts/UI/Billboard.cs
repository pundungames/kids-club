using UnityEngine;

public class Billboard : MonoBehaviour
{
    void LateUpdate()
    {
        // Canvas'ýn yüzünü kameranýn baktýðý yöne eþitler.
        // Bu sayede karakter dönse bile canvas kameraya paralel kalýr.
        transform.forward = Camera.main.transform.forward;
    }
}