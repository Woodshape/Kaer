//=======================================================================
// Copyright Martin "quill18" Glaude 2015.
//		http://quill18.com
//=======================================================================

using UnityEngine;
using System.Collections;
using System;

// LooseObjects are things that are lying on the floor/stockpile, like a bunch of metal bars
// or potentially a non-installed copy of furniture (e.g. a cabinet still in the box from Ikea)

public class Inventory
{
    public string objectType = "steel_plate";

    public int maxStackSize = 50;

    protected int _stackSize = 1;
    public int stackSize
    {
        get { return _stackSize; }
        set
        {
            if (_stackSize != value)
            {
                _stackSize = value;
                if (cbInventoryChanged != null)
                {
                    cbInventoryChanged(this);
                }
            }
        }
    }

    //  Is the inventory on the ground or carried by a character?
    public Tile tile;
    public Character character;

    // The function we callback any time our inventory's data changes
    Action<Inventory> cbInventoryChanged;

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public Inventory() { }
    
    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public Inventory(string objectType, int stackSize)
    {
        this.objectType = objectType;
        this.stackSize = stackSize;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public Inventory(string objectType, int stackSize, int maxStackSize)
    {
        this.objectType = objectType;
        this.maxStackSize = maxStackSize;
        this.stackSize = stackSize;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    protected Inventory(Inventory other)
    {
        objectType = other.objectType;
        maxStackSize = other.maxStackSize;
        stackSize = other.stackSize;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public virtual Inventory Clone()
    {
        return new Inventory(this);
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Register a function to be called back when our tile type changes.
    /// </summary>
    public void RegisterInventoryChangedCallback(Action<Inventory> callback)
    {
        cbInventoryChanged += callback;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Unregister a callback.
    /// </summary>
    public void UnregisterInventoryChangedCallback(Action<Inventory> callback)
    {
        cbInventoryChanged -= callback;
    }
}
