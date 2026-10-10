using UnityEngine;
using UnityEngine.InputSystem;

namespace OldGods.Runtime
{
    /// <summary>
    /// One shared copy of the Old Gods input actions, loaded from Resources.
    /// Gameplay reads actions through here so menus can pause input in one place.
    /// </summary>
    public static class GameInput
    {
        static InputActionAsset asset;
        static InputActionMap player;

        public static InputAction Move { get; private set; }
        public static InputAction Look { get; private set; }
        public static InputAction Jump { get; private set; }
        public static InputAction Slide { get; private set; }
        public static InputAction Interact { get; private set; }
        public static InputAction Pause { get; private set; }
        public static InputAction Refresh { get; private set; }
        public static InputAction Skip { get; private set; }
        public static InputAction Banish { get; private set; }

        /// <summary>Scripted input for tests and the smoke runner; overrides the devices when set.</summary>
        public static Vector2? MoveOverride;

        /// <summary>Actions held down by script (the play bot), with the frame each went down.</summary>
        static readonly System.Collections.Generic.Dictionary<InputAction, int> scripted = new System.Collections.Generic.Dictionary<InputAction, int>();

        /// <summary>Holds or releases an action by script. Pressed fires on the frame it goes down.</summary>
        public static void Script(InputAction action, bool held)
        {
            if (action == null) return;
            if (!held) scripted.Remove(action);
            else if (!scripted.ContainsKey(action)) scripted[action] = Time.frameCount;
        }

        public static void ClearScripted() => scripted.Clear();

        public static bool Ready => asset != null;

        public static void Ensure()
        {
            if (asset != null) return;
            asset = Resources.Load<InputActionAsset>("OldGodsInput");
            if (asset == null)
            {
                Debug.LogError("OldGods: Resources/OldGodsInput.inputactions is missing");
                return;
            }
            player = asset.FindActionMap("Player", true);
            Move = player.FindAction("Move", true);
            Look = player.FindAction("Look", true);
            Jump = player.FindAction("Jump", true);
            Slide = player.FindAction("Slide", true);
            Interact = player.FindAction("Interact", true);
            Pause = player.FindAction("Pause", true);
            Refresh = player.FindAction("Refresh", true);
            Skip = player.FindAction("Skip", true);
            Banish = player.FindAction("Banish", true);
            player.Enable();
        }

        public static Vector2 MoveValue
        {
            get
            {
                if (MoveOverride.HasValue) return MoveOverride.Value;
                Ensure();
                return Move != null ? Vector2.ClampMagnitude(Move.ReadValue<Vector2>(), 1f) : Vector2.zero;
            }
        }

        public static Vector2 LookValue
        {
            get
            {
                Ensure();
                return Look != null ? Look.ReadValue<Vector2>() : Vector2.zero;
            }
        }

        /// <summary>
        /// True when the look input comes from a stick, which reports a held rate rather than
        /// the distance moved since last frame (mouse delta).
        /// </summary>
        public static bool LookIsRate
        {
            get
            {
                Ensure();
                return Look != null && !(Look.activeControl?.device is Pointer);
            }
        }

        public static bool Pressed(InputAction action)
        {
            Ensure();
            if (action == null) return false;
            return action.WasPressedThisFrame() || (scripted.TryGetValue(action, out int frame) && frame == Time.frameCount);
        }

        public static bool Held(InputAction action)
        {
            Ensure();
            return action != null && (action.IsPressed() || scripted.ContainsKey(action));
        }

        public static void SetCursorLocked(bool locked)
        {
            if (Application.isBatchMode) return;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
