using System;
using System.Collections.Generic;
using Computer;
using UnityEngine;
using UnityEngine.UI;

namespace ComputerUI
{
    // The websites of the Hellnet. Every page is made with code, from the same few parts a real site has:
    // a header with links, a big banner, cards in a row, text and a footer.
    public static class BrowserPages
    {
        public const string HomeAddress = "hoogle.hell";

        private static readonly Color Cream = new Color(1f, 0.96f, 0.82f);
        private static readonly Color LinkColor = new Color32(0x1a, 0x4f, 0x9a, 255);
        private static readonly Color UrlColor = new Color32(0x1f, 0x6b, 0x2f, 255);
        private static readonly Color MutedColor = new Color32(0x5a, 0x52, 0x5c, 255);

        // What Hoogle knows. The sites that are not here can only be reached by typing their address.
        private class Entry
        {
            public string title;
            public string address;
            public string snippet;

            public Entry(string title, string address, string snippet)
            {
                this.title = title;
                this.address = address;
                this.snippet = snippet;
            }
        }

        private static readonly Entry[] Index =
        {
            new Entry("Imp Handlers Guild - Official Site", "guild.hell", "The official home of every imp handler in the pit. Rules, news and who to call when it leaks."),
            new Entry("Rules of the Guild", "guild.hell/rules", "Rule one: the imp does what the work order says, not what you meant. Rule two: scan before you trust a label."),
            new Entry("The Brimstone Gazette - Guild news", "guild.hell/news", "Depot clerk sends wagons back. Hellhound sleeps after a very large dinner. Ash forecast inside."),
            new Entry("Contact the Guild", "guild.hell/contact", "Lost an imp? Found a tail? Write to the Guild. Messages are placed in the Oubliette."),
            new Entry("About Hoogle", "hoogle.hell/about", "Hoogle finds everything in the pit, except what you were looking for.")
        };

        // ---------- Addresses ----------

        // What the player typed becomes an address. Words without a dot become a search.
        public static string Normalize(string text)
        {
            string address = text.Trim();
            foreach (string scheme in new[] { "https://", "http://", "hell://" })
            {
                if (address.ToLowerInvariant().StartsWith(scheme))
                    address = address.Substring(scheme.Length);
            }

            if (address.Length == 0)
                return HomeAddress;
            if (address.Contains(" ") || !address.Contains("."))
                return SearchAddress(address);

            return address.TrimEnd('/').ToLowerInvariant();
        }

        public static string SearchAddress(string query)
        {
            return HomeAddress + "/search?q=" + query.Trim();
        }

        private static string SearchQuery(string address)
        {
            int start = address.IndexOf("?q=");
            return start < 0 ? "" : address.Substring(start + 3);
        }

        // ---------- Showing a page ----------

        // Puts the page of the address on the screen and gives back the title for the tab
        public static string Render(string address, WebPage page)
        {
            if (address == HomeAddress)
            {
                Home(page);
                return "Hoogle";
            }
            if (address.StartsWith(HomeAddress + "/search"))
            {
                string query = SearchQuery(address);
                Results(page, query);
                return query + " - Hoogle Search";
            }
            if (address == HomeAddress + "/about")
            {
                About(page);
                return "About Hoogle";
            }

            switch (address)
            {
                case "guild.hell": GuildHome(page); return "Imp Handlers Guild";
                case "guild.hell/rules": GuildRules(page); return "Rules - Imp Handlers Guild";
                case "guild.hell/news": GuildNews(page); return "News - Imp Handlers Guild";
                case "guild.hell/contact": GuildContact(page); return "Contact - Imp Handlers Guild";
                case "guild.hell/sent": GuildSent(page); return "Message sent - Imp Handlers Guild";
                case "depot.hell": Depot(page); return "Depot Notice Board";
                case "hound.hell": Kennel(page); return "The Kennel";
            }

            NotFound(page, address);
            return "Page not found";
        }

        // ---------- Hoogle ----------

        private static string Logo()
        {
            string[] colors = { "#d23b3b", "#e8b44a", "#2f9a4a", "#3f5fc8", "#e8b44a", "#d23b3b" };
            string letters = "Hoogle";
            string text = "";
            for (int i = 0; i < letters.Length; i++)
                text += "<color=" + colors[i] + ">" + letters[i] + "</color>";
            return text;
        }

        private static void Home(WebPage page)
        {
            page.Space(34f);

            RectTransform center = page.Center();
            Text logo = UiKit.AddText(center, "Logo", Logo(), TextRole.Title, 46, Palette.Ink, TextAnchor.MiddleCenter);
            logo.horizontalOverflow = HorizontalWrapMode.Overflow;
            UiKit.Size(logo.gameObject, 300f, 62f, -1f, -1f);

            Text tagline = UiKit.AddText(center, "Tagline", "Search the whole pit", TextRole.Body, 12, MutedColor, TextAnchor.MiddleCenter);
            UiKit.Size(tagline.gameObject, 300f, 22f, -1f, -1f);

            InputField field = page.SearchBox(center, 300f, "");
            RectTransform buttons = UiKit.NewRect(center, "Buttons");
            UiKit.MakeRow(buttons.gameObject, 0, 8, 0, 0, 8f, TextAnchor.MiddleCenter);
            UiKit.Size(buttons.gameObject, 300f, 34f, -1f, -1f);
            UiKit.AddToolButton(buttons, "Hoogle Search", 100f).onClick.AddListener(delegate { page.Go(SearchAddress(field.text)); });
            UiKit.AddToolButton(buttons, "I'm Feeling Cursed", 120f).onClick.AddListener(delegate
            {
                page.Go(Index[UnityEngine.Random.Range(0, Index.Length)].address);
            });

            Text offered = UiKit.AddText(center, "Offered", "Hoogle is also offered in Imp, Demon and Common", TextRole.Body, 10, MutedColor, TextAnchor.MiddleCenter);
            UiKit.Size(offered.gameObject, 360f, 40f, -1f, -1f);

            page.Footer("Hoogle - 2026 - About, Privacy (there is none), Terms (we own your soul)");
        }

        private static void Results(WebPage page, string query)
        {
            // The top bar of the results: a small logo and the search box again
            Image bar = UiKit.AddImage(page.Root, "Bar", new Color(0.96f, 0.95f, 0.93f));
            UiKit.MakeRow(bar.gameObject, 14, 6, 14, 6, 12f, TextAnchor.MiddleLeft);
            UiKit.Size(bar.gameObject, -1f, 44f, -1f, -1f);
            Text small = UiKit.AddText(bar.transform, "Logo", Logo(), TextRole.Title, 22, Palette.Ink, TextAnchor.MiddleLeft);
            small.horizontalOverflow = HorizontalWrapMode.Overflow;
            page.SearchBox(bar.transform, -1f, query);

            // The words of the search are looked for in the titles and the texts
            string[] words = query.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            List<Entry> found = new List<Entry>();
            List<int> scores = new List<int>();
            foreach (Entry entry in Index)
            {
                string text = (entry.title + " " + entry.snippet + " " + entry.address).ToLowerInvariant();
                int score = 0;
                foreach (string word in words)
                {
                    if (text.Contains(word))
                        score++;
                }
                if (score == 0)
                    continue;

                int place = 0;
                while (place < scores.Count && scores[place] >= score)
                    place++;
                found.Insert(place, entry);
                scores.Insert(place, score);
            }

            RectTransform count = page.Block();
            Text about = UiKit.AddText(count, "Count", found.Count == 0 ? "" : "About " + found.Count + " results (0.00 seconds, the imps were fast)", TextRole.Body, 10, MutedColor, TextAnchor.MiddleLeft);
            UiKit.Size(about.gameObject, -1f, 18f, -1f, -1f);

            if (found.Count == 0)
            {
                RectTransform none = page.Block();
                UiKit.AddText(none, "None", "No results for \"" + query + "\".", TextRole.Title, 15, Palette.Ink, TextAnchor.UpperLeft);
                UiKit.AddText(none, "Hint", "Not even the oubliette has what you are looking for. Try other words, or type the address of a site yourself.", TextRole.Body, 12, MutedColor, TextAnchor.UpperLeft);
            }

            foreach (Entry entry in found)
            {
                RectTransform result = page.Block();
                UiKit.AddText(result, "Address", entry.address, TextRole.Body, 10, UrlColor, TextAnchor.UpperLeft);
                Button title = UiKit.AddButton(result, "Title", entry.title, SkinRole.Fill, new Color(1f, 1f, 1f, 0f), LinkColor, 15);
                title.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleLeft;
                UiKit.Size(title.gameObject, -1f, 22f, -1f, -1f);
                string target = entry.address;
                title.onClick.AddListener(delegate { page.Go(target); });
                UiKit.AddText(result, "Snippet", entry.snippet, TextRole.Body, 11, Palette.Ink, TextAnchor.UpperLeft);
            }

            page.Space(8f);
            page.Footer("Hoogle - 2026");
        }

        private static void About(WebPage page)
        {
            page.Header("Hoogle", new Color32(0xf4, 0xf0, 0xe8, 255), LinkColor, new[] { "Search|hoogle.hell" });
            page.Heading("About Hoogle");
            page.Paragraph("Hoogle is the biggest search engine in the pit. It knows every page on the Hellnet, apart from the ones it was told to forget.");
            page.Paragraph("Our imps read every site, every day. Some sites are never found. If you know the address of one, you can type it in the bar at the top of the window.");
            page.Footer("Hoogle - 2026");
        }

        // ---------- The Imp Handlers Guild ----------

        private static readonly Color GuildColor = new Color32(0x5a, 0x0f, 0x16, 255);

        private static void GuildHeader(WebPage page)
        {
            page.Header("IMP HANDLERS GUILD", GuildColor, Palette.Gold, new[] { "Home|guild.hell", "Rules|guild.hell/rules", "News|guild.hell/news", "Contact|guild.hell/contact" });
        }

        private static void GuildFooter(WebPage page)
        {
            page.Footer("(c) The Imp Handlers Guild. Not responsible for sticky tails.");
        }

        private static void GuildHome(WebPage page)
        {
            GuildHeader(page);
            page.Hero("Every imp deserves a handler who reads the label.", "The Guild has trained handlers in the pit for a thousand years. Join us and learn to send an imp to do your work, and to come back with the right crate.", "Read the rules", "guild.hell/rules", new Color32(0x7a, 0x14, 0x1e, 255));
            page.Heading("Why join the Guild?");
            page.Cards(new[]
            {
                "Scan first|A crate is only what it says on the tin until you scan it.|guild.hell/rules",
                "Seal your orders|Write it, seal it, walk away. The imp takes it from there.|guild.hell/rules",
                "Mind the hound|A fed hound is a quiet hound. Check what is in the sack.|guild.hell/news"
            });
            page.Heading("Trusted by thousands");
            page.Paragraph("\"I lost my imp and the Guild found him within the hour. He was in the oubliette. They all are.\" - a satisfied handler");
            GuildFooter(page);
        }

        private static void GuildRules(WebPage page)
        {
            GuildHeader(page);
            page.Hero("The Rules", "Four rules. Break one and the imp will remember.", null, null, new Color32(0x7a, 0x14, 0x1e, 255));
            page.Cards(new[]
            {
                "1. The word of the order|The imp does what the work order says, not what you meant.|",
                "2. Scan before you trust|A label is only a label. Look at what is really inside.|",
            });
            page.Cards(new[]
            {
                "3. Leaks are problems|A crate that leaks is not a crate. Do not give it to the imp.|",
                "4. Never argue|Not with the hound, not with the imp, not with the Demon Lord.|"
            });
            GuildFooter(page);
        }

        private static void GuildNews(WebPage page)
        {
            GuildHeader(page);
            page.Heading("The Brimstone Gazette");
            page.Article("Depot clerk sends three wagons back, still angry", "Day 1", "The quartermaster said it was the third time this week. The wagons were not available for comment.");
            page.Article("Hellhound sleeps through the night after a very large dinner", "Day 2", "Handlers say it was a perfect dinner: meat, bread, and not a single turnip.");
            page.Article("The Oubliette is full again", "Day 3", "The Demon Lord is pleased. Please do not ask what is in there.");
            page.Article("Weather: ash, with a chance of ash", "Day 4", "A short pause in the afternoon is expected, followed by ash.");
            GuildFooter(page);
        }

        private static void GuildContact(WebPage page)
        {
            GuildHeader(page);
            page.Heading("Contact the Guild");
            page.Paragraph("Lost an imp? Found a tail? Write to us. Every message is read, and then placed in the Oubliette.");

            RectTransform form = page.Block();
            UiKit.AddText(form, "NameLabel", "Your name", TextRole.Body, 11, MutedColor, TextAnchor.UpperLeft);
            page.Field(form, "Handler number 000");
            UiKit.AddText(form, "MessageLabel", "Your message", TextRole.Body, 11, MutedColor, TextAnchor.UpperLeft);
            page.Field(form, "My imp is...");

            RectTransform row = UiKit.NewRect(form, "Send");
            UiKit.MakeRow(row.gameObject, 0, 6, 0, 0, 0f, TextAnchor.MiddleLeft);
            UiKit.Size(row.gameObject, -1f, 32f, -1f, -1f);
            UiKit.AddSealButton(row, "Send message", 110f).onClick.AddListener(delegate { page.Go("guild.hell/sent"); });

            GuildFooter(page);
        }

        private static void GuildSent(WebPage page)
        {
            GuildHeader(page);
            page.Hero("Message sent", "Your message has been placed in the Oubliette. Nothing ever comes back from there, so please do not wait for an answer.", "Back to the Guild", "guild.hell", new Color32(0x2f, 0x6b, 0x4a, 255));
            GuildFooter(page);
        }

        // ---------- Sites that Hoogle does not know ----------

        private static void Depot(WebPage page)
        {
            Color color = new Color32(0x6b, 0x44, 0x12, 255);
            page.Header("DEPOT NOTICE BOARD", color, Cream, new string[0]);
            page.Hero("Notices for all handlers", "Read before you load anything. The quartermaster will know if you did not.", null, null, new Color32(0x9a, 0x6a, 0x1e, 255));
            page.Cards(new[]
            {
                "Wagons|Three wagons wait in the yard. Each carries a different load, so look at the numbers before you fill one.|",
                "Lamp oil|A barrel that leaks must not be near the torches. Check it before it goes on a wagon.|",
                "Grain|Rocks are not grain, even when the crate says so. Some crates will spoil before the camp sees them.|"
            });
            page.Footer("Depot Services. Please do not feed the quartermaster.");
        }

        private static void Kennel(WebPage page)
        {
            Color color = new Color32(0x2a, 0x1a, 0x3a, 255);
            page.Header("THE KENNEL", color, Palette.Gold, new string[0]);
            page.Hero("Feed me.", "The hound is very hungry. It will eat meat and bread. It does not like turnips. It will tell you.", null, null, new Color32(0x5a, 0x2a, 0x7a, 255));
            page.Cards(new[]
            {
                "Meat|Always welcome.|",
                "Bread|Welcome, if it is not stale.|",
                "Turnips|No.|"
            });
            page.Footer("The Kennel. Visitors are fed first.");
        }

        // ---------- A page that is not there ----------

        private static void NotFound(WebPage page, string address)
        {
            page.Space(46f);
            RectTransform center = page.Center();
            Text sad = UiKit.AddText(center, "Title", "This page can't be reached", TextRole.Title, 22, Palette.Ink, TextAnchor.MiddleCenter);
            UiKit.Size(sad.gameObject, 380f, 40f, -1f, -1f);
            Text what = UiKit.AddText(center, "Text", address + " fell into the oubliette. Nothing ever comes back from there.", TextRole.Body, 12, MutedColor, TextAnchor.UpperCenter);
            UiKit.Size(what.gameObject, 380f, 40f, -1f, -1f);
            Text code = UiKit.AddText(center, "Code", "ERR_PAGE_IN_OUBLIETTE", TextRole.Code, 11, MutedColor, TextAnchor.UpperCenter);
            UiKit.Size(code.gameObject, 380f, 24f, -1f, -1f);
            RectTransform row = UiKit.NewRect(center, "Buttons");
            UiKit.MakeRow(row.gameObject, 0, 6, 0, 0, 6f, TextAnchor.MiddleCenter);
            UiKit.Size(row.gameObject, 380f, 34f, -1f, -1f);
            UiKit.AddToolButton(row, "Go to Hoogle", 100f).onClick.AddListener(delegate { page.Go(HomeAddress); });
        }
    }

    // The parts a page is made of. A page puts them one under the other in the scroll area of the browser.
    public class WebPage
    {
        private static readonly Color LinkColor = new Color32(0x1a, 0x4f, 0x9a, 255);
        private static readonly Color MutedColor = new Color32(0x5a, 0x52, 0x5c, 255);
        private static readonly Color Cream = new Color(1f, 0.96f, 0.82f);

        public readonly RectTransform Root;
        private readonly Action<string> go;

        public WebPage(RectTransform root, Action<string> go)
        {
            Root = root;
            this.go = go;
        }

        public void Go(string address)
        {
            go(address);
        }

        public void Space(float height)
        {
            UiKit.Size(UiKit.NewRect(Root, "Space").gameObject, -1f, height, -1f, -1f);
        }

        // A block with room at the sides, for text
        public RectTransform Block()
        {
            RectTransform block = UiKit.NewRect(Root, "Block");
            UiKit.MakeColumn(block.gameObject, 26, 4, 26, 4, 3f);
            return block;
        }

        // A column in the middle of the page, whose children have the size they ask for
        public RectTransform Center()
        {
            RectTransform column = UiKit.NewRect(Root, "Center");
            VerticalLayoutGroup layout = UiKit.MakeColumn(column.gameObject, 0, 0, 0, 0, 4f);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childForceExpandWidth = false;
            return column;
        }

        // The band on top with the name of the site and its links
        public void Header(string name, Color color, Color nameColor, string[] links)
        {
            Image band = UiKit.AddImage(Root, "Header", color);
            UiKit.MakeRow(band.gameObject, 16, 0, 10, 0, 2f, TextAnchor.MiddleLeft);
            UiKit.Size(band.gameObject, -1f, 38f, -1f, -1f);

            Text logo = UiKit.AddText(band.transform, "Name", name, TextRole.Title, 16, nameColor, TextAnchor.MiddleLeft);
            logo.horizontalOverflow = HorizontalWrapMode.Overflow;
            UiKit.Size(logo.gameObject, -1f, -1f, 1f, -1f);

            foreach (string link in links)
            {
                string[] parts = link.Split('|');
                string target = parts[1];
                Button button = UiKit.AddButton(band.transform, "Link", parts[0], SkinRole.Fill, new Color(1f, 1f, 1f, 0f), Cream, 11);
                UiKit.Size(button.gameObject, parts[0].Length * 7f + 20f, 26f, -1f, -1f);
                button.onClick.AddListener(delegate { go(target); });
            }
        }

        // The big banner under the header
        public void Hero(string title, string text, string buttonText, string target, Color color)
        {
            Image band = UiKit.AddImage(Root, "Hero", color);
            UiKit.MakeColumn(band.gameObject, 26, 24, 26, 24, 8f);

            UiKit.AddText(band.transform, "Title", title, TextRole.Title, 24, Cream, TextAnchor.UpperLeft);
            UiKit.AddText(band.transform, "Text", text, TextRole.Body, 12, new Color(0.95f, 0.9f, 0.82f), TextAnchor.UpperLeft);

            if (buttonText != null)
            {
                RectTransform row = UiKit.NewRect(band.transform, "Button");
                UiKit.MakeRow(row.gameObject, 0, 4, 0, 0, 0f, TextAnchor.MiddleLeft);
                UiKit.Size(row.gameObject, -1f, 32f, -1f, -1f);
                UiKit.AddSealButton(row, buttonText, buttonText.Length * 7f + 30f).onClick.AddListener(delegate { go(target); });
            }
        }

        public void Heading(string text)
        {
            RectTransform block = Block();
            Text heading = UiKit.AddText(block, "Heading", text, TextRole.Title, 18, Palette.Blood, TextAnchor.LowerLeft);
            UiKit.Size(heading.gameObject, -1f, 32f, -1f, -1f);
        }

        public void Paragraph(string text)
        {
            RectTransform block = Block();
            UiKit.AddText(block, "Text", text, TextRole.Body, 12, Palette.Ink, TextAnchor.UpperLeft);
        }

        // A news item: its title, the day, and a short text
        public void Article(string title, string day, string text)
        {
            Image card = UiKit.AddImage(Root, "Article", new Color(0.97f, 0.94f, 0.9f));
            UiKit.MakeColumn(card.gameObject, 26, 8, 26, 8, 2f);
            UiKit.AddText(card.transform, "Day", day.ToUpper(), TextRole.Body, 10, Palette.Blood, TextAnchor.UpperLeft);
            UiKit.AddText(card.transform, "Title", title, TextRole.Title, 14, Palette.Ink, TextAnchor.UpperLeft);
            UiKit.AddText(card.transform, "Text", text, TextRole.Body, 11, MutedColor, TextAnchor.UpperLeft);
            Space(4f);
        }

        // Cards in a row. A card is "title|text|address", and the address can be empty.
        public void Cards(string[] cards)
        {
            RectTransform row = UiKit.NewRect(Root, "Cards");
            HorizontalLayoutGroup layout = UiKit.MakeRow(row.gameObject, 26, 8, 26, 8, 10f, TextAnchor.UpperLeft);
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            foreach (string card in cards)
            {
                string[] parts = card.Split('|');
                Image box = UiKit.AddImage(row, "Card", new Color(0.96f, 0.93f, 0.88f));
                UiKit.MakeColumn(box.gameObject, 12, 10, 12, 10, 5f);
                UiKit.Size(box.gameObject, 10f, -1f, 1f, -1f);

                Image stripe = UiKit.AddPicture(box.transform, "Stripe", Palette.Blood);
                UiKit.Size(stripe.gameObject, -1f, 3f, -1f, -1f);
                UiKit.AddText(box.transform, "Title", parts[0], TextRole.Title, 14, Palette.Blood, TextAnchor.UpperLeft);
                UiKit.AddText(box.transform, "Text", parts[1], TextRole.Body, 11, Palette.Ink, TextAnchor.UpperLeft);

                if (parts.Length > 2 && parts[2].Length > 0)
                {
                    string target = parts[2];
                    Button more = UiKit.AddButton(box.transform, "More", "Read more >", SkinRole.Fill, new Color(1f, 1f, 1f, 0f), LinkColor, 11);
                    more.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleLeft;
                    UiKit.Size(more.gameObject, -1f, 18f, -1f, -1f);
                    more.onClick.AddListener(delegate { go(target); });
                }
            }
        }

        // The dark band at the bottom of a page
        public void Footer(string text)
        {
            Space(14f);
            Image band = UiKit.AddImage(Root, "Footer", new Color32(0x1f, 0x16, 0x20, 255));
            UiKit.MakeColumn(band.gameObject, 26, 14, 26, 14, 4f);
            UiKit.AddText(band.transform, "Text", text, TextRole.Body, 10, new Color(0.7f, 0.66f, 0.74f), TextAnchor.UpperLeft);
        }

        // A box to type in, with a thin edge
        public InputField Field(RectTransform parent, string hint)
        {
            Image edge = UiKit.AddImage(parent, "Edge", new Color(0.55f, 0.5f, 0.55f));
            UiKit.MakeColumn(edge.gameObject, 1, 1, 1, 1, 0f);
            UiKit.Size(edge.gameObject, -1f, 26f, -1f, -1f);

            InputField field = UiKit.AddInputField(edge.transform, "Field", false, Color.white, Palette.Ink, TextRole.Body, 12);
            UiKit.Size(field.gameObject, -1f, 24f, 1f, -1f);
            return field;
        }

        // The search box of Hoogle. A width below zero makes it as wide as the room it has.
        public InputField SearchBox(Transform parent, float width, string text)
        {
            Image edge = UiKit.AddImage(parent, "SearchEdge", new Color(0.55f, 0.5f, 0.55f));
            UiKit.MakeColumn(edge.gameObject, 1, 1, 1, 1, 0f);
            UiKit.Size(edge.gameObject, width, 28f, width < 0f ? 1f : -1f, -1f);

            InputField field = UiKit.AddInputField(edge.transform, "Search", false, Color.white, Palette.Ink, TextRole.Body, 13);
            UiKit.Size(field.gameObject, -1f, 26f, 1f, -1f);
            field.text = text;
            field.onEndEdit.AddListener(delegate(string typed)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                    go(BrowserPages.SearchAddress(typed));
            });
            return field;
        }
    }
}
