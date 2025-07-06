// Some platforms may report incorrect finger ID data, or be too strict with how close a finger must be between taps
// If you're developing on a platform or device like this, you can uncomment this to enable manual override of the ID.
//#define LEAN_ALLOW_RECLAIM

using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using CW.Common;

namespace Lean.Touch
{
	/// <summary>If you add this component to your scene, then it will convert all mouse and touch data into easy to use data.
	/// You can access this data via Lean.Touch.LeanTouch.Instance.Fingers, or hook into the Lean.Touch.LeanTouch.On___ events.
	/// NOTE: If you experience a one frame input delay you should edit your ScriptExecutionOrder to force this script to update before your other scripts.</summary>
	[ExecuteInEditMode]
	[DefaultExecutionOrder(-100)]
	[DisallowMultipleComponent]
	[HelpURL(HelpUrlPrefix + "LeanTouch")]
	[AddComponentMenu(ComponentPathPrefix + "Touch")]
	public partial class LeanTouch : MonoBehaviour
	{
		public const string ComponentPathPrefix = "Lean/Touch/Lean ";

		public const string HelpUrlPrefix = "https://carloswilkes.com/Documentation/LeanTouch#";

		public const string PlusHelpUrlPrefix = "https://carloswilkes.com/Documentation/LeanTouchPlus#";

		public const int MOUSE_FINGER_INDEX = -1;

		public const int HOVER_FINGER_INDEX = -42;

		private const int DEFAULT_REFERENCE_DPI = 200;

		private const int DEFAULT_GUI_LAYERS = 1 << 5;

		private const float DEFAULT_TAP_THRESHOLD = 0.2f;

		private const float DEFAULT_SWIPE_THRESHOLD = 100.0f;

		private const float DEFAULT_RECORD_LIMIT = 10.0f;

		/// <summary>This contains all the active and enabled LeanTouch instances</summary>
		public static List<LeanTouch> Instances = new List<LeanTouch>();

		/// <summary>This list contains all fingers currently touching the screen (or have just stopped touching the screen).
		/// NOTE: This list includes simulated fingers, as well as the mouse hover finger.</summary>
		public static List<LeanFinger> Fingers = new List<LeanFinger>(10);

		/// <summary>This list contains all fingers that were once touching the screen. This is used to manage finger tapping, as well as 'inactive' fingers that are so old they're no longer eligible for tapping.</summary>
		public static List<LeanFinger> InactiveFingers = new List<LeanFinger>(10);

		/// <summary>This gets fired when a finger begins touching the screen (LeanFinger = The current finger)</summary>
		public static event System.Action<LeanFinger> OnFingerDown;

		/// <summary>This gets fired every frame a finger is touching the screen (LeanFinger = The current finger)</summary>
		public static event System.Action<LeanFinger> OnFingerUpdate;

		/// <summary>This gets fired when a finger stops touching the screen (LeanFinger = The current finger)</summary>
		public static event System.Action<LeanFinger> OnFingerUp;

		/// <summary>This gets fired when a finger has been touching the screen for longer than <b>TapThreshold</b> seconds, causing it to be ineligible for the tap and swipe events.</summary>
		public static event System.Action<LeanFinger> OnFingerOld;

		/// <summary>This gets fired when a finger taps the screen (this is when a finger begins and stops touching the screen within the 'TapThreshold' time).</summary>
		public static event System.Action<LeanFinger> OnFingerTap;

		/// <summary>This gets fired when a finger swipes the screen (this is when a finger begins and stops touching the screen within the 'TapThreshold' time, and also moves more than the 'SwipeThreshold' distance) (LeanFinger = The current finger)</summary>
		public static event System.Action<LeanFinger> OnFingerSwipe;

		/// <summary>This gets fired every frame at least one finger is touching the screen (List = Fingers).</summary>
		public static event System.Action<List<LeanFinger>> OnGesture;

		/// <summary>This gets fired after a finger has stopped touching the screen for longer than <b>TapThreshold</b> seconds, making it ineligible for any future taps. This can be used to detect when you've done a single tap instead of a double tap, etc.</summary>
		public static event System.Action<LeanFinger> OnFingerExpired;

		/// <summary>This gets fired the frame after a finger went up.</summary>
		public static event System.Action<LeanFinger> OnFingerInactive;

		/// <summary>This will be invoked when it's time to simulate fingers. You can call the <b>AddFinger</b> method to simulate them.</summary>
		public event System.Action OnSimulateFingers;

		/// <summary>This allows you to set how many seconds are required between a finger down/up for a tap to be registered.</summary>
		public float TapThreshold { set { tapThreshold = value; } get { return tapThreshold; } } [SerializeField] private float tapThreshold = DEFAULT_TAP_THRESHOLD;

		public static float CurrentTapThreshold
		{
			get
			{
				return Instances.Count > 0 ? Instances[0].tapThreshold : DEFAULT_TAP_THRESHOLD;
			}
		}

		/// <summary>This allows you to set how many pixels of movement (relative to the ReferenceDpi) are required within the TapThreshold for a swipe to be triggered.</summary>
		public float SwipeThreshold { set { swipeThreshold = value; } get { return swipeThreshold; } } [SerializeField] private float swipeThreshold = DEFAULT_SWIPE_THRESHOLD;


		public static float CurrentSwipeThreshold
		{
			get
			{
				return Instances.Count > 0 ? Instances[0].swipeThreshold : DEFAULT_SWIPE_THRESHOLD;
			}
		}

#if LEAN_ALLOW_RECLAIM
		/// <summary>This allows you to set how many pixels (relative to the ReferenceDpi) away from a previous finger the new touching finger must be for it to be reclaimed. This is useful on platforms that give incorrect finger ID data.</summary>
		public float ReclaimThreshold { set { reclaimThreshold = value; } get { return reclaimThreshold; } } [SerializeField] private float reclaimThreshold = DEFAULT_RECLAIM_THRESHOLD;

		public const float DEFAULT_RECLAIM_THRESHOLD = 10.0f;

		public static float CurrentReclaimThreshold
		{
			get
			{
				return Instances.Count > 0 ? Instances[0].reclaimThreshold : DEFAULT_RECLAIM_THRESHOLD;
			}
		}
#endif

		/// <summary>This allows you to set the default DPI you want the input scaling to be based on. For example, if you set this to 200 and your display has a DPI of 400, then the <b>ScaledDelta</b> finger value will be half the distance of the pixel space <b>ScreenDelta</b> value.</summary>
		public int ReferenceDpi { set { referenceDpi = value; } get { return referenceDpi; } } [SerializeField] private int referenceDpi = DEFAULT_REFERENCE_DPI;

		public static int CurrentReferenceDpi
		{
			get
			{
				return Instances.Count > 0 ? Instances[0].referenceDpi : DEFAULT_REFERENCE_DPI;
			}
		}

		/// <summary>This allows you to set which layers your GUI is on, so it can be ignored by each finger.</summary>
		public LayerMask GuiLayers { set { guiLayers = value; } get { return guiLayers; } } [SerializeField] private LayerMask guiLayers = (LayerMask)DEFAULT_GUI_LAYERS;

		public static LayerMask CurrentGuiLayers
		{
			get
			{
				return Instances.Count > 0 ? Instances[0].guiLayers : (LayerMask)DEFAULT_GUI_LAYERS;
			}
		}

		/// <summary>If you disable this then lean touch will act as if you stopped touching the screen.</summary>
		public bool UseTouch { set { useTouch = value; } get { return useTouch; } } [SerializeField] private bool useTouch = true;

		/// <summary>Should the mouse hover position be stored as a finger?
		/// NOTE: It will be given a finger <b>Index</b> of HOVER_FINGER_INDEX = -42.</summary>
		public bool UseHover { set { useHover = value; } get { return useHover; } } [SerializeField] private bool useHover = true;

		/// <summary>Should any mouse button press be stored as a finger?
		/// NOTE: It will be given a finger <b>Index</b> of MOUSE_FINGER_INDEX = -1.</summary>
		public bool UseMouse { set { useMouse = value; } get { return useMouse; } } [SerializeField] private bool useMouse = true;

		/// <summary>Should components hooked into the <b>OnSimulateFingers</b> event be used? (e.g. LeanTouchSimulator)</summary>
		public bool UseSimulator { set { useSimulator = value; } get { return useSimulator; } } [SerializeField] private bool useSimulator = true;

		/// <summary>When using the old/legacy input system, by default it will convert touch data into mouse data, even if there is no mouse. Enabling this setting will disable this behavior.</summary>
		public bool DisableMouseEmulation { set { disableMouseEmulation = value; UpdateMouseEmulation(); } get { return disableMouseEmulation; } } [SerializeField] private bool disableMouseEmulation = true;

		/// <summary>Should each finger record snapshots of their screen positions?</summary>
		public bool RecordFingers { set { recordFingers = value; } get { return recordFingers; } } [SerializeField] private bool recordFingers = true;

		/// <summary>This allows you to set the amount of pixels a finger must move for another snapshot to be stored.</summary>
		public float RecordThreshold { set { recordThreshold = value; } get { return recordThreshold; } } [SerializeField] private float recordThreshold = 5.0f;

		/// <summary>This allows you to set the maximum amount of seconds that can be recorded, 0 = unlimited.</summary>
		public float RecordLimit { set { recordLimit = value; } get { return recordLimit; } } [SerializeField] private float recordLimit = DEFAULT_RECORD_LIMIT;

		// Used to find if the GUI is in use
		private static List<RaycastResult> tempRaycastResults = new List<RaycastResult>(10);

		// Used to return non GUI fingers
		private static List<LeanFinger> filteredFingers = new List<LeanFinger>(10);

		// Used by RaycastGui
		private static PointerEventData tempPointerEventData;

		// Used by RaycastGui
		private static EventSystem tempEventSystem;

		/// <summary>The first active and enabled LeanTouch instance.</summary>
		public static LeanTouch Instance
		{
			get
			{
				return Instances.Count > 0 ? Instances[0] : null;
			}
		}

		/// <summary>If you multiply this value with any other pixel delta (e.g. ScreenDelta), then it will become device resolution independent relative to the device DPI.</summary>
		public static float ScalingFactor
		{
			get
			{
				// Get the current screen DPI
				var dpi = Screen.dpi;

				// If it's 0 or less, it's invalid, so return the default scale of 1.0
				if (dpi <= 0)
				{
					return 1.0f;
				}

				// DPI seems valid, so scale it against the reference DPI
				return CurrentReferenceDpi / dpi;
			}
		}

		/// <summary>If you multiply this value with any other pixel delta (e.g. ScreenDelta), then it will become device resolution independent relative to the screen pixel size.</summary>
		public static float ScreenFactor
		{
			get
			{
				// Get shortest size
				var size = Mathf.Min(Screen.width, Screen.height);

				// If it's 0 or less, it's invalid, so return the default scale of 1.0
				if (size <= 0)
				{
					return 1.0f;
				}

				// Return reciprocal for easy multiplication
				return 1.0f / size;
			}
		}

		/// <summary>This will return true if the mouse or any finger is currently using the GUI.</summary>
		public static bool GuiInUse
		{
			get
			{
				// Legacy GUI in use?
				if (GUIUtility.hotControl > 0)
				{
					return true;
				}

				// New GUI in use?
				foreach (var finger in Fingers)
				{
					if (finger.StartedOverGui == true)
					{
						return true;
					}
				}

				return false;
			}
		}

		public static bool ElementOverlapped(GameObject element, Vector2 screenPosition)
		{
			var results = RaycastGui(screenPosition, -1);

			if (results != null && results.Count > 0)
			{
				if (results[0].gameObject == element)
				{
					return true;
				}
			}

			return false;
		}

		public static EventSystem GetEventSystem()
		{
			var currentEventSystem = EventSystem.current;

			if (currentEventSystem == null)
			{
				currentEventSystem = CwHelper.FindAnyObjectByType<EventSystem>();
			}

			return currentEventSystem;
		}

		/// <summary>This will return true if the specified screen point is over any GUI elements.</summary>
		public static bool PointOverGui(Vector2 screenPosition)
		{
			return RaycastGui(screenPosition).Count > 0;
		}

		/// <summary>This will return all the RaycastResults under the specified screen point using the current layerMask.
		/// NOTE: The first result (0) will be the top UI element that was first hit.</summary>
		public static List<RaycastResult> RaycastGui(Vector2 screenPosition)
		{
			return RaycastGui(screenPosition, CurrentGuiLayers);
		}

		/// <summary>This will return all the RaycastResults under the specified screen point using the specified layerMask.
		/// NOTE: The first result (0) will be the top UI element that was first hit.</summary>
		public static List<RaycastResult> RaycastGui(Vector2 screenPosition, LayerMask layerMask)
		{
			tempRaycastResults.Clear();

			var currentEventSystem = GetEventSystem();

			if (currentEventSystem != null)
			{
				// Create point event data for this event system?
				if (currentEventSystem != tempEventSystem)
				{
					tempEventSystem = currentEventSystem;

					if (tempPointerEventData == null)
					{
						tempPointerEventData = new PointerEventData(tempEventSystem);
					}
					else
					{
						tempPointerEventData.Reset();
					}
				}

				// Raycast event system at the specified point
				tempPointerEventData.position = screenPosition;

				currentEventSystem.RaycastAll(tempPointerEventData, tempRaycastResults);

				// Loop through all results and remove any that don't match the layer mask
				if (tempRaycastResults.Count > 0)
				{
					for (var i = tempRaycastResults.Count - 1; i >= 0; i--)
					{
						var raycastResult = tempRaycastResults[i];
						var raycastLayer  = 1 << raycastResult.gameObject.layer;

						if ((raycastLayer & layerMask) == 0)
						{
							tempRaycastResults.RemoveAt(i);
						}
					}
				}
			}
			else
			{
				Debug.LogError("Failed to RaycastGui because your scene doesn't have an event system! To add one, go to: GameObject/UI/EventSystem");
			}

			return tempRaycastResults;
		}

		/// <summary>This allows you to filter all the fingers based on the specified requirements.
		/// NOTE: If ignoreGuiFingers is set, Fingers will be filtered to remove any with StartedOverGui.
		/// NOTE: If requiredFingerCount is greater than 0, this method will return null if the finger count doesn't match.
		/// NOTE: If requiredSelectable is set, and its SelectingFinger isn't null, it will return just that finger.</summary>
		public static List<LeanFinger> GetFingers(bool ignoreIfStartedOverGui, bool ignoreIfOverGui, int requiredFingerCount = 0, bool ignoreHoverFinger = true)
		{
			filteredFingers.Clear();

			foreach (var finger in Fingers)
			{
				// Ignore?
				if (ignoreIfStartedOverGui == true && finger.StartedOverGui == true)
				{
					continue;
				}

				if (ignoreIfOverGui == true && finger.IsOverGui == true)
				{
					continue;
				}

				if (ignoreHoverFinger == true && finger.Index == HOVER_FINGER_INDEX)
				{
					continue;
				}

				// Add
				filteredFingers.Add(finger);
			}

			if (requiredFingerCount > 0)
			{
				if (filteredFingers.Count != requiredFingerCount)
				{
					filteredFingers.Clear();

					return filteredFingers;
				}
			}

			return filteredFingers;
		}

		private static LeanFinger simulatedTapFinger = new LeanFinger();

		/// <summary>This allows you to simulate a tap on the screen at the specified location.</summary>
		public static void SimulateTap(Vector2 screenPosition, float pressure = 1.0f, int tapCount = 1)
		{
			if (OnFingerTap != null)
			{
				simulatedTapFinger.Index               = -5;
				simulatedTapFinger.Age                 = 0.0f;
				simulatedTapFinger.Set                 = false;
				simulatedTapFinger.LastSet             = true;
				simulatedTapFinger.Tap                 = true;
				simulatedTapFinger.TapCount            = tapCount;
				simulatedTapFinger.Swipe               = false;
				simulatedTapFinger.Old                 = false;
				simulatedTapFinger.Expired             = false;
				simulatedTapFinger.LastPressure        = pressure;
				simulatedTapFinger.Pressure            = pressure;
				simulatedTapFinger.StartScreenPosition = screenPosition;
				simulatedTapFinger.LastScreenPosition  = screenPosition;
				simulatedTapFinger.ScreenPosition      = screenPosition;
				simulatedTapFinger.StartedOverGui      = simulatedTapFinger.IsOverGui;
				simulatedTapFinger.ClearSnapshots();
				simulatedTapFinger.RecordSnapshot();

				OnFingerTap(simulatedTapFinger);
			}
		}

		/// <summary>You can call this method if you want to stop all finger events. You can then disable this component to prevent new ones from updating.</summary>
		public void Clear()
		{
			UpdateFingers(0.001f, false);
			UpdateFingers(1.0f, false);
		}

		/// <summary>This will update Unity based on the current <b>DisableMouseEmulation</b> setting.</summary>
		public void UpdateMouseEmulation()
		{
			if (disableMouseEmulation == true)
			{
				Input.simulateMouseWithTouches = false;
			}
			else
			{
				Input.simulateMouseWithTouches = true;
			}
		}

		protected virtual void OnEnable()
		{
			Instances.Add(this);

			UpdateMouseEmulation();
		}

		protected virtual void OnDisable()
		{
			Instances.Remove(this);
		}

		protected virtual void Update()
		{
#if UNITY_EDITOR
			if (Application.isPlaying == false)
			{
				return;
			}
#endif

			// Only run the update methods if this is the first instance (i.e. if your scene has more than one LeanTouch component, only use the first)
			if (Instances[0] == this)
			{
				UpdateFingers(Time.unscaledDeltaTime, true);
			}
		}

		private void UpdateFingers(float deltaTime, bool poll)
		{
			// Prepare old finger data for new information
			BeginFingers(deltaTime);

			// Poll current touch + mouse data and convert it to fingers
			if (poll == true)
			{
				PollFingers();
			}

			// Process any no longer used fingers
			EndFingers(deltaTime);

			// Update events based on new finger data
			UpdateEvents();
		}

		// Update all Fingers and InactiveFingers so they're ready for the new frame
		private void BeginFingers(float deltaTime)
		{
			// Age inactive fingers
			for (var i = InactiveFingers.Count - 1; i >= 0; i--)
			{
				var inactiveFinger = InactiveFingers[i];

				inactiveFinger.Age += deltaTime;

				// Just expired?
				if (inactiveFinger.Expired == false && inactiveFinger.Age > tapThreshold)
				{
					inactiveFinger.Expired = true;

					if (OnFingerExpired != null) OnFingerExpired(inactiveFinger);
				}
			}

			// Reset finger data
			for (var i = Fingers.Count - 1; i >= 0; i--)
			{
				var finger = Fingers[i];

				// Was this set to up last time? If so, it's now inactive
				if (finger.Up == true || finger.Set == false)
				{
					// Make finger inactive
					Fingers.RemoveAt(i); InactiveFingers.Add(finger);

					// Reset age so we can time how long it's been inactive
					finger.Age = 0.0f;

					// Pool old snapshots
					finger.ClearSnapshots();

					if (OnFingerInactive != null) OnFingerInactive(finger);
				}
				else
				{
					finger.LastSet            = finger.Set;
					finger.LastPressure       = finger.Pressure;
					finger.LastScreenPosition = finger.ScreenPosition;

					finger.Set   = false;
					finger.Tap   = false;
					finger.Swipe = false;
				}
			}

			// Store all active fingers (these are later removed in AddFinger)
			missingFingers.Clear();

			foreach (var finger in Fingers)
			{
				missingFingers.Add(finger);
			}
		}

		// Update all Fingers based on the new finger data
		private void EndFingers(float deltaTime)
		{
			// Force missing fingers to go up (there normally shouldn't be any)
			tempFingers.Clear();

			tempFingers.AddRange(missingFingers);

			foreach (var finger in tempFingers)
			{
				AddFinger(finger.Index, finger.ScreenPosition, finger.Pressure, false);
			}

			// Update fingers
			foreach (var finger in Fingers)
			{
				// Up?
				if (finger.Up == true)
				{
					// Tap or Swipe?
					if (finger.Age <= tapThreshold)
					{
						if (finger.SwipeScreenDelta.magnitude * ScalingFactor < swipeThreshold)
						{
							finger.Tap       = true;
							finger.TapCount += 1;
						}
						else
						{
							finger.TapCount = 0;
							finger.Swipe    = true;
						}
					}
					else
					{
						finger.TapCount = 0;
					}
				}
				// Down?
				else if (finger.Down == false)
				{
					// Age it
					finger.Age += deltaTime;

					// Too old?
					if (finger.Age > tapThreshold && finger.Old == false)
					{
						finger.Old = true;

						if (OnFingerOld != null) OnFingerOld(finger);
					}
				}
			}
		}

		// This allows us to track if Unity incorrectly removes fingers without first indicating they went up
		private static HashSet<LeanFinger> missingFingers = new HashSet<LeanFinger>();

		private static List<LeanFinger> tempFingers = new List<LeanFinger>();

		// Read new hardware finger data
		private void PollFingers()
		{
			// Submit real fingers?
			if (useTouch == true && CwInput.GetTouchCount() > 0)
			{
				for (var i = 0; i < CwInput.GetTouchCount(); i++)
				{
					int id; Vector2 position; float pressure; bool set;

					CwInput.GetTouch(i, out id, out position, out pressure, out set);

					AddFinger(id, position, pressure, set);
				}
			}

			// Submit mouse hover as finger?
			if (useHover == true && CwInput.GetMouseExists() == true)
			{
				var mousePosition = CwInput.GetMousePosition();
				var hoverFinger   = AddFinger(HOVER_FINGER_INDEX, mousePosition, 0.0f, true);

				hoverFinger.StartedOverGui = false;
				hoverFinger.LastSet        = true;
			}

			// Submit mouse buttons as finger?
			if (useMouse == true && CwInput.GetMouseExists() == true)
			{
				var mouseSet = false;
				var mouseUp  = false;

				for (var i = 0; i < 5; i++)
				{
					mouseSet |= CwInput.GetMouseIsHeld(i);
					mouseUp  |= CwInput.GetMouseWentUp(i);
				}

				if (mouseSet == true || mouseUp == true)
				{
					var mousePosition = CwInput.GetMousePosition();

					// Is the mouse within the screen?
					//if (new Rect(0, 0, Screen.width, Screen.height).Contains(mousePosition) == true)
					{
						AddFinger(MOUSE_FINGER_INDEX, mousePosition, 1.0f, mouseSet);
					}
				}
			}

			// Simulate other fingers?
			if (useSimulator == true)
			{
				if (OnSimulateFingers != null) OnSimulateFingers.Invoke();
			}
		}

		private void UpdateEvents()
		{
			var fingerCount = Fingers.Count;

			if (fingerCount > 0)
			{
				for (var i = 0; i < fingerCount; i++)
				{
					var finger = Fingers[i];

					if (finger.Tap   == true && OnFingerTap    != null) OnFingerTap(finger);
					if (finger.Swipe == true && OnFingerSwipe  != null) OnFingerSwipe(finger);
					if (finger.Down  == true && OnFingerDown   != null) OnFingerDown(finger);
					if (                        OnFingerUpdate != null) OnFingerUpdate(finger);
					if (finger.Up    == true && OnFingerUp     != null) OnFingerUp(finger);
				}

				if (OnGesture != null)
				{
					filteredFingers.Clear();
					filteredFingers.AddRange(Fingers);

					OnGesture(filteredFingers);
				}
			}
		}

		// Add a finger based on index, or return the existing one
		public LeanFinger AddFinger(int index, Vector2 screenPosition, float pressure, bool set)
		{
			var finger = FindFinger(index);

			// Finger is already active, so remove it from the missing collection
			if (finger != null)
			{
				missingFingers.Remove(finger);
			}
			// No finger found? Find or create it
			else
			{
				// If a finger goes up but hasn't been registered yet then it will mess up the event flow, so skip it (this shouldn't normally occur).
				if (set == false)
				{
					return null;
				}

				var inactiveIndex = FindInactiveFingerIndex(index);

				// Use inactive finger?
				if (inactiveIndex >= 0)
				{
					finger = InactiveFingers[inactiveIndex]; InactiveFingers.RemoveAt(inactiveIndex);

					// Inactive for too long?
					if (finger.Age > tapThreshold)
					{
						finger.TapCount = 0;
					}

					// Reset values
					finger.Age     = 0.0f;
					finger.Old     = false;
					finger.Set     = false;
					finger.LastSet = false;
					finger.Tap     = false;
					finger.Swipe   = false;
					finger.Expired = false;
				}
				else
				{
#if LEAN_ALLOW_RECLAIM
					// Before we create a new finger, try reclaiming one in case the finger ID was given incorrectly
					finger = ReclaimFinger(index, screenPosition);
#endif

					// Create new finger?
					if (finger == null)
					{
						finger = new LeanFinger();

						finger.Index = index;
					}
				}

				finger.StartScreenPosition = screenPosition;
				finger.LastScreenPosition  = screenPosition;
				finger.LastPressure        = pressure;
				finger.StartedOverGui      = PointOverGui(screenPosition);

				Fingers.Add(finger);
			}

			finger.Set            = set;
			finger.ScreenPosition = screenPosition;
			finger.Pressure       = pressure;

			// Record?
			if (recordFingers == true)
			{
				// Too many snapshots?
				if (recordLimit > 0.0f)
				{
					if (finger.SnapshotDuration > recordLimit)
					{
						var removeCount = LeanSnapshot.GetLowerIndex(finger.Snapshots, finger.Age - recordLimit);

						finger.ClearSnapshots(removeCount);
					}
				}
				// Make sure the hover finger doesn't record forever
				else if (finger.Index == HOVER_FINGER_INDEX)
				{
					if (finger.SnapshotDuration > DEFAULT_RECORD_LIMIT)
					{
						var removeCount = LeanSnapshot.GetLowerIndex(finger.Snapshots, finger.Age - DEFAULT_RECORD_LIMIT);

						finger.ClearSnapshots(removeCount);
					}
				}

				// Record snapshot?
				if (recordThreshold > 0.0f)
				{
					if (finger.Snapshots.Count == 0 || finger.LastSnapshotScreenDelta.magnitude >= recordThreshold)
					{
						finger.RecordSnapshot();
					}
				}
				else
				{
					finger.RecordSnapshot();
				}
			}

			return finger;
		}

		// Find the finger with the specified index, or return null
		private LeanFinger FindFinger(int index)
		{
			foreach (var finger in Fingers)
			{
				if (finger.Index == index)
				{
					return finger;
				}
			}

			return null;
		}

#if LEAN_ALLOW_RECLAIM
		// Some platforms may give unexpected finger ID information, override it?
		private LeanFinger ReclaimFinger(int index, Vector2 screenPosition)
		{
			for (var i = InactiveFingers.Count - 1; i>= 0; i--)
			{
				var finger = InactiveFingers[i];

				if (finger.Expired == false && Vector2.Distance(finger.ScreenPosition, screenPosition) * ScalingFactor < reclaimThreshold)
				{
					finger.Index = index;

					InactiveFingers.RemoveAt(i);

					Fingers.Add(finger);

					return finger;
				}
			}

			return null;
		}
#endif

		/// Find the index of the inactive finger with the specified index, or return -1
		private int FindInactiveFingerIndex(int index)
		{
			for (var i = InactiveFingers.Count - 1; i>= 0; i--)
			{
				if (InactiveFingers[i].Index == index)
				{
					return i;
				}
			}

			return -1;
		}
	}
}

#if UNITY_EDITOR
namespace Lean.Touch.Editor
{
	using UnityEditor;
	using TARGET = LeanTouch;

	[CustomEditor(typeof(TARGET))]
	public class LeanTouch_Editor : CwEditor
	{
		private static List<LeanFinger> allFingers = new List<LeanFinger>();

		private static GUIStyle fadingLabel;

		public static event System.Action<TARGET> OnExtendInspector;

		[System.NonSerialized] TARGET tgt; [System.NonSerialized] TARGET[] tgts;

		[MenuItem("GameObject/Lean/Touch", false, 1)]
		public static void CreateTouch()
		{
			var gameObject = new GameObject(typeof(LeanTouch).Name);

			Undo.RegisterCreatedObjectUndo(gameObject, "Create Touch");

			gameObject.AddComponent<LeanTouch>();

			Selection.activeGameObject = gameObject;
		}

		// Draw the whole inspector
		protected override void OnInspector()
		{
			GetTargets(out tgt, out tgts);

			if (LeanTouch.Instances.Count > 1)
			{
				Warning("There is more than one active and enabled LeanTouch...");

				Separator();
			}

			var touch = (LeanTouch)target;

			Separator();

			DrawSettings(touch);

			Separator();

			if (OnExtendInspector != null)
			{
				OnExtendInspector.Invoke(tgt);
			}

			Separator();

			DrawFingers(touch);

			Separator();

			Repaint();
		}

		private void DrawSettings(LeanTouch touch)
		{
			var updateMouseEmulation = false;

			Draw("tapThreshold", "This allows you to set how many seconds are required between a finger down/up for a tap to be registered.");
			Draw("swipeThreshold", "This allows you to set how many pixels of movement (relative to the ReferenceDpi) are required within the TapThreshold for a swipe to be triggered.");
#if LEAN_ALLOW_RECLAIM
			Draw("reclaimThreshold", "This allows you to set how many pixels (relative to the ReferenceDpi) away from a previous finger the new touching finger must be for it to be reclaimed. This is useful on platforms that give incorrect finger ID data.");
#endif
			Draw("referenceDpi", "This allows you to set the default DPI you want the input scaling to be based on. For example, if you set this to 200 and your display has a DPI of 400, then the <b>ScaledDelta</b> finger value will be half the distance of the pixel space <b>ScreenDelta</b> value.");
			Draw("guiLayers", "This allows you to set which layers your GUI is on, so it can be ignored by each finger.");

			Separator();

			Draw("useTouch", "If you disable this then lean touch will act as if you stopped touching the screen.");
			Draw("useHover", "Should the mouse hover position be stored as a finger?\n\nNOTE: It will be given a finger <b>Index</b> of HOVER_FINGER_INDEX = -42.");
			Draw("useMouse", "Should any mouse button press be stored as a finger?\n\nNOTE: It will be given a finger <b>Index</b> of MOUSE_FINGER_INDEX = -1.");
			Draw("useSimulator", "Should components hooked into the <b>OnSimulateFingers</b> event be used? (e.g. LeanTouchSimulator)");

			Separator();

			Draw("disableMouseEmulation", ref updateMouseEmulation, "When using the old/legacy input system, by default it will convert touch data into mouse data, even if there is no mouse. Enabling this setting will disable this behavior.");
			Draw("recordFingers", "Should each finger record snapshots of their screen positions?");

			if (touch.RecordFingers == true)
			{
				BeginIndent();
					Draw("recordThreshold", "This allows you to set the amount of pixels a finger must move for another snapshot to be stored.");
					Draw("recordLimit", "This allows you to set the maximum amount of seconds that can be recorded, 0 = unlimited.");
				EndIndent();
			}

			if (updateMouseEmulation == true)
			{
				Each(tgts, t => t.UpdateMouseEmulation(), true);
			}
		}

		private void DrawFingers(LeanTouch touch)
		{
			EditorGUILayout.LabelField(new GUIContent("Fingers", "Index - State - Taps - X, Y - Age"), EditorStyles.boldLabel);

			allFingers.Clear();
			allFingers.AddRange(LeanTouch.Fingers);
			allFingers.AddRange(LeanTouch.InactiveFingers);
			allFingers.Sort((a, b) => a.Index.CompareTo(b.Index));

			for (var i = 0; i < allFingers.Count; i++)
			{
				var finger   = allFingers[i];
				var progress = touch.TapThreshold > 0.0f ? finger.Age / touch.TapThreshold : 0.0f;
				var style    = GetFadingLabel(finger.Set, progress);

				if (style.normal.textColor.a > 0.0f)
				{
					var screenPosition = finger.ScreenPosition;
					var state          = "UPDATE";

					if (finger.Down     == true ) state = "DOWN";
					if (finger.Up       == true ) state = "UP";
					if (finger.IsActive == false) state = "INACTIVE";
					if (finger.Expired  == true ) state = "EXPIRED";

					EditorGUILayout.LabelField(finger.Index + " - " + state + " - " + finger.TapCount + "  + " + Mathf.FloorToInt(screenPosition.x) + ", " + Mathf.FloorToInt(screenPosition.y) + ") - " + finger.Age.ToString("0.0"), style);
				}
			}
		}

		private static GUIStyle GetFadingLabel(bool active, float progress)
		{
			if (fadingLabel == null)
			{
				fadingLabel = new GUIStyle(EditorStyles.label);
			}

			var a = EditorStyles.label.normal.textColor;
			var b = a; b.a = active == true ? 0.5f : 0.0f;

			fadingLabel.normal.textColor = Color.Lerp(a, b, progress);

			return fadingLabel;
		}
	}
}
#endif