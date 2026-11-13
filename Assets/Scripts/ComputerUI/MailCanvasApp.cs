using System.Collections.Generic;
using Computer;
using UnityEngine;
using UnityEngine.UI;

namespace ComputerUI
{
    // The mail of the depot, laid out like a webmail: a search bar on top, the folders on the left and
    // the list of messages on the right. Opening a message replaces the list, so the panes never change size.
    // What was read, starred and deleted stays like that until the game is closed.
    public class MailCanvasApp : CanvasApp
    {
        private class Message
        {
            public string from;
            public string subject;
            public string category;
            public string time;
            public string body;

            public Message(string from, string subject, string category, string time, string body)
            {
                this.from = from;
                this.subject = subject;
                this.category = category;
                this.time = time;
                this.body = body;
            }
        }

        private static readonly Message[] Messages =
        {
            new Message("Overseer Vrakk", "Welcome to the depot", "Important", "Day 1, 06:00",
                "New handler,\n\nYou have been given a desk, a screen and an imp. Look after the imp.\n\n" +
                "Scan things with the scanner and they appear in the Scanned tab of your work order. " +
                "Write what the imp must do, seal it with F5 and step away from the screen. The imp does the rest.\n\n" +
                "Do not lose the imp.\n\nVrakk"),
            new Message("Quartermaster Gruk", "About the camp order", "Orders", "Day 1, 07:30",
                "The camp wants grain, salted meat, arrows, lamp oil and bandages. The Missions app has the exact numbers.\n\n" +
                "Some of what is in the depot is not worth sending. A crate can be spoiled, full of rocks or leaking, " +
                "and the camp sends all of that back. I do not like being sent things back.\n\nGruk"),
            new Message("Skritch", "pls", "Personal", "Day 1, 09:12",
                "hi handler\n\ni carried the leaking lamp oil last time. my tail is still sticky.\n\n" +
                "if you can check the crate first i will be very happy. scan it. look at what it says about itself.\n\nskritch"),
            new Message("Hound Warden", "Feeding the hellhound", "Orders", "Day 1, 10:45",
                "The hound is hungry. It must eat until it sleeps.\n\n" +
                "Meat and bread are fine. Turnips make it sulk. Remember that the label on a sack is only a label: " +
                "look at what is really inside.\n\n" +
                "It is also nice if the hound is fed in one go, without being refused anything.\n\nThe Warden"),
            new Message("The Demon Lord", "Performance review", "Important", "Day 1, 12:00",
                "I have read your files.\n\nThe Oubliette is very full. I am pleased.\n\nKeep going.\n\n- D.L."),
            new Message("Desktop", "New programs", "Important", "Day 1, 12:30",
                "Your desktop has a few new programs: Mail, Missions, Clock, Calendar, Paint and a Browser.\n\n" +
                "The Browser only knows the Hellnet. The real world has no wires down here."),
            new Message("Dark Prize Dept.", "YOU HAVE WON A CURSED AMULET!!!", "Spam", "Day 1, 13:01",
                "CONGRATULATIONS!!!\n\nYou are the 1,000,000th handler to open this mail. Claim your free amulet today.\n\n" +
                "Side effects include being cursed. Click nowhere to claim."),
            new Message("Cheap Wings Ltd.", "50% off bat wings, today only", "Spam", "Day 1, 13:20",
                "Tired of walking? Our wings are made from 100% real bat.\n\nSome assembly required. No refunds. No bats."),
            new Message("Oubliette Storage", "Your oubliette is 100% full", "Spam", "Day 1, 14:02",
                "Your oubliette has run out of room.\n\nUpgrade today for only three souls a month. Souls are not refundable."),
            new Message("The Brimstone Gazette", "Weekly newsletter: ash forecast", "Junk", "Day 1, 15:00",
                "This week: ash, then more ash, then a short pause, then ash.\n\nAlso inside: the winner of the Best Tail contest."),
            new Message("Chain Letter", "FWD: send this to 7 demons or else", "Junk", "Day 1, 15:30",
                "If you do not send this to seven demons within an hour, absolutely nothing will happen.\n\nBut please send it anyway."),
            new Message("Pit Facilities", "Reminder: the lava cooler is broken again", "Junk", "Day 1, 16:10",
                "The lava cooler on level 3 is broken again. It was never working.\n\nPlease stop filing reports about it.")
        };

        // The folders of the sidebar. The first ones show a mix, the others show one category each.
        private const int InboxFolder = 0;
        private const int StarredFolder = 1;
        private const int TrashFolder = 7;
        private static readonly string[] Folders = { "Inbox", "Starred", "Important", "Orders", "Personal", "Spam", "Junk", "Trash" };

        private static readonly string[] Categories = { "Important", "Orders", "Personal", "Spam", "Junk" };
        private static readonly Color32[] CategoryColors =
        {
            new Color32(0xd9, 0x30, 0x25, 255), new Color32(0x1a, 0x73, 0xe8, 255), new Color32(0x18, 0x80, 0x38, 255),
            new Color32(0xe3, 0x74, 0x00, 255), new Color32(0x5f, 0x63, 0x68, 255)
        };

        // What was read, starred and deleted. It lives longer than the window.
        private static readonly HashSet<int> Read = new HashSet<int>();
        private static readonly HashSet<int> Starred = new HashSet<int> { 0 };
        private static readonly HashSet<int> Deleted = new HashSet<int>();

        private static readonly Color SideColor = new Color32(0xf6, 0xf8, 0xfc, 255);
        private static readonly Color SearchColor = new Color32(0xe9, 0xee, 0xf6, 255);
        private static readonly Color SelectedColor = new Color32(0xd3, 0xe3, 0xfd, 255);
        private static readonly Color ComposeColor = new Color32(0xc2, 0xe7, 0xff, 255);
        private static readonly Color ReadRowColor = new Color32(0xf2, 0xf6, 0xfc, 255);
        private static readonly Color DividerColor = new Color32(0xe0, 0xe3, 0xe8, 255);
        private static readonly Color GreyText = new Color32(0x5f, 0x63, 0x68, 255);
        private static readonly Color StarOn = new Color32(0xf4, 0xb4, 0x00, 255);
        private static readonly Color StarOff = new Color32(0xb4, 0xb8, 0xbe, 255);

        private const float SidebarWidth = 150f;
        private const float SenderWidth = 118f;

        // The room left for the subject in a row, next to the sender and the time
        private const float SubjectLeft = 162f;
        private const float SubjectRight = 84f;

        private DesktopView desktop;
        private RectTransform folderList;
        private RectTransform main;
        private InputField search;
        private Text searchHint;
        private int folder;
        private int selected = -1;
        private string searchText = "";
        private float shownWidth;

        // The texts of the list that are cut to fit, with the full text and the width they get
        private readonly List<Text> fitted = new List<Text>();
        private readonly List<string> fullTexts = new List<string>();
        private readonly List<float> fitWidths = new List<float>();

        public override string Name
        {
            get
            {
                int unread = UnreadCount(InboxFolder);
                return unread > 0 ? "Mail (" + unread + ")" : "Mail";
            }
        }

        public override string IconName
        {
            get { return "Mail"; }
        }

        public override string ShortName
        {
            get { return "Mail"; }
        }

        public override Vector2 WindowSize
        {
            get { return new Vector2(680f, 380f); }
        }

        public override Vector2 MinSize
        {
            get { return new Vector2(640f, 340f); }
        }

        // ---------- Which message is where ----------

        private static bool InFolder(int index, int which)
        {
            bool deleted = Deleted.Contains(index);
            if (which == TrashFolder)
                return deleted;
            if (deleted)
                return false;

            string category = Messages[index].category;
            if (which == InboxFolder)
                return category != "Spam" && category != "Junk";
            if (which == StarredFolder)
                return Starred.Contains(index);
            return category == Folders[which];
        }

        private static int UnreadCount(int which)
        {
            if (which == TrashFolder)
                return 0;

            int count = 0;
            for (int i = 0; i < Messages.Length; i++)
            {
                if (InFolder(i, which) && !Read.Contains(i))
                    count++;
            }
            return count;
        }

        // The messages to show, newest first, with the words of the search box
        private List<int> Matching()
        {
            List<int> found = new List<int>();
            string words = searchText.Trim().ToLower();
            for (int i = Messages.Length - 1; i >= 0; i--)
            {
                if (!InFolder(i, folder))
                    continue;

                Message message = Messages[i];
                if (words.Length > 0 && !message.from.ToLower().Contains(words) && !message.subject.ToLower().Contains(words)
                    && !message.body.ToLower().Contains(words))
                    continue;
                found.Add(i);
            }
            return found;
        }

        private static string ShortTime(string time)
        {
            int comma = time.IndexOf(',');
            return comma >= 0 ? time.Substring(comma + 2) : time;
        }

        private static string FirstLine(string body)
        {
            int end = body.IndexOf('\n');
            return end >= 0 ? body.Substring(0, end) : body;
        }

        // ---------- Building the window ----------

        public override void Build(DesktopView owner, RectTransform body)
        {
            desktop = owner;

            Image window = UiKit.AddImage(body, "Mail", Color.white);
            UiKit.MakeColumn(window.gameObject, 0, 0, 0, 0, 0f);

            // The bar on top: the name of the mail and the search box
            Image top = UiKit.AddImage(window.transform, "TopBar", SideColor);
            UiKit.MakeRow(top.gameObject, 14, 5, 14, 5, 14f, TextAnchor.MiddleLeft);
            UiKit.Size(top.gameObject, -1f, 38f, -1f, -1f);

            Text logo = UiKit.AddText(top.transform, "Logo", "<color=#d93025>Hell</color><color=#5f6368>mail</color>", TextRole.Title, 17, Palette.Ink, TextAnchor.MiddleLeft);
            logo.horizontalOverflow = HorizontalWrapMode.Overflow;
            UiKit.Size(logo.gameObject, 120f, -1f, -1f, -1f);

            Image pill = UiKit.AddImage(top.transform, "Search", SearchColor);
            UiKit.MakeColumn(pill.gameObject, 0, 0, 0, 0, 0f);
            UiKit.Size(pill.gameObject, 0f, 28f, 1f, -1f);
            search = UiKit.AddInputField(pill.transform, "Field", false, new Color(0f, 0f, 0f, 0f), Palette.Ink, TextRole.Body, 12);
            UiKit.Size(search.gameObject, -1f, 28f, 1f, -1f);
            search.onValueChanged.AddListener(OnSearchChanged);

            // The hint sits over the box until something is typed
            searchHint = UiKit.AddText(pill.transform, "Hint", "Search mail", TextRole.Body, 12, GreyText, TextAnchor.MiddleLeft);
            searchHint.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            UiKit.Stretch(searchHint.rectTransform, 6f, 0f, 6f, 0f);

            // The folders on the left and the messages on the right
            RectTransform area = UiKit.NewRect(window.transform, "Area");
            UiKit.MakeRow(area.gameObject, 0, 0, 0, 0, 0f, TextAnchor.UpperLeft);
            area.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;
            UiKit.Size(area.gameObject, -1f, -1f, 1f, 1f);

            Image side = UiKit.AddImage(area, "Sidebar", SideColor);
            UiKit.MakeColumn(side.gameObject, 10, 8, 10, 6, 2f);
            UiKit.Size(side.gameObject, SidebarWidth, -1f, 0f, 1f).minWidth = SidebarWidth;

            Button compose = UiKit.AddButton(side.transform, "Compose", "Compose", SkinRole.Fill, ComposeColor, Palette.Ink, 12);
            UiKit.Size(compose.gameObject, -1f, 32f, -1f, -1f);
            compose.onClick.AddListener(delegate { desktop.ShowToast("The imps ran out of ink"); });

            RectTransform gap = UiKit.NewRect(side.transform, "Gap");
            UiKit.Size(gap.gameObject, -1f, 6f, -1f, -1f);

            folderList = UiKit.NewRect(side.transform, "Folders");
            UiKit.MakeColumn(folderList.gameObject, 0, 0, 0, 0, 1f);

            // The right side has a fixed width of nothing and grows to the rest, so no message can change its size
            Image right = UiKit.AddImage(area, "Main", Color.white);
            UiKit.MakeColumn(right.gameObject, 0, 0, 0, 0, 0f);
            UiKit.Size(right.gameObject, 0f, 0f, 1f, 1f);
            main = right.rectTransform;

            RebuildAll();
        }

        private void RebuildAll()
        {
            RebuildFolders();
            RebuildMain();
        }

        private void OnSearchChanged(string text)
        {
            searchText = text;
            searchHint.gameObject.SetActive(text.Length == 0);
            selected = -1;
            RebuildMain();
        }

        private void RebuildFolders()
        {
            UiKit.Clear(folderList);

            for (int i = 0; i < Folders.Length; i++)
            {
                int index = i;
                bool on = i == folder;
                Button item = UiKit.AddButton(folderList, "Folder", Folders[i], SkinRole.Fill, on ? SelectedColor : new Color(1f, 1f, 1f, 0f), Palette.Ink, 12);
                UiKit.Size(item.gameObject, -1f, 26f, -1f, -1f);

                Text label = item.GetComponentInChildren<Text>();
                label.alignment = TextAnchor.MiddleLeft;
                UiKit.Stretch(label.rectTransform, 12f, 0f, 34f, 0f);
                label.color = on ? Palette.Ink : GreyText;

                int unread = UnreadCount(i);
                Text count = UiKit.AddText(item.transform, "Count", unread > 0 ? unread.ToString() : "", TextRole.Body, 11, Palette.Ink, TextAnchor.MiddleRight);
                UiKit.Stretch(count.rectTransform, 0f, 0f, 10f, 0f);

                item.onClick.AddListener(delegate { SelectFolder(index); });
            }

            desktop.Look.ApplyTo(folderList.gameObject);
        }

        private void SelectFolder(int index)
        {
            folder = index;
            selected = -1;
            RebuildAll();
        }

        private void RebuildMain()
        {
            UiKit.Clear(main);
            fitted.Clear();
            fullTexts.Clear();
            fitWidths.Clear();

            if (selected < 0)
                ShowList();
            else
                ShowMessage();

            desktop.Look.ApplyTo(main.gameObject);

            // The fonts are in place now, so the texts can be cut to the width they have
            for (int i = 0; i < fitted.Count; i++)
                UiKit.FitLines(fitted[i], fullTexts[i], fitWidths[i], 1);
        }

        // The bar over the list or the message, with a thin line under it
        private Transform AddToolbar()
        {
            Image bar = UiKit.AddImage(main, "Toolbar", Color.white);
            UiKit.MakeRow(bar.gameObject, 12, 4, 12, 4, 6f, TextAnchor.MiddleLeft);
            UiKit.Size(bar.gameObject, -1f, 34f, -1f, -1f);

            Image line = UiKit.AddPicture(main, "Line", DividerColor);
            UiKit.Size(line.gameObject, -1f, 1f, -1f, -1f);
            return bar.transform;
        }

        // ---------- The list ----------

        private void ShowList()
        {
            List<int> items = Matching();

            Transform bar = AddToolbar();
            Text name = UiKit.AddText(bar, "Folder", Folders[folder], TextRole.Title, 14, Palette.Ink, TextAnchor.MiddleLeft);
            UiKit.Size(name.gameObject, 0f, -1f, 1f, -1f);
            Text total = UiKit.AddText(bar, "Total", items.Count + (items.Count == 1 ? " message" : " messages"), TextRole.Body, 11, GreyText, TextAnchor.MiddleRight);
            total.horizontalOverflow = HorizontalWrapMode.Overflow;

            RectTransform list = UiKit.AddScrollList(main, "Messages", DividerColor);
            UiKit.Size(list.parent.parent.gameObject, -1f, -1f, 1f, 1f);
            UiKit.MakeColumn(list.gameObject, 0, 0, 0, 0, 1f);

            float subjectWidth = SubjectWidth();
            foreach (int i in items)
            {
                int index = i;
                Message message = Messages[i];
                bool unread = !Read.Contains(i);
                Color textColor = unread ? Palette.Ink : GreyText;

                Button row = UiKit.AddButton(list, "Message", message.from, SkinRole.Fill, unread ? Color.white : ReadRowColor, textColor, 12);
                UiKit.Size(row.gameObject, -1f, 28f, -1f, -1f);
                row.onClick.AddListener(delegate { Select(index); });

                // Written at once, because the sender is the label of the button
                Text sender = row.GetComponentInChildren<Text>();
                sender.alignment = TextAnchor.MiddleLeft;
                sender.color = textColor;
                UiKit.Stretch(sender.rectTransform, 32f, 0f, 0f, 0f);

                Text subject = UiKit.AddText(row.transform, "Subject", message.subject, TextRole.Body, 12, textColor, TextAnchor.MiddleLeft);
                UiKit.Stretch(subject.rectTransform, SubjectLeft, 0f, SubjectRight, 0f);

                Text time = UiKit.AddText(row.transform, "Time", ShortTime(message.time), TextRole.Body, 10, GreyText, TextAnchor.MiddleRight);
                UiKit.Stretch(time.rectTransform, 0f, 0f, 12f, 0f);

                Button star = UiKit.AddButton(row.transform, "Star", "*", SkinRole.Fill, new Color(1f, 1f, 1f, 0f), Starred.Contains(i) ? StarOn : StarOff, 16);
                UiKit.Place(star.GetComponent<RectTransform>(), 6f, 4f, 22f, 20f);
                star.onClick.AddListener(delegate { ToggleStar(index); });

                fitted.Add(sender);
                fullTexts.Add(message.from);
                fitWidths.Add(SenderWidth);
                fitted.Add(subject);
                fullTexts.Add(message.subject + " - " + FirstLine(message.body));
                fitWidths.Add(subjectWidth);
            }

            if (items.Count == 0)
            {
                string empty = searchText.Length > 0 ? "No messages match your search." : "Nothing in here.";
                Text none = UiKit.AddText(list, "Empty", empty, TextRole.Body, 12, GreyText, TextAnchor.MiddleCenter);
                UiKit.Size(none.gameObject, -1f, 80f, -1f, -1f);
            }
        }

        // The width of the subject follows the window
        private float SubjectWidth()
        {
            float width = shownWidth > 0f ? shownWidth : WindowSize.x - SidebarWidth;
            return width - SubjectLeft - SubjectRight - 8f;
        }

        private void Select(int index)
        {
            selected = index;
            bool wasNew = Read.Add(index);
            RebuildAll();
            if (wasNew)
                RaiseTitleChanged();
        }

        private void ToggleStar(int index)
        {
            if (!Starred.Remove(index))
                Starred.Add(index);
            RebuildAll();
        }

        // ---------- The message ----------

        private void ShowMessage()
        {
            Message message = Messages[selected];
            int categoryIndex = System.Array.IndexOf(Categories, message.category);
            Color categoryColor = CategoryColors[Mathf.Max(0, categoryIndex)];
            bool inTrash = Deleted.Contains(selected);

            Transform bar = AddToolbar();
            UiKit.AddToolButton(bar, "Back", 46f).onClick.AddListener(delegate { selected = -1; RebuildMain(); });
            UiKit.AddToolButton(bar, inTrash ? "Restore" : "Delete", 56f).onClick.AddListener(inTrash ? (UnityEngine.Events.UnityAction)RestoreSelected : DeleteSelected);
            UiKit.AddToolButton(bar, "Unread", 52f).onClick.AddListener(MarkUnread);
            UiKit.AddToolButton(bar, Starred.Contains(selected) ? "Unstar" : "Star", 52f).onClick.AddListener(delegate { ToggleStar(selected); });

            RectTransform text = UiKit.AddScrollList(main, "Body", Color.white);
            UiKit.Size(text.parent.parent.gameObject, -1f, -1f, 1f, 1f);
            UiKit.MakeColumn(text.gameObject, 24, 16, 24, 16, 10f);

            // The subject and the label of its category
            UiKit.AddText(text, "Subject", message.subject, TextRole.Title, 19, Palette.Ink, TextAnchor.UpperLeft);

            RectTransform labelRow = UiKit.NewRect(text, "LabelRow");
            UiKit.MakeRow(labelRow.gameObject, 0, 0, 0, 0, 0f, TextAnchor.MiddleLeft);
            UiKit.Size(labelRow.gameObject, -1f, 18f, -1f, -1f);
            Image chip = UiKit.AddPicture(labelRow, "Chip", categoryColor);
            UiKit.MakeColumn(chip.gameObject, 8, 1, 8, 1, 0f);
            Text chipText = UiKit.AddText(chip.transform, "Category", message.category, TextRole.Body, 10, Color.white, TextAnchor.MiddleCenter);
            chipText.horizontalOverflow = HorizontalWrapMode.Overflow;

            // Who wrote it: a coloured square with the first letter, the name and the time
            RectTransform sender = UiKit.NewRect(text, "Sender");
            UiKit.MakeRow(sender.gameObject, 0, 4, 0, 4, 10f, TextAnchor.MiddleLeft);
            UiKit.Size(sender.gameObject, -1f, 44f, -1f, -1f);

            Image avatar = UiKit.AddPicture(sender, "Avatar", CategoryColors[(message.from[0] + 1) % CategoryColors.Length]);
            UiKit.MakeColumn(avatar.gameObject, 0, 0, 0, 0, 0f);
            UiKit.Size(avatar.gameObject, 34f, 34f, -1f, -1f);
            UiKit.AddText(avatar.transform, "Initial", message.from.Substring(0, 1).ToUpper(), TextRole.Title, 16, Color.white, TextAnchor.MiddleCenter);

            RectTransform names = UiKit.NewRect(sender, "Names");
            UiKit.MakeColumn(names.gameObject, 0, 0, 0, 0, 0f);
            UiKit.Size(names.gameObject, 0f, -1f, 1f, -1f);
            Text from = UiKit.AddText(names, "From", message.from, TextRole.Body, 13, Palette.Ink, TextAnchor.MiddleLeft);
            UiKit.Size(from.gameObject, -1f, 18f, -1f, -1f);
            Text to = UiKit.AddText(names, "To", "to me", TextRole.Body, 11, GreyText, TextAnchor.MiddleLeft);
            UiKit.Size(to.gameObject, -1f, 14f, -1f, -1f);

            Text sent = UiKit.AddText(sender, "Sent", message.time, TextRole.Body, 11, GreyText, TextAnchor.MiddleRight);
            sent.horizontalOverflow = HorizontalWrapMode.Overflow;

            UiKit.AddText(text, "Text", message.body, TextRole.Body, 12, Palette.Ink, TextAnchor.UpperLeft);

            RectTransform answers = UiKit.NewRect(text, "Answers");
            UiKit.MakeRow(answers.gameObject, 0, 8, 0, 0, 6f, TextAnchor.MiddleLeft);
            UiKit.Size(answers.gameObject, -1f, 30f, -1f, -1f);
            UiKit.AddToolButton(answers, "Reply", 60f).onClick.AddListener(delegate { desktop.ShowToast("Imps can not write back"); });
            UiKit.AddToolButton(answers, "Forward", 66f).onClick.AddListener(delegate { desktop.ShowToast("Nobody down here wants it"); });
        }

        private void DeleteSelected()
        {
            Deleted.Add(selected);
            selected = -1;
            RebuildAll();
            RaiseTitleChanged();
        }

        private void RestoreSelected()
        {
            Deleted.Remove(selected);
            selected = -1;
            RebuildAll();
            RaiseTitleChanged();
        }

        private void MarkUnread()
        {
            Read.Remove(selected);
            selected = -1;
            RebuildAll();
            RaiseTitleChanged();
        }

        public override void OnLookChanged(ComputerLook look)
        {
            RebuildAll();
        }

        // The list is cut again when the window gets another width
        public override void Tick()
        {
            float width = main.rect.width;
            if (Mathf.Abs(width - shownWidth) < 1f)
                return;

            shownWidth = width;
            if (selected < 0)
                RebuildMain();
        }
    }
}
