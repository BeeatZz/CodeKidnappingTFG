using System;
using UnityEngine;
using UnityEngine.UI;

namespace ComputerUI
{
    // The box that shows a message or asks for a name. It sits on the dark layer that covers the desktop while it is open.
    public class DialogView : MonoBehaviour
    {
        public Text title;
        public GameObject iconRow;
        public Text message;
        public InputField input;
        public Button okButton;
        public Text okLabel;
        public Button cancelButton;

        public bool IsOpen
        {
            get { return gameObject.activeSelf; }
        }

        private Action<string> onOk;
        private Action onYes;
        private Action onNo;
        private Text cancelLabel;
        private string cancelDefault;

        private void Awake()
        {
            okButton.onClick.AddListener(Confirm);
            cancelButton.onClick.AddListener(Cancel);
        }

        // The label of the second button is found the first time it is needed, so it also works before Awake
        private Text CancelLabel()
        {
            if (cancelLabel == null)
            {
                cancelLabel = cancelButton.GetComponentInChildren<Text>(true);
                cancelDefault = cancelLabel.text;
            }
            return cancelLabel;
        }

        // A message with one button
        public void ShowMessage(string heading, string text, string okText)
        {
            Open(heading, text, okText);
            iconRow.SetActive(true);
            input.gameObject.SetActive(false);
            cancelButton.gameObject.SetActive(false);
            onOk = null;
            onYes = null;
            onNo = null;
        }

        // A question with two answers. Esc closes it without choosing, so nothing happens.
        public void ShowQuestion(string heading, string text, string yesText, string noText, Action yes, Action no)
        {
            Open(heading, text, yesText);
            iconRow.SetActive(true);
            input.gameObject.SetActive(false);
            cancelButton.gameObject.SetActive(true);
            CancelLabel().text = noText;
            onOk = null;
            onYes = yes;
            onNo = no;
        }

        // Asks for a name. Enter confirms and Esc cancels.
        public void ShowPrompt(string heading, string text, string initial, string okText, Action<string> confirmed)
        {
            Open(heading, text, okText);
            iconRow.SetActive(false);
            input.gameObject.SetActive(true);
            cancelButton.gameObject.SetActive(true);
            onOk = confirmed;
            onYes = null;
            onNo = null;

            input.text = initial;
            input.ActivateInputField();
            input.selectionAnchorPosition = 0;
            input.selectionFocusPosition = initial.Length;
        }

        private void Open(string heading, string text, string okText)
        {
            gameObject.SetActive(true);
            CancelLabel().text = cancelDefault;
            transform.SetAsLastSibling();
            title.text = heading;
            message.text = text;
            okLabel.text = okText;
        }

        private void Update()
        {
            if (input.gameObject.activeSelf && input.isFocused && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
                Confirm();
        }

        private void Confirm()
        {
            Action<string> callback = onOk;
            Action yes = onYes;
            string name = input.text.Replace("/", "").Replace("\\", "").Trim();
            Close();
            if (yes != null)
                yes();
            else if (callback != null && name.Length > 0)
                callback(name);
        }

        private void Cancel()
        {
            Action no = onNo;
            Close();
            if (no != null)
                no();
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }
    }
}
