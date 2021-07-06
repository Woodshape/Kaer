using UnityEngine;
using System.Collections;

public class AutomaticSize : MonoBehaviour {

    public float childHeight = 50f;

	// Use this for initialization
	void Start () {
        AdjustSize();
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    void AdjustSize()
    {
        Vector2 size = this.GetComponent<RectTransform>().sizeDelta;
        size.y = this.transform.childCount * childHeight;

        this.GetComponent<RectTransform>().sizeDelta = size;
    }
}
