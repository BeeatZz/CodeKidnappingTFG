using System;
using System.Collections.Generic;
using System.Text;
using CodeRunner.Bridge;
using Computer;
using Game;
using UnityEngine;
using UnityEngine.UI;

namespace ComputerUI
{
    // The Scanned tab: every object the player has scanned, and what is known about the selected one
    public class ScannedPane
    {
        private static readonly Color NoteColor = new Color32(0x6b, 0x52, 0x30, 255);

        // The player pressed Insert name
        public event Action<string> InsertRequested;

        public readonly GameObject Root;

        private readonly RectTransform list;
        private readonly RectTransform card;
        private ComputerLook look;

        // Room for a name or a type in a row of the list
        private const float RowTextWidth = 108f;

        private string selected = "imp";
        private string lastSeenScan;
        private int shownCount = -1;
        private string shownCard = "";
        private float refreshTimer;

        private TestSceneSetup Setup
        {
            get { return TestSceneSetup.Instance; }
        }

        public ScannedPane(RectTransform parent, ComputerLook look)
        {
            this.look = look;

            RectTransform page;
            Root = UiKit.AddPage(parent, "Scanned", out page).gameObject;
            UiKit.MakeColumn(page.gameObject, 0, 0, 0, 0, 0f);
            UiKit.AddRoller(page.transform);

            // The list of names on the left, the card of the selected one on the right
            RectTransform body = UiKit.NewRect(page.transform, "Body");
            UiKit.MakeRow(body.gameObject, 0, 0, 0, 0, 0f, TextAnchor.UpperLeft);
            body.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;
            UiKit.Size(body.gameObject, -1f, -1f, 1f, 1f);

            list = UiKit.AddScrollList(body, "Names", new Color(0f, 0f, 0f, 0f));
            UiKit.Size(list.GetComponentInParent<ScrollRect>().gameObject, 132f, -1f, -1f, 1f);
            UiKit.MakeColumn(list.gameObject, 0, 4, 0, 4, 0f);

            card = UiKit.AddScrollList(body, "Card", new Color(0f, 0f, 0f, 0f));
            UiKit.Size(card.GetComponentInParent<ScrollRect>().gameObject, -1f, -1f, 1f, 1f);
            UiKit.MakeColumn(card.gameObject, 10, 6, 10, 6, 0f);

            UiKit.AddRoller(page.transform);
            look.ApplyTo(Root);
        }

        // The desktop changed look: the rows and the card are made again with the new fonts
        public void SetLook(ComputerLook newLook)
        {
            look = newLook;
            shownCount = -1;
            shownCard = "";
        }

        // Called every frame while the tab can be seen
        public void Tick()
        {
            // Select the newest scan
            if (Setup.LastScanned != lastSeenScan)
            {
                lastSeenScan = Setup.LastScanned;
                if (lastSeenScan != null)
                    selected = lastSeenScan;
                shownCount = -1;
            }

            if (Setup.ScannedNames.Count != shownCount)
                RebuildList();

            // The values of an object change while the imp works, so the card is checked a few times a second
            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer <= 0f)
            {
                refreshTimer = 0.25f;
                RefreshCard();
            }
        }

        private void RebuildList()
        {
            shownCount = Setup.ScannedNames.Count;
            UiKit.Clear(list);

            // The texts that may be too long for a row, with what they should say
            List<Text> fitted = new List<Text>();
            List<string> fullTexts = new List<string>();

            foreach (string name in Setup.ScannedNames)
            {
                string current = name;
                object target;
                string typeName = Setup.Handles.TryGet(name, out target) ? TypeNames.Of(target.GetType()) : "";

                // A row is a button with the name on the first line and the type under it
                Button row = UiKit.AddButton(list, "Row", "", SkinRole.Fill, name == selected ? new Color(0.85f, 0.7f, 0.45f, 0.6f) : new Color(1f, 1f, 1f, 0f), Palette.Ink, 14);
                UiKit.Size(row.gameObject, -1f, 32f, -1f, -1f);

                Text nameLabel = row.GetComponentInChildren<Text>();
                nameLabel.alignment = TextAnchor.UpperLeft;
                nameLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
                row.GetComponentInChildren<LookText>().role = TextRole.Code;
                UiKit.Stretch(nameLabel.rectTransform, 6f, 2f, 6f, 0f);

                Text type = UiKit.AddText(row.transform, "Type", typeName, TextRole.Body, 10, NoteColor, TextAnchor.LowerLeft);
                UiKit.Stretch(type.rectTransform, 6f, 0f, 6f, 2f);

                fitted.Add(nameLabel);
                fullTexts.Add(name);
                fitted.Add(type);
                fullTexts.Add(typeName);

                row.onClick.AddListener(delegate { selected = current; shownCount = -1; shownCard = ""; });
            }

            look.ApplyTo(list.gameObject);

            // The fonts are in place now, so the texts can be cut to the width of a row
            for (int i = 0; i < fitted.Count; i++)
                UiKit.FitLines(fitted[i], fullTexts[i], RowTextWidth, 1);
            shownCard = "";
            RefreshCard();
        }

        private void RefreshCard()
        {
            ObjectDescription description = Setup.Describe(selected);
            if (description == null)
                return;

            // Only rebuild the card when a value changed, so scrolling and clicking are not interrupted
            StringBuilder signature = new StringBuilder(selected);
            foreach (PropertyDescription property in description.Properties)
                signature.Append('|').Append(property.Value);
            if (signature.ToString() == shownCard)
                return;
            shownCard = signature.ToString();

            UiKit.Clear(card);

            Text title = UiKit.AddText(card, "Title", selected, TextRole.Title, 18, Palette.Blood, TextAnchor.MiddleLeft);
            UiKit.Size(title.gameObject, -1f, 28f, -1f, -1f);

            string subtitle = description.TypeName;
            if (description.Interfaces.Count > 0)
                subtitle += " : " + string.Join(", ", description.Interfaces);
            Text sub = UiKit.AddText(card, "Subtitle", subtitle, TextRole.Body, 12, NoteColor, TextAnchor.UpperLeft);
            UiKit.Size(sub.gameObject, -1f, 18f, -1f, -1f);

            AddHeading("Properties");
            foreach (PropertyDescription property in description.Properties)
            {
                AddLine(property.TypeName + " " + property.Name + " = " + property.Value, new Color32(0x1f, 0x4f, 0x8a, 255));
                if (!string.IsNullOrEmpty(property.Description))
                    AddNote(property.Description);
            }

            AddHeading("Methods");
            foreach (MethodDescription method in description.Methods)
            {
                if (method.ChangesWorld)
                    AddLine("[action] " + method.Signature, new Color32(0xc2, 0x54, 0x1a, 255));
                else
                    AddLine(method.Signature, new Color32(0x8a, 0x4a, 0x10, 255));
                if (!string.IsNullOrEmpty(method.Description))
                    AddNote(method.Description);
            }

            RectTransform buttons = UiKit.NewRect(card, "Buttons");
            UiKit.MakeRow(buttons.gameObject, 0, 8, 0, 0, 6f, TextAnchor.MiddleLeft);

            string name = selected;
            UiKit.AddToolButton(buttons, "Insert \"" + name + "\" in code", 130f).onClick.AddListener(delegate { if (InsertRequested != null) InsertRequested(name); });
            UiKit.AddToolButton(buttons, "Copy name").onClick.AddListener(delegate { GUIUtility.systemCopyBuffer = name; });

            look.ApplyTo(card.gameObject);
        }

        private void AddHeading(string text)
        {
            Text heading = UiKit.AddText(card, "Heading", text, TextRole.Title, 15, Palette.Blood, TextAnchor.MiddleLeft);
            UiKit.Size(heading.gameObject, -1f, 22f, -1f, -1f);
        }

        private void AddLine(string text, Color color)
        {
            UiKit.AddText(card, "Line", text, TextRole.Code, 14, color, TextAnchor.UpperLeft);
        }

        private void AddNote(string text)
        {
            UiKit.AddText(card, "Note", "    " + text, TextRole.Body, 11, NoteColor, TextAnchor.UpperLeft);
        }
    }
}
