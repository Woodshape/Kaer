using UnityEngine;
using System.Collections.Generic;
using System;

public class Job {
    // This class holds info for a queued up job, which can include
    // things like placing furniture, moving stored inventory,
    // working at a desk, and maybe even fighting enemies.

    public Tile tile;

    public float jobTime { get; protected set; }

    public string jobObjectType { get; protected set; }

    public bool acceptsAnyInventoryItem = false;
    public bool canTakeInventoryFromStockpile = true;

    Action<Job> cbJobComplete;
    Action<Job> cbJobCancel;
    Action<Job> cbJobWorked;

    public Dictionary<string, Inventory> inventoryRequirements;

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public Job(Tile tile, string jobObjectType, Action<Job> cbJobComplete, float jobTime, Inventory[] inventoryRequirements) {
        this.tile = tile;
        this.jobObjectType = jobObjectType;
        this.cbJobComplete += cbJobComplete;
        this.jobTime = jobTime;

        this.inventoryRequirements = new Dictionary<string, Inventory>();
        if (inventoryRequirements != null) {
            foreach (Inventory inv in inventoryRequirements) {
                this.inventoryRequirements[inv.objectType] = inv.Clone();
            }
        }
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    protected Job(Job other) {
        this.tile = other.tile;
        this.jobObjectType = other.jobObjectType;
        this.cbJobComplete = other.cbJobComplete;
        this.jobTime = other.jobTime;

        this.inventoryRequirements = new Dictionary<string, Inventory>();
        if (inventoryRequirements != null) {
            foreach (Inventory inv in other.inventoryRequirements.Values) {
                this.inventoryRequirements[inv.objectType] = inv.Clone();
            }
        }
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    virtual public Job Clone() {
        return new Job(this);
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public void RegisterJobCompleteCallback(Action<Job> cb) {
        cbJobComplete += cb;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public void RegisterJobCancelCallback(Action<Job> cb) {
        cbJobCancel += cb;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public void UnregisterJobCompleteCallback(Action<Job> cb) {
        cbJobComplete -= cb;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public void UnregisterJobCancelCallback(Action<Job> cb) {
        cbJobCancel -= cb;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public void RegisterJobWorkedCallback(Action<Job> cb) {
        cbJobWorked += cb;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public void UnregisterJobWorkedCallback(Action<Job> cb) {
        cbJobWorked -= cb;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public void DoWork(float workTime) {
        if (!HasAllMaterial()) {
            // Job can't be worked, but we still want to call all callbacks (for animations etc.)
            if (cbJobWorked != null)
                cbJobWorked(this);
            
            return;
        }


        jobTime -= workTime;

        if (cbJobWorked != null)
            cbJobWorked(this);

        if (jobTime <= 0) {
            if (cbJobComplete != null)
                cbJobComplete(this);
        }
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public void CancelJob() {
        if (cbJobCancel != null)
            cbJobCancel(this);

        tile.world.jobQueue.Remove(this);
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public bool HasAllMaterial() {
        foreach (Inventory inv in inventoryRequirements.Values) {
            if (inv.maxStackSize > inv.stackSize)
                return false;
        }

        return true;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public int DesiresInventoryType(Inventory inv) {
        if (acceptsAnyInventoryItem) {
            return inv.maxStackSize;
        }

        if (inventoryRequirements.ContainsKey(inv.objectType) == false) {
            return 0;
        }

        if (inventoryRequirements[inv.objectType].stackSize >= inventoryRequirements[inv.objectType].maxStackSize) {
            // We already have all that we need!
            return 0;
        }

        // The inventory is of a type we want, and we still need more.
        return inventoryRequirements[inv.objectType].maxStackSize - inventoryRequirements[inv.objectType].stackSize;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public Inventory GetFirstDesiredInventory() {
        foreach (Inventory inv in inventoryRequirements.Values) {
            if (inv.maxStackSize > inv.stackSize)
                return inv;
        }

        return null;
    }
}
