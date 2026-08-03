using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

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
		protected AssetLabelReference StartBlocksLabel;
		[SerializeField]
		protected AssetLabelReference MiddleBlocksLabel;
		[SerializeField]
		protected AssetLabelReference EndBlocksLabel;
		[System.NonSerialized]
		protected Block[] m_StartBlocks;
		[System.NonSerialized]
		protected Block[] m_MiddleBlocks;
		[System.NonSerialized]
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

		public AssetLabelReference StartBlocksLabelReference {
			get {
				return StartBlocksLabel;
			}
		}
		public AssetLabelReference MiddleBlocksLabelReference {
			get {
				return MiddleBlocksLabel;
			}
		}
		public AssetLabelReference EndBlocksLabelReference {
			get {
				return EndBlocksLabel;
			}
		}

		public BackgroundLayer[] BackgroundLayers {
			get {
				return m_BackgroundLayers;
			}
		}
		public void SetBlocks (Block[] startBlocks, Block[] middleBlocks)
		{
			m_StartBlocks = startBlocks;
			m_MiddleBlocks = middleBlocks;
		}

	}

}