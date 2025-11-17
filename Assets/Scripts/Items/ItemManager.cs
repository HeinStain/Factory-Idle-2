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
    [SerializeField] public List<ItemData> itemList;

#endregion 


#region Object Pooling

    public static List<ObjectPool> objectPools = new List<ObjectPool>();

    public static GameObject SpawnObject(Belt spawnBelt, ItemData itemData)
    {
        Vector3 spawnPosition = spawnBelt.rearSlot.position;
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

        // Get object (new or pooled)
        GameObject spawnObj = null;
        if (pool.inactiveObjects.Count > 0)
        {
            spawnObj = pool.inactiveObjects[0];
            pool.inactiveObjects.RemoveAt(0);
            spawnObj.SetActive(true);
        }
        else
        {
            spawnObj = Instantiate(Instance.itemPrefab);
        }

        // Common setup for both paths
        spawnObj.transform.position = spawnPosition;
        spawnObj.transform.rotation = Quaternion.identity;
        spawnObj.name = itemData.displayName;

        var item = spawnObj.GetComponent<Item>();
        item.ItemSetup(itemData, spawnBelt);

        return spawnObj;
    }


    public static void ReturnObjectToPool(GameObject obj)
    {
        // Take off "(Clone)" from the name 
        string objName = obj.name;

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