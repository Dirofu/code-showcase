using System;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Core.Scripts.NetworkConnection.API;

namespace Core.Scripts.UI
{
	//[CreateAssetMenu(fileName = "new LoadScreenTips", menuName = "Configs/Create LoadScreenTips")]
	public class LoadScreenTips : ScriptableObject
	{
		[SerializeField] private List<TipsByType> _tips;

		private Dictionary<TipType, TipsByType> _cachedDictionary;

		public IReadOnlyDictionary<TipType, TipsByType> Tips
		{
			get
			{
				if (_cachedDictionary == null)
				{
					_cachedDictionary = _tips
						.Where(c => c != null)
						.ToDictionary(c => c.Type, c => c);
				}
				return _cachedDictionary;
			}
		}
	}

	[Serializable]
	public class TipsByType
	{
		[SerializeField] private TipType _type;
		[SerializeField] private List<APILanguageData> _tips;

		public TipType Type => _type;
		public IReadOnlyList<APILanguageData> Tips => _tips;
	}

	public enum TipType
	{
		Any,
		Heroes,
		PVE,
		PVP,
		Functions,
		Narrative
	}
}