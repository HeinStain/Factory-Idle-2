using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemManager : MonoBehaviour
{

    public static ItemManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

#region Items

    [SerializeField] public GameObject itemPrefab;
    [SerializeField] public List<ItemData> oresList;
    [SerializeField] public List<ItemData> gemsList;

#endregion 


#region Object Pooling

    public static List<ObjectPool> objectPools = new List<ObjectPool>();

    public static GameObject SpawnObject(GameObject itemPrefab, ItemData itemData, Vector3 spawnPosition, Quaternion spawnRotation)
    {
        // Look for the object pool of this item type
        ObjectPool pool = null;
        foreach (ObjectPool objPool in objectPools)
        {
            if (objPool.poolName == itemData.displayName)
            {
                pool = objPool;
                break;
            }
        }

        // If the pool doesn't exist, create it
        if (pool == null)
        {
            pool = new ObjectPool()  {  poolName = itemData.displayName };
            objectPools.Add(pool);
        }

        // Check if there are any inactive objects in the pool
        GameObject spawnObj = null;
        foreach (GameObject inactiveObj in pool.inactiveObjects)
        {
            if (inactiveObj != null)
            {
                spawnObj = inactiveObj;
                break;
            }
        }

        if (spawnObj == null)
        {
            // If there are no inactive objects, create a new one, and set it up
            spawnObj = Instantiate(itemPrefab, spawnPosition, spawnRotation);
            spawnObj.GetComponent<Item>().ItemSetup(itemData);
        }
        else
        {
            // If there is an inactive object, reactivate it
            spawnObj.transform.position = spawnPosition;
            spawnObj.transform.rotation = spawnRotation;
            pool.inactiveObjects.Remove(spawnObj);
            spawnObj.SetActive(true);
        }

        return spawnObj;
    }


    public static void ReturnObjectToPool(GameObject obj)
    {
        // Take off "(Clone)" from the name 
        string objName = obj.name.Substring(0, obj.name.Length - 7);

        // Look for the object pool of this item type
        ObjectPool pool = null;
        foreach (ObjectPool objPool in objectPools)
        {
            if (objPool.poolName == objName)
            {
                pool = objPool;
                break;
            }
        }

        if (pool == null)
        {
            // If pool isn't found, something went wrong
            Debug.LogWarning("Trying to release an object that is not pooled: "  + objName);
        }
        else
        {
            // If found, deactivate the item, and return it to the pool
            obj.SetActive(false);
            pool.inactiveObjects.Add(obj);
        }
    }


}

public class ObjectPool
{
    public string poolName;
    public List<GameObject> inactiveObjects = new List<GameObject>();
}

#endregion 