using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LockText : MonoBehaviour
{
    [SerializeField] Image bg;
    [SerializeField] TextMeshProUGUI lockText;
    [SerializeField] float yValue = 100f;
    public void SetTextAnimation(string text)
    {
        //if (SceneManager.GetActiveScene().buildIndex == 2)
            transform.localScale /= 2;
        lockText.text = text;
        transform.DOMoveY(transform.position.y + yValue, 1.5f).OnComplete(() => Destroy(gameObject));
        bg.DOFade(0, 1.5f);
        lockText.DOFade(0, 1.5f);
    }
}
