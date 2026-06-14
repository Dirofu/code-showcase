using System;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Core.Scripts.UI.Universal.ViewUICustomer
{
	public class ViewUICustomer<TType, TSlotElementType> : MonoBehaviour
		where TType : Enum
		where TSlotElementType : Enum
	{
#if UNITY_EDITOR
		[FoldoutGroup("Debug settings")]
		[SerializeField] private bool _isDebug = false;
		[FoldoutGroup("Debug settings"), ShowIf("_isDebug")]
		[SerializeField] private bool _showFilterSettings = false;
		[FoldoutGroup("Debug settings"), ShowIf("_isDebug")]
		[SerializeField] private TType _debugStatusType;
		[FoldoutGroup("Debug settings"), ShowIf("_isDebug")]
		[SerializeField] private ViewUIStateType _debugStateType;
		
		[FoldoutGroup("Debug settings"), ReadOnly]
		[SerializeField] private TType _settedStatusType;
		[FoldoutGroup("Debug settings"), ReadOnly]
		[SerializeField] private ViewUIStateType _settedStateType;
		
		[PropertySpace]

		[FoldoutGroup("Filter settings"), ShowIf("@_isDebug && _showFilterSettings")]
		[SerializeField] private TType _filterStatusType;
		[FoldoutGroup("Filter settings"), ShowIf("@_isDebug && _showFilterSettings")]
		[SerializeField] private ViewUIStateType _filterStateType;
		[FoldoutGroup("Filter settings"), ShowIf("@_isDebug && _showFilterSettings")]
		[SerializeField] private TSlotElementType _filterElementType;

		[FoldoutGroup("Filter settings"), ShowIf("@_isDebug && _showFilterSettings")] [Button(ButtonHeight = 50, DirtyOnClick = true)]
		private void Search()
		{
			if (_isDebug == false)
				return;

			_statesDic = _states.ToDictionary(state => state.StateType);

			foreach (TType type in Enum.GetValues(typeof(TType)))
			{
				if (_statesDic.ContainsKey(type) == false || Convert.ToInt32(type) < 0)
					continue;

				this[type].UpdateGroups();
				bool typeMatched = (EqualityComparer<TType>.Default.Equals(type, _filterStatusType) && Convert.ToInt32(type) >= 0) || Convert.ToInt32(_filterStatusType) == -1;
				this[type].UpdateVisibleState(typeMatched);

				foreach (ViewUIStateType state in Enum.GetValues(typeof(ViewUIStateType)))
				{
					if (this[type].Groups.ContainsKey(state) == false || Convert.ToInt32(state) < 0)
						continue;
					
					this[type][state].UpdateElements();
					bool stateMatched = (EqualityComparer<ViewUIStateType>.Default.Equals(state, _filterStateType) && Convert.ToInt32(state) >= 0) || Convert.ToInt32(_filterStateType) == -1;
					this[type][state].UpdateVisibleState(stateMatched);


					foreach (TSlotElementType item in Enum.GetValues(typeof(TSlotElementType)))
					{
						if (this[type][state].Elements.ContainsKey(item) == false || Convert.ToInt32(item) < 0)
							continue;

						bool elementMatched = (EqualityComparer<TSlotElementType>.Default.Equals(item, _filterElementType) && Convert.ToInt32(item) >= 0) || Convert.ToInt32(_filterElementType) == -1;
						this[type][state][item].UpdateVisibleState(elementMatched);
					}
				}
			}
		}

		[FoldoutGroup("Filter settings"), ShowIf("@_isDebug && _showFilterSettings")]
		[Button(ButtonHeight = 50, DirtyOnClick = true)]
		private void ResetSearch()
		{
			if (_isDebug == false)
				return;

			_statesDic = _states.ToDictionary(state => state.StateType);

			foreach (TType type in Enum.GetValues(typeof(TType)))
			{
				if (_statesDic.ContainsKey(type) == false || Convert.ToInt32(type) < 0)
					continue;

				this[type].UpdateGroups();
				this[type].UpdateVisibleState(true);

				foreach (ViewUIStateType state in Enum.GetValues(typeof(ViewUIStateType)))
				{
					if (this[type].Groups.ContainsKey(state) == false || Convert.ToInt32(state) < 0)
						continue;

					this[type][state].UpdateElements();
					this[type][state].UpdateVisibleState(true);

					foreach (TSlotElementType item in Enum.GetValues(typeof(TSlotElementType)))
					{
						if (this[type][state].Elements.ContainsKey(item) == false || Convert.ToInt32(item) < 0)
							continue;

						this[type][state][item].UpdateVisibleState(true);
					}
				}
			}
		}

		[FoldoutGroup("Debug settings"), ShowIf("_isDebug")] [Button(ButtonHeight = 50, DirtyOnClick = true)]
		private void LoadCurrentState()
		{
			if (_isDebug == false)
				return;

			_statesDic = _states.ToDictionary(state => state.StateType);

			foreach (TType type in Enum.GetValues(typeof(TType)))
			{
				if (_statesDic.ContainsKey(type) == false)
					continue;

				this[type].UpdateGroups();

				foreach (ViewUIStateType state in Enum.GetValues(typeof(ViewUIStateType)))
				{
					if (this[type].Groups.ContainsKey(state) == false)
						continue;

					this[type][state].UpdateElements();
				}
			}

			SetState(_debugStateType, _debugStatusType);
			_states = _statesDic.Values.ToList();
			EditorUtility.SetDirty(gameObject);
		}

		[FoldoutGroup("Debug settings"), ShowIf("_isDebug")] [Button(ButtonHeight = 50, DirtyOnClick = true)]
		private void SaveCurrentState()
		{
			if (_isDebug == false)
				return;

			_statesDic.Clear();
			_statesDic = _states.ToDictionary(state => state.StateType);

			if (_statesDic.ContainsKey(_debugStatusType) == false)
			{
				ViewUIState<TType, TSlotElementType> newState = new();
				_statesDic.Add(_debugStatusType, newState);
			}

			this[_debugStatusType].UpdateGroups();

			if (this[_debugStatusType].Groups.ContainsKey(_debugStateType) == false)
			{
				ViewUIGroupType<TType, TSlotElementType> newGroup = new();
				this[_debugStatusType].AddNewGroup(_debugStateType, _debugStatusType, newGroup);
			}

			foreach (var item in this[_debugStatusType].Groups)
				item.Value.UpdateElements();

			ViewUIGetHelper<TSlotElementType>[] getHelpers = gameObject.GetComponentsInChildren<ViewUIGetHelper<TSlotElementType>>(true);

			foreach (var helper in getHelpers)
			{
				if (this[_debugStatusType][_debugStateType].Elements.TryGetValue(helper.Type, out ViewUIElement<TSlotElementType> element) == false)
				{
					ViewUIElement<TSlotElementType> newElement = new();
					this[_debugStatusType][_debugStateType].AddNewElement(_debugStateType, helper.Type, newElement, helper.gameObject);
				}
				else
				{
					this[_debugStatusType][_debugStateType].RewriteElement(_debugStateType, helper.Type, element, helper.gameObject);
				}
			}

			_states = _statesDic.Values.ToList();
			EditorUtility.SetDirty(gameObject);
		}

		[FoldoutGroup("Debug settings"), ShowIf("_isDebug")] [Button(ButtonHeight = 50, DirtyOnClick = true)]
		private void ClearSettings()
		{
			ViewUIGetHelper<TSlotElementType>[] getHelpers = gameObject.GetComponentsInChildren<ViewUIGetHelper<TSlotElementType>>(true);

			foreach (TType type in Enum.GetValues(typeof(TType)))
			{
				if (_statesDic.ContainsKey(type) == false)
					continue;
				;
				foreach (ViewUIStateType state in Enum.GetValues(typeof(ViewUIStateType)))
				{
					if (this[type].Groups.ContainsKey(state) == false)
						continue;

					foreach (TSlotElementType item in Enum.GetValues(typeof(TSlotElementType)))
					{
						this[type][state].Reset();
					}
					this[type].Reset();
				}
			}

			_statesDic.Clear();
			_states.Clear();
		}
#endif

		[FoldoutGroup("ViewUICustomer settings")]
		[SerializeField] private List<ViewUIState<TType, TSlotElementType>> _states;
		[FoldoutGroup("ViewUICustomer settings")]
		[SerializeField] private bool _buttonSupport = false;
		[FoldoutGroup("ViewUICustomer settings"), ShowIf("_buttonSupport")]
		[SerializeField] private ViewUIButton _button;

		private TType _currentType;
		private ViewUIStateType _currentStateType = ViewUIStateType.Idle;

		private Dictionary<TType, ViewUIState<TType, TSlotElementType>> _statesDic = new();
		public ViewUIState<TType, TSlotElementType> this[TType type] => _statesDic[type];
		public TType Type => _currentType;
		public ViewUIStateType StateType => _currentStateType;

		public event Action<TType> StateChanged = delegate { };

		private void Awake()
		{
			UpdateStates();
		}

		private void OnEnable()
		{
			SubscribeButton(true);
		}

		private void OnDisable()
		{
			SubscribeButton(false);
		}

		/// <summary>
		/// Use this for set view state.<br/><br/><paramref name="stateType"/> setted idle, highlight, active etc. On base use <paramref name="ViewUIStateType"/>. <br /><paramref name="type"/> setted global type like a Ally\Enemy.
		/// </summary>
		/// <param name="stateType">Slot state type</param>
		/// <param name="type">Slot element type</param>
		public void SetState(ViewUIStateType stateType, TType type)
		{
			if (_statesDic.Count == 0)
				UpdateStates();

			var state = _statesDic[type];
			_currentType = type;
			_currentStateType = stateType;

			#if UNITY_EDITOR
			_settedStatusType = type;
			_settedStateType = stateType;
			#endif
			
			if (state[stateType]?.Elements != null)
			{
				foreach (var element in state[stateType].Elements.Values)
					element.SetSettings();
			}

			StateChanged.Invoke(type);
		}

		private void UpdateStates()
		{
			_statesDic.Clear();
			_statesDic = _states.ToDictionary(state => state.StateType);

			foreach (TType type in Enum.GetValues(typeof(TType)))
			{
				if (_statesDic.ContainsKey(type) == false)
					continue;

				this[type].UpdateGroups();
				
				foreach (ViewUIStateType state in Enum.GetValues(typeof(ViewUIStateType)))
				{
					if (this[type].Groups.ContainsKey(state) == false)
						continue;

					this[type][state].UpdateElements();
				}
			}
		}

		protected virtual void SubscribeButton(bool subscribe)
		{
			if (_buttonSupport == false || _button == null)
				return;

			if (subscribe == true)
			{
				_button.ButtonClicked += OnButtonClicked;
				_button.ButtonPointerUp += OnButtonPointerUp;
				_button.ButtonPointerDown += OnButtonPointerDown;
				_button.ButtonPointerExit += OnButtonPointerExit;
				_button.ButtonPointerEnter += OnButtonPointerEnter;
			}
			else
			{
				_button.ButtonClicked -= OnButtonClicked;
				_button.ButtonPointerUp -= OnButtonPointerUp;
				_button.ButtonPointerDown -= OnButtonPointerDown;
				_button.ButtonPointerExit -= OnButtonPointerExit;
				_button.ButtonPointerEnter -= OnButtonPointerEnter;
			}
		}

		private void OnButtonPointerExit()
		{
			SetState(ViewUIStateType.Idle, _currentType);
		}

		private void OnButtonPointerEnter()
		{
			SetState(ViewUIStateType.Highlight, _currentType);
		}

		private void OnButtonPointerUp()
		{
			SetState(ViewUIStateType.Idle, _currentType);
		}

		private void OnButtonPointerDown()
		{
			SetState(ViewUIStateType.Pressed, _currentType);
		}

		private void OnButtonClicked()
		{
			SetState(ViewUIStateType.Active, _currentType);
			_currentStateType = ViewUIStateType.Active;
		}
	}
}