using UnityEngine;

public class CowController : MonoBehaviour
{
    [Header("Baðlantýlar")]
    public CounterManager counterManager; // Sütü hangi masaya gönderecek?

    // InputManager tarafýndan çaðrýlýr
    public void OnClicked()
    {
        if (counterManager != null)
        {
            // GÜNCELLEME:
            // Artýk parantez içi boþ deðil! 
            // transform.position gönderiyoruz ki süt ineðin olduðu yerden uçmaya baþlasýn.
            counterManager.AddMilk(transform.position);

            // Ýstersen ineðin týklanýnca ufak bir zýplama/büyüme efekti de eklenebilir ama þu an þart deðil.
            Debug.Log("Möö! Süt uçarak gidiyor.");
        }
        else
        {
            Debug.LogError("HATA: Ýnek scriptine 'CounterManager' atanmamýþ!");
        }
    }
}