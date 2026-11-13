using CodeRunner.Bridge;
using ComputerUI;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    // Crosshair, interact prompt, help text, the scan result and the imp's speech bubble.
    // The objects are made by the GameCanvasBuilder, this script only shows, hides and fills them.
    public class GameHud : MonoBehaviour
    {
        [Header("The game")]
        public PlayerInteractor interactor;
        public CanvasComputer computer;
        public ImpActionPlayer impPlayer;
        public float scanPanelSeconds = 15f;

        [Header("Parts of the screen")]
        public GameObject hudRoot;              // everything that is hidden while the computer is open
        public RectTransform canvasRect;
        public RectTransform promptPanel;
        public Text promptText;
        public GameObject scanPanel;
        public RectTransform scanContent;
        public RectTransform bubblePanel;
        public Text bubbleText;
        public Font font;                       // the pixel font of the computer

        private string describedHandle;
        private string describedSignature;

        private void Update()
        {
            // The computer covers the whole screen, so there is nothing to show
            bool computerOpen = computer != null && computer.IsOpen;
            hudRoot.SetActive(!computerOpen);
            if (computerOpen)
                return;

            UpdatePrompt();
            UpdateScanPanel();
            UpdateSpeechBubble();
        }

        private void UpdatePrompt()
        {
            bool looking = interactor.Current != null;
            promptPanel.gameObject.SetActive(looking);
            if (!looking)
                return;

            promptText.text = interactor.Current.GetPrompt();
            promptPanel.sizeDelta = new Vector2(promptText.preferredWidth + 36f, promptText.preferredHeight + 22f);
        }

        private void UpdateScanPanel()
        {
            TestSceneSetup setup = TestSceneSetup.Instance;
            bool show = setup.LastScanned != null && Time.time - setup.LastScanTime <= scanPanelSeconds;
            scanPanel.SetActive(show);
            if (!show)
                return;

            // The list is only built again when the object or one of its values changed
            CodeRunner.Bridge.ObjectDescription description = setup.Describe(setup.LastScanned);
            if (description == null)
                return;

            string signature = setup.LastScanned;
            foreach (PropertyDescription property in description.Properties)
                signature += "|" + property.Value;
            if (signature == describedSignature)
                return;

            describedHandle = setup.LastScanned;
            describedSignature = signature;
            ScannerView.Fill(scanContent, describedHandle, description, font);
        }

        // The bubble follows the imp on the screen
        private void UpdateSpeechBubble()
        {
            bool speaking = !string.IsNullOrEmpty(impPlayer.CurrentSpeech) && Camera.main != null;
            Vector3 screen = speaking ? Camera.main.WorldToScreenPoint(impPlayer.imp.position + Vector3.up * 1.4f) : Vector3.zero;
            speaking = speaking && screen.z > 0f;

            bubblePanel.gameObject.SetActive(speaking);
            if (!speaking)
                return;

            bubbleText.text = impPlayer.CurrentSpeech;

            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out local);
            bubblePanel.anchoredPosition = local + new Vector2(0f, 4f);
        }
    }
}
