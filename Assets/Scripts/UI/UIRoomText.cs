using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class UIRoomText : MonoBehaviour {

	Text myText;
	MouseController mouseController;

	// Use this for initialization
	void Start () {
	
		myText = GetComponent<Text>();

		if (myText == null) {

			this.enabled = false;
			return;
		}
	}
	
	// Update is called once per frame
	void Update () {

		mouseController = GameObject.FindObjectOfType<MouseController>();

		if (mouseController == null) {

			return;
		}

		Tile t = mouseController.GetMouseoverTile();
		myText.text = "Room: " + t.world.rooms.IndexOf( t.room ).ToString();
	
	}
}
