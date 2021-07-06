using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class InventorySpriteController : MonoBehaviour {
    public GameObject InventoryUIPrefab;

    Dictionary<Inventory, GameObject> inventoryGameObjectMap;

    Dictionary<string, Sprite> inventorySprites;

    World world {
        get { return WorldController.Instance.world; }
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////
    // Use this for initialization
    void Start() {
        LoadSprites();

        // Instantiate our dictionary that tracks which GameObject is rendering which Tile data.
        inventoryGameObjectMap = new Dictionary<Inventory, GameObject>();

        // Register our callback so that our GameObject gets updated whenever
        // the tile's type changes.
        world.RegisterInventoryCreated(OnInventoryCreated);

        // Check for pre-existing inventory, which won't do the callback.
        foreach (string objectType in world.inventoryManager.inventories.Keys) {
            foreach (Inventory inv in world.inventoryManager.inventories[objectType])
                OnInventoryCreated(inv);
        }


        //c.SetDestination( world.GetTileAt( world.Width/2 + 5, world.Height/2 ) );
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    void LoadSprites() {
        inventorySprites = new Dictionary<string, Sprite>();
        Sprite[] sprites = Resources.LoadAll<Sprite>("Images/Inventory/");

        //Debug.Log("LOADED RESOURCE:");
        foreach (Sprite s in sprites) {
            //Debug.Log(s);
            inventorySprites[s.name] = s;
        }
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public void OnInventoryCreated(Inventory inv) {
        Debug.Log("OnInventoryCreated");
        // Create a visual GameObject linked to this data.

        // FIXME: Does not consider multi-tile objects nor rotated objects

        // This creates a new GameObject and adds it to our scene.
        GameObject inv_go = new GameObject();

        // Add our tile/GO pair to the dictionary.
        inventoryGameObjectMap.Add(inv, inv_go);

        inv_go.name = inv.objectType;
        inv_go.transform.position = new Vector3(inv.tile.X, inv.tile.Y, 0);
        inv_go.transform.SetParent(this.transform, true);

        SpriteRenderer sr = inv_go.AddComponent<SpriteRenderer>();
        sr.sprite = inventorySprites[inv.objectType];
        sr.sortingLayerName = "Inventory";

        //  Handle inventory UI
        if (inv.maxStackSize > 1) {
            //  This is an item that can be stacked so we can show the current stack size
            GameObject ui_go = Instantiate(InventoryUIPrefab);
            ui_go.transform.SetParent(inv_go.transform);
            ui_go.transform.localPosition = Vector3.zero;

            ui_go.GetComponentInChildren<Text>().text = inv.stackSize.ToString();
        }

        // Register our callback so that our GameObject gets updated whenever
        // the object's into changes.
        inv.RegisterInventoryChangedCallback(OnInventoryChanged);
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    void OnInventoryChanged(Inventory inv) {
        // Make sure the furniture's graphics are correct.
        if (inventoryGameObjectMap.ContainsKey(inv) == false) {
            Debug.LogError("OnInventoryChanged -- trying to change visuals for inventory not in our map.");
            return;
        }

        GameObject inv_go = inventoryGameObjectMap[inv];

        if (inv.stackSize > 0) {
            Text text = inv_go.GetComponentInChildren<Text>();

            if (text != null) {
                text.text = inv.stackSize.ToString();
            }
        }
        else {
            //  Inventory stack has gone to 0, so we want to clean the inventory
            Destroy(inv_go);
            inventoryGameObjectMap.Remove(inv);
            inv.UnregisterInventoryChangedCallback(OnInventoryChanged);
        }

        // inv_go.transform.position = new Vector3(inv.tile.X, inv.tile.Y, 0);
    }
}
