using UnityEngine;

[CreateAssetMenu(menuName = "Belt Visual Library")]
public class BeltVisualLibrary : ScriptableObject
{

    [Header("Sprites per BeltVisualState")]
    public Sprite straight;
    public Sprite leftTurn;
    public Sprite rightTurn;
    public Sprite start;
    public Sprite end;
    public Sprite straightWithLeft;
    public Sprite straightWithRight;
    public Sprite bothSides;          
    public Sprite straightWithSides;  
    public Sprite none;  

    public Sprite GetSprite(BeltVisualState state)
    {
        return state switch
        {
            BeltVisualState.Straight          => straight,
            BeltVisualState.LeftTurn          => leftTurn,
            BeltVisualState.RightTurn         => rightTurn,
            BeltVisualState.Start             => start,
            BeltVisualState.End               => end,
            BeltVisualState.StraightWithLeft  => straightWithLeft,
            BeltVisualState.StraightWithRight => straightWithRight,
            BeltVisualState.BothSides         => bothSides,
            BeltVisualState.StraightWithSides => straightWithSides,
            BeltVisualState.None              => none,
            _ => null
        };
    }
}
