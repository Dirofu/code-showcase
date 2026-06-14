using System;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.UI;
using TMPro;
using Shared.Extensions.Unity;

namespace Core.Scripts.UI.Universal.ViewUICustomer
{
	[Serializable][FoldoutGroup("Element settings")]
	public class ViewUIElement<TSlotElementType> 
		where TSlotElementType : Enum
	{
		[ShowIf("_isVisible")]
		[SerializeField] private TSlotElementType _slotType;
		[ShowIf("_isVisible")]
		[SerializeField] private GameObject _element;
		[ShowIf("_isVisible")]
		[SerializeField] private bool _isActive;
		[ShowIf("_isVisible")]
		[SerializeField] private Color _color;
		[ShowIf("_isVisible")]
		[SerializeField] private Material _material;
		[ShowIf("_isVisible")]
		[SerializeField] private Vector3 _rect;
		[ShowIf("_isVisible")]
		[SerializeField] private Vector2 _sizeDelta;

		private bool _isVisible = true;

		public TSlotElementType SlotType => _slotType;

		public void UpdateVisibleState(bool newState) => _isVisible = newState;

		public void SaveSettings(GameObject element, TSlotElementType type)
		{
			_slotType = type;
			_element = element;
			_isActive = element.activeSelf;
			_rect = element.GetRectTransform().localPosition;
			_sizeDelta = element.GetRectTransform().sizeDelta;

			if (element.TryGetComponent(out Image image))
				SaveSettings(image);
			else if (element.TryGetComponent(out TMP_Text text))
				SaveSettings(text);
		}

		public void SetSettings()
		{
			if (_isActive == false)
			{
				_element.SetActive(false);
				return;
			}

			_element.SetActive(true);
			_element.GetRectTransform().localPosition = _rect;

			if (_sizeDelta != Vector2.zero)
				_element.GetRectTransform().sizeDelta = _sizeDelta;

			if (_element.TryGetComponent(out Image image))
				SetSettings(image);
			else if (_element.TryGetComponent(out TMP_Text text))
				SetSettings(text);
		}

		private void SaveSettings(Image image)
		{
			_color = image.color;
			_material = image.material;
		}

		private void SaveSettings(TMP_Text text)
		{
			_color = text.color;
		}

		private void SetSettings(Image image)
		{
			image.color = _color;
			image.material = _material;
		}

		private void SetSettings(TMP_Text text)
		{
			text.color = _color;
		}
	}
}