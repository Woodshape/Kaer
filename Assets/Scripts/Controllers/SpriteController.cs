//=======================================================================
// Copyright Martin "quill18" Glaude 2015.
//		http://quill18.com
//=======================================================================

using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

public class SpriteController : MonoBehaviour {


	// The only tile sprite we have right now, so this
	// it a pretty simple way to handle it.
	public Sprite floorSprite;	// FIXME!
    public Sprite emptySprite;

	Dictionary<Tile, GameObject> tileGameObjectMap;
	Dictionary<Furniture, GameObject> furnitureGameObjectMap;

	Dictionary<string, Sprite> furnitureSprites;

	World world {
		get { return WorldController.Instance.world;}
	}

	// Use this for initialization
	void OnStart () {

		LoadSprites();


		// Instantiate our dictionary that tracks which GameObject is rendering which Tile data.
		tileGameObjectMap = new Dictionary<Tile, GameObject>();
		furnitureGameObjectMap = new Dictionary<Furniture, GameObject>();


		// Create a GameObject for each of our tiles, so they show visually. (and redunt reduntantly)
		for (int x = 0; x < world.Width; x++) {
			for (int y = 0; y < world.Height; y++) {
				// Get the tile data
				Tile tile_data = world.GetTileAt(x, y);

				// This creates a new GameObject and adds it to our scene.
				GameObject tile_go = new GameObject();

				// Add our tile/GO pair to the dictionary.
				tileGameObjectMap.Add( tile_data, tile_go );

				tile_go.name = "Tile_" + x + "_" + y;
				tile_go.transform.position = new Vector3( tile_data.X, tile_data.Y, 0);
				tile_go.transform.SetParent(this.transform, true);

				// Add a sprite renderer, but don't bother setting a sprite
				// because all the tiles are empty right now.
				tile_go.AddComponent<SpriteRenderer>().sprite = emptySprite;


			}
		}


		// Register our callback so that our GameObject gets updated whenever
		// the tile's type changes.

		world.RegisterTileChanged( OnTileChanged );
		world.RegisterFurnitureCreated(OnFurnitureCreated);

	}

	void LoadSprites() {
		furnitureSprites = new Dictionary<string, Sprite>();
		Sprite[] sprites = Resources.LoadAll<Sprite>("Images/Furniture/");

		Debug.Log("LOADED RESOURCE:");
		foreach(Sprite s in sprites) {
			Debug.Log(s);
			furnitureSprites[s.name] = s;
		}
	}

	// Update is called once per frame
	void Update () {

	}

	// THIS IS AN EXAMPLE -- NOT CURRENTLY USED (and probably out of date)
	void DestroyAllTileGameObjects() {
		// This function might get called when we are changing floors/levels.
		// We need to destroy all visual **GameObjects** -- but not the actual tile data!

		while(tileGameObjectMap.Count > 0) {
			Tile tile_data = tileGameObjectMap.Keys.First();
			GameObject tile_go = tileGameObjectMap[tile_data];

			// Remove the pair from the map
			tileGameObjectMap.Remove(tile_data);

			// Unregister the callback!
			tile_data.UnregisterTileTypeChangedCallback( OnTileChanged );

			// Destroy the visual GameObject
			Destroy( tile_go );
		}

		// Presumably, after this function gets called, we'd be calling another
		// function to build all the GameObjects for the tiles on the new floor/level
	}

	// This function should be called automatically whenever a tile's type gets changed.
	void OnTileChanged( Tile tile_data ) {

		if(tileGameObjectMap.ContainsKey(tile_data) == false) {
			Debug.LogError("tileGameObjectMap doesn't contain the tile_data -- did you forget to add the tile to the dictionary? Or maybe forget to unregister a callback?");
			return;
		}

		GameObject tile_go = tileGameObjectMap[tile_data];

		if(tile_go == null) {
			Debug.LogError("tileGameObjectMap's returned GameObject is null -- did you forget to add the tile to the dictionary? Or maybe forget to unregister a callback?");
			return;
		}

		if(tile_data.Type == TileType.Floor) {
			tile_go.GetComponent<SpriteRenderer>().sprite = floorSprite;
		}
		else if( tile_data.Type == TileType.Empty ) {
			tile_go.GetComponent<SpriteRenderer>().sprite = null;
		}
		else {
			Debug.LogError("OnTileChanged - Unrecognized tile type.");
		}


	}
		

	public void OnFurnitureCreated( Furniture furn ) {
		//Debug.Log("OnFurnitureCreated");
		// Create a visual GameObject linked to this data.

		// FIXME: Does not consider multi-tile objects nor rotated objects

		// This creates a new GameObject and adds it to our scene.
		GameObject furn_go = new GameObject();

		// Add our tile/GO pair to the dictionary.
		furnitureGameObjectMap.Add( furn, furn_go );

		furn_go.name = furn.objectType + "_" + furn.tile.X + "_" + furn.tile.Y;
		furn_go.transform.position = new Vector3( furn.tile.X, furn.tile.Y, 0);
		furn_go.transform.SetParent(this.transform, true);

		furn_go.AddComponent<SpriteRenderer>().sprite = GetSpriteForFurniture(furn);

		// Register our callback so that our GameObject gets updated whenever
		// the object's into changes.
		furn.RegisterOnChangedCallback( OnFurnitureChanged );

	}

	void OnFurnitureChanged( Furniture furn ) {
		//Debug.Log("OnFurnitureChanged");
		// Make sure the furniture's graphics are correct.

		if(furnitureGameObjectMap.ContainsKey(furn) == false) {
			Debug.LogError("OnFurnitureChanged -- trying to change visuals for furniture not in our map.");
			return;
		}

		GameObject furn_go = furnitureGameObjectMap[furn];
		//Debug.Log(furn_go);
		//Debug.Log(furn_go.GetComponent<SpriteRenderer>());

		furn_go.GetComponent<SpriteRenderer>().sprite = GetSpriteForFurniture(furn);
	}




	Sprite GetSpriteForFurniture(Furniture obj) {
		if(obj.linksToNeighbour == false) {
			return furnitureSprites[obj.objectType];
		}

		// Otherwise, the sprite name is more complicated.

		string spriteName = obj.objectType + "_";

		// Check for neighbours North, East, South, West

		int x = obj.tile.X;
		int y = obj.tile.Y;

		Tile t;

		t = world.GetTileAt(x, y+1);
		if(t != null && t.furniture != null && t.furniture.objectType == obj.objectType) {
			spriteName += "N";
		}
		t = world.GetTileAt(x+1, y);
		if(t != null && t.furniture != null && t.furniture.objectType == obj.objectType) {
			spriteName += "E";
		}
		t = world.GetTileAt(x, y-1);
		if(t != null && t.furniture != null && t.furniture.objectType == obj.objectType) {
			spriteName += "S";
		}
		t = world.GetTileAt(x-1, y);
		if(t != null && t.furniture != null && t.furniture.objectType == obj.objectType) {
			spriteName += "W";
		}

		// For example, if this object has all four neighbours of
		// the same type, then the string will look like:
		//       Wall_NESW

		if(furnitureSprites.ContainsKey(spriteName) == false) {
			Debug.LogError("GetSpriteForInstalledObject -- No sprites with name: " + spriteName);
			return null;
		}

		return furnitureSprites[spriteName];

	}

}
