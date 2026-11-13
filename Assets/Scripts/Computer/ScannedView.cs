using System;
using System.Text;
using CodeRunner.Bridge;
using Game;
using UnityEngine;
using UnityEngine.UIElements;

namespace Computer
{
    // The Scanned tab: every object the player has scanned, and what is known about the selected one
    public class ScannedView : VisualElement
    {
        // The player pressed Insert name
        public event Action<string> InsertRequested;

        private readonly VisualElement list;
        private readonly ScrollView card;

        private string selected = "imp";
        private string lastSeenScan;
        private int shownCount = -1;
        private string shownCard = "";
        private float refreshTimer;

        private TestSceneSetup Setup
        {
            get { return TestSceneSetup.Instance; }
        }

        public ScannedView()
        {
            AddToClassList("pane");
            Add(UiParts.Roller());

            VisualElement body = new VisualElement();
            body.AddToClassList("scanned-body");
            Add(body);

            ScrollView listScroll = new ScrollView(ScrollViewMode.Vertical);
            listScroll.AddToClassList("scanned-list");
            listScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            body.Add(listScroll);
            list = listScroll;

            card = new ScrollView(ScrollViewMode.Vertical);
            card.AddToClassList("scanned-card");
            body.Add(card);

            Add(UiParts.Roller());
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
            list.Clear();

            foreach (string name in Setup.ScannedNames)
            {
                string current = name;
                object target;
                string typeName = Setup.Handles.TryGet(name, out target) ? TypeNames.Of(target.GetType()) : "";

                Button row = new Button(delegate { selected = current; shownCount = -1; shownCard = ""; });
                row.AddToClassList("handle-row");
                row.EnableInClassList("handle-row-selected", name == selected);
                row.Add(new Label(name));

                Label type = new Label(typeName);
                type.AddToClassList("handle-type");
                row.Add(type);
                list.Add(row);
            }

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

            card.Clear();

            Label title = new Label(selected);
            title.AddToClassList("card-title");
            card.Add(title);

            string subtitle = description.TypeName;
            if (description.Interfaces.Count > 0)
                subtitle += " : " + string.Join(", ", description.Interfaces);
            Label sub = new Label(subtitle);
            sub.AddToClassList("card-subtitle");
            card.Add(sub);

            AddHeading("Properties");
            foreach (PropertyDescription property in description.Properties)
            {
                AddLine(property.TypeName + " " + property.Name + " = " + property.Value, "card-property");
                if (!string.IsNullOrEmpty(property.Description))
                    AddLine(property.Description, "card-note");
            }

            AddHeading("Methods");
            foreach (MethodDescription method in description.Methods)
            {
                if (method.ChangesWorld)
                    AddLine("[action] " + method.Signature, "card-action");
                else
                    AddLine(method.Signature, "card-method");
                if (!string.IsNullOrEmpty(method.Description))
                    AddLine(method.Description, "card-note");
            }

            VisualElement buttons = new VisualElement();
            buttons.AddToClassList("card-buttons");
            card.Add(buttons);

            string name = selected;
            Button insert = new Button(delegate { if (InsertRequested != null) InsertRequested(name); });
            insert.text = "Insert \"" + name + "\" in code";
            insert.AddToClassList("tool-button");
            buttons.Add(insert);

            Button copy = new Button(delegate { GUIUtility.systemCopyBuffer = name; });
            copy.text = "Copy name";
            copy.AddToClassList("tool-button");
            buttons.Add(copy);
        }

        private void AddHeading(string text)
        {
            Label heading = new Label(text);
            heading.AddToClassList("pane-heading");
            card.Add(heading);
        }

        private void AddLine(string text, string className)
        {
            Label label = new Label(text);
            label.AddToClassList("card-line");
            label.AddToClassList(className);
            card.Add(label);
        }
    }
}
