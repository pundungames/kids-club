using UnityEngine;

public class TableClicker : MonoBehaviour
{
    // Buraya CounterManager'ýn olduðu ana objeyi sürükleyeceksin
    public CounterManager manager;

    // Bu scriptin olduðu objede MUTLAKA Collider olmalý
    void OnMouseDown()
    {
        if (manager != null)
        {
            // Manager'daki satýþ fonksiyonunu tetikle
            manager.SellProductToCustomer();

            // Týklandýðýný anlamak için ufak bir log (Test için)
            Debug.Log("Masaya Týklandý!");
        }
        else
        {
            Debug.LogError("TableClicker scriptine Manager atanmamýþ!");
        }
    }
}