using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TickVisual : MonoBehaviour
{
    
    [SerializeField] TextMeshProUGUI tickTimerText;

    void Start()
    {
        TickManager.OnTick += delegate (object sender, TickManager.OnTickEventArgs e) 
        { 
            tickTimerText.text = e.tick.ToString(); 
        };
    }

}
