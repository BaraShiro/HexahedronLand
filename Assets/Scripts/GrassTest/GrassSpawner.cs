using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrassSpawner : MonoBehaviour
{
    public GameObject grass1ObjectPrefab;
    public GameObject grass2ObjectPrefab;
    public int xs = 10;
    public int zs = 10;
    
    private void Awake()
    {
        
    }

    private void Start()
    {
        Vector3 pos = transform.position;
        for (int x = (int)pos.x - xs; x < (int)pos.x + xs; x++)
        {
            for (int z = (int)pos.z - zs; z < (int)pos.z + zs; z++)
            {
                Vector3 instPos = new Vector3(x , 0, z);
                Quaternion instRot = Quaternion.Euler(new Vector3(0, (x + x) * (x + 1) * (z + z) * (z + 1), 0));
                if ((x % 2 == 0 && z % 2 == 1) || (x % 2 == 1 && z % 2 == 0))
                {
                    Instantiate(grass1ObjectPrefab, instPos, instRot);   
                }
                else
                {
                    Instantiate(grass2ObjectPrefab, instPos, instRot);
                }

            }
        }
    }

    private void Update()
    {
        
    }
}

