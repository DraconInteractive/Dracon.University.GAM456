using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TiledWorldGenerator : MonoBehaviour {
	// defines the information for a tile
	[System.Serializable]
	public class AvailableTile
	{
		public GameObject Prefab;
		public int Frequency = 1;

		protected float _threshold;
		public float Threshold
		{
			get
			{
				return _threshold;
			}
			set
			{
				_threshold = value;
			}
		}
	}

	// suppoted tiles to generate
	public AvailableTile[] TilePalette;

	// dimensions of the world to generate
	[RangeAttribute(1, 1000)]
	public int Length = 20;
	[RangeAttribute(1, 1000)]
	public int Width = 20;

	protected Tile[] world;

	// Use this for initialization
	void Start () {
		NormaliseProbabilities();
		ClearWorld();
		GenerateWorld();
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	void NormaliseProbabilities() {
		// sum all of the tile frequencies
		int frequencySum = 0;
		foreach(AvailableTile tile in TilePalette) {
			frequencySum += tile.Frequency;
		}

		// set the overall probability thresholds
		float currentThreshold = 0;
		for(int index = 0; index < TilePalette.Length; ++index) {
			currentThreshold += (float)TilePalette[index].Frequency / (float)frequencySum;
			TilePalette[index].Threshold = currentThreshold;
		}
	}

	void ClearWorld() {
		// Find all objects with tile class attached
		Tile[] tiles = FindObjectsOfType<Tile>();
		
		// Delete all of the tiles
		for (int index = 0; index < tiles.Length; ++index)
			GameObject.Destroy(tiles[index]);

		// clear the world array
		world = null;
	}

	void GenerateWorld() {
		// invalid tile palette?
		if (TilePalette.Length == 0)
		{
			Debug.LogError("No entries in the tile palette!");
			return;
		}

		// reset the world array
		world = new Tile[Length * Width];

		// generate the world
		for(int lengthIndex = 0; lengthIndex < Length; ++lengthIndex)
		{
			for(int widthIndex = 0; widthIndex < Width; ++widthIndex)
			{
				// Roll a random number from 0 to 1
				float roll = Random.Range(0f, 1f);

				// find the matching prefab (default to pure random)
				GameObject prefab = null;
				foreach(AvailableTile tile in TilePalette) {
					if (roll <= tile.Threshold) {
						prefab = tile.Prefab;
						break;
					}
				}
				if (prefab == null) {
					prefab = TilePalette[Random.Range(0, TilePalette.Length)].Prefab;
				}

				// instantiate the prefab
				GameObject newTile = Instantiate(prefab, Vector3.forward * lengthIndex + Vector3.right * widthIndex + Vector3.up * Random.Range(0.0f, 0.5f), Quaternion.identity);
				newTile.transform.SetParent(transform);
				world[lengthIndex * Width + widthIndex] = newTile.GetComponent<Tile>();
			}
		}
	}
}
