using System;

public class MenuEventArgs : EventArgs
{
    public bool MenuOpen { get; }
    public Machine Machine{ get; }

    public MenuEventArgs(bool menuOpen, Machine machineData)
    {
        MenuOpen = menuOpen;
        Machine = machineData;
    }
}

