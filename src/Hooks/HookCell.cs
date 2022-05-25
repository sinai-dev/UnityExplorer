using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.UI.Widgets.ScrollView;

namespace UnityExplorer.Hooks
{
    public class HookCell : ICell
    {
        public bool Enabled => UIRoot.activeSelf;

        public RectTransform Rect { get; set; }
        public GameObject UIRoot { get; set; }

        public float DefaultHeight => 30;

        public Text MethodNameLabel;
        public ButtonRef EditPatchButton;
        public ButtonRef ToggleActiveButton;
        public ButtonRef DeleteButton;

        public int CurrentDisplayedIndex;

        private void OnToggleActiveClicked()
        {
            HookList.EnableOrDisableHookClicked(CurrentDisplayedIndex);
        }

        private void OnDeleteClicked()
        {
            HookList.DeleteHookClicked(CurrentDisplayedIndex);
            HookCreator.AddHooksScrollPool.Refresh(true, false);
        }

        private void OnEditPatchClicked()
        {
            HookList.EditPatchClicked(CurrentDisplayedIndex);
        }

        public GameObject CreateContent(GameObject parent)
        {
            UIRoot = UIFactory.CreateUIObject(this.GetType().Name, parent, new Vector2(100, 30));
            Rect = UIRoot.GetComponent<RectTransform>();
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(UIRoot, false, false, true, true, 4, childAlignment: TextAnchor.UpperLeft);
            UIFactory.SetLayoutElement(UIRoot, minWidth: 100, flexibleWidth: 9999, minHeight: 30, flexibleHeight: 600);
            UIRoot.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            MethodNameLabel = UIFactory.CreateLabel(UIRoot, "MethodName", "NOT SET", TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(MethodNameLabel.gameObject, minHeight: 25, flexibleWidth: 9999);

            ToggleActiveButton = UIFactory.CreateButton(UIRoot, "ToggleActiveBtn", "On", new Color(0.15f, 0.2f, 0.15f));
            UIFactory.SetLayoutElement(ToggleActiveButton.Component.gameObject, minHeight: 25, minWidth: 35);
            ToggleActiveButton.OnClick += OnToggleActiveClicked;

            EditPatchButton = UIFactory.CreateButton(UIRoot, "EditButton", "Edit", new Color(0.15f, 0.15f, 0.15f));
            UIFactory.SetLayoutElement(EditPatchButton.Component.gameObject, minHeight: 25, minWidth: 35);
            EditPatchButton.OnClick += OnEditPatchClicked;

            DeleteButton = UIFactory.CreateButton(UIRoot, "DeleteButton", "X", new Color(0.2f, 0.15f, 0.15f));
            UIFactory.SetLayoutElement(DeleteButton.Component.gameObject, minHeight: 25, minWidth: 35);
            DeleteButton.OnClick += OnDeleteClicked;

            return UIRoot;
        }

        public void Disable()
        {
            UIRoot.SetActive(false);
        }

        public void Enable()
        {
            UIRoot.SetActive(true);
        }
    }
}
