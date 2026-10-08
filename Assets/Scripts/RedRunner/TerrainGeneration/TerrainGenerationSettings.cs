using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RedRunner.TerrainGeneration
{

	[CreateAssetMenu (menuName = "Create Terrain Generator Settings")]
	public class TerrainGenerationSettings : ScriptableObject
	{

		[SerializeField]
		protected float m_LevelLength = 200f;
		[SerializeField]
		protected int m_StartBlocksCount = 1;
		[SerializeField]
		protected int m_MiddleBlocksCount = -1;
		[SerializeField]
		protected int m_EndBlocksCount = 1;
		[SerializeField]
		protected Block[] m_StartBlocks;
		[SerializeField]
		protected Block[] m_MiddleBlocks;
		[SerializeField]
		protected Block[] m_EndBlocks;
		[SerializeField]
		protected BackgroundLayer[] m_BackgroundLayers;

		public float LevelLength {
			get {
				return m_LevelLength;
			}
		}

		public int StartBlocksCount {
			get {
				return m_StartBlocksCount;
			}
		}

		public int MiddleBlocksCount {
			get {
				return m_MiddleBlocksCount;
			}
		}

		public int EndBlocksCount {
			get {
				return m_EndBlocksCount;
			}
		}

		public Block[] StartBlocks {
			get {
				return m_StartBlocks;
			}
		}

		public Block[] MiddleBlocks {
			get {
				return m_MiddleBlocks;
			}
		}

		public Block[] EndBlocks {
			get {
				return m_EndBlocks;
			}
		}

		public BackgroundLayer[] BackgroundLayers {
			get {
				return m_BackgroundLayers;
			}
		}

		public List<LevelBlock> Layout ( int seed )
		{
			if ( m_StartBlocksCount <= 0 || m_MiddleBlocksCount <= 0 || m_EndBlocksCount <= 0 )
			{
				throw new System.InvalidOperationException ( name + " needs positive start, middle and end block counts to lay out a seeded level" );
			}
			List<LevelBlock> layout = new List<LevelBlock> ();
			foreach ( RedRunner.Simulation.PlacedBlock placed in RedRunner.Simulation.LevelLayout.Lay ( seed, Choices ( m_StartBlocks ), m_StartBlocksCount, Choices ( m_MiddleBlocks ), m_MiddleBlocksCount, Choices ( m_EndBlocks ), m_EndBlocksCount ) )
			{
				Block[] blocks = placed.Part == RedRunner.Simulation.LevelPart.Start ? m_StartBlocks : placed.Part == RedRunner.Simulation.LevelPart.Middle ? m_MiddleBlocks : m_EndBlocks;
				layout.Add ( new LevelBlock ( blocks [ placed.Choice ], placed.X, ( LevelPart )placed.Part ) );
			}
			return layout;
		}

		public static List<RedRunner.Simulation.BlockChoice> Choices ( Block[] blocks )
		{
			List<RedRunner.Simulation.BlockChoice> choices = new List<RedRunner.Simulation.BlockChoice> ( blocks.Length );
			foreach ( Block block in blocks )
			{
				choices.Add ( new RedRunner.Simulation.BlockChoice ( block.Width, block.Probability ) );
			}
			return choices;
		}

	}

}