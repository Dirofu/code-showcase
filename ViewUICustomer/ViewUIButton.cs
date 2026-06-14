using System;
using CommonTools.Audio;
using UnityEngine;
using Core.Scripts.Utils;
using UnityEngine.EventSystems;

namespace Core.Scripts.UI.Universal.ViewUICustomer
{
	[RequireComponent(typeof(RaycastTarget), typeof(CanvasRenderer))]
	public class ViewUIButton : MonoBehaviour, 
		IPointerClickHandler, 
		IPointerEnterHandler,
		IPointerExitHandler,
		IPointerDownHandler,
		IPointerUpHandler
	{
		
		protected SoundsController SoundInst => SoundsController.Instance;
		
		public virtual event Action ButtonPointerEnter = delegate { };
		public virtual event Action ButtonPointerExit = delegate { };
		public virtual event Action ButtonPointerDown = delegate { };
		public virtual event Action ButtonPointerUp = delegate { };
		public virtual event Action ButtonClicked = delegate { };

		public virtual void OnPointerEnter(PointerEventData eventData)
		{
			ButtonPointerEnter.Invoke();
			SoundInst.PlayOneShotSFXSound(SFXUIType.ButtonHover);
		}

		public virtual void OnPointerExit(PointerEventData eventData)
		{
			ButtonPointerExit.Invoke();
		}

		public virtual void OnPointerClick(PointerEventData eventData)
		{
			ButtonClicked.Invoke();
			SoundInst.PlayOneShotSFXSound(SFXUIType.ButtonClick);
		}

		public virtual void OnPointerDown(PointerEventData eventData)
		{
			ButtonPointerDown.Invoke();
		}

		public virtual void OnPointerUp(PointerEventData eventData)
		{
			ButtonPointerUp.Invoke();
		}
	}
}