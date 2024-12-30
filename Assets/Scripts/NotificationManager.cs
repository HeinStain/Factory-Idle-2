using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance { get; private set; }
    [SerializeField] GameObject notificationPrefab;
    [SerializeField] private Image errorImage;
    public float transitionDuration = 0.5f;

    private Coroutine errorCoroutine;

    private void Awake()
    {
        Instance = this;
    }

    public void DisplayNotification(string text)
    {
        GameObject notificationObj = Instantiate(notificationPrefab, transform.Find("Notifications").transform);
        notificationObj.GetComponentInChildren<Notification>().Setup(text);
    }

    public void Error()
    {
        // Stop the existing coroutine if it's running
        if (errorCoroutine != null)
        {
            StopCoroutine(errorCoroutine);
        }

        // Start a new coroutine
        errorCoroutine = StartCoroutine(ShowError());
    }

    private IEnumerator ShowError()
    {
        Color startColor = new Color(1, 0, 0, 0.25f);
        Color endColor = new Color(1, 0, 0, 0);

        float elapsedTime = 0f;

        while (elapsedTime < transitionDuration)
        {
            errorImage.color = Color.Lerp(startColor, endColor, elapsedTime / transitionDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure the final color is set
        errorImage.color = endColor;

        // Reset the coroutine reference
        errorCoroutine = null;
    }
}
