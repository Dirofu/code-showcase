using System;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;

namespace Core.Scripts.UI.Universal.ViewUICustomer
{
	[Serializable] [FoldoutGroup("State settings")]
	public class ViewUIState<TType, TSlotElementType>
		where TType : Enum
		where TSlotElementType : Enum
	{
		[SerializeField, ShowIf("_isVisible")] private TType _stateType;
		[SerializeField, ShowIf("_isVisible")] private List<ViewUIGroupType<TType, TSlotElementType>> _groups = new();

		private bool _isVisible = true;

		private Dictionary<ViewUIStateType, ViewUIGroupType<TType, TSlotElementType>> _groupDic = new();

		public TType StateType => _stateType;
		public IReadOnlyDictionary<ViewUIStateType, ViewUIGroupType<TType, TSlotElementType>> Groups => _groupDic;

		public ViewUIGroupType<TType, TSlotElementType> this[ViewUIStateType type] => _groupDic[type];

		public event Action<ViewUIStateType> StateChanged = delegate { };

		public void UpdateVisibleState(bool newState) => _isVisible = newState;

		public void InitDictionary()
		{
			foreach (var group in _groups)
			{
				group.InitDictionary();
				_groupDic.Add(group.Type, group);
			}
		}

		public void UpdateGroups()
		{
			_groupDic = _groups.ToDictionary(state => state.Type);
		}

		public void AddNewGroup(ViewUIStateType elementType, TType type, ViewUIGroupType<TType, TSlotElementType> newGroup)
		{
			_stateType = type;
			_groups.Add(newGroup);
			_groupDic.Add(elementType, newGroup);
		}

		public void Reset()
		{
			_groups.Clear();
			_groupDic.Clear();
		}
	}
}