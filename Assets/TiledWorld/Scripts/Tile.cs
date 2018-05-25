using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour {
	public enum TileType
	{
		Free,
		Obstructed
	}

	public TileType Type;
}
