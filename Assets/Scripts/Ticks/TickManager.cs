using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TickManager : MonoBehaviour
{
    public static TickManager Instance { get; private set; }

    public class OnTickEventArgs : EventArgs
    {
        public int tick;
    }
    public static event EventHandler<OnTickEventArgs> OnTick;

    public static float beltsPerSecond =  1f;
    public static float ticksPer = 30f;
    public static float tickTimerMax => 1f / ticksPer;

    private int tick;
    private float tickTimer;


    private void Awake()
    {  
        Instance = this;
        tick = 0;
    }
    
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

    public int GetCurrentTick()
    {
        return tick;
    }

}
