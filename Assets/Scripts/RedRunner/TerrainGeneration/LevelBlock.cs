namespace RedRunner.TerrainGeneration
{

	public enum LevelPart
	{
		Start,
		Middle,
		End
	}

	public readonly struct LevelBlock
	{

		public LevelBlock ( Block prefab, float x, LevelPart part )
		{
			Prefab = prefab;
			X = x;
			Part = part;
		}

		public Block Prefab { get; }

		public float X { get; }

		public LevelPart Part { get; }

	}

}
