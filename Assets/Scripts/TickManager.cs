using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TickManager : MonoBehaviour
{
    public class OnTickEventArgs : EventArgs
    {
        public int tick;
    }
    public static event EventHandler<OnTickEventArgs> OnTick;

    private const float tickTimerMax = .2f;
    private int tick;
    private float tickTimer;


    private void Awake()
    {  
        tick = 0;
    }
    
    // Update is called once per frame
    void Update()
    {
        tickTimer += Time.deltaTime;
        if (tickTimer >= tickTimerMax)
        {
            tickTimer -= tickTimerMax;
            tick++;
            if (OnTick != null) OnTick(this, new OnTickEventArgs { tick = tick });
            
        }
    }
}
