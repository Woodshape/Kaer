//=======================================================================
// Copyright Martin "quill18" Glaude 2015.
//		http://quill18.com
//=======================================================================

using UnityEngine;
using System.Collections.Generic;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public class World : IXmlSerializable {
    // A two-dimensional array to hold our tile data.
    Tile[,] tiles;
    public List<Character> characters;
    public List<Furniture> furnitures;
    public List<Room> rooms;

    public InventoryManager inventoryManager;

    // The pathfinding graph used to navigate our world map.
    public Path_TileGraph tileGraph;

    Dictionary<string, Furniture> furniturePrototypes;
    public Dictionary<string, Job> furnitureJobPrototypes;

    // The tile width of the world.
    public int Width { get; protected set; }

    // The tile height of the world
    public int Height { get; protected set; }

    Action<Furniture> cbFurnitureCreated;
    Action<Character> cbCharacterCreated;
    Action<Inventory> cbInventoryCreated;
    Action<Tile> cbTileChanged;

    // TODO: Most likely this will be replaced with a dedicated
    // class for managing job queues (plural!) that might also
    // be semi-static or self initializing or some damn thing.
    // For now, this is just a PUBLIC member of World
    public JobQueue jobQueue;

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Initializes a new instance of the <see cref="World"/> class.
    /// </summary>
    /// <param name="width">Width in tiles.</param>
    /// <param name="height">Height in tiles.</param>
    public World(int width, int height) {
        // Creates an empty world.
        SetupWorld(width, height);

        // Make one character
        Character c = CreateCharacter(GetTileAt(Width / 2, Height / 2));
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    //  Default constructor, used when loading a world from a file.
    public World() { }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public Room GetOutsideRoom() {
        return rooms[0];
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public void AddRoom(Room r) {
        rooms.Add(r);
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public void DeleteRoom(Room r) {
        if (r == GetOutsideRoom()) {
            Debug.LogError("Tried to delete the outside room.");
            return;
        }

        // Remove this room from our rooms list.
        rooms.Remove(r);

        // All tiles that belonged to this room should be re-assigned to
        // the outside.
        r.UnAssignAllTiles();
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    void SetupWorld(int width, int height) {
        jobQueue = new JobQueue();

        Width = width;
        Height = height;

        tiles = new Tile[Width, Height];

        rooms = new List<Room>();
        rooms.Add(new Room()); // Create the outside?

        for (int x = 0; x < Width; x++) {
            for (int y = 0; y < Height; y++) {
                tiles[x, y] = new Tile(this, x, y);
                tiles[x, y].RegisterTileTypeChangedCallback(OnTileChanged);
                tiles[x, y].room = GetOutsideRoom(); // Rooms 0 is always going to be outside, and that is our default room
            }
        }

        Debug.Log("World created with " + (Width * Height) + " tiles.");

        CreateFurniturePrototypes();

        characters = new List<Character>();
        furnitures = new List<Furniture>();

        //  DEBUG-ONLY
        inventoryManager = new InventoryManager();
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public void Update(float deltaTime) {
        foreach (Character c in characters) {
            c.Update(deltaTime);
        }

        foreach (Furniture f in furnitures) {
            f.Update(deltaTime);
        }
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public Character CreateCharacter(Tile t) {
        Debug.Log("CreateCharacter");
        Character c = new Character(t);

        characters.Add(c);

        if (cbCharacterCreated != null)
            cbCharacterCreated(c);

        return c;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    void CreateFurniturePrototypes() {
        // This will be replaced by a function that reads all of our furniture data
        // from a text file in the future.

        furniturePrototypes = new Dictionary<string, Furniture>();
        furnitureJobPrototypes = new Dictionary<string, Job>();

        ///

        furniturePrototypes.Add("Stockpile",
            new Furniture(
                "Stockpile",
                1, // Doesn't take up any movement
                1, // Width
                1, // Height
                false, // Links to neighbours and "sort of" becomes part of a large object
                false // Enclose rooms
            )
        );

        furnitureJobPrototypes.Add("Stockpile",
            new Job(
                null,
                "Stockpile",
                FurnitureActions.JobComplete_BuildFurniture,
                -1.0f, // Will be placed instantly without worker moving to finish the job
                null
            )
        );

        ///

        furniturePrototypes.Add("Wall",
            new Furniture(
                "Wall",
                0, // Impassable
                1, // Width
                1, // Height
                true, // Links to neighbours and "sort of" becomes part of a large object
                true // Enclose rooms
            )
        );

        furnitureJobPrototypes.Add("Wall",
            new Job(
                null,
                "Wall",
                FurnitureActions.JobComplete_BuildFurniture,
                1.0f,
                new Inventory[] {new Inventory("steel_plate", 0, 5)}
            )
        );

        ///

        furniturePrototypes.Add("Door",
            new Furniture(
                "Door",
                1, // Door pathfinding cost
                1, // Width
                1, // Height
                false, // Links to neighbours and "sort of" becomes part of a large object
                true // Enclose rooms
            )
        );

        furnitureJobPrototypes.Add("Door",
            new Job(
                null,
                "Door",
                FurnitureActions.JobComplete_BuildFurniture,
                1.0f,
                new Inventory[] {new Inventory("steel_plate", 0, 3)}
            )
        );

        // What if the object behaviours were scriptable? And therefore were part of the text file
        // we are reading in now?

        /// DOOR    ///
        furniturePrototypes["Door"].SetFurnitureParameter("openness", 0);
        furniturePrototypes["Door"].SetFurnitureParameter("is_opening", 0);
        furniturePrototypes["Door"].RegisterUpdateAction(FurnitureActions.Door_UpdateAction);

        furniturePrototypes["Door"].IsEnterable = FurnitureActions.Door_IsEnterable;

        /// STOCKPILE    ///

        furniturePrototypes["Stockpile"].RegisterUpdateAction(FurnitureActions.Stockpile_UpdateAction);
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// A function for testing out the system
    /// </summary>
    public void RandomizeTiles() {
        Debug.Log("RandomizeTiles");
        for (int x = 0; x < Width; x++) {
            for (int y = 0; y < Height; y++) {
                if (UnityEngine.Random.Range(0, 2) == 0) {
                    tiles[x, y].Type = TileType.Empty;
                }
                else {
                    tiles[x, y].Type = TileType.Floor;
                }
            }
        }
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public void SetupPathfindingExample() {
        Debug.Log("SetupPathfindingExample");

        // Make a set of floors/walls to test pathfinding with.

        int l = Width / 2 - 5;
        int b = Height / 2 - 5;

        for (int x = l - 5; x < l + 15; x++) {
            for (int y = b - 5; y < b + 15; y++) {
                tiles[x, y].Type = TileType.Floor;


                if (x == l || x == (l + 9) || y == b || y == (b + 9)) {
                    if (x != (l + 9) && y != (b + 4)) {
                        PlaceFurniture("Wall", tiles[x, y]);
                    }
                }
            }
        }
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Gets the tile data at x and y.
    /// </summary>
    /// <returns>The <see cref="Tile"/>.</returns>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    public Tile GetTileAt(int x, int y) {
        if (x >= Width || x < 0 || y >= Height || y < 0) {
            //Debug.LogError("Tile ("+x+","+y+") is out of range.");
            return null;
        }

        return tiles[x, y];
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public Furniture PlaceFurniture(string objectType, Tile t) {
        //Debug.Log("PlaceInstalledObject");
        // TODO: This function assumes 1x1 tiles -- change this later!

        if (furniturePrototypes.ContainsKey(objectType) == false) {
            Debug.LogError("furniturePrototypes doesn't contain a proto for key: " + objectType);
            return null;
        }

        Furniture furn = Furniture.PlaceInstance(furniturePrototypes[objectType], t);

        if (furn == null) {
            // Failed to place object -- most likely there was already something there.
            return null;
        }

        furnitures.Add(furn);

        // Do we need to recalculate our rooms?
        if (furn.roomEnclosure) {
            Room.DoRoomFloodFill(furn);
        }

        if (cbFurnitureCreated != null) {
            cbFurnitureCreated(furn);

            if (furn.movementCost != 1) {
                // Since tiles return movement cost as their base cost multiplied
                // buy the furniture's movement cost, a furniture movement cost
                // of exactly 1 doesn't impact our pathfinding system, so we can
                // occasionally avoid invalidating pathfinding graphs
                InvalidateTileGraph(); // Reset the pathfinding system
            }
        }

        return furn;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public void RegisterFurnitureCreated(Action<Furniture> callbackfunc) {
        cbFurnitureCreated += callbackfunc;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public void UnregisterFurnitureCreated(Action<Furniture> callbackfunc) {
        cbFurnitureCreated -= callbackfunc;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public void RegisterCharacterCreated(Action<Character> callbackfunc) {
        cbCharacterCreated += callbackfunc;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public void UnregisterCharacterCreated(Action<Character> callbackfunc) {
        cbCharacterCreated -= callbackfunc;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public void RegisterInventoryCreated(Action<Inventory> callbackfunc) {
        cbInventoryCreated += callbackfunc;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public void UnregisterInventoryCreated(Action<Inventory> callbackfunc) {
        cbInventoryCreated -= callbackfunc;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public void RegisterTileChanged(Action<Tile> callbackfunc) {
        cbTileChanged += callbackfunc;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public void UnregisterTileChanged(Action<Tile> callbackfunc) {
        cbTileChanged -= callbackfunc;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    // Gets called whenever ANY tile changes
    void OnTileChanged(Tile t) {
        if (cbTileChanged == null)
            return;

        cbTileChanged(t);

        InvalidateTileGraph();
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    // This should be called whenever a change to the world
    // means that our old pathfinding info is invalid.
    public void InvalidateTileGraph() {
        tileGraph = null;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public bool IsFurniturePlacementValid(string furnitureType, Tile t) {
        return furniturePrototypes[furnitureType].IsValidPosition(t);
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public Furniture GetFurniturePrototype(string objectType) {
        if (furniturePrototypes.ContainsKey(objectType) == false) {
            Debug.LogError("No furniture with type: " + objectType);
            return null;
        }

        return furniturePrototypes[objectType];
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////
    /// 
    /// 						SAVING & LOADING
    /// 
    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public XmlSchema GetSchema() {
        return null;
    }

    public void WriteXml(XmlWriter writer) {
        // Save info here
        writer.WriteAttributeString("Width", Width.ToString());
        writer.WriteAttributeString("Height", Height.ToString());

        writer.WriteStartElement("Tiles");
        for (int x = 0; x < Width; x++) {
            for (int y = 0; y < Height; y++) {
                if (tiles[x, y].Type != TileType.Empty) {
                    writer.WriteStartElement("Tile");
                    tiles[x, y].WriteXml(writer);
                    writer.WriteEndElement();
                }
            }
        }

        writer.WriteEndElement();

        writer.WriteStartElement("Furnitures");
        foreach (Furniture furn in furnitures) {
            writer.WriteStartElement("Furniture");
            furn.WriteXml(writer);
            writer.WriteEndElement();
        }

        writer.WriteEndElement();

        writer.WriteStartElement("Characters");
        foreach (Character c in characters) {
            writer.WriteStartElement("Character");
            c.WriteXml(writer);
            writer.WriteEndElement();
        }

        writer.WriteEndElement();

        /*		writer.WriteStartElement("Width");
                writer.WriteValue(Width);
                writer.WriteEndElement();
        */
    }

    public void ReadXml(XmlReader reader) {
        Debug.Log("World::ReadXml");
        // Load info here

        Width = int.Parse(reader.GetAttribute("Width"));
        Height = int.Parse(reader.GetAttribute("Height"));

        SetupWorld(Width, Height);

        while (reader.Read()) {
            switch (reader.Name) {
                case "Tiles":
                    ReadXml_Tiles(reader);
                    break;
                case "Furnitures":
                    ReadXml_Furnitures(reader);
                    break;
                case "Characters":
                    ReadXml_Characters(reader);
                    break;
            }
        }

        //  DEBUG-ONLY
        Debug.Log("DEBUG: placing steel_plate on ground");
        int w = Width / 2;
        int h = Height / 2;

        //  Create inventory
        Inventory inv = new Inventory("steel_plate", 3);
        Tile t = GetTileAt(w, h);
        inventoryManager.PlaceInventoryOnTile(t, inv);
        if (cbInventoryCreated != null) {
            cbInventoryCreated(t.inventory);
        }

        inv = new Inventory("steel_plate", 10);
        t = GetTileAt(w + 2, h);
        inventoryManager.PlaceInventoryOnTile(t, inv);
        if (cbInventoryCreated != null) {
            cbInventoryCreated(t.inventory);
        }

        inv = new Inventory("steel_plate", 50);
        t = GetTileAt(w, h + 2);
        inventoryManager.PlaceInventoryOnTile(t, inv);
        if (cbInventoryCreated != null) {
            cbInventoryCreated(t.inventory);
        }
    }

    void ReadXml_Tiles(XmlReader reader) {
        Debug.Log("ReadXml_Tiles");
        // We are in the "Tiles" element, so read elements until
        // we run out of "Tile" nodes.

        if (reader.ReadToDescendant("Tile")) {
            // We have at least one tile, so do something with it.

            do {
                int x = int.Parse(reader.GetAttribute("X"));
                int y = int.Parse(reader.GetAttribute("Y"));
                tiles[x, y].ReadXml(reader);
            } while (reader.ReadToNextSibling("Tile"));
        }
    }

    void ReadXml_Furnitures(XmlReader reader) {
        Debug.Log("ReadXml_Furnitures");

        if (reader.ReadToDescendant("Furniture")) {
            do {
                int x = int.Parse(reader.GetAttribute("X"));
                int y = int.Parse(reader.GetAttribute("Y"));

                Furniture furn = PlaceFurniture(reader.GetAttribute("objectType"), tiles[x, y]);
                furn.ReadXml(reader);
            } while (reader.ReadToNextSibling("Furniture"));
        }
    }

    void ReadXml_Characters(XmlReader reader) {
        Debug.Log("ReadXml_Characters");
        if (reader.ReadToDescendant("Character")) {
            do {
                int x = int.Parse(reader.GetAttribute("X"));
                int y = int.Parse(reader.GetAttribute("Y"));

                Character c = CreateCharacter(tiles[x, y]);
                c.ReadXml(reader);
            } while (reader.ReadToNextSibling("Character"));
        }
    }
}
