using Menu.Remix.MixedUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using RWCustom;
using UnityEngine;

namespace CustomSlugcatUtils.Tools
{
    // thanks Harvie
    public class ErrorTracker
    {
        public static ErrorTracker Instance { get; private set; }
        public static float maxWidth = 750f;
        public static Dictionary<string, TrackedError> errors = new ();
        public static List<TrackedError> allTrackers = new ();

        public FStage stage;
        public FContainer container;
        public List<ErrorButton> buttons = new ();

        ErrorButton mainButton;
        ErrorButton textBox;
        ErrorButton toggleTextButton;
        ErrorButton nextButton;
        ErrorButton prevButton;

        public int mouseDownCoolCounter;
        public bool lastMouseDown;
        public bool mouseDown;

        public bool show;
        public bool showOrigMessages;

        public int currentTracker;

        public bool MouseClicked => !lastMouseDown && mouseDown && mouseDownCoolCounter == 0;
        public Vector2 ScreenSize => Custom.rainWorld.options.ScreenSize;

        public ErrorTracker()
        {
            Instance = this;
            stage = new FStage("CustomSlugcatUtils_ErrorTracker");
            container = new FContainer();
            Futile.AddStage(stage);
            stage.MoveToFront();
            stage.AddChild(container);

            InitButtons();
            Signal("NewTracker", null);
        }

        void InitButtons()
        {
            mainButton = new ErrorButton(this, "Main");
            mainButton.size = new Vector2(50f, 50f);
            mainButton.text = "  !";
            mainButton.pos = ScreenSize;
            mainButton.Enabled = allTrackers.Count > 0;
            buttons.Add(mainButton);

            textBox = new ErrorButton(this, "TextBox");
            textBox.interactive = false;
            textBox.size = new Vector2(300f, 200f);
            textBox.pos = ScreenSize - new Vector2(0f, 50f);
            textBox.Enabled = false;
            buttons.Add(textBox);

            toggleTextButton = new ErrorButton(this, "ToggleText");
            toggleTextButton.size = new Vector2(100f, 50f);
            toggleTextButton.pos = ScreenSize;
            toggleTextButton.text = "ToggleText";
            buttons.Add(toggleTextButton);

            nextButton = new ErrorButton(this, "Next");
            nextButton.size = new Vector2(100f, 50f);
            nextButton.pos = ScreenSize;
            nextButton.text = "Next";
            buttons.Add(nextButton);

            prevButton = new ErrorButton(this, "Prev");
            prevButton.size = new Vector2(100f, 50f);
            prevButton.pos = ScreenSize;
            prevButton.text = "Prev";
            buttons.Add(prevButton);
        }

        public void Update()
        {
            lastMouseDown = mouseDown;
            mouseDown = Input.GetMouseButton(0);

            foreach (var button in buttons)
            {
                button.Update();
            }

            if (mouseDownCoolCounter > 0)
                mouseDownCoolCounter--;
        }

        public void Signal(string text, ErrorButton button)
        {
            if (text == "Main")
            {
                show = !show;
            }
            else if (text == "Prev")
            {
                currentTracker = Mathf.Max(currentTracker - 1, 0);
            }
            else if (text == "Next")
            {
                currentTracker = Mathf.Min(currentTracker + 1, allTrackers.Count - 1);
            }
            else if (text == "ToggleText")
            {
                showOrigMessages = !showOrigMessages;
            }
            UpdateButtons();
        }

        public void UpdateButtons()
        {
            if (show && allTrackers.Count > 0)
            {
                string text = showOrigMessages ? allTrackers[currentTracker].origMessage : allTrackers[currentTracker].streamlineInfo;
                text = text.WrapText(false, maxWidth);

                textBox.text = text;
                float textBoxWidth = Mathf.Max(textBox.label.textRect.width, 300f) * textBox.label.scale;
                float textBoxHeight = textBox.label.textRect.height * textBox.label.scale + 20f;
                float smallButtonWidth = textBoxWidth / 3f;

                mainButton.size = new Vector2(textBoxWidth, 50f);
                mainButton.text = LabelTest.TrimText(allTrackers[currentTracker].typeName, textBoxWidth - 40f, true, false) + $" x{allTrackers[currentTracker].count}";
                textBox.size = new Vector2(textBoxWidth, textBoxHeight);

                nextButton.pos = new Vector2(ScreenSize.x, Mathf.Max(ScreenSize.y - 50f - textBoxHeight, 50));
                nextButton.size = new Vector2(smallButtonWidth, 50f);

                prevButton.pos = new Vector2(ScreenSize.x - smallButtonWidth, Mathf.Max(ScreenSize.y - 50f - textBoxHeight, 50));
                prevButton.size = new Vector2(smallButtonWidth, 50f);

                toggleTextButton.pos = new Vector2(ScreenSize.x - smallButtonWidth * 2f, Mathf.Max(ScreenSize.y - 50f - textBoxHeight, 50));
                toggleTextButton.size = new Vector2(smallButtonWidth, 50f);


                textBox.Enabled = true;
                nextButton.Enabled = currentTracker < allTrackers.Count - 1;
                prevButton.Enabled = currentTracker > 0 && show;
                toggleTextButton.Enabled = show;
            }
            else
            {
                foreach (var button in buttons)
                {
                    if (button != mainButton || allTrackers.Count == 0)
                    {
                        button.Enabled = false;
                    }
                    else
                    {
                        button.size = new Vector2(50f, 50f);
                        button.text = "  !";
                        button.Enabled = true;
                    }
                }
            }
        }

        public static void TrackError(Exception e, string streamlineInfo)
        {
            string key = e.StackTrace ?? e.Message + streamlineInfo;
            if (errors.ContainsKey(key))
            {
                //errors[key].count++;
            }
            else
            {
                var tracker = new TrackedError(e, streamlineInfo);
                errors.Add(key, tracker);
                allTrackers.Add(tracker);
            }

            Instance?.Signal("NewTracker", null);
        }

        public static void TrackError(Exception e, string header, string streamlineInfo)
        {
            string key = e.StackTrace ?? e.Message + streamlineInfo;
            if (errors.ContainsKey(key))
            {
                //errors[key].count++;
            }
            else
            {
                var tracker = new TrackedError(e, header, streamlineInfo);
                errors.Add(key, tracker);
                allTrackers.Add(tracker);
            }

            Instance?.Signal("NewTracker", null);
        }
        public static void TrackError(string header, string message)
        {
            string key = header + message;
            if (errors.ContainsKey(key))
            {
                //errors[key].count++;
            }
            else
            {
                var tracker = new TrackedError(header, message);
                errors.Add(key, tracker);
                allTrackers.Add(tracker);
            }

            Instance?.Signal("NewTracker", null);
        }
        /// <summary>
        /// 右上角定位
        /// </summary>
        public class ErrorButton
        {
            public ErrorTracker tracker;

            public FSprite background;
            public FLabel label;

            public bool Enabled
            {
                get => label.isVisible;
                set
                {
                    label.isVisible = value;
                    background.isVisible = value;
                }
            }
            public bool interactive = true;

            public string signalText;

            public Vector2 pos
            {
                get => background.GetPosition();
                set
                {
                    background.SetPosition(value);
                    label.SetPosition(value - Vector2.one * 10f);
                }
            }
            public Vector2 size
            {
                get => new (background.width, background.height);
                set
                {
                    background.width = value.x;
                    background.height = value.y;
                }
            }

            public string text
            {
                get => label.text;
                set => label.text = value;
            }

            public Color color
            {
                get => background.color;
                set => background.color = value;
            }

            public ErrorButton(ErrorTracker tracker, string signalText = "")
            {
                this.tracker = tracker;
                this.signalText = signalText;

                background = new FSprite("pixel", true)
                {
                    anchorX = 1f,
                    anchorY = 1f,
                    isVisible = false
                };
                label = new FLabel(Custom.GetDisplayFont(), "")
                {
                    anchorX = 1f,
                    anchorY = 1f,
                    isVisible = false,
                    scale = 0.8f,
                };
                tracker.container.AddChild(background);
                tracker.container.AddChild(label);
            }

            public void Update()
            {
                if (!Enabled)
                    return;
                if (!interactive)
                {
                    color = Color.white * 0.3f;
                    return;
                }

                Vector2 mousePos = Futile.mousePosition;
                if (mousePos.x < pos.x &&
                    mousePos.x > pos.x - size.x &&
                    mousePos.y < pos.y &&
                    mousePos.y > pos.y - size.y)
                {
                    if (tracker.MouseClicked)
                    {
                        tracker.Signal(signalText, this);
                        tracker.mouseDownCoolCounter = 20;
                    }
                    color = Color.white * 0.7f;
                }
                else
                {
                    color = Color.white * 0.5f;
                }
            }
        }

        public class TrackedError
        {
            public string typeName;
            public string streamlineInfo;
            public string origMessage;
            public int count = 1;

            public TrackedError(Exception exception,string header, string streamlineInfo)
            {
                typeName = header;
                origMessage = exception.Message + "\n" + exception.StackTrace;
            }


            public TrackedError(Exception exception, string streamlineInfo)
            {
                typeName = exception.GetType().Name;
                origMessage = exception.Message + "\n" + exception.StackTrace;
                this.streamlineInfo = streamlineInfo;
            }

            public TrackedError(string header, string message)
            {
                typeName = header;
                origMessage = message;
                this.streamlineInfo = "";
            }
        }

        public static IEnumerator LateCreateExceptionTracker()
        {
            while (Custom.rainWorld.processManager.currentMainLoop == null || Custom.rainWorld.processManager.currentMainLoop.ID != ProcessManager.ProcessID.MainMenu)
                yield return new WaitForSeconds(1);

            _ =new ErrorTracker();
            yield break;
        }
    }
}
