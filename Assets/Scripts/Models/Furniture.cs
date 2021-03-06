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

// InstalledObjects are things like walls, doors, and furniture (e.g. a sofa)

public class Furniture : IXmlSerializable {
    protected Dictionary<string, float> furnParameters;
    protected Action<Furniture, float> updateActions;

    //  A list of jobs the furniture creates on its own (so, no construction jobs etc.)
    List<Job> jobs;

    public Func<Furniture, Enterability> IsEnterable;

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public void Update(float deltaTime) {
        if (updateActions != null) {
            updateActions(this, deltaTime);
        }
    }

    // This represents the BASE tile of the object -- but in practice, large objects may actually occupy
    // multile tiles.
    public Tile tile { get; protected set; }

    // This "objectType" will be queried by the visual system to know what sprite to render for this object
    public string objectType { get; protected set; }

    // This is a multipler. So a value of "2" here, means you move twice as slowly (i.e. at half speed)
    // Tile types and other environmental effects may be combined.
    // For example, a "rough" tile (cost of 2) with a table (cost of 3) that is on fire (cost of 3)
    // would have a total movement cost of (2+3+3 = 8), so you'd move through this tile at 1/8th normal speed.
    // SPECIAL: If movementCost = 0, then this tile is impassible. (e.g. a wall).
    public float movementCost { get; protected set; }

    public bool roomEnclosure { get; protected set; }

    // For example, a sofa might be 3x2 (actual graphics only appear to cover the 3x1 area, but the extra row is for leg room.)
    int width;
    int height;

    public bool linksToNeighbour { get; protected set; }

    public Action<Furniture> cbOnChanged;

    Func<Tile, bool> funcPositionValidation;

    // TODO: Implement larger objects
    // TODO: Implement object rotation

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////
    // Empty constructor is used for serialization
    public Furniture() {
        furnParameters = new Dictionary<string, float>();
        jobs = new List<Job>();
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    // Copy Constructor
    protected Furniture(Furniture other) {
        this.objectType = other.objectType;
        this.movementCost = other.movementCost;
        this.roomEnclosure = other.roomEnclosure;
        this.width = other.width;
        this.height = other.height;
        this.linksToNeighbour = other.linksToNeighbour;

        this.furnParameters = new Dictionary<string, float>(other.furnParameters);
        this.jobs = new List<Job>();

        if (other.updateActions != null)
            this.updateActions = (Action<Furniture, float>) other.updateActions.Clone();

        this.IsEnterable = other.IsEnterable;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    virtual public Furniture Clone() {
        return new Furniture(this);
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    // Create furniture from parameters -- this will probably ONLY ever be used for prototypes
    public Furniture(string objectType, float movementCost = 1f, int width = 1, int height = 1, bool linksToNeighbour = false,
        bool roomEnclosure = false) {
        this.objectType = objectType;
        this.movementCost = movementCost;
        this.roomEnclosure = roomEnclosure;
        this.width = width;
        this.height = height;
        this.linksToNeighbour = linksToNeighbour;

        this.funcPositionValidation = this.DEFAULT__IsValidPosition;

        furnParameters = new Dictionary<string, float>();
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public float GetFurnitureParameter(string key, float default_val = 0.0f) {
        if (furnParameters.ContainsKey(key) == false) {
            return default_val;
        }

        return furnParameters[key];
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public void SetFurnitureParameter(string key, float value) {
        furnParameters[key] = value;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public void ChangeFurnitureParameter(string key, float value) {
        if (furnParameters.ContainsKey(key) == false) {
            furnParameters[key] = value;
        }
        else {
            furnParameters[key] += value;
        }
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    static public Furniture PlaceInstance(Furniture proto, Tile tile) {
        if (proto.funcPositionValidation(tile) == false) {
            Debug.LogError("[Furniture::PlaceInstance] Position Validity Function returned FALSE.");
            return null;
        }

        // We know our placement destination is valid.
        Furniture obj = proto.Clone();
        obj.tile = tile;

        // FIXME: This assumes we are 1x1!
        if (tile.PlaceFurniture(obj) == false) {
            // For some reason, we weren't able to place our object in this tile.
            // (Probably it was already occupied.)

            // Do NOT return our newly instantiated object.
            // (It will be garbage collected.)
            return null;
        }

        if (obj.linksToNeighbour) {
            // This type of furniture links itself to its neighbours,
            // so we should inform our neighbours that they have a new
            // buddy.  Just trigger their OnChangedCallback.

            Tile t;
            int x = tile.X;
            int y = tile.Y;

            t = tile.world.GetTileAt(x, y + 1);
            if (t != null && t.furniture != null && t.furniture.cbOnChanged != null && t.furniture.objectType == obj.objectType) {
                // We have a Northern Neighbour with the same object type as us, so
                // tell it that it has changed by firing is callback.
                t.furniture.cbOnChanged(t.furniture);
            }

            t = tile.world.GetTileAt(x + 1, y);
            if (t != null && t.furniture != null && t.furniture.cbOnChanged != null && t.furniture.objectType == obj.objectType) {
                t.furniture.cbOnChanged(t.furniture);
            }

            t = tile.world.GetTileAt(x, y - 1);
            if (t != null && t.furniture != null && t.furniture.cbOnChanged != null && t.furniture.objectType == obj.objectType) {
                t.furniture.cbOnChanged(t.furniture);
            }

            t = tile.world.GetTileAt(x - 1, y);
            if (t != null && t.furniture != null && t.furniture.cbOnChanged != null && t.furniture.objectType == obj.objectType) {
                t.furniture.cbOnChanged(t.furniture);
            }
        }

        return obj;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public void RegisterOnChangedCallback(Action<Furniture> callbackFunc) {
        cbOnChanged += callbackFunc;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public void UnregisterOnChangedCallback(Action<Furniture> callbackFunc) {
        cbOnChanged -= callbackFunc;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public bool IsValidPosition(Tile t) {
        return funcPositionValidation(t);
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    // FIXME: These functions should never be called directly,
    // so they probably shouldn't be public functions of Furniture
    protected bool DEFAULT__IsValidPosition(Tile t) {
        // Make sure tile is FLOOR
        if (t.Type != TileType.Floor) {
            return false;
        }

        // Make sure tile doesn't already have furniture
        if (t.furniture != null) {
            return false;
        }

        return true;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public int JobCount() {
        return jobs.Count;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public void AddJob(Job job) {
        jobs.Add(job);

        tile.world.jobQueue.Enqueue(job);
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public void RemoveJob(Job job) {
        jobs.Remove(job);
        job.CancelJob();
        tile.world.jobQueue.Remove(job);
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public void ClearJobs() {
        foreach (var job in jobs) {
            RemoveJob(job);
        }
    }
    
    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public bool IsStockpile() {
        return objectType.Equals("Stockpile");
    }

    //  Registers an action that will be called every Update
    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public void RegisterUpdateAction(Action<Furniture, float> action) {
        updateActions += action;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public void UnregisterUpdateAction(Action<Furniture, float> action) {
        updateActions -= action;
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

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public void WriteXml(XmlWriter writer) {
        writer.WriteAttributeString("X", tile.X.ToString());
        writer.WriteAttributeString("Y", tile.Y.ToString());
        writer.WriteAttributeString("objectType", objectType);
        //writer.WriteAttributeString( "movementCost", movementCost.ToString() );

        foreach (string k in furnParameters.Keys) {
            writer.WriteStartElement("Param");
            writer.WriteAttributeString("name", k);
            writer.WriteAttributeString("value", furnParameters[k].ToString());
            writer.WriteEndElement();
        }
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public void ReadXml(XmlReader reader) {
        // X, Y, and objectType have already been set, and we should already
        // be assigned to a tile.  So just read extra data.

        //movementCost = int.Parse( reader.GetAttribute("movementCost") );

        if (reader.ReadToDescendant("Param")) {
            do {
                string k = reader.GetAttribute("name");
                float v = float.Parse(reader.GetAttribute("value"));
                furnParameters[k] = v;
            } while (reader.ReadToNextSibling("Param"));
        }
    }
}
