using System;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;

namespace Core.Scripts.UI.Universal.ViewUICustomer
{
	[Serializable] [FoldoutGroup("Group settings")]
	public class ViewUIGroupType<TType, TSlotElementType>
		where TType : Enum
		where TSlotElementType: Enum
	{
		[SerializeField, ShowIf("_isVisible")] protected ViewUIStateType _type;
		[SerializeField, ShowIf("_isVisible")] protected List<ViewUIElement<TSlotElementType>> _elements = new();

		private bool _isVisible = true;

		private Dictionary<TSlotElementType, ViewUIElement<TSlotElementType>> _elementsDic = new();
		
		public ViewUIStateType Type => _type;
		public ViewUIElement<TSlotElementType> this[TSlotElementType stateType] => _elementsDic[stateType];
		public Dictionary<TSlotElementType, ViewUIElement<TSlotElementType>> Elements => _elementsDic;

		public void UpdateVisibleState(bool newState) => _isVisible = newState;

		public void InitDictionary()
		{
			foreach (var element in _elements)
				_elementsDic.Add(element.SlotType, element);
		}

		public void AddNewElement(ViewUIStateType elementType, TSlotElementType type, ViewUIElement<TSlotElementType> newElement, GameObject @object)
		{
			_type = elementType;
			newElement.SaveSettings(@object, type);
			_elements.Add(newElement);
			_elementsDic.Add(type, newElement);
		}

		public void RewriteElement(ViewUIStateType elementType, TSlotElementType type, ViewUIElement<TSlotElementType> element, GameObject @object)
		{
			_type = elementType;
			element.SaveSettings(@object, type);
		}

		public void UpdateElements()
		{
			_elementsDic = _elements.ToDictionary(state => state.SlotType);
		}

		public void Reset()
		{
			_elements.Clear();
			_elementsDic.Clear();
		}
	}
}