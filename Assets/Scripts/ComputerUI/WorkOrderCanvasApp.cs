using System.Collections;
using System.Collections.Generic;
using CodeRunner;
using CodeRunner.Interpreting;
using Computer;
using Game;
using UnityEngine;
using UnityEngine.UI;

namespace ComputerUI
{
    // The program where the player writes code and runs it. It has four tabs:
    // the work order itself, the run log, the console and the list of scanned objects.
    public class WorkOrderCanvasApp : CanvasApp
    {
        private enum Tab
        {
            WorkOrder,
            RunLog,
            Console,
            Scanned
        }

        private const string StartingCode =
            "// Work order\n" +
            "// Scan things (E, or the scrying glass),\n" +
            "// then use their names here.\n" +
            "// F5 runs all, F6 the selected lines.\n" +
            "\n" +
            "imp.Say(\"Ready for work!\");\n";

        private static readonly Color HintColor = new Color32(0x6b, 0x52, 0x30, 255);

        private static string savedCode;

        private readonly List<CodeError> problems = new List<CodeError>();
        private readonly List<int> runtimeErrorLines = new List<int>();

        private DesktopView desktop;
        private CodeEditorView editor;
        private RunLogPane logPane;
        private ConsolePane consolePane;
        private ScannedPane scannedPane;

        private readonly GameObject[] panes = new GameObject[4];
        private readonly Image[] tabBackgrounds = new Image[4];
        private readonly Text[] tabLabels = new Text[4];
        private readonly LayoutElement[] tabSizes = new LayoutElement[4];
        private readonly Image[] tabBadges = new Image[4];
        private readonly Text[] tabBadgeTexts = new Text[4];
        private Button runButton;
        private Button stopButton;
        private Text statusLabel;
        private Text leftStatus;
        private Text positionLabel;
        private Text waxLabel;
        private Button waxFixButton;
        private Image waxLine;
        private CodeError waxError;     // the problem the wax bar shows, which its button fixes

        // With run on exit, sealing the work order only queues it. It runs when the player leaves the computer
        // and can see the world, which matters when the scrying glass is not there yet.
        private bool runOnExit;
        private bool queued;
        private string queuedSource;
        private int queuedFirst;
        private int queuedLast;
        private Button exitToggle;

        private Tab currentTab;
        private bool running;
        private Coroutine runCoroutine;
        private bool problemsDirty = true;
        private float problemTimer;
        private int lastScannedCount = -1;

        private TestSceneSetup Setup
        {
            get { return TestSceneSetup.Instance; }
        }

        // runOnExit is how the toggle starts, for example on in a mission without the scrying glass
        public WorkOrderCanvasApp(bool runOnExit)
        {
            this.runOnExit = runOnExit;
        }

        public override string Name
        {
            get { return "Work order"; }
        }

        public override string IconName
        {
            get { return "WorkOrder"; }
        }

        public override Vector2 WindowSize
        {
            get { return new Vector2(500f, 380f); }
        }

        public override Vector2 MinSize
        {
            get { return new Vector2(380f, 300f); }
        }

        // ---------- Building the window ----------

        public override void Build(DesktopView owner, RectTransform body)
        {
            desktop = owner;
            ComputerLook look = owner.Look;

            // From top to bottom: the tabs, the toolbar, the pane of the selected tab and the status bar
            RectTransform root = UiKit.NewRect(body, "WorkOrder");
            UiKit.Stretch(root);
            UiKit.MakeColumn(root.gameObject, 0, 0, 0, 0, 0f);

            BuildTabs(root);
            BuildToolbar(root);

            RectTransform area = UiKit.NewRect(root, "Area");
            UiKit.Size(area.gameObject, -1f, -1f, 1f, 1f);
            UiKit.Stretch(area);
            UiKit.MakeColumn(area.gameObject, 0, 2, 0, 0, 0f);

            panes[(int)Tab.WorkOrder] = BuildEditorPane(area, look);

            logPane = new RunLogPane(area, look);
            logPane.FixRequested += ApplyFix;
            panes[(int)Tab.RunLog] = logPane.Root;

            consolePane = new ConsolePane(area, look);
            panes[(int)Tab.Console] = consolePane.Root;

            scannedPane = new ScannedPane(area, look);
            scannedPane.InsertRequested += InsertName;
            panes[(int)Tab.Scanned] = scannedPane.Root;

            foreach (GameObject pane in panes)
                UiKit.Size(pane, -1f, -1f, 1f, 1f);

            BuildStatusBar(root);

            editor.Text = savedCode != null ? savedCode : StartingCode;
            SelectTab(Tab.WorkOrder);
        }

        private void BuildTabs(RectTransform root)
        {
            RectTransform strip = UiKit.NewRect(root, "Tabs");
            UiKit.MakeRow(strip.gameObject, 4, 0, 0, 0, 2f, TextAnchor.LowerLeft);
            UiKit.Size(strip.gameObject, -1f, 21f, -1f, -1f);

            string[] names = { "Work order", "Run log", "Console", "Scanned" };
            for (int i = 0; i < names.Length; i++)
            {
                Tab tab = (Tab)i;

                // A tab is a button with its name and a small badge with a number
                Image background = UiKit.AddSkin(strip, names[i] + "Tab", SkinRole.Tab);
                background.raycastTarget = true;
                Button button = background.gameObject.AddComponent<Button>();
                button.targetGraphic = background;
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(delegate { SelectTab(tab); });
                UiKit.MakeRow(background.gameObject, 8, 0, 8, 0, 5f, TextAnchor.MiddleLeft);
                tabSizes[i] = UiKit.Size(background.gameObject, -1f, 18f, -1f, -1f);

                Text label = UiKit.AddText(background.transform, "Label", names[i], TextRole.Body, 11, Palette.Parchment, TextAnchor.MiddleLeft);
                label.horizontalOverflow = HorizontalWrapMode.Overflow;

                Image badge = UiKit.AddPicture(background.transform, "Badge", new Color(0f, 0f, 0f, 0.35f));
                UiKit.MakeRow(badge.gameObject, 4, 0, 4, 0, 0f, TextAnchor.MiddleCenter);
                Text badgeText = UiKit.AddText(badge.transform, "Number", "", TextRole.Body, 10, Palette.Parchment, TextAnchor.MiddleCenter);
                badgeText.horizontalOverflow = HorizontalWrapMode.Overflow;
                badge.gameObject.SetActive(false);

                tabBackgrounds[i] = background;
                tabLabels[i] = label;
                tabBadges[i] = badge;
                tabBadgeTexts[i] = badgeText;
            }
        }

        private void BuildToolbar(RectTransform root)
        {
            Image bar = UiKit.AddSkin(root, "Toolbar", SkinRole.Bar);
            UiKit.MakeRow(bar.gameObject, 6, 4, 6, 4, 6f, TextAnchor.MiddleLeft);
            UiKit.Size(bar.gameObject, -1f, 26f, -1f, -1f);

            runButton = UiKit.AddSealButton(bar.transform, "Seal and run  F5", 104f);
            runButton.onClick.AddListener(RunAll);

            UiKit.AddToolButton(bar.transform, "Run selection  F6", 104f).onClick.AddListener(RunSelection);

            stopButton = UiKit.AddToolButton(bar.transform, "Stop", 36f);
            stopButton.onClick.AddListener(Stop);

            exitToggle = UiKit.AddToolButton(bar.transform, "[ ] Run on exit", 92f);
            exitToggle.onClick.AddListener(ToggleRunOnExit);
            UpdateExitToggle();

            statusLabel = UiKit.AddText(bar.transform, "Status", "", TextRole.Body, 11, UiKit.GoldDark, TextAnchor.MiddleRight);
            UiKit.Size(statusLabel.gameObject, -1f, -1f, 1f, -1f);
        }

        private GameObject BuildEditorPane(RectTransform area, ComputerLook look)
        {
            RectTransform pane;
            RectTransform paneRoot = UiKit.AddPage(area, "WorkOrderPane", out pane);
            UiKit.MakeColumn(pane.gameObject, 0, 0, 0, 0, 0f);
            UiKit.AddRoller(pane.transform);

            editor = CodeEditorView.Create((RectTransform)pane.transform);
            UiKit.Size(editor.gameObject, -1f, -1f, 1f, 1f);
            editor.IsHandle = delegate(string name) { return Setup.Handles.Contains(name); };
            editor.TextChanged += OnCodeChanged;
            editor.CaretMoved += OnCaretMoved;

            // The wax bar under the editor explains the problem on the cursor line
            Image waxBar = UiKit.AddSkin(pane.transform, "WaxBar", SkinRole.Bar);
            UiKit.MakeRow(waxBar.gameObject, 8, 5, 8, 3, 6f, TextAnchor.MiddleLeft);
            UiKit.Size(waxBar.gameObject, -1f, 24f, -1f, -1f);

            waxLine = UiKit.AddPicture(waxBar.transform, "Line", Palette.Blood);
            RectTransform lineRect = waxLine.rectTransform;
            lineRect.anchorMin = new Vector2(0f, 1f);
            lineRect.anchorMax = new Vector2(1f, 1f);
            lineRect.pivot = new Vector2(0.5f, 1f);
            lineRect.anchoredPosition = Vector2.zero;
            lineRect.sizeDelta = new Vector2(0f, 2f);
            waxLine.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

            waxLabel = UiKit.AddText(waxBar.transform, "Text", "", TextRole.Body, 12, new Color32(0x5a, 0x10, 0x18, 255), TextAnchor.MiddleLeft);
            UiKit.Size(waxLabel.gameObject, -1f, -1f, 1f, -1f);

            waxFixButton = UiKit.AddToolButton(waxBar.transform, "Use name", 90f);
            waxFixButton.onClick.AddListener(delegate { ApplyFix(waxError); });

            UiKit.AddRoller(pane.transform);
            return paneRoot.gameObject;
        }

        private void BuildStatusBar(RectTransform root)
        {
            Image bar = UiKit.AddSkin(root, "StatusBar", SkinRole.Panel);
            UiKit.MakeRow(bar.gameObject, 6, 1, 6, 1, 6f, TextAnchor.MiddleLeft);
            UiKit.Size(bar.gameObject, -1f, 16f, -1f, -1f);

            Color textColor = new Color32(0xc9, 0xb3, 0xa8, 255);
            leftStatus = UiKit.AddText(bar.transform, "Left", "", TextRole.Body, 11, textColor, TextAnchor.MiddleLeft);
            UiKit.Size(leftStatus.gameObject, -1f, -1f, 1f, -1f);

            positionLabel = UiKit.AddText(bar.transform, "Position", "Line 1, column 1", TextRole.Body, 11, textColor, TextAnchor.MiddleRight);
            UiKit.Size(positionLabel.gameObject, 100f, -1f, -1f, -1f);
        }

        // ---------- Tabs ----------

        private void SelectTab(Tab tab)
        {
            currentTab = tab;
            for (int i = 0; i < panes.Length; i++)
            {
                bool active = i == (int)tab;
                panes[i].SetActive(active);

                // The tab in front is taller and has the colour of the page
                tabBackgrounds[i].GetComponent<SkinImage>().SetRole(active ? SkinRole.TabActive : SkinRole.Tab);
                tabLabels[i].color = active ? (Color)Palette.Ink : new Color32(0xc9, 0xb3, 0xa8, 255);
                tabSizes[i].preferredHeight = active ? 21f : 18f;
            }

            if (tab == Tab.Console)
                consolePane.FocusInput();
        }

        private void SetBadge(Tab tab, string text, bool isError)
        {
            int index = (int)tab;
            if (tabBadgeTexts[index].text == text && tabBadges[index].color.r > 0.1f == isError)
                return;

            tabBadgeTexts[index].text = text;
            tabBadges[index].gameObject.SetActive(!string.IsNullOrEmpty(text));
            tabBadges[index].color = isError ? (Color)Palette.Blood : new Color(0f, 0f, 0f, 0.35f);
            tabBadgeTexts[index].color = isError ? Color.white : (Color)Palette.Parchment;
        }

        // ---------- App events ----------

        // The panes remember the look they were made with, so they are told about the new one
        public override void OnLookChanged(ComputerLook look)
        {
            if (logPane == null)
                return;

            logPane.SetLook(look);
            consolePane.SetLook(look);
            scannedPane.SetLook(look);
        }

        public override void OnShown()
        {
            if (editor == null)
                return;

            problemsDirty = true;
            problemTimer = 0f;
            editor.Recolor();
        }

        public override void Tick()
        {
            if (Input.GetKeyDown(KeyCode.F5))
                RunAll();
            else if (Input.GetKeyDown(KeyCode.F6))
                RunSelection();

            // The scope changes when the player scans something, which can fix or cause problems
            int scannedCount = Setup.ScannedNames.Count;
            if (scannedCount != lastScannedCount)
            {
                lastScannedCount = scannedCount;
                problemsDirty = true;
                problemTimer = 0f;
                editor.Recolor();
            }

            if (problemsDirty)
            {
                problemTimer -= Time.unscaledDeltaTime;
                if (problemTimer <= 0f)
                    RefreshProblems();
            }

            editor.SetRunningLine(Setup.Session.CurrentLine);
            logPane.Refresh(problems, Setup.Session.Log.Entries, running, queued);
            if (currentTab == Tab.RunLog)
                logPane.Tick();
            if (currentTab == Tab.Console)
                consolePane.Tick();
            if (currentTab == Tab.Scanned)
                scannedPane.Tick();

            UpdateStatus();
        }

        // ---------- Problems ----------

        private void OnCodeChanged()
        {
            // Kept so the code survives the window being made again
            savedCode = editor.Text;
            runtimeErrorLines.Clear();
            problemsDirty = true;
            problemTimer = 0.25f;
        }

        private void OnCaretMoved()
        {
            string text = editor.Text;
            int cursor = Mathf.Min(editor.CursorIndex, text.Length);
            int line = editor.LineOf(cursor);
            int lineStart = cursor > 0 ? text.LastIndexOf('\n', cursor - 1) + 1 : 0;
            positionLabel.text = "Line " + line + ", column " + (cursor - lineStart + 1);
            UpdateWaxBar();
        }

        private void RefreshProblems()
        {
            problemsDirty = false;
            problems.Clear();
            problems.AddRange(Setup.Session.FindErrors(editor.Text, RunMode.WorkOrder));
            editor.SetProblems(problems);

            int count = problems.Count;
            SetBadge(Tab.RunLog, count > 0 ? count.ToString() : "", true);
            UpdateWaxBar();
        }

        // The wax bar under the editor explains the problem on the cursor line, or the first problem
        private void UpdateWaxBar()
        {
            CodeError shown = null;
            int cursorLine = editor.LineOf(editor.CursorIndex);
            foreach (CodeError problem in problems)
            {
                if (problem.Line == cursorLine)
                {
                    shown = problem;
                    break;
                }
            }
            if (shown == null && problems.Count > 0)
                shown = problems[0];

            bool clean = shown == null;
            waxLine.color = clean ? new Color32(0xa9, 0x8a, 0x55, 255) : (Color)Palette.Blood;
            waxLabel.color = clean ? new Color32(0x5c, 0x6b, 0x2a, 255) : new Color32(0x5a, 0x10, 0x18, 255);
            if (clean)
            {
                waxLabel.text = "The code reads clean. Press F5 to seal and run it.";
                waxFixButton.gameObject.SetActive(false);
                return;
            }

            waxLabel.text = "Line " + shown.Line + ": " + QuickFix.MessageOf(shown);

            string fix = QuickFix.NameFor(shown);
            waxFixButton.gameObject.SetActive(fix != null);
            waxError = shown;
            if (fix != null)
                UiKit.SetLabel(waxFixButton, "Use " + fix);
        }

        private void ApplyFix(CodeError error)
        {
            string name = QuickFix.NameFor(error);
            if (name == null)
                return;

            editor.ReplaceWord(error.Line, error.Column, name);
            SelectTab(Tab.WorkOrder);
        }

        private void InsertName(string name)
        {
            SelectTab(Tab.WorkOrder);
            editor.FocusEditor();
            editor.InsertAtCursor(name);
        }

        // ---------- Running code ----------

        private void RunAll()
        {
            StartRun(1, int.MaxValue);
        }

        private void RunSelection()
        {
            string code = editor.Text;
            int start = Mathf.Clamp(editor.SelectionStart, 0, code.Length);
            int end = Mathf.Clamp(editor.SelectionEnd, 0, code.Length);

            // A selection that ends right after a line break does not include the next line
            if (end > start && code[end - 1] == '\n')
                end--;

            StartRun(editor.LineOf(start), editor.LineOf(end));
        }

        private void StartRun(int firstLine, int lastLine)
        {
            if (running)
                return;

            SelectTab(Tab.RunLog);
            string source = editor.Text;

            // Mistakes are shown right away even when the run waits, so the player can fix them first
            if (runOnExit && !HasErrors(source, firstLine, lastLine))
            {
                queued = true;
                queuedSource = source;
                queuedFirst = firstLine;
                queuedLast = lastLine;
                return;
            }

            queued = false;
            runCoroutine = desktop.Host.StartCoroutine(RunRoutine(source, firstLine, lastLine));
        }

        private bool HasErrors(string source, int firstLine, int lastLine)
        {
            foreach (CodeError error in Setup.Session.FindErrors(source, RunMode.WorkOrder))
            {
                if (error.Line >= firstLine && error.Line <= lastLine)
                    return true;
            }
            return false;
        }

        private void ToggleRunOnExit()
        {
            runOnExit = !runOnExit;

            // Turning it off means the player wants to run things right now, so a waiting run is dropped
            if (!runOnExit)
                queued = false;
            UpdateExitToggle();
        }

        private void UpdateExitToggle()
        {
            UiKit.SetLabel(exitToggle, runOnExit ? "[x] Run on exit" : "[ ] Run on exit");
            UiKit.SetToolButtonActive(exitToggle, runOnExit);
            UiKit.SetLabel(runButton, runOnExit ? "Queue run  F5" : "Seal and run  F5");
        }

        // The player left the computer: a queued work order starts now, with the world in view
        public override void OnComputerClosed()
        {
            if (!queued)
                return;

            queued = false;
            if (!running)
                runCoroutine = desktop.Host.StartCoroutine(RunRoutine(queuedSource, queuedFirst, queuedLast));
        }

        private IEnumerator RunRoutine(string source, int firstLine, int lastLine)
        {
            running = true;
            runtimeErrorLines.Clear();
            editor.SetRuntimeErrorLines(runtimeErrorLines);
            Setup.Session.Log.Clear();

            RunResult result = new RunResult();
            yield return Setup.Session.Run(source, RunMode.WorkOrder, result, firstLine, lastLine);

            foreach (CodeError error in result.Errors)
                runtimeErrorLines.Add(error.Line);
            editor.SetRuntimeErrorLines(runtimeErrorLines);
            running = false;
        }

        // Stop also cancels a queued run
        private void Stop()
        {
            if (queued)
            {
                queued = false;
                return;
            }

            if (runCoroutine != null)
                desktop.Host.StopCoroutine(runCoroutine);
            running = false;
            Setup.Session.SetCurrentLine(0);
        }

        // ---------- Status ----------

        private void UpdateStatus()
        {
            if (running)
                statusLabel.text = "Running...";
            else if (queued)
                statusLabel.text = "Queued, runs when you leave";
            else
                statusLabel.text = problems.Count > 0 ? problems.Count + " problem(s) to fix" : "No problems found";
            statusLabel.color = !running && !queued && problems.Count > 0 ? (Color)Palette.Blood : UiKit.GoldDark;
            leftStatus.text = Setup.GetStatus();

            runButton.interactable = !running;
            stopButton.interactable = running || queued;

            if (currentTab != Tab.RunLog)
            {
                int entries = Setup.Session.Log.Entries.Count;
                if (problems.Count == 0)
                    SetBadge(Tab.RunLog, entries > 0 ? entries.ToString() : "", false);
            }
            SetBadge(Tab.Scanned, Setup.ScannedNames.Count.ToString(), false);
        }
    }
}
