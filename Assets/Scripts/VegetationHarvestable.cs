using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VegetationHarvestable : Vegetation, IHarvestable
{
    public int health = 1;
    public string resource;
    public float resourceAmount = 0f;

    public void Harvest(int damage)
    {
        health -= damage;

        if (health <= 0)
        {
            Destroy();
        }
    }

    public override void Destroy()
    {
        base.Destroy();
        PlayerController.AddToInventory(resource, resourceAmount);
        ParentBlock.vegetation = null;
        ParentBlock.changed = true;
        Destroy(gameObject);
    }

    protected override void ParentBlockDestroyed(object sender, EventArgs e)
    {
        PlayerController.AddToInventory(resource, resourceAmount);
        ParentBlock.vegetation = null;
        ParentBlock.changed = true;
        Destroy(gameObject);
    }
}
