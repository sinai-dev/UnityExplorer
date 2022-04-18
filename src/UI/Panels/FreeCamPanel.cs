using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UniverseLib;
using UniverseLib.Input;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.Utility;

namespace UnityExplorer.UI.Panels
{
    internal class FreeCamPanel : UEPanel
    {
        public FreeCamPanel(UIBase owner) : base(owner)
        {
        }

        public override string Name => "Freecam";
        public override UIManager.Panels PanelType => UIManager.Panels.Freecam;

        public override int MinWidth => 400;
        public override int MinHeight => 300;
        public override Vector2 DefaultAnchorMin => new(0.4f, 0.4f);
        public override Vector2 DefaultAnchorMax => new(0.6f, 0.6f);
        public override bool NavButtonWanted => true;
        public override bool ShouldSaveActiveState => true;

        internal static bool inFreeCamMode;
        internal static Camera ourCamera;
        internal static Camera lastMainCamera;
        internal static Vector3 lastCameraMainPos;
        internal static Quaternion lastCameraMainRot;

        internal static float desiredMoveSpeed = 10f;
        internal static Vector3 previousMousePosition;
        internal static Vector3 lastSetCameraPosition;

        static ButtonRef startStopButton;
        static InputFieldRef positionInput;
        static InputFieldRef moveSpeedInput;
        static ButtonRef inspectButton;

        void BeginFreecam()
        {
            inFreeCamMode = true;

            previousMousePosition = InputManager.MousePosition;

            Camera currentMain = Camera.main;
            if (currentMain)
            {
                lastMainCamera = currentMain;
                lastCameraMainPos = currentMain.transform.position;
                lastCameraMainRot = currentMain.transform.rotation;
                lastMainCamera.enabled = false;
            }
            else
                lastCameraMainRot = Quaternion.identity;

            if (!ourCamera)
            {
                ourCamera = new GameObject("UE_Freecam").AddComponent<Camera>();
                ourCamera.gameObject.tag = "MainCamera";
                GameObject.DontDestroyOnLoad(ourCamera.gameObject);
                ourCamera.gameObject.hideFlags = HideFlags.HideAndDontSave;
                ourCamera.gameObject.AddComponent<FreeCamBehaviour>();
            }

            ourCamera.gameObject.SetActive(true);
            ourCamera.transform.position = lastCameraMainPos;
            ourCamera.transform.rotation = lastCameraMainRot;

            inspectButton.GameObject.SetActive(true);
        }

        void EndFreecam()
        {
            inFreeCamMode = false;

            if (ourCamera)
                ourCamera.gameObject.SetActive(false);
            else
                inspectButton.GameObject.SetActive(false);

            if (lastMainCamera)
                lastMainCamera.enabled = true;
        }

        void SetToggleButtonState()
        {
            if (inFreeCamMode)
            {
                RuntimeHelper.SetColorBlockAuto(startStopButton.Component, new(0.4f, 0.2f, 0.2f));
                startStopButton.ButtonText.text = "End Freecam";
            }
            else
            {
                RuntimeHelper.SetColorBlockAuto(startStopButton.Component, new(0.2f, 0.4f, 0.2f));
                startStopButton.ButtonText.text = "Begin Freecam";
            }
        }

        void SetCameraPosition(Vector3 pos)
        {
            if (!ourCamera || lastSetCameraPosition == pos)
                return;

            ourCamera.transform.position = pos;
            lastSetCameraPosition = pos;
        }

        internal static void UpdatePositionInput()
        {
            if (!ourCamera)
                return;

            lastSetCameraPosition = ourCamera.transform.position;
            positionInput.Text = ParseUtility.ToStringForInput<Vector3>(lastSetCameraPosition);
        }

        protected override void ConstructPanelContent()
        {
            startStopButton = UIFactory.CreateButton(ContentRoot, "ToggleButton", "Freecam");
            UIFactory.SetLayoutElement(startStopButton.GameObject, minWidth: 150, minHeight: 25, flexibleWidth: 9999);
            startStopButton.OnClick += ToggleButton_OnClick;
            SetToggleButtonState();

            AddSpacer(5);

            AddInputField("Position", "Camera Pos:", "eg. 0 0 0", out positionInput, PositionInput_OnEndEdit);

            AddSpacer(5);

            AddInputField("MoveSpeed", "Move Speed:", "Default: 1", out moveSpeedInput, MoveSpeedInput_OnEndEdit);
            moveSpeedInput.Text = desiredMoveSpeed.ToString();

            AddSpacer(5);

            string instructions = @"Controls:
- WASD/Arrows: Movement
- Space/PgUp: Move up
- LeftControl/PgDown: Move down
- Right Mouse Button: Free look
- Left Shift: Super speed";

            Text instructionsText = UIFactory.CreateLabel(ContentRoot, "Instructions", instructions, TextAnchor.UpperLeft);
            UIFactory.SetLayoutElement(instructionsText.gameObject, flexibleWidth: 9999, flexibleHeight: 9999);

            AddSpacer(5);

            inspectButton = UIFactory.CreateButton(ContentRoot, "InspectButton", "Inspect Free Camera");
            UIFactory.SetLayoutElement(inspectButton.GameObject, flexibleWidth: 9999, minHeight: 25);
            inspectButton.OnClick += () => { InspectorManager.Inspect(ourCamera); };
            inspectButton.GameObject.SetActive(false);

            AddSpacer(5);
        }

        void AddSpacer(int height)
        {
            GameObject obj = UIFactory.CreateUIObject("Spacer", ContentRoot);
            UIFactory.SetLayoutElement(obj, minHeight: height, flexibleHeight: 0);
        }

        GameObject AddInputField(string name, string labelText, string placeHolder, out InputFieldRef inputField, Action<string> onInputEndEdit)
        {
            GameObject row = UIFactory.CreateHorizontalGroup(ContentRoot, $"{name}_Group", false, false, true, true, 3, default, new(1, 1, 1, 0));

            Text posLabel = UIFactory.CreateLabel(row, $"{name}_Label", labelText);
            UIFactory.SetLayoutElement(posLabel.gameObject, minWidth: 100, minHeight: 25);

            inputField = UIFactory.CreateInputField(row, $"{name}_Input", placeHolder);
            UIFactory.SetLayoutElement(inputField.GameObject, minWidth: 125, minHeight: 25, flexibleWidth: 9999);
            inputField.Component.GetOnEndEdit().AddListener(onInputEndEdit);

            return row;
        }

        void ToggleButton_OnClick()
        {
            InputManager.SetSelectedEventSystemGameObject(null);

            if (inFreeCamMode)
                EndFreecam();
            else
                BeginFreecam();

            SetToggleButtonState();
        }

        private void PositionInput_OnEndEdit(string input)
        {
            InputManager.SetSelectedEventSystemGameObject(null);

            if (!ParseUtility.TryParse(input, out Vector3 parsed, out Exception parseEx))
            {
                ExplorerCore.LogWarning($"Could not parse position to Vector3: {parseEx.ReflectionExToString()}");
                UpdatePositionInput();
                return;
            }

            SetCameraPosition(parsed);
        }

        private void MoveSpeedInput_OnEndEdit(string input)
        {
            InputManager.SetSelectedEventSystemGameObject(null);

            if (!ParseUtility.TryParse(input, out float parsed, out Exception parseEx))
            {
                ExplorerCore.LogWarning($"Could not parse value: {parseEx.ReflectionExToString()}");
                moveSpeedInput.Text = desiredMoveSpeed.ToString();
                return;
            }

            desiredMoveSpeed = parsed;
        }
    }

    internal class FreeCamBehaviour : MonoBehaviour
    {
#if CPP
        static FreeCamBehaviour()
        {
            UnhollowerRuntimeLib.ClassInjector.RegisterTypeInIl2Cpp<FreeCamBehaviour>();
        }

        public FreeCamBehaviour(IntPtr ptr) : base(ptr) { }
#endif

        internal void Update()
        {
            if (FreeCamPanel.inFreeCamMode)
            {
                Transform transform = FreeCamPanel.ourCamera.transform;

                float moveSpeed = FreeCamPanel.desiredMoveSpeed * Time.deltaTime;

                if (InputManager.GetKey(KeyCode.LeftShift) || InputManager.GetKey(KeyCode.RightShift))
                    moveSpeed *= 10f;

                if (InputManager.GetKey(KeyCode.LeftArrow) || InputManager.GetKey(KeyCode.A))
                    transform.position += transform.right * -1 * moveSpeed;

                if (InputManager.GetKey(KeyCode.RightArrow) || InputManager.GetKey(KeyCode.D))
                    transform.position += transform.right * moveSpeed;

                if (InputManager.GetKey(KeyCode.UpArrow) || InputManager.GetKey(KeyCode.W))
                    transform.position += transform.forward * moveSpeed;

                if (InputManager.GetKey(KeyCode.DownArrow) || InputManager.GetKey(KeyCode.S))
                    transform.position += transform.forward * -1 * moveSpeed;

                if (InputManager.GetKey(KeyCode.Space) || InputManager.GetKey(KeyCode.PageUp))
                    transform.position += transform.up * moveSpeed;

                if (InputManager.GetKey(KeyCode.LeftControl) || InputManager.GetKey(KeyCode.PageDown))
                    transform.position += transform.up * -1 * moveSpeed;

                if (InputManager.GetMouseButton(1))
                {
                    Vector3 mouseDelta = InputManager.MousePosition - FreeCamPanel.previousMousePosition;

                    float newRotationX = transform.localEulerAngles.y + mouseDelta.x * 0.3f;
                    float newRotationY = transform.localEulerAngles.x - mouseDelta.y * 0.3f;
                    transform.localEulerAngles = new Vector3(newRotationY, newRotationX, 0f);
                }

                FreeCamPanel.UpdatePositionInput();

                FreeCamPanel.previousMousePosition = InputManager.MousePosition;
            }
        }
    }
}
