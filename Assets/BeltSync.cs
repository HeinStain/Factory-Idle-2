using UnityEngine;

public class BeltSync : MonoBehaviour
{
    public static BeltSync Instance { get; private set; }
    public static float sharedTimer;
    private float speed;

    private void Awake()
    {
        Instance = this;
        sharedTimer = 0f;
    }

    private void Update()
    {
        sharedTimer += Time.deltaTime;
    }
}