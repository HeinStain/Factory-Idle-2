using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Notification : MonoBehaviour
{

    [SerializeField] private Animator notificationAnimator;
    [SerializeField] private TextMeshProUGUI text;

    

    private void Awake()
    {
        StartCoroutine(NotificationAnimation());
    }

    public void Setup(string dialogue)
    {
        text.SetText(dialogue);
        text.ForceMeshUpdate();
    }

    IEnumerator NotificationAnimation()
    {
        notificationAnimator.SetBool("Display", true);
        yield return new WaitForSeconds(4);
        notificationAnimator.SetBool("Display", false);
        yield return new WaitForSeconds(1);
        Destroy(gameObject);
    }
}
