using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VegetationUnharvestable : Vegetation
{
    public override void Destroy()
    {
        base.Destroy();
        ParentBlock.vegetation = null;
        ParentBlock.changed = true;
        Destroy(gameObject);
    }

    protected override void ParentBlockDestroyed(object sender, EventArgs e)
    {
        ParentBlock.vegetation = null;
        ParentBlock.changed = true;
        Destroy(gameObject);
    }
}
