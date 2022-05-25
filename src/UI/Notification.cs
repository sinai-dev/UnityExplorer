using UniverseLib.UI;

namespace UnityExplorer.UI
{
    public static class Notification
    {
        private static Text popupLabel;

        private static string _currentNotification;
        private static float _timeOfLastNotification;

        public static void Init()
        {
            ConstructUI();
        }

        public static void ShowMessage(string message)
        {
            popupLabel.text = message;
            _currentNotification = message;
            _timeOfLastNotification = Time.realtimeSinceStartup;

            popupLabel.transform.localPosition = UIManager.UIRootRect.InverseTransformPoint(DisplayManager.MousePosition) + (Vector3.up * 25);
        }

        public static void Update()
        {
            if (_currentNotification != null)
            {
                if (Time.realtimeSinceStartup - _timeOfLastNotification > 2f)
                {
                    _currentNotification = null;
                    popupLabel.text = "";
                }
            }
        }

        private static void ConstructUI()
        {
            popupLabel = UIFactory.CreateLabel(UIManager.UIRoot, "ClipboardNotification", "", TextAnchor.MiddleCenter);
            popupLabel.rectTransform.sizeDelta = new(500, 100);
            popupLabel.gameObject.AddComponent<Outline>();
            CanvasGroup popupGroup = popupLabel.gameObject.AddComponent<CanvasGroup>();
            popupGroup.blocksRaycasts = false;
        }
    }
}
