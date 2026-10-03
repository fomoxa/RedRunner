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
			System.Random random = new System.Random ( seed );
			List<LevelBlock> layout = new List<LevelBlock> ();
			float x = 0f;
			AddBlocks ( layout, m_StartBlocks, m_StartBlocksCount, LevelPart.Start, random, ref x );
			AddBlocks ( layout, m_MiddleBlocks, m_MiddleBlocksCount, LevelPart.Middle, random, ref x );
			AddBlocks ( layout, m_EndBlocks, m_EndBlocksCount, LevelPart.End, random, ref x );
			return layout;
		}

		private static void AddBlocks ( List<LevelBlock> layout, Block[] blocks, int count, LevelPart part, System.Random random, ref float x )
		{
			for ( int i = 0; i < count; i++ )
			{
				Block block = TerrainGenerator.ChooseFrom ( blocks, ( float )random.NextDouble () );
				if ( block == null )
				{
					throw new System.InvalidOperationException ( "no " + part + " block to choose from" );
				}
				layout.Add ( new LevelBlock ( block, x, part ) );
				x += block.Width;
			}
		}

	}

}