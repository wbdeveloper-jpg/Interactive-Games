using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;

public class FloatingText : MonoBehaviour
{
    public TextMeshProUGUI text;

    public void Show(string message, Color color)
    {
        text.text = message;
        text.color = color;

        transform.localScale = Vector3.zero;

        // Pop in
        transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);

        // Move up + fade
        transform.DOMoveY(transform.position.y + 1f, 1f);

        if(transform.GetComponent<Image>()  != null)
        {
            transform.GetComponent<Image>().DOFade(0, 3f).OnComplete(() =>
            {
                Destroy(gameObject);
            });
            text.DOFade(0, 3f).OnComplete(() =>
            {
                Destroy(gameObject);
            });
        }
        else
        {
            text.DOFade(0, 3f).OnComplete(() =>
            {
                Destroy(gameObject);
            });
        }
    }
}