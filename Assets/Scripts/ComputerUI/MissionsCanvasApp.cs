using System.Text;
using Computer;
using Game;
using Mission;
using UnityEngine;
using UnityEngine.UI;

namespace ComputerUI
{
    // The mission board: a dark header with the name of the mission and its state, and a card for every goal
    // with a bar that fills up. It only reads the state of the mission, it never changes it.
    public class MissionsCanvasApp : CanvasApp
    {
        private static readonly Color NoteColor = new Color32(0x6b, 0x52, 0x30, 255);
        private static readonly Color GoodColor = new Color32(0x2f, 0x7a, 0x3a, 255);
        private static readonly Color AmberColor = new Color32(0xc2, 0x85, 0x1a, 255);
        private static readonly Color RedColor = new Color32(0xa3, 0x20, 0x2b, 255);
        private static readonly Color GreyColor = new Color32(0x7a, 0x6a, 0x86, 255);
        private static readonly Color HeaderColor = new Color32(0x2a, 0x12, 0x1c, 255);
        private static readonly Color CardColor = new Color(1f, 1f, 1f, 0.55f);
        private static readonly Color Cream = new Color(1f, 0.96f, 0.82f);

        private DesktopView desktop;
        private RectTransform header;
        private RectTransform content;
        private string shown = "";
        private float refreshTimer;

        public override string Name
        {
            get { return "Missions"; }
        }

        public override string IconName
        {
            get { return "Missions"; }
        }

        public override Vector2 WindowSize
        {
            get { return new Vector2(380f, 340f); }
        }

        public override Vector2 MinSize
        {
            get { return new Vector2(320f, 240f); }
        }

        public override void Build(DesktopView owner, RectTransform body)
        {
            desktop = owner;

            RectTransform page;
            UiKit.AddPage(body, "Missions", out page);
            UiKit.MakeColumn(page.gameObject, 0, 0, 0, 0, 0f);

            // The header is rebuilt with the board, because its colour and its stamp follow the state of the mission
            header = UiKit.NewRect(page, "Header");
            UiKit.MakeColumn(header.gameObject, 0, 0, 0, 0, 0f);

            RectTransform scroll = UiKit.AddScrollList(page, "Board", new Color(0f, 0f, 0f, 0f));
            content = scroll;
            UiKit.Size(content.parent.parent.gameObject, -1f, -1f, 1f, 1f);
            UiKit.MakeColumn(content.gameObject, 8, 8, 8, 8, 5f);

            Refresh();
        }

        public override void OnShown()
        {
            shown = "";
            Refresh();
        }

        public override void OnLookChanged(ComputerLook look)
        {
            shown = "";
            Refresh();
        }

        // The numbers change while the imp works, so the board is checked a few times a second
        public override void Tick()
        {
            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer > 0f)
                return;

            refreshTimer = 0.25f;
            Refresh();
        }

        private void Refresh()
        {
            TestSceneSetup setup = TestSceneSetup.Instance;
            if (setup == null)
            {
                Rebuild("none", delegate
                {
                    AddHeader("No mission", "WAITING", GreyColor);
                    AddCaption("Nothing is loaded.");
                });
                return;
            }

            if (setup.Kitchen != null)
                ShowKitchen(setup.Kitchen);
            else if (setup.Level != null)
                ShowCamp(setup.Level);
        }

        // The board is only made again when its numbers changed, so scrolling is not interrupted
        private void Rebuild(string signature, System.Action fill)
        {
            if (signature == shown)
                return;
            shown = signature;

            UiKit.Clear(header);
            UiKit.Clear(content);
            fill();
            desktop.Look.ApplyTo(header.gameObject);
            desktop.Look.ApplyTo(content.gameObject);
        }

        // ---------- The two missions ----------

        private void ShowKitchen(Kitchen kitchen)
        {
            int hunger = kitchen.Hound.Hunger;
            string signature = "kitchen|" + hunger + "|" + kitchen.RunsSinceReset + "|" + kitchen.OptionalGoalMet;

            Rebuild(signature, delegate
            {
                bool asleep = kitchen.IsMissionComplete;
                AddHeader("Feed the hellhound", asleep ? "COMPLETE" : "IN PROGRESS", asleep ? GoodColor : AmberColor);

                AddCard("Briefing", "", -1f, GreyColor, "The hound wakes up hungry. Feed it until it falls asleep.");

                AddCaption("GOALS");
                float fed = 1f - (float)hunger / Hellhound.StartHunger;
                AddCard("Put the hound to sleep", "hunger " + hunger + " / " + Hellhound.StartHunger, fed, asleep ? GoodColor : AmberColor, null);

                string state = "OPEN";
                Color optionalColor = GreyColor;
                if (kitchen.OptionalGoalMet)
                {
                    state = "DONE";
                    optionalColor = GoodColor;
                }
                else if (asleep)
                {
                    state = "MISSED";
                    optionalColor = RedColor;
                }
                AddCard("Optional: one run, nothing refused", state, -1f, optionalColor, null);

                AddCaption("STATUS");
                AddCard("Work orders run", kitchen.RunsSinceReset.ToString(), -1f, GreyColor, null);
            });
        }

        private void ShowCamp(FirstMission level)
        {
            Camp camp = level.Camp;
            StringBuilder state = new StringBuilder("camp");
            foreach (var entry in level.Ledger.Order)
                state.Append('|').Append(camp.CountAccepted(entry.Key));
            state.Append('|').Append(camp.Rejected.Count);

            Rebuild(state.ToString(), delegate
            {
                bool complete = MissionRules.IsMissionComplete(level.Ledger, camp);
                string stamp = complete ? "COMPLETE" : camp.Rejected.Count > 0 ? "PROBLEMS" : "IN PROGRESS";
                AddHeader("Supply the camp", stamp, complete ? GoodColor : camp.Rejected.Count > 0 ? RedColor : AmberColor);

                AddCard("Briefing", "", -1f, GreyColor, "Load the wagons with what the camp ordered and send them. Check every crate first.");

                AddCaption("THE ORDER");
                foreach (var entry in level.Ledger.Order)
                {
                    int accepted = camp.CountAccepted(entry.Key);
                    bool done = accepted >= entry.Value;
                    AddCard(entry.Key, accepted + " / " + entry.Value, Mathf.Clamp01((float)accepted / entry.Value), done ? GoodColor : AmberColor, null);
                }

                AddCaption("THE CAMP SAYS");
                AddCard("Accepted", camp.Accepted.Count.ToString(), -1f, GoodColor, null);
                for (int i = 0; i < camp.Rejected.Count && i < 5; i++)
                    AddCard("Sent back: " + camp.Rejected[i].Cargo.Label, "", -1f, RedColor, camp.Rejected[i].Reason);
            });
        }

        // ---------- Pieces of the board ----------

        // The dark band on top: what the mission is and a stamp with how it goes
        private void AddHeader(string title, string stamp, Color stampColor)
        {
            Image band = UiKit.AddImage(header, "Band", HeaderColor);
            UiKit.MakeRow(band.gameObject, 14, 10, 12, 10, 8f, TextAnchor.MiddleLeft);

            RectTransform names = UiKit.NewRect(band.transform, "Names");
            UiKit.MakeColumn(names.gameObject, 0, 0, 0, 0, 1f);
            UiKit.Size(names.gameObject, -1f, -1f, 1f, -1f);
            UiKit.AddText(names, "Label", "MISSION BOARD", TextRole.Body, 10, Palette.Gold, TextAnchor.UpperLeft);
            UiKit.AddText(names, "Title", title, TextRole.Title, 20, Cream, TextAnchor.UpperLeft);

            Image pill = UiKit.AddPicture(band.transform, "Stamp", stampColor);
            UiKit.MakeColumn(pill.gameObject, 9, 4, 9, 4, 0f);
            Text stampText = UiKit.AddText(pill.transform, "Text", stamp, TextRole.Title, 12, Cream, TextAnchor.MiddleCenter);
            stampText.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        private void AddCaption(string text)
        {
            Text caption = UiKit.AddText(content, "Caption", text, TextRole.Title, 12, UiKit.GoldDark, TextAnchor.LowerLeft);
            UiKit.Size(caption.gameObject, -1f, 20f, -1f, -1f);
        }

        // A card with a coloured edge, a name, a value, and a bar if the ratio is not negative
        private void AddCard(string title, string value, float ratio, Color accent, string note)
        {
            Image card = UiKit.AddImage(content, "Card", CardColor);
            UiKit.MakeRow(card.gameObject, 0, 0, 0, 0, 0f, TextAnchor.UpperLeft);
            card.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;

            Image edge = UiKit.AddPicture(card.transform, "Edge", accent);
            UiKit.Size(edge.gameObject, 5f, -1f, -1f, -1f);

            RectTransform column = UiKit.NewRect(card.transform, "Column");
            UiKit.MakeColumn(column.gameObject, 10, 6, 10, 7, 4f);
            UiKit.Size(column.gameObject, -1f, -1f, 1f, -1f);

            RectTransform line = UiKit.NewRect(column, "Line");
            UiKit.MakeRow(line.gameObject, 0, 0, 0, 0, 6f, TextAnchor.MiddleLeft);
            Text name = UiKit.AddText(line, "Name", title, TextRole.Code, 13, Palette.Ink, TextAnchor.MiddleLeft);
            name.horizontalOverflow = HorizontalWrapMode.Overflow;
            UiKit.Size(name.gameObject, -1f, 18f, 1f, -1f);
            if (value.Length > 0)
            {
                Text amount = UiKit.AddText(line, "Value", value, TextRole.Title, 13, accent, TextAnchor.MiddleRight);
                amount.horizontalOverflow = HorizontalWrapMode.Overflow;
                UiKit.Size(amount.gameObject, -1f, 18f, -1f, -1f);
            }

            if (ratio >= 0f)
            {
                Image back = UiKit.AddPicture(column, "Bar", new Color(0f, 0f, 0f, 0.2f));
                UiKit.Size(back.gameObject, -1f, 9f, -1f, -1f);
                Image fill = UiKit.AddPicture(back.transform, "Fill", accent);
                fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
            }

            if (!string.IsNullOrEmpty(note))
                UiKit.AddText(column, "Note", note, TextRole.Body, 11, NoteColor, TextAnchor.UpperLeft);
        }
    }
}
