namespace Computer
{
    // The files the computer starts with: what the previous operator, Orlo, left behind.
    // He never worked out how the machine works, so he touched very little. Change the text here to change the lore.
    public static class DefaultFiles
    {
        public static FileSystem Create()
        {
            FileSystem files = new FileSystem();
            FileNode desktop = files.Desktop;

            files.CreateText(desktop, "README.txt",
                "If you are reading this, you are the new operator.\n" +
                "I am sorry.\n" +
                "\n" +
                "The Lord is not a bad employer. He is a loud one.\n" +
                "\n" +
                "Things I learned:\n" +
                " - Scan before you code. The runes do not lie. Labels do.\n" +
                " - The imp is more helpful than he looks.\n" +
                " - Do not ask about the printer.\n" +
                "\n" +
                "- Orlo, former operator");

            files.CreateText(desktop, "todo.txt",
                "TODO\n" +
                "[x] Arrive\n" +
                "[x] Scream\n" +
                "[ ] Work out how the scanner works\n" +
                "[ ] Work out how the imp works\n" +
                "[ ] Finish the Lord's work order (due: last Friday)\n" +
                "[ ] Stop hearing the hum");

            files.CreateText(desktop, "notes_to_self.txt",
                "Things the imp does NOT understand:\n" +
                " - loops (I asked him to repeat himself. He did. Once.)\n" +
                " - foreach (he looked offended)\n" +
                " - being asked politely\n" +
                "\n" +
                "Things that worked:\n" +
                " - imp.Say(\"hello\");\n" +
                "   He said it. Out loud. I nearly cried.");

            files.CreateText(desktop, "passwords.txt",
                "lord_login: ilovekingdoms\n" +
                "router: 666666\n" +
                "wifi: hellfire-guest / password\n" +
                "\n" +
                "Do not tell the Lord I wrote these down.");

            // A folder of old work
            FileNode oldStuff = files.CreateFolder(desktop, "Old stuff");
            files.CreateText(oldStuff, "work_order_DRAFT.txt",
                "// TODO load the grain\n" +
                "// the grain is the yellow ones\n" +
                "// (it was not the yellow ones)\n" +
                "imp.Say(\"help\");");
            files.CreateText(oldStuff, "backup_FINAL_v2.txt",
                "Same as backup_FINAL.txt,\n" +
                "which is the same as backup.txt,\n" +
                "which is empty.");
            files.CreateText(oldStuff, "i_was_here.txt",
                "Day 1: arrived.\n" +
                "Day 3: still here.\n" +
                "Day 11: the imp has started to hum.\n" +
                "Day 14: I have stopped counting.\n" +
                "\n" +
                "If you find a crate that rattles, leave it alone.");
            FileNode screenshots = files.CreateFolder(oldStuff, "Screenshots");
            files.CreateText(screenshots, "screenshot_001.txt",
                "[This image cannot be shown.\n" +
                " Orlo took pictures of the screen with a quill.]");

            // A folder about the boss
            FileNode lord = files.CreateFolder(desktop, "Demon Lord");
            files.CreateText(lord, "complaints.txt",
                "Complaints received by the Lord:\n" +
                "1. Too slow\n" +
                "2. Too loud\n" +
                "3. Why is the imp humming\n" +
                "4. The printer (there is no printer)");
            files.CreateText(lord, "kingdom_plan_v7.txt",
                "KINGDOM PLAN v7\n" +
                "Step 1: feed the army\n" +
                "Step 2: ???\n" +
                "Step 3: crown\n" +
                "\n" +
                "Note from Orlo: step 2 is where I come in.\n" +
                "Was.");

            // Things already in the oubliette
            FileNode resignation = files.CreateText(files.Trash, "resignation.txt",
                "Dear Lord, I quit.\n" +
                "- Orlo\n" +
                "\n" +
                "(He read it. He laughed.)");
            resignation.CanRestore = false;
            files.CreateText(files.Trash, "my_old_life.txt", "A photo of a sandwich. It was a good sandwich.");
            files.CreateText(files.Trash, "meeting_notes_FINAL_final2.txt", "Meeting notes:\nThere was a meeting.");

            return files;
        }
    }
}
