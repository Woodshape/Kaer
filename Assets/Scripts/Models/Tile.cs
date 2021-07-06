//=======================================================================
// Copyright Martin "quill18" Glaude 2015.
//		http://quill18.com
//=======================================================================

using UnityEngine;
using System.Collections;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;


// TileType is the base type of the tile. In some tile-based games, that might be
// the terrain type. For us, we only need to differentiate between empty space
// and floor (a.k.a. the station structure/scaffold). Walls/Doors/etc... will be
// InstalledObjects sitting on top of the floor.
public enum TileType { Empty, Floor };

public enum ENTERABILITY { Yes, Never, Soon };

public class Tile : IXmlSerializable
{
    private TileType _type = TileType.Empty;
    public TileType Type
    {
        get { return _type; }
        set
        {
            TileType oldType = _type;
            _type = value;
            // Call the callback and let things know we've changed.

            if (cbTileChanged != null && oldType != _type)
            {
                cbTileChanged(this);
            }
        }
    }

    // LooseObject is something like a drill or a stack of metal sitting on the floor
    public Inventory inventory;
    public Inventory Inventory { get => inventory; protected set => inventory = value; }

    public Room room;

    // Furniture is something like a wall, door, or sofa.
    public Furniture furniture
    {
        get; protected set;
    }

    // FIXME: This seems like a terrible way to flag if a job is pending
    // on a tile.  This is going to be prone to errors in set/clear.
    public Job pendingFurnitureJob;

    // We need to know the context in which we exist. Probably. Maybe.
    public World world { get; protected set; }

    public int X { get; protected set; }
    public int Y { get; protected set; }

    // FIXME: This is just hardcoded for now.  Basically just a reminder of something we
    // might want to do more with in the future.
    const float baseTileMovementCost = 1;

    public float movementCost
    {
        get
        {

            if (Type == TileType.Empty)
                return 0;   // 0 is unwalkable

            if (furniture == null)
                return baseTileMovementCost;

            return baseTileMovementCost * furniture.movementCost;
        }
    }

    // The function we callback any time our tile's data changes
    Action<Tile> cbTileChanged;

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Initializes a new instance of the <see cref="Tile"/> class.
    /// </summary>
    /// <param name="world">A World instance.</param>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    public Tile(World world, int x, int y)
    {
        this.world = world;
        this.X = x;
        this.Y = y;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Register a function to be called back when our tile type changes.
    /// </summary>
    public void RegisterTileTypeChangedCallback(Action<Tile> callback)
    {
        cbTileChanged += callback;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Unregister a callback.
    /// </summary>
    public void UnregisterTileTypeChangedCallback(Action<Tile> callback)
    {
        cbTileChanged -= callback;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public bool PlaceFurniture(Furniture objInstance)
    {
        if (objInstance == null)
        {
            // We are uninstalling whatever was here before.
            furniture = null;
            return true;
        }

        // objInstance isn't null

        if (furniture != null)
        {
            Debug.LogError("Trying to assign a furniture to a tile that already has one!");
            return false;
        }

        // At this point, everything's fine!

        furniture = objInstance;
        return true;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public bool PlaceInventory(Inventory inv)
    {
        if (inv == null)
        {
            inventory = null;
            return true;
        }

        if (inventory != null)
        {
            //  Ok, there is already something here, maybe we can combine a stack.
            if (inventory.objectType != inv.objectType)
            {
                Debug.LogError("Trying to assign inventory to a tile that has some form of different type of inventory here!");
                return false;
            }

            //  We want to make sure that the stack size of the item doesn't exeed it's max size.
            //  If it does, the inventories can be combined with some leftover
            int numToMove = inv.stackSize;
            if (inventory.stackSize + numToMove > inventory.maxStackSize)
            {
                numToMove = inventory.maxStackSize -= inventory.stackSize;
            }

            inventory.stackSize += numToMove;
            inv.stackSize -= numToMove;

            return true;
        }

        //  At this point, we know that the current inventory is null!
        //  Which means that either there is no inventory (stuff on the ground) here or
        //  there is something here, but the inventory stacks can be combined.

        //  TODO: So, the inventory manager needs to know that the old stack needs to be removed from the list.
        inventory = inv.Clone();
        inventory.tile = this;
        inv.stackSize = 0;

        return true;

    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    // Tells us if two tiles are adjacent.
    public bool IsNeighbour(Tile tile, bool diagOkay = false)
    {
        // Check to see if we have a difference of exactly ONE between the two
        // tile coordinates.  Is so, then we are vertical or horizontal neighbours.
        return
            Mathf.Abs(this.X - tile.X) + Mathf.Abs(this.Y - tile.Y) == 1 ||  // Check hori/vert adjacency
            (diagOkay && (Mathf.Abs(this.X - tile.X) == 1 && Mathf.Abs(this.Y - tile.Y) == 1)) // Check diag adjacency
            ;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Gets the neighbours.
    /// </summary>
    /// <returns>The neighbours.</returns>
    /// <param name="diagOkay">Is diagonal movement okay?.</param>
    public Tile[] GetNeighbours(bool diagOkay = false)
    {
        Tile[] ns;

        if (diagOkay == false)
        {
            ns = new Tile[4];   // Tile order: N E S W
        }
        else
        {
            ns = new Tile[8];   // Tile order : N E S W NE SE SW NW
        }

        Tile n;

        n = world.GetTileAt(X, Y + 1);
        ns[0] = n;  // Could be null, but that's okay.
        n = world.GetTileAt(X + 1, Y);
        ns[1] = n;  // Could be null, but that's okay.
        n = world.GetTileAt(X, Y - 1);
        ns[2] = n;  // Could be null, but that's okay.
        n = world.GetTileAt(X - 1, Y);
        ns[3] = n;  // Could be null, but that's okay.

        if (diagOkay == true)
        {
            n = world.GetTileAt(X + 1, Y + 1);
            ns[4] = n;  // Could be null, but that's okay.
            n = world.GetTileAt(X + 1, Y - 1);
            ns[5] = n;  // Could be null, but that's okay.
            n = world.GetTileAt(X - 1, Y - 1);
            ns[6] = n;  // Could be null, but that's okay.
            n = world.GetTileAt(X - 1, Y + 1);
            ns[7] = n;  // Could be null, but that's okay.
        }

        return ns;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public ENTERABILITY IsEnterable()
    {
        // This returns true if you can enter this tile right this moment.
        if (movementCost == 0)
            return ENTERABILITY.Never;

        // Check out furniture to see if it has a special block on enterability
        if (furniture != null && furniture.IsEnterable != null)
        {
            return furniture.IsEnterable(furniture);
        }

        return ENTERABILITY.Yes;
    }

    public Tile North()
    {
        return world.GetTileAt(X, Y + 1);
    }
    public Tile South()
    {
        return world.GetTileAt(X, Y - 1);
    }
    public Tile East()
    {
        return world.GetTileAt(X + 1, Y);
    }
    public Tile West()
    {
        return world.GetTileAt(X - 1, Y);
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////
    /// 
    /// 						SAVING & LOADING
    /// 
    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public XmlSchema GetSchema()
    {
        return null;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public void WriteXml(XmlWriter writer)
    {
        writer.WriteAttributeString("X", X.ToString());
        writer.WriteAttributeString("Y", Y.ToString());
        writer.WriteAttributeString("Type", ((int)Type).ToString());
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public void ReadXml(XmlReader reader)
    {
        Type = (TileType)int.Parse(reader.GetAttribute("Type"));
    }
}
