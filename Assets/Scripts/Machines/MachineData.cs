using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Machine Data")]
public class MachineData : ScriptableObject
{
    
    public string machineName;
    public Transform prefab;
    public Transform visual;
    public int width;
    public int height;
    public bool hasDispenseBelt;
    public bool hasIntakeBelt;
    public int defaultCraftingInterval;
    public int energyCost;
    
    public enum Direction
    {
        Up,
        Right,
        Down,
        Left,
    }

    public static Vector2Int GetNextTileXY(Direction direction) {
        switch (direction) {
            default:
            case Direction.Down:  return new Vector2Int( 0, -1);
            case Direction.Left:  return new Vector2Int(-1,  0);
            case Direction.Up:    return new Vector2Int( 0, +1);
            case Direction.Right: return new Vector2Int(+1,  0);
        }
    }

    public static Vector2Int GetRightTileXY(Direction direction) {
        switch (direction) {
            default:
            case Direction.Down:  return new Vector2Int(-1, 0);
            case Direction.Left:  return new Vector2Int(0, +1);
            case Direction.Up:    return new Vector2Int(+1, 0);
            case Direction.Right: return new Vector2Int(0, -1);
        }
    }

    public static Direction GetNextRotationDirection(Direction direction)
    {
        switch (direction) 
        {
            default:
            case Direction.Up:    return Direction.Right;
            case Direction.Right:     return Direction.Down;
            case Direction.Down:     return Direction.Left;
            case Direction.Left:       return Direction.Up;
        }
    }

    public int GetRotationAngle(Direction direction)
    {
        switch(direction)
        {
            default:
            case Direction.Up:   return 0;
            case Direction.Right:    return -90;
            case Direction.Down:    return -180;
            case Direction.Left:      return -270;
        }
    }

    public Vector2Int GetRotationOffset(Direction direction)
    {
        switch (direction)
        {
            default:
            case Direction.Up:   return new Vector2Int(0, 0);
            case Direction.Right:    return new Vector2Int(0, width);
            case Direction.Down:    return new Vector2Int(width, height);
            case Direction.Left:      return new Vector2Int(height, 0);
        }
    }


    public List<Vector2Int> GetGridPositionList(Vector2Int offset, Direction direction) 
    {
        List<Vector2Int> gridPositionList = new List<Vector2Int>();
        
        switch (direction)
        {
            default:
            case Direction.Up:
            case Direction.Down:
            for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++) 
                    {
                        gridPositionList.Add(offset + new Vector2Int(x, y));
                    }
                }
                break;

            case Direction.Right:
            case Direction.Left:
                for (int x = 0; x < height; x++)
                {
                    for (int y = 0; y < width; y++) 
                    {
                        gridPositionList.Add(offset + new Vector2Int(x, y));
                    }
                }
                break;
                
        }

        return gridPositionList;
    }
}
