using System.Collections;
using System.Collections.Generic;
using CodeRunner;
using CodeRunner.Interpreting;
using Game;
using UnityEngine;
using UnityEngine.UIElements;

namespace Computer
{
    // The program where the player writes code and runs it. It has four tabs:
    // the work order itself, the run log, the console and the list of scanned objects.
    public class WorkOrderApp : DesktopApp
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

        private static string savedCode;

        private readonly Texture2D icon = PixelIcons.WorkOrder();
        private readonly List<CodeError> problems = new List<CodeError>();
        private readonly List<int> runtimeErrorLines = new List<int>();

        private Desktop desktop;
        private CodeEditorView editor;
        private RunLogView logView;
        private ConsoleView console;
        private ScannedView scanned;

        private readonly VisualElement[] panes = new VisualElement[4];
        private readonly Button[] tabButtons = new Button[4];
        private readonly Label[] tabBadges = new Label[4];
        private Button runButton;
        private Button stopButton;
        private Label statusLabel;
        private Label leftStatus;
        private Label positionLabel;
        private Label waxLabel;
        private Button waxFixButton;
        private VisualElement waxBar;
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
        public WorkOrderApp(bool runOnExit)
        {
            this.runOnExit = runOnExit;
        }

        public override string Name
        {
            get { return "Work order"; }
        }

        public override Texture2D Icon
        {
            get { return icon; }
        }

        public override Vector2 WindowSize
        {
            get { return new Vector2(500f, 380f); }
        }

        // ---------- Building the window ----------

        public override VisualElement Build(Desktop owner)
        {
            desktop = owner;

            VisualElement root = new VisualElement();
            root.AddToClassList("work-order");

            root.Add(BuildTabs());
            root.Add(BuildToolbar());

            VisualElement area = new VisualElement();
            area.AddToClassList("pane-area");
            root.Add(area);

            panes[(int)Tab.WorkOrder] = BuildEditorPane();
            logView = new RunLogView();
            logView.FixRequested += ApplyFix;
            panes[(int)Tab.RunLog] = logView;
            console = new ConsoleView();
            panes[(int)Tab.Console] = console;
            scanned = new ScannedView();
            scanned.InsertRequested += InsertName;
            panes[(int)Tab.Scanned] = scanned;

            foreach (VisualElement pane in panes)
                area.Add(pane);

            root.Add(BuildStatusBar());

            editor.Text = savedCode != null ? savedCode : StartingCode;
            SelectTab(Tab.WorkOrder);
            return root;
        }

        private VisualElement BuildTabs()
        {
            VisualElement strip = new VisualElement();
            strip.AddToClassList("tab-strip");

            string[] names = { "Work order", "Run log", "Console", "Scanned" };
            for (int i = 0; i < names.Length; i++)
            {
                Tab tab = (Tab)i;
                Button button = new Button(delegate { SelectTab(tab); });
                button.AddToClassList("tab");
                button.Add(new Label(names[i]));

                Label badge = new Label("");
                badge.AddToClassList("tab-badge");
                badge.style.display = DisplayStyle.None;
                button.Add(badge);

                tabButtons[i] = button;
                tabBadges[i] = badge;
                strip.Add(button);
            }
            return strip;
        }

        private VisualElement BuildToolbar()
        {
            VisualElement bar = new VisualElement();
            bar.AddToClassList("toolbar");

            runButton = new Button(RunAll);
            runButton.text = "Seal and run  F5";
            runButton.AddToClassList("seal-button");
            bar.Add(runButton);

            Button selectionButton = new Button(RunSelection);
            selectionButton.text = "Run selection  F6";
            selectionButton.AddToClassList("tool-button");
            bar.Add(selectionButton);

            stopButton = new Button(Stop);
            stopButton.text = "Stop";
            stopButton.AddToClassList("tool-button");
            bar.Add(stopButton);

            exitToggle = new Button(ToggleRunOnExit);
            exitToggle.AddToClassList("tool-button");
            bar.Add(exitToggle);
            UpdateExitToggle();

            statusLabel = new Label("");
            statusLabel.AddToClassList("toolbar-status");
            bar.Add(statusLabel);
            return bar;
        }

        private VisualElement BuildEditorPane()
        {
            VisualElement pane = new VisualElement();
            pane.AddToClassList("pane");
            pane.Add(UiParts.Roller());

            editor = new CodeEditorView();
            editor.IsHandle = delegate(string name) { return Setup.Handles.Contains(name); };
            editor.TextChanged += OnCodeChanged;
            editor.CaretMoved += OnCaretMoved;
            pane.Add(editor);

            waxBar = new VisualElement();
            waxBar.AddToClassList("wax-bar");
            waxLabel = new Label("");
            waxLabel.AddToClassList("wax-text");
            waxBar.Add(waxLabel);
            waxFixButton = new Button(delegate { ApplyFix(waxError); });
            waxFixButton.AddToClassList("tool-button");
            waxBar.Add(waxFixButton);
            pane.Add(waxBar);

            pane.Add(UiParts.Roller());
            return pane;
        }

        private VisualElement BuildStatusBar()
        {
            VisualElement bar = new VisualElement();
            bar.AddToClassList("status-bar");

            leftStatus = new Label("");
            leftStatus.AddToClassList("status-left");
            bar.Add(leftStatus);

            positionLabel = new Label("Line 1, column 1");
            bar.Add(positionLabel);
            return bar;
        }

        // ---------- Tabs ----------

        private void SelectTab(Tab tab)
        {
            currentTab = tab;
            for (int i = 0; i < panes.Length; i++)
            {
                bool active = i == (int)tab;
                panes[i].style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
                tabButtons[i].EnableInClassList("tab-active", active);
            }

            if (tab == Tab.Console)
                console.FocusInput();
        }

        private void SetBadge(Tab tab, string text, bool isError)
        {
            Label badge = tabBadges[(int)tab];
            if (badge.text == text && badge.ClassListContains("tab-badge-error") == isError)
                return;

            badge.text = text;
            badge.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex;
            badge.EnableInClassList("tab-badge-error", isError);
        }

        // ---------- App events ----------

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
            logView.Refresh(problems, Setup.Session.Log.Entries, running, queued);
            if (currentTab == Tab.Scanned)
                scanned.Tick();

            UpdateStatus();
        }

        // ---------- Problems ----------

        private void OnCodeChanged()
        {
            // Kept so the code survives the desktop being built again, for example when the look is switched
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

            waxBar.EnableInClassList("wax-bar-ok", shown == null);
            if (shown == null)
            {
                waxLabel.text = "The code reads clean. Press F5 to seal and run it.";
                waxFixButton.style.display = DisplayStyle.None;
                return;
            }

            waxLabel.text = "Line " + shown.Line + ": " + QuickFix.MessageOf(shown);

            string fix = QuickFix.NameFor(shown);
            waxFixButton.style.display = fix == null ? DisplayStyle.None : DisplayStyle.Flex;
            waxError = shown;
            if (fix != null)
                waxFixButton.text = "Use " + fix;
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
            exitToggle.text = runOnExit ? "[x] Run on exit" : "[ ] Run on exit";
            exitToggle.EnableInClassList("tool-button-active", runOnExit);
            runButton.text = runOnExit ? "Queue run  F5" : "Seal and run  F5";
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
            statusLabel.EnableInClassList("toolbar-status-bad", !running && !queued && problems.Count > 0);
            leftStatus.text = Setup.GetStatus();

            runButton.SetEnabled(!running);
            stopButton.SetEnabled(running || queued);

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
