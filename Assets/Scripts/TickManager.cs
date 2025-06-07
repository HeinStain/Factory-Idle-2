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

    // 1 / 0.05 = 20 t/s
    private const float tickTimerMax = 1f/30f;


    private int tick;
    private float tickTimer;


    private void Awake()
    {  
        Instance = this;
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

    //////////
    ///
    
    public static TickManager Instance { get; private set; }

    public int GetCurrentTick()
    {
        return tick;
    }

}
