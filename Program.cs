using ConsoleSharp;
using static ConsoleSharp.CSDisplay;

var display = new CSDisplay(name: "ConsoleSharp Demo", dimensions: new Size(1920, 1080), bgColor: Colors.DarkRed);
CSColor textColor = Colors.LightBlue;
CSFont textFont = new CSFont(fontFamily: FontFamily.GenericSansSerif, fontSize: 30);
Effect textEffect = new TypeWriter(100);
CSColor inputColor = Colors.LightGreen;
display.Print(new TextBlock("Hello and welcome!", textColor: textColor, font: textFont, effect: textEffect));
CSInput input = new CSInput(textColor: inputColor, font: textFont, cursorColor: Colors.Blue);
string userInput = await display.ReadLine(prompt: new Line(new TextBlock("Please enter your name: ", textColor: textColor, font: textFont, effect: textEffect)), inputStyling: input);
display.Print(new Line(new TextBlock($"Welcome to the game {userInput}.", textColor: textColor, font: textFont, effect: textEffect)));

namespace ConsoleSharp
{
    using Microsoft.VisualBasic.Devices;
    using System.Text.RegularExpressions;
    using System.Windows.Forms;
    using System.Reflection;
    using static CSDisplay;
    using System.Threading.Tasks;

    /// <summary>
    /// Class for creating and handling a single ConsoleSharp window.
    /// </summary>
    public class CSDisplay
    {
        /// <summary>
        /// The default pixel size for a new window if no dimensions are provided.
        /// </summary>
        public static Size DefaultDims { get; set; } = new Size(500, 500);
        /// <summary>
        /// Whether closing the last window also quits the application.
        /// </summary>
        public bool DoNotQuit
        {
            get { return _donotquit; }
            set
            {
                _donotquit = value;
                if (!value && AllInstances.Count == 0) Environment.Exit(0);
            }
        }
        /// <summary>
        /// Internal property storing the value of DoNotQuit.
        /// </summary>
        bool _donotquit = false;
        /// <summary>
        /// The window managed by the display instance.
        /// </summary>
        Window window;
        /// <summary>
        /// The user input handler for the instance.
        /// </summary>
        readonly InputHandler inputHandler;
        /// <summary>
        /// The UI thread which the window operates on.
        /// </summary>
        readonly Thread UIThread;
        /// <summary>
        /// The main thread which the display instance operates on.
        /// </summary>
        readonly Thread MainThread;
        /// <summary>
        /// List of all display instances with open windows.
        /// </summary>
        static readonly List<CSDisplay> AllInstances = [];

        /// <summary>
        /// Prints a prompt and reads the input entered by the user.
        /// </summary>
        /// <param name="prompt">A Line class representing the prompt to be printed.</param>
        /// <param name="inputStyling">The styling data for the input field.</param>
        /// <returns>A string containing the user input.</returns>
        public async Task<string> ReadLine(Line prompt, CSInput? inputStyling = null)
        {
            Print(prompt);
            return await ReadLineImpl(inputStyling);
        }

        /// <summary>
        /// Prints a prompt and reads the input entered by the user.
        /// </summary>
        /// <param name="prompt">A list of TextBlock classes representing the prompt to be printed.</param>
        /// <param name="inputStyling">The styling data for the input field.</param>
        /// <returns>A string containing the user input.</returns>
        public async Task<string> ReadLine(List<TextBlock> prompt, CSInput? inputStyling = null)
        {
            Print(prompt);
            return await ReadLineImpl(inputStyling);
        }

        /// <summary>
        /// Prints a prompt and reads the input entered by the user.
        /// </summary>
        /// <param name="prompt">A TextBlock class representing the prompt to be printed.</param>
        /// <param name = "inputStyling">The styling data for the input field.</param>
        /// <returns>A string containing the user input.</returns>
        public async Task<string> ReadLine(TextBlock prompt, CSInput? inputStyling = null)
        {
            Print(prompt);
            return await ReadLineImpl(inputStyling);
        }

        /// <summary>
        /// Prints a prompt and reads the input entered by the user.
        /// </summary>
        /// <param name="prompt">A string with optional embedded styling representing the prompt to be printed.</param>
        /// <param name="inputStyling">The styling data for the input field.</param>
        /// <returns>A string containing the user input.</returns>
        public async Task<string> ReadLine(string prompt, CSInput? inputStyling = null)
        {
            Print(prompt);
            return await ReadLineImpl(inputStyling);
        }

        /// <summary>
        /// Creates a new input field and reads the user input to from it.
        /// </summary>
        /// <param name="inputStyling">The styling data for the input field.</param>
        /// <returns></returns>
        private async Task<string> ReadLineImpl(CSInput? inputStyling)
        {
            MainThread.Join(100);
            CSColor textColor = Colors.White;
            CSColor bgColor = window.BGColor;
            CSFont font = new();
            CSColor cursorColor = Colors.White;
            if (inputStyling != null)
            {
                textColor = inputStyling.TextColor ?? textColor;
                bgColor = inputStyling.BGColor ?? bgColor;
                font = inputStyling.Font ?? font;
                cursorColor = inputStyling.CursorColor ?? cursorColor;
            }
            Point? pos = null;
            if (window.Labels.Count > 0)
            {
                var prevField = window.Labels.Last();
                pos = new Point(0, prevField.Location.Y + prevField.GetPreferredSize(new Size(prevField.Width, 0)).Height);
            }
            var field = AddInputField(pos, textColor, bgColor, font, cursorColor);
            return await inputHandler.CaptureInput(field);
        }

        /// <summary>
        /// Prints multiple lines of text to the screen.
        /// </summary>
        /// <param name="lines">A list of Line classes representing the text to be printed.</param>
        public void Print(List<Line> lines)
        {
            foreach (var line in lines)
            {
                line.PrintText(display: this);
            }
        }

        /// <summary>
        /// Prints a single line of text to the screen.
        /// </summary>
        /// <param name="line">A Line class representing the text to be printed.</param>
        public void Print(Line line)
        {
            line.PrintText(display: this);
        }

        /// <summary>
        /// Appends multiple blocks of text to the current line on the screen.
        /// </summary>
        /// <param name="texts">A list of TextBlock classes representing the text to be appended.</param>
        public void Print(List<TextBlock> texts)
        {
            foreach (var text in texts)
            {
                text.PrintText(display: this);
            }
        }

        /// <summary>
        /// Appends a single block of text to the current line on the screen.
        /// </summary>
        /// <param name="text">A TextBlock class representing the text to be appended.</param>
        public void Print(TextBlock text)
        {
            text.PrintText(display: this);
        }

        /// <summary>
        /// Prints a string with optional embedded styling to the screen.
        /// </summary>
        /// <param name="text">The string to be printed.</param>
        public void Print(string text)
        {
            var lines = Utils.BuildFromString(text);
            foreach (var line in lines)
            {
                line.PrintText(display: this);
            }
        }

        /// <summary>
        /// Prints an empty line to the screen.
        /// </summary>
        /// <param name="fontSize">The font size for the empty line.</param>
        public void Print(int? fontSize = null)
        {
            Point? pos = null;
            if (window.Labels.Count > 0)
            {
                var prevField = window.Labels.Last();
                pos = new Point(0, prevField.Location.Y + prevField.Height);
            }
            AddLabel(pos, fontSize);
        }

        /// <summary>
        /// Prints a horizontal divider across the width of the screen.
        /// </summary>
        /// <param name="font">The font for the divider.</param>
        /// <param name="textColor">The color of the divider.</param>
        /// <param name="bgColor">The color of the background of the divider.</param>
        /// <param name="effect">The text effect with which the divider is printed.</param>
        public void PrintDivider(CSFont? font = null, CSColor? textColor = null, CSColor? bgColor = null, Effect? effect = null)
        {
            font ??= new CSFont();
            textColor ??= Colors.White;
            bgColor ??= window.BGColor;
            effect ??= new NoEffect();

            var sizingLabel = new Label();
            sizingLabel.AutoSize = true;
            sizingLabel.Font = font.FontData;
            sizingLabel.Text += "-";
            sizingLabel.AutoSize = false;
            sizingLabel.Width = window.Width;

            string fillString = "";
            bool maxxed = false;
            while (!maxxed)
            {
                fillString += "-";
                var textSize = TextRenderer.MeasureText(text: fillString, font: font.FontData);
                if (textSize.Width > sizingLabel.Width)
                {
                    fillString = fillString.Remove(fillString.Length - 1);
                    maxxed = true;
                }
            }

            Line fillLine = new Line(new TextBlock(text: fillString, textColor: textColor, bgColor: bgColor, font: font, effect: effect));
            Print(fillLine);
        }

        /// <summary>
        /// Plays a .WAV file using the window's audio device.
        /// </summary>
        /// <param name="file">The name or path of the file to be played.</param>
        public void PlayWAV(string file)
        {
            window.BeginInvoke(() => window.Audio.Play(file));
        }

        /// <summary>
        /// Adds a new label for displaying text to the window.
        /// </summary>
        /// <param name="pos">Position of the new label on the window.</param>
        /// <param name="fontSize">Size of the font of the new label.</param>
        /// <returns>A reference to the new label.</returns>
        private Label AddLabel(Point? pos = null, int? fontSize = null)
        {
            CSFont font = new CSFont(fontSize: fontSize, fontStyle: null);
            var label = new Label();
            label.AutoSize = true;
            if (pos != null)
            {
                label.Location = pos.Value;
            }
            label.Font = font.FontData;
            window.Invoke(() =>
            {
                window.Labels.Add(label);
                window.Controls.Add(label);
            });
            return window.Labels.Last();
        }

        /// <summary>
        /// Adds a new input field for reading user input to the window.
        /// </summary>
        /// <param name="pos">The position of the new input field on the window.</param>
        /// <param name="textColor">The text color of the new input field.</param>
        /// <param name="bgColor">The background color of the new input field.</param>
        /// <param name="font">The font of the new input field.</param>
        /// <param name="cursorColor">The color of the cursor of the new input field.</param>
        /// <returns>A reference to the new input field.</returns>
        private InputField AddInputField(Point? pos = null, CSColor? textColor = null, CSColor? bgColor = null, CSFont? font = null, CSColor? cursorColor = null)
        {
            textColor ??= Colors.White;
            bgColor ??= Colors.Black;
            font ??= new CSFont();
            var field = new InputField(cursorColor) { AutoSize = true };
            if (pos != null)
            {
                field.Location = pos.Value;
            }
            field.Font = font.FontData;
            field.ForeColor = textColor.ColorData;
            field.BackColor = bgColor.ColorData;
            window.Invoke(() =>
            {
                window.Labels.Add(field);
                window.Controls.Add(field);
            });
            return window.Labels.Last() as InputField;
        }

        /// <summary>
        /// Creates a new instance of CSDisplay and a corresponding window.
        /// </summary>
        /// <param name="name">The name of the new window.</param>
        /// <param name="dimensions">The pixel dimensions of the new window.</param>
        /// <param name="position">The initial position of the new window on the screen.</param>
        /// <param name="bgColor">The background color of the new window.</param>
        public CSDisplay(string name = "New Display", Size? dimensions = null, Point? position = null, CSColor? bgColor = null)
        {
            MainThread = Thread.CurrentThread;
            inputHandler = new InputHandler();
            var formReady = new AutoResetEvent(false);
            Size dims = dimensions ?? DefaultDims;
            Point pos = position ?? new Point(Screen.PrimaryScreen.Bounds.Width / 2 - dims.Width / 2, Screen.PrimaryScreen.Bounds.Height / 2 - dims.Height / 2);
            bgColor = bgColor ?? Colors.Black;
            window = new Window(display: this, bgColor); // Prevents warning due to Window not technically being assigned in the constructor.

            UIThread = new Thread(() =>
            {
                window = new Window(display: this, bgColor);
                window.HandleCreated += (_, __) => formReady.Set();
                window.StartPosition = FormStartPosition.Manual;
                window.AutoScroll = true;
                window.Text = name;
                window.BackColor = bgColor.ColorData;
                window.Size = dims;
                window.Location = pos;
                Application.Run(window);
            });

            UIThread.SetApartmentState(ApartmentState.STA);
            UIThread.Start();
            formReady.WaitOne();
            AllInstances.Add(this);
        }

        /// <summary>
        /// Passes user key presses from the window to the input handler.
        /// </summary>
        /// <param name="e">Arguments from the key press event.</param>
        void KeyPressed(KeyPressEventArgs e) { inputHandler.KeyPressed(e); }

        /// <summary>
        /// Passes user key downs from the window to the input handler.
        /// </summary>
        /// <param name="e">Arguments from the key down event.</param>
        void KeyDown(KeyEventArgs e) { inputHandler.KeyDown(e); }

        /// <summary>
        /// Handles the closing of the window.
        /// </summary>
        void WindowClosed()
        {
            AllInstances.Remove(this);
            if (!DoNotQuit && AllInstances.Count == 0) Environment.Exit(0);
        }

        /// <summary>
        /// Class for handling the reading of user inputs.
        /// </summary>
        class InputHandler
        {
            bool Capturing = false;
            readonly object _lock = new();
            InputField? CapturingField;

            /// <summary>
            /// Captures the input typed by the user into a given input field.
            /// </summary>
            /// <param name="field">The input field which the user is typing in.</param>
            /// <returns>A string containing the user input.</returns>
            public async Task<string> CaptureInput(InputField field)
            {
                CapturingField = field;
                Capturing = true;
                CapturingField.Invoke(() =>
                {
                    CapturingField.Text = string.Empty;
                });
                lock (_lock)
                {
                    while (Capturing) Monitor.Wait(_lock);
                }
                CapturingField.BeginInvoke(() => CapturingField.Capturing = false);
                return CapturingField.Text;
            }

            /// <summary>
            /// Handles user key presses during capturing and passes them to the capturing input field.
            /// </summary>
            /// <param name="e">Arguments from the key press event.</param>
            public void KeyPressed(KeyPressEventArgs e)
            {
                if (Capturing)
                {
                    if (e.KeyChar == (char)Keys.Enter)
                    {
                        lock (_lock)
                        {
                            Capturing = false;
                            Monitor.Pulse(_lock);
                        }
                    }
                    else
                    {
                        CapturingField?.BeginInvoke(() =>
                        {
                            CapturingField.HandleKeyPress(e);
                        });
                    }
                }
            }

            /// <summary>
            /// Passes user key downs to the capturing input field.
            /// </summary>
            /// <param name="e">Arguments from the key down event.</param>
            public void KeyDown(KeyEventArgs e)
            {
                if (Capturing)
                {
                    CapturingField?.BeginInvoke(() =>
                    {
                        CapturingField.HandleKeyDown(e);
                    });
                }
            }
        }

        /// <summary>
        /// Class representing the styling data of a ConsoleSharp input field.
        /// </summary>
        public class CSInput
        {
            /// <summary>
            /// The text color of the input field.
            /// </summary>
            public CSColor? TextColor;
            /// <summary>
            /// The background color of the input field.
            /// </summary>
            public CSColor? BGColor;
            /// <summary>
            /// The font of the inptu field.
            /// </summary>
            public CSFont? Font;
            /// <summary>
            /// The color of the cursor of the input field.
            /// </summary>
            public CSColor? CursorColor;

            /// <summary>
            /// Creates a new CSInput instance.
            /// </summary>
            /// <param name="textColor">The text color of the input field.</param>
            /// <param name="bgColor">The background color of the input field.</param>
            /// <param name="font">The font of the input field.</param>
            /// <param name="cursorColor">The color of the cursor of the input field.</param>
            public CSInput(CSColor? textColor = null, CSColor? bgColor = null, CSFont? font = null, CSColor? cursorColor = null)
            {
                TextColor = textColor;
                BGColor = bgColor;
                Font = font;
                CursorColor = cursorColor;
            }
        }

        /// <summary>
        /// Parent class for classes representing text in ConsoleSharp.
        /// </summary>
        public class TextCont
        {
            /// <summary>
            /// Parent method for printing text contained in ConsoleSharp text classes.
            /// </summary>
            /// <param name="display">The display to print to.</param>
            /// <param name="field">The text field to print to.</param>
            /// <exception cref="NotImplementedException"></exception>
            public virtual void PrintText(CSDisplay display, Label? field = null)
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Class representing a single line of text.
        /// </summary>
        public class Line : TextCont
        {
            /// <summary>
            /// List of TextBlocks which make up the line.
            /// </summary>
            public List<TextBlock> TextBlocks { get; set; } = [];

            /// <summary>
            /// Prints the entire text contents of the line.
            /// </summary>
            /// <param name="display">The display to print to.</param>
            /// <param name="field">The first field to print to.</param>
            public override void PrintText(CSDisplay display, Label? field = null)
            {
                Point? pos = null;
                if (display.window.Labels.Count > 0)
                {
                    var prevField = display.window.Labels.Last();
                    pos = new Point(0, prevField.Location.Y + prevField.GetPreferredSize(new Size(prevField.Width, 0)).Height);
                }
                var firstField = display.AddLabel(pos);
                bool firstText = true;
                foreach (var text in TextBlocks)
                {
                    if (firstText)
                    {
                        text.PrintText(display, firstField);
                        firstText = false;
                    }
                    else
                    {
                        text.PrintText(display);
                    }
                }
            }

            /// <summary>
            /// Creates an empty Line instance.
            /// </summary>
            public Line() { }

            /// <summary>
            /// Creates a Line instance containing a single TextBlock.
            /// </summary>
            /// <param name="text">A TextBlock class representing the contents of the line.</param>
            public Line(TextBlock text)
            {
                TextBlocks.Add(text);
            }

            /// <summary>
            /// Creates a Line instance containing multiple TextBlocks.
            /// </summary>
            /// <param name="textBlocks">A list of TextBlock classes representing the contents of the line.</param>
            public Line(List<TextBlock> textBlocks)
            {
                TextBlocks.AddRange(textBlocks);
            }
        }

        /// <summary>
        /// Class representing a single block of text within a line.
        /// </summary>
        public class TextBlock : TextCont
        {
            /// <summary>
            /// The default text printing effect for new TextBlock instances.
            /// </summary>
            public static Effect DefaultEffect { get; set; } = new NoEffect();
            /// <summary>
            /// The string represented by the TextBlock.
            /// </summary>
            public string Text { get; set; } = "";
            /// <summary>
            /// The text color of the TextBlock.
            /// </summary>
            public CSColor TextColor { get; set; } = Colors.White;
            /// <summary>
            /// The background color of the TextBlock.
            /// </summary>
            public CSColor? BGColor { get; set; }
            /// <summary>
            /// The font of the TextBlock.
            /// </summary>
            public CSFont Font { get; set; } = new CSFont();
            /// <summary>
            /// The printing effect used by the TextBlock.
            /// </summary>
            public Effect Effect { get; set; }

            /// <summary>
            /// Prints the entire text contents of the TextBlock.
            /// </summary>
            /// <param name="display">The display to print to.</param>
            /// <param name="field">The initial field to print to.</param>
            public override void PrintText(CSDisplay display, Label? field = null)
            {
                BGColor ??= display.window.BGColor;
                if (field == null)
                {
                    Point? pos = null;
                    if (display.window.Labels.Count > 0)
                    {
                        var prevField = display.window.Labels.Last();
                        pos = new Point(prevField.Location.X + prevField.Width, prevField.Location.Y);
                    }
                    field = display.AddLabel(pos);
                }
                field.BeginInvoke(() =>
                {
                    field.BackColor = BGColor.ColorData;
                    field.ForeColor = TextColor.ColorData;
                    field.Font = Font.FontData;
                });
                Effect.PrintEffect(Text, field);
            }

            /// <summary>
            /// Creates a new TextBlock instance.
            /// </summary>
            /// <param name="text">The string represented by the TextBlock.</param>
            /// <param name="textColor">The text color of the TextBlock.</param>
            /// <param name="bgColor">The background color of the TextBlock.</param>
            /// <param name="font">The font of the TextBlock.</param>
            /// <param name="effect">The printing effect used by the TextBlock.</param>
            public TextBlock(string? text = null, CSColor? textColor = null, CSColor? bgColor = null, CSFont? font = null, Effect? effect = null)
            {
                Text = text ?? Text;
                TextColor = textColor ?? TextColor;
                BGColor = bgColor;
                Font = font ?? Font;
                Effect = effect ?? DefaultEffect;
            }
        }

        /// <summary>
        /// Class representing color data in ConsoleSharp.
        /// </summary>
        public class CSColor
        {
            /// <summary>
            /// The red value of the color.
            /// </summary>
            public int R
            {
                get { return _r; }
                set
                {
                    if (value > 255 || value < 0) throw new ArgumentOutOfRangeException(paramName: "R", message: $"{value} is outside the range. Value must be between 0 and 255");
                    else
                    {
                        _r = value;
                        UpdateColorData();
                    }
                }
            }
            /// <summary>
            /// The green value of the color.
            /// </summary>
            public int G
            {
                get { return _g; }
                set
                {
                    if (value > 255 || value < 0) throw new ArgumentOutOfRangeException(paramName: "G", message: $"{value} is outside the range. Value must be between 0 and 255");
                    else
                    {
                        _g = value;
                        UpdateColorData();
                    }
                }
            }
            /// <summary>
            /// The blue value of the color.
            /// </summary>
            public int B
            {
                get { return _b; }
                set
                {
                    if (value > 255 || value < 0) throw new ArgumentOutOfRangeException(paramName: "B", message: $"{value} is outside the range. Value must be between 0 and 255");
                    else
                    {
                        _b = value;
                        UpdateColorData();
                    }
                }
            }
            /// <summary>
            /// The alpha value of the color.
            /// </summary>
            public int A
            {
                get { return _a; }
                set
                {
                    if (value > 255 || value < 0) throw new ArgumentOutOfRangeException(paramName: "A", message: $"{value} is outside the range. Value must be between 0 and 255");
                    else
                    {
                        _a = value;
                        UpdateColorData();
                    }
                }
            }

            /// <summary>
            /// Internal property storing the red value of the color.
            /// </summary>
            private int _r = 0;
            /// <summary>
            /// Internal property storing the green value of the color.
            /// </summary>
            private int _g = 0;
            /// <summary>
            /// Internal property storing the blue value of the color.
            /// </summary>
            private int _b = 0;
            /// <summary>
            /// Internal property storing the alpha value of the color.
            /// </summary>
            private int _a = 0;

            /// <summary>
            /// The C# representation of the RGBA data of the color.
            /// </summary>
            public Color ColorData { get; private set; }

            /// <summary>
            /// Updates the C# representation to match the current RGBA values.
            /// </summary>
            void UpdateColorData()
            {
                ColorData = Color.FromArgb(_a, _r, _g, _b);
            }

            /// <summary>
            /// Creates a new CSColor instance with the identical RGBA values.
            /// </summary>
            /// <returns>A reference to the new instance.</returns>
            public CSColor Clone()
            {
                return new CSColor(_r, _g, _b, _a);
            }

            /// <summary>
            /// Creates a new CSColor instance defaulting to black.
            /// </summary>
            public CSColor()
            {
                (R, G, B, A) = (0, 0, 0, 255);
            }

            /// <summary>
            /// Creates a new CSColor instance using the given RGBA values.
            /// </summary>
            /// <param name="r">The red value of the color.</param>
            /// <param name="g">The green value of the color.</param>
            /// <param name="b">The blue value of the color.</param>
            /// <param name="a">The alpha value of the color.</param>
            public CSColor(int? r = null, int? g = null, int? b = null, int? a = null)
            {
                R = (r != null) ? r.Value : 0;
                G = (g != null) ? g.Value : 0;
                B = (b != null) ? b.Value : 0;
                A = (a != null) ? a.Value : 255;
            }

            /// <summary>
            /// Creates a new CSColor instance using the given hexadecimal code and A value.
            /// </summary>
            /// <param name="hex">The hexadecimal code of the color.</param>
            /// <param name="a">The alpha value of the color.</param>
            public CSColor(string? hex = null, int? a = null)
            {
                hex ??= "000000";
                (R, G, B) = Utils.HexToRGB(hex);
                A = (a != null) ? a.Value : 255;
            }
        }

        /// <summary>
        /// Class representing font data in ConsoleSharp.
        /// </summary>
        public class CSFont
        {
            /// <summary>
            /// The default font family for new CSFont instances.
            /// </summary>
            public static FontFamily DefaultFontFamily { get; set; } = FontFamily.GenericMonospace;
            /// <summary>
            /// The default font size for new CSFont instances.
            /// </summary>
            public static int DefaultFontSize { get; set; } = 12;
            /// <summary>
            /// The default font style for new CSFont instances.
            /// </summary>
            public static FontStyle DefaultFontStyle { get; set; } = FontStyle.Regular;
            /// <summary>
            /// The C# representation of the font data.
            /// </summary>
            public Font FontData { get; private set; }

            /// <summary>
            /// Creates a new CSFont instance using all default parameters.
            /// </summary>
            public CSFont()
            {
                FontData = new Font(DefaultFontFamily, DefaultFontSize, DefaultFontStyle);
            }

            /// <summary>
            /// Creates a new CSFont instance with the specified parameters.
            /// </summary>
            /// <param name="fontSize">The size of the font.</param>
            public CSFont(int fontSize)
            {
                FontData = new Font(DefaultFontFamily, fontSize, DefaultFontStyle);
            }

            /// <summary>
            /// Creates a new CSFont instance with the specified parameters.
            /// </summary>
            /// <param name="fontFamily">The font family of the font.</param>
            /// <param name="fontSize">The size of the font.</param>
            public CSFont(FontFamily? fontFamily = null, int? fontSize = null)
            {
                FontFamily family = fontFamily ?? DefaultFontFamily;
                int size = fontSize ?? DefaultFontSize;
                FontData = new Font(family, size);
            }

            /// <summary>
            /// Creates a new CSFont instance with the specified parameters.
            /// </summary>
            /// <param name="fontFamily">The font family of the font.</param>
            /// <param name="fontSize">The size of the font.</param>
            /// <param name="fontStyle">A single style to be applied to the font.</param>
            public CSFont(FontFamily? fontFamily = null, int? fontSize = null, FontStyle? fontStyle = null) : this(fontFamily, fontSize)
            {
                FontStyle style = fontStyle ?? DefaultFontStyle;
                FontData = new Font(FontData.FontFamily, FontData.SizeInPoints, style);
            }

            /// <summary>
            /// Creates a new CSFont instance with the specified parameters.
            /// </summary>
            /// <param name="fontFamily">The font family of the font.</param>
            /// <param name="fontSize">The size of the font.</param>
            /// <param name="fontStyles">A list of styles to be applied to the font.</param>
            public CSFont(FontFamily? fontFamily = null, int? fontSize = null, List<FontStyle>? fontStyles = null) : this(fontFamily, fontSize)
            {
                FontStyle style = DefaultFontStyle;
                if (fontStyles != null)
                {
                    style = FontStyle.Regular;
                    foreach (var fontStyle in fontStyles)
                    {
                        style |= fontStyle;
                    }
                }
                FontData = new Font(FontData.FontFamily, FontData.SizeInPoints, style);
            }
        }

        /// <summary>
        /// Parent class for effects to be applied while printing text.
        /// </summary>
        public class Effect
        {
            /// <summary>
            /// Represents the type of parameter used by the effect.
            /// </summary>
            public Type? ParamType { get; private set; } = null;

            /// <summary>
            /// Prints a string using the logic of the effect.
            /// </summary>
            /// <param name="text">The string to be printed.</param>
            /// <param name="field">The field to print to.</param>
            /// <exception cref="NotImplementedException"></exception>
            public virtual void PrintEffect(string text, Label field)
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// Sets the parameter of the effect.
            /// </summary>
            /// <param name="newParam">The new value of the parameter</param>
            /// <exception cref="NotImplementedException"></exception>
            public virtual void SetParam(dynamic newParam)
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Effect subclass for not applying any effect to a string.
        /// </summary>
        public class NoEffect : Effect
        {
            /// <summary>
            /// Prints a string without applying any effect.
            /// </summary>
            /// <param name="text">The string to be printed.</param>
            /// <param name="field">The field to print to.</param>
            public override void PrintEffect(string text, Label field)
            {
                field.BeginInvoke(() => field.Text += text);
            }
        }

        /// <summary>
        /// Effect subclass for applying a type writer-esque effect to a string.
        /// </summary>
        public class TypeWriter : Effect
        {
            /// <summary>
            /// The delay in milliseconds between characters.
            /// </summary>
            public int Delay { get; set; }
            /// <summary>
            /// The default delay.
            /// </summary>
            private readonly int DefaultDelay = 200;
            /// <summary>
            /// Represents the type of parameter used by the effect.
            /// </summary>
            public new Type? ParamType { get; private set; }

            /// <summary>
            /// Prints a string with a delay applied after every character.
            /// </summary>
            /// <param name="text">The string to be printed.</param>
            /// <param name="field">The field to print to.</param>
            public override void PrintEffect(string text, Label field)
            {
                var chars = Utils.ParseString(text);
                foreach (var chara in chars)
                {
                    field.BeginInvoke(() => field.Text += chara);
                    Thread.Sleep(Delay);
                }
            }

            /// <summary>
            /// Sets the parameter of the effect.
            /// </summary>
            /// <param name="newParam">The new value of the parameter.</param>
            public override void SetParam(dynamic newParam)
            {
                Delay = newParam;
            }

            /// <summary>
            /// Creates a new TypeWriter instance using the default delay.
            /// </summary>
            public TypeWriter()
            {
                Delay = DefaultDelay;
                ParamType = Delay.GetType();
            }

            /// <summary>
            /// Creates a new TypeWriter instance.
            /// </summary>
            /// <param name="delay">The delay in milliseconds between characters.</param>
            public TypeWriter(int delay = 500)
            {
                Delay = delay;
                ParamType = Delay.GetType();
            }
        }

        /// <summary>
        /// Class representing the visual window created by the display.
        /// </summary>
        class Window : Form
        {
            /// <summary>
            /// The display managing the window.
            /// </summary>
            readonly CSDisplay Display;
            /// <summary>
            /// The background color of the window.
            /// </summary>
            public readonly CSColor BGColor;
            /// <summary>
            /// List of all text fields displayed on the window.
            /// </summary>
            public List<Label> Labels { get; private set; } = new List<Label>();
            /// <summary>
            /// The audio player of the window.
            /// </summary>
            public Audio Audio { get; private set; } = new Audio();

            /// <summary>
            /// Passes user key presses to the display managing the window.
            /// </summary>
            /// <param name="e">Arguments from the key press event.</param>
            void WindowKeyPress(KeyPressEventArgs e)
            {
                e.Handled = true;
                Display.KeyPressed(e);
            }

            /// <summary>
            /// Passes user key downs to the display managing the window.
            /// </summary>
            /// <param name="e">Arguments from the key down event.</param>
            void WindowKeyDown(KeyEventArgs e)
            {
                e.Handled = true;
                Display.KeyDown(e);
            }

            /// <summary>
            /// Passes the close event to the display managing the window.
            /// </summary>
            /// <param name="e">Arguments from the close event.</param>
            void WindowClosed(FormClosedEventArgs e)
            {
                Display.WindowClosed();
            }

            /// <summary>
            /// Creates a new Window instance.
            /// </summary>
            /// <param name="display">The display managing the window.</param>
            /// <param name="bgColor">The background color of the window.</param>
            public Window(CSDisplay display, CSColor bgColor) : base()
            {
                Display = display;
                BGColor = bgColor;
                KeyPreview = true;
                KeyPress += new KeyPressEventHandler((sender, e) => WindowKeyPress(e));
                KeyDown += new KeyEventHandler((sender, e) => WindowKeyDown(e));
                FormClosed += new FormClosedEventHandler((sender, e) => WindowClosed(e));
            }
        }

        /// <summary>
        /// Class representing the input field for reading and displaying user input.
        /// </summary>
        class InputField : Label
        {
            /// <summary>
            /// The current position of the cursor in the string.
            /// </summary>
            int CursorIndex = 0;
            /// <summary>
            /// The color of the cursor.
            /// </summary>
            readonly CSColor CursorColor;
            /// <summary>
            /// Whether the input field is actively capturing user input.
            /// </summary>
            public bool Capturing = true;

            /// <summary>
            /// Handles user key press events for typing.
            /// </summary>
            /// <param name="e">Arguments from the key press event.</param>
            public void HandleKeyPress(KeyPressEventArgs e)
            {
                BeginInvoke(() =>
                {
                    if (e.KeyChar == (char)Keys.Back)
                    {
                        if (CursorIndex > 0)
                        {
                            Text = Text.Remove(startIndex: CursorIndex - 1, count: 1);
                            CursorIndex--;
                        }
                    }
                    else
                    {
                        Text = Text.Insert(CursorIndex, e.KeyChar.ToString());
                        CursorIndex++;
                    }
                });
            }

            /// <summary>
            /// Handles user key down events for navigating the cursor.
            /// </summary>
            /// <param name="e">Arguments from the key down event.</param>
            public void HandleKeyDown(KeyEventArgs e)
            {
                BeginInvoke(() =>
                {
                    if (e.KeyCode == Keys.Left && CursorIndex > 0) CursorIndex--;
                    else if (e.KeyCode == Keys.Right && CursorIndex < Text.Length) CursorIndex++;
                });
                Invalidate();
            }

            /// <summary>
            /// Updates the visual position of the cursor when the field is repainted.
            /// </summary>
            /// <param name="g">The graphics object of the field.</param>
            void UpdateCursor(Graphics g)
            {
                if (Capturing)
                {
                    int stringWidth = (int)Math.Round(Font.Size / 10, MidpointRounding.AwayFromZero) * 5;
                    if (CursorIndex > 0)
                    {
                        var chars = Utils.ParseString(Text);
                        var precString = "";
                        for (int i = 0; i < CursorIndex; i++)
                        {
                            precString += chars[i];
                        }
                        var refLabel = new Label() { AutoSize = true, Font = this.Font, Text = precString };
                        stringWidth = refLabel.PreferredWidth;
                    }

                    int width = (int)Math.Round(Font.Size / 8, MidpointRounding.AwayFromZero);
                    if (width < 1) width = 1;
                    int height = (int)Math.Round(Font.Size / 8, MidpointRounding.AwayFromZero);
                    if (height < 1) height = 1;
                    int xPos = stringWidth - (int)Math.Round(Font.Size / 3, MidpointRounding.AwayFromZero);
                    int yPos = (int)Math.Round(PreferredHeight * 0.73, MidpointRounding.AwayFromZero);

                    var cursor = new Rectangle(xPos, yPos, width, height);
                    using (var pen = new Pen(CursorColor.ColorData))
                    {
                        g.DrawRectangle(pen, cursor);
                    }
                    using (var brush = new SolidBrush(CursorColor.ColorData))
                    {
                        g.FillRectangle(brush, cursor);
                    }
                }
            }

            /// <summary>
            /// Creates a new InputField instance.
            /// </summary>
            /// <param name="cursorColor">The color of the cursor of the input field.</param>
            public InputField(CSColor? cursorColor = null) : base()
            {
                CursorColor = cursorColor ?? Colors.White;
                Paint += new PaintEventHandler((sender, e) => UpdateCursor(e.Graphics));
            }
        }
    }

    /// <summary>
    /// Provides utility methods for ConsoleSharp funtionality.
    /// </summary>
    public static partial class Utils
    {
        /// <summary>
        /// Conversions from hexadecimal conversions to their integer equivalents.
        /// </summary>
        static readonly Dictionary<string, int> Convs = new()
        {
            {"0",0}, {"1",1}, {"2",2}, {"3",3}, {"4",4}, {"5",5}, {"6",6}, {"7",7}, {"8",8}, {"9",9}, {"a",10}, {"b",11}, {"c",12}, {"d",13}, {"e",14}, {"f",15}
        };
        /// <summary>
        /// Stores the names and corresponding data for all font families of the system.
        /// </summary>
        static Dictionary<string, FontFamily>? Families = null;
        /// <summary>
        /// Stores the names and corresponding data for all font styles.
        /// </summary>
        static readonly Dictionary<string, FontStyle> Styles = new()
        {
            {"Regular",FontStyle.Regular}, {"Italic",FontStyle.Italic}, {"Bold",FontStyle.Bold}, {"Strikeout",FontStyle.Strikeout}, {"Underline",FontStyle.Underline}
        };
        /// <summary>
        /// Stores the names and corresponding metadata for all typing effects.
        /// </summary>
        static Dictionary<string, Type>? Effects = null;

        /// <summary>
        /// Regex for reading RGB values from a hexadecimal code.
        /// </summary>
        /// <returns>The reference to the HexRegex.</returns>
        [GeneratedRegex(@"(?<r>[0-9a-f]{2})(?<g>[0-9a-f]{2})(?<b>[0-9a-f]{2})")]
        private static partial Regex HexRegex();

        /// <summary>
        /// Regex for extracting individual lines from strings with embedded styling.
        /// </summary>
        /// <returns>The reference to the LineRegex.</returns>
        [GeneratedRegex(@"(?:(?:\\ln(?<line>.*?)/ln)|(?<line>(?:(?:[^\\])|(?:\\(?!ln)))+))|(?<line>(?<!.)(?!.))")]
        private static partial Regex LineRegex();

        /// <summary>
        /// Regex for extracting individual text blocks and their styling parameters from strings with embedded styling.
        /// </summary>
        /// <returns>The reference to the TextBlockRegex.</returns>
        [GeneratedRegex(@"(?:(?:\\tb(?: *< *(?:tc: *(?:(?:(?:(?<tc_r>[0-9]{1,3})(?:, *(?<tc_g>[0-9]{1,3}))(?:, *(?<tc_b>[0-9]{1,3})))|(?<tc_hex>[0-9a-f]{6}))(?:, *(?<tc_a>[0-9]{1,3}))?)?; *)?(?:bc: *(?:(?:(?:(?<bc_r>[0-9]{1,3}), *(?<bc_g>[0-9]{1,3}), *(?<bc_b>[0-9]{1,3}))|(?<bc_hex>[0-9a-f]{6}))(?:, *(?<bc_a>[0-9]{1,3}))?)?; *)?(?:ft: *(?:(?<fam>[A-Za-z]+)?(?:(?:(?:(?<=ft: *))|(?:(?<!ft: *), *))(?<size>[0-9]+))?(?:(?:(?:(?<=ft: *))|(?:(?<!ft: *), *))\((?<style>[A-Za-z]+(?:, *[A-Za-z]+)?)\))?)?; *)?(?:ef: *(?:(?<ef_name>[A-Za-z]+)(?:, *(?<ef_param>[\w]+))?)?; *)? *>)?(?<body>.*?)/tb)|(?<body>(?:(?:[^\\])|(?:\\(?!tb)))+))|(?<body>(?<!.)(?!.))")]
        private static partial Regex TextBlockRegex();

        /// <summary>
        /// Regex for extracting inidivual font styles from the list of styles found by the TextBlockRegex in a string with embedded styling.
        /// </summary>
        /// <returns>The reference to the StyleRegex.</returns>
        [GeneratedRegex(@"(?<style>[A-Za-z]+)")]
        private static partial Regex StyleRegex();

        /// <summary>
        /// Creates a representation of a string with embedded styling using ConsoleSharp classes.
        /// </summary>
        /// <param name="text">The string to create a representation of.</param>
        /// <returns>A list of Lines representing the string and its styling.</returns>
        public static List<Line> BuildFromString(string text)
        {
            var output = new List<Line>();
            var lines = SplitToLines(text);
            foreach (var line in lines)
            {
                var texts = SplitToTextBlocks(line);
                output.Add(new Line(texts));
            }
            return output;
        }

        /// <summary>
        /// Divides a string into substrings containing individual lines using its embedded styling.
        /// </summary>
        /// <param name="text">The string to be divided.</param>
        /// <returns>A list of strings each containing one line.</returns>
        static List<string> SplitToLines(string text)
        {
            var lines = new List<string>();
            if (LineRegex().IsMatch(text))
            {
                var matches = LineRegex().Matches(text);
                foreach (Match match in matches)
                {
                    lines.Add(match.Groups["line"].Value);
                }
            }
            else lines.Add(text);

            return lines;
        }

        /// <summary>
        /// Divides a line into individual TextBlocks using its embedded styling.
        /// </summary>
        /// <param name="line">The line to be divided.</param>
        /// <returns>A list of TextBlocks representing each section of styling in the string.</returns>
        static List<TextBlock> SplitToTextBlocks(string line)
        {
            var textBlocks = new List<TextBlock>();
            if (TextBlockRegex().IsMatch(line))
            {
                var matches = TextBlockRegex().Matches(line); // Each match represents 1 TextBlock
                foreach (Match match in matches)
                {
                    // Text Color Parameters
                    var tc_r = match.Groups["tc_r"].Success ? match.Groups["tc_r"].Value : null;
                    var tc_g = match.Groups["tc_g"].Success ? match.Groups["tc_g"].Value : null;
                    var tc_b = match.Groups["tc_b"].Success ? match.Groups["tc_b"].Value : null;
                    var tc_hex = match.Groups["tc_hex"].Success ? match.Groups["tc_hex"].Value : null;
                    var tc_a = match.Groups["tc_a"].Success ? match.Groups["tc_a"].Value : null;

                    // Background Color Parameters
                    var bc_r = match.Groups["bc_r"].Success ? match.Groups["bc_r"].Value : null;
                    var bc_g = match.Groups["bc_g"].Success ? match.Groups["bc_g"].Value : null;
                    var bc_b = match.Groups["bc_b"].Success ? match.Groups["bc_b"].Value : null;
                    var bc_hex = match.Groups["bc_hex"].Success ? match.Groups["bc_hex"].Value : null;
                    var bc_a = match.Groups["bc_a"].Success ? match.Groups["bc_a"].Value : null;

                    // Font Parameters
                    var fam = match.Groups["fam"].Success ? match.Groups["fam"].Value : null;
                    var size = match.Groups["size"].Success ? match.Groups["size"].Value : null;
                    var style = match.Groups["style"].Success ? match.Groups["style"].Value : null;

                    // Effect Parameters
                    var ef_name = match.Groups["ef_name"].Success ? match.Groups["ef_name"].Value : null;
                    var ef_param = match.Groups["ef_param"].Success ? match.Groups["ef_param"].Value : null;

                    var body = match.Groups["body"].Value;

                    // Instantiate Text Color
                    int? r;
                    int? g;
                    int? b;
                    int? a = (tc_a != null) ? Convert.ToInt32(tc_a) : null;
                    if (tc_hex != null)
                    {
                        (r, g, b) = HexToRGB(tc_hex);
                    }
                    else
                    {
                        r = (tc_r != null) ? Convert.ToInt32(tc_r) : null;
                        g = (tc_g != null) ? Convert.ToInt32(tc_g) : null;
                        b = (tc_b != null) ? Convert.ToInt32(tc_b) : null;
                    }
                    if ((r, g, b) == (null, null, null)) (r, g, b) = (255, 255, 255);
                    var tc = new CSColor(r, g, b, a);

                    // Instantiate Background Color
                    a = (bc_a != null) ? Convert.ToInt32(bc_a) : null;
                    if (bc_hex != null)
                    {
                        (r, g, b) = HexToRGB(bc_hex);
                    }
                    else
                    {
                        r = (bc_r != null) ? Convert.ToInt32(bc_r) : null;
                        g = (bc_g != null) ? Convert.ToInt32(bc_g) : null;
                        b = (bc_b != null) ? Convert.ToInt32(bc_b) : null;
                    }
                    var bc = new CSColor(r, g, b, a);

                    // Instantiate Font
                    if (Families == null) InitFamilyDict();

                    FontFamily? fontFam = null;
                    if (fam != null && Families.TryGetValue(fam, out FontFamily? value))
                    {
                        fontFam = value;
                    }
                    int? fontSize = (size != null) ? Convert.ToInt32(size) : null;
                    var styles = ExtractStyles(style);
                    var font = new CSFont(fontFam, fontSize, styles);

                    // Instantiate Effect
                    var effect = ExtractEffect(ef_name, ef_param);

                    var block = new TextBlock(body, tc, bc, font, effect);
                    textBlocks.Add(block);
                }
            }
            return textBlocks;
        }

        /// <summary>
        /// Extracts all font styles from a string containing their embedded styling.
        /// </summary>
        /// <param name="text">The string to extract from.</param>
        /// <returns>A list of all font styles found in the string.</returns>
        static List<FontStyle>? ExtractStyles(string? text)
        {
            List<FontStyle>? styles = null;
            if (text != null)
            {
                if (StyleRegex().IsMatch(text))
                {
                    var matches = StyleRegex().Matches(text);
                    foreach (Match match in matches)
                    {
                        var styleName = match.Groups["style"].Value;
                        if (Styles.TryGetValue(styleName, out FontStyle value))
                        {
                            styles ??= [];
                            styles.Add(value);
                        }
                    }
                }
            }
            return styles;
        }

        /// <summary>
        /// Builds an Effect instance based on the given name and parameter.
        /// </summary>
        /// <param name="ef_name">The name of the effect to be built.</param>
        /// <param name="ef_param">The parameter passed to the given effect.</param>
        /// <returns>An instance of the defined effect.</returns>
        static Effect ExtractEffect(string? ef_name, string? ef_param)
        {
            Effect effect = new NoEffect();

            if (Effects == null) InitEffectDict();

            dynamic? param = null;
            if (ef_name != null && Effects.TryGetValue(ef_name, out Type value))
            {
                effect = Activator.CreateInstance(value) as Effect;
                if (value == typeof(TypeWriter))
                {
                    param = (ef_param != null) ? Convert.ToInt32(ef_param) : null;
                    effect.SetParam(param);
                }
            }
            return effect;
        }

        /// <summary>
        /// Adds a new effect subclass to the Effects dictionary.
        /// </summary>
        /// <param name="newEffect">The metadata of the new effect.</param>
        public static void AddEffectSubclass(Type newEffect)
        {
            if (Effects == null) InitEffectDict();
            else Effects.Add(newEffect.Name, newEffect);
        }

        /// <summary>
        /// Initializes the Effects dictionary.
        /// </summary>
        static void InitEffectDict()
        {
            if (Effects == null)
            {
                Effects = [];
                var efSubclasses = GetInheritedClasses(typeof(Effect));
                foreach (var efSubclass in efSubclasses)
                {
                    Effects.Add(efSubclass.Name, efSubclass);
                }
            }
        }

        /// <summary>
        /// Find all classes which inherit from a given class.
        /// </summary>
        /// <param name="baseType">The parent class.</param>
        /// <returns>A list of the metadata of all subclasses of the parent class.</returns>
        static List<Type> GetInheritedClasses(Type baseType)
        {
            return [.. Assembly.GetAssembly(baseType).GetTypes().Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(baseType))];
        }

        /// <summary>
        /// Initilizes the font family dictionary.
        /// </summary>
        static void InitFamilyDict()
        {
            if (Families == null)
            {
                Families = [];
                foreach (var family in FontFamily.Families)
                {
                    Families.Add(family.Name, family);
                }
            }
        }

        /// <summary>
        /// Converts a hexadecimal code into RGB values.
        /// </summary>
        /// <param name="hex">The hexadecimal code to convert.</param>
        /// <returns>The RGB values of the hexadecimal code.</returns>
        public static (int r, int g, int b) HexToRGB(string hex)
        {
            if (HexRegex().IsMatch(hex))
            {
                var match = HexRegex().Match(hex);
                var groups = match.Groups;
                var (rHex, gHex, bHex) = (groups.GetValueOrDefault("r").Value, groups.GetValueOrDefault("g").Value, groups.GetValueOrDefault("b").Value);
                var (r, g, b) = (HexToDec(rHex), HexToDec(gHex), HexToDec(bHex));
                return (r, g, b);
            }
            else return (-1, -1, -1);
        }

        /// <summary>
        /// Converts a number from hexadecimal notation to decimal notation.
        /// </summary>
        /// <param name="hex">The hexadecimal number to convert.</param>
        /// <returns>The decimal equivalent of the hexadecimal number.</returns>
        public static int HexToDec(string hex)
        {
            var chars = ParseString(hex);
            chars.Reverse();
            int dec = 0;
            for (int i = 0; i < chars.Count; i++)
            {
                int val = Convs[chars[i]];
                dec += val * (int)Math.Pow(16, i);
            }
            return dec;
        }

        /// <summary>
        /// Splits a string into its individual characters.
        /// </summary>
        /// <param name="input">The string to be split.</param>
        /// <returns>A list of strings each containing a single character.</returns>
        public static List<string> ParseString(string input)
        {
            List<string> chars = new List<string>();
            foreach (var chara in input.ToCharArray())
            {
                chars.Add(chara.ToString());
            }
            return chars.ToList();
        }
    }

    /// <summary>
    /// Class storing color presets - contains all presets given provided by CSS.
    /// </summary>
    public static class Colors
    {
        public static CSColor AliceBlue { get { return _aliceBlue.Clone(); } }
        private static readonly CSColor _aliceBlue = new CSColor(240, 248, 255);
        public static CSColor AntiqueWhite { get { return _antiqueWhite.Clone(); } }
        private static readonly CSColor _antiqueWhite = new CSColor(250, 235, 215);
        public static CSColor Aqua { get { return _aqua.Clone(); } }
        private static readonly CSColor _aqua = new CSColor(0, 255, 255);
        public static CSColor Aquamarine { get { return _aquamarine.Clone(); } }
        private static readonly CSColor _aquamarine = new CSColor(127, 255, 212);
        public static CSColor Azure { get { return _azure.Clone(); } }
        private static readonly CSColor _azure = new CSColor(240, 255, 255);
        public static CSColor Beige { get { return _beige.Clone(); } }
        private static readonly CSColor _beige = new CSColor(245, 245, 220);
        public static CSColor Bisque { get { return _bisque.Clone(); } }
        private static readonly CSColor _bisque = new CSColor(255, 228, 196);
        public static CSColor Black { get { return _black.Clone(); } }
        private static readonly CSColor _black = new CSColor(0, 0, 0);
        public static CSColor BlanchedAlmond { get { return _blanchedAlmond.Clone(); } }
        private static readonly CSColor _blanchedAlmond = new CSColor(255, 235, 205);
        public static CSColor Blue { get { return _blue.Clone(); } }
        private static readonly CSColor _blue = new CSColor(0, 0, 255);
        public static CSColor BlueViolet { get { return _blueViolet.Clone(); } }
        private static readonly CSColor _blueViolet = new CSColor(138, 43, 226);
        public static CSColor Brown { get { return _brown.Clone(); } }
        private static readonly CSColor _brown = new CSColor(165, 42, 42);
        public static CSColor BurlyWood { get { return _burlyWood.Clone(); } }
        private static readonly CSColor _burlyWood = new CSColor(222, 184, 135);
        public static CSColor CadetBlue { get { return _cadetBlue.Clone(); } }
        private static readonly CSColor _cadetBlue = new CSColor(95, 158, 160);
        public static CSColor Chartreuse { get { return _chartreuse.Clone(); } }
        private static readonly CSColor _chartreuse = new CSColor(127, 255, 0);
        public static CSColor Chocolate { get { return _chocolate.Clone(); } }
        private static readonly CSColor _chocolate = new CSColor(210, 105, 30);
        public static CSColor Coral { get { return _coral.Clone(); } }
        private static readonly CSColor _coral = new CSColor(255, 127, 80);
        public static CSColor CornflowerBlue { get { return _cornflowerBlue.Clone(); } }
        private static readonly CSColor _cornflowerBlue = new CSColor(100, 149, 237);
        public static CSColor Cornsilk { get { return _cornsilk.Clone(); } }
        private static readonly CSColor _cornsilk = new CSColor(255, 248, 220);
        public static CSColor Crimson { get { return _crimson.Clone(); } }
        private static readonly CSColor _crimson = new CSColor(220, 20, 60);
        public static CSColor Cyan { get { return _cyan.Clone(); } }
        private static readonly CSColor _cyan = new CSColor(0, 255, 255);
        public static CSColor DarkBlue { get { return _darkBlue.Clone(); } }
        private static readonly CSColor _darkBlue = new CSColor(0, 0, 139);
        public static CSColor DarkCyan { get { return _darkCyan.Clone(); } }
        private static readonly CSColor _darkCyan = new CSColor(0, 139, 139);
        public static CSColor DarkGoldenRod { get { return _darkGoldenRod.Clone(); } }
        private static readonly CSColor _darkGoldenRod = new CSColor(184, 134, 11);
        public static CSColor DarkGray { get { return _darkGray.Clone(); } }
        private static readonly CSColor _darkGray = new CSColor(169, 169, 169);
        public static CSColor DarkGrey { get { return _darkGrey.Clone(); } }
        private static readonly CSColor _darkGrey = new CSColor(169, 169, 169);
        public static CSColor DarkGreen { get { return _darkGreen.Clone(); } }
        private static readonly CSColor _darkGreen = new CSColor(0, 100, 0);
        public static CSColor DarkKhaki { get { return _darkKhaki.Clone(); } }
        private static readonly CSColor _darkKhaki = new CSColor(189, 183, 107);
        public static CSColor DarkMagenta { get { return _darkMagenta.Clone(); } }
        private static readonly CSColor _darkMagenta = new CSColor(139, 0, 139);
        public static CSColor DarkOliveGreen { get { return _darkOliveGreen.Clone(); } }
        private static readonly CSColor _darkOliveGreen = new CSColor(85, 107, 47);
        public static CSColor DarkOrange { get { return _darkOrange.Clone(); } }
        private static readonly CSColor _darkOrange = new CSColor(255, 140, 0);
        public static CSColor DarkOrchid { get { return _darkOrchid.Clone(); } }
        private static readonly CSColor _darkOrchid = new CSColor(153, 50, 204);
        public static CSColor DarkRed { get { return _darkRed.Clone(); } }
        private static readonly CSColor _darkRed = new CSColor(139, 0, 0);
        public static CSColor DarkSalmon { get { return _darkSalmon.Clone(); } }
        private static readonly CSColor _darkSalmon = new CSColor(233, 150, 122);
        public static CSColor DarkSeaGreen { get { return _darkSeaGreen.Clone(); } }
        private static readonly CSColor _darkSeaGreen = new CSColor(143, 188, 143);
        public static CSColor DarkSlateBlue { get { return _darkSlateBlue.Clone(); } }
        private static readonly CSColor _darkSlateBlue = new CSColor(72, 61, 139);
        public static CSColor DarkSlateGray { get { return _darkSlateGray.Clone(); } }
        private static readonly CSColor _darkSlateGray = new CSColor(47, 79, 79);
        public static CSColor DarkSlateGrey { get { return _darkSlateGrey.Clone(); } }
        private static readonly CSColor _darkSlateGrey = new CSColor(47, 79, 79);
        public static CSColor DarkTurquoise { get { return _darkTurquoise.Clone(); } }
        private static readonly CSColor _darkTurquoise = new CSColor(0, 206, 209);
        public static CSColor DarkViolet { get { return _darkViolet.Clone(); } }
        private static readonly CSColor _darkViolet = new CSColor(148, 0, 211);
        public static CSColor DeepPink { get { return _deepPink.Clone(); } }
        private static readonly CSColor _deepPink = new CSColor(255, 20, 147);
        public static CSColor DeepSkyBlue { get { return _deepSkyBlue.Clone(); } }
        private static readonly CSColor _deepSkyBlue = new CSColor(0, 191, 255);
        public static CSColor DimGray { get { return _dimGray.Clone(); } }
        private static readonly CSColor _dimGray = new CSColor(105, 105, 105);
        public static CSColor DimGrey { get { return _dimGrey.Clone(); } }
        private static readonly CSColor _dimGrey = new CSColor(105, 105, 105);
        public static CSColor DodgerBlue { get { return _dodgerBlue.Clone(); } }
        private static readonly CSColor _dodgerBlue = new CSColor(30, 144, 255);
        public static CSColor FireBrick { get { return _fireBrick.Clone(); } }
        private static readonly CSColor _fireBrick = new CSColor(178, 34, 34);
        public static CSColor FloralWhite { get { return _floralWhite.Clone(); } }
        private static readonly CSColor _floralWhite = new CSColor(255, 250, 240);
        public static CSColor ForestGreen { get { return _forestGreen.Clone(); } }
        private static readonly CSColor _forestGreen = new CSColor(34, 139, 34);
        public static CSColor Fuchsia { get { return _fuchsia.Clone(); } }
        private static readonly CSColor _fuchsia = new CSColor(255, 0, 255);
        public static CSColor Gainsboro { get { return _gainsboro.Clone(); } }
        private static readonly CSColor _gainsboro = new CSColor(220, 220, 220);
        public static CSColor GhostWhite { get { return _ghostWhite.Clone(); } }
        private static readonly CSColor _ghostWhite = new CSColor(248, 248, 255);
        public static CSColor Gold { get { return _gold.Clone(); } }
        private static readonly CSColor _gold = new CSColor(255, 215, 0);
        public static CSColor GoldenRod { get { return _goldenRod.Clone(); } }
        private static readonly CSColor _goldenRod = new CSColor(218, 165, 32);
        public static CSColor Gray { get { return _gray.Clone(); } }
        private static readonly CSColor _gray = new CSColor(128, 128, 128);
        public static CSColor Grey { get { return _grey.Clone(); } }
        private static readonly CSColor _grey = new CSColor(128, 128, 128);
        public static CSColor Green { get { return _green.Clone(); } }
        private static readonly CSColor _green = new CSColor(0, 128, 0);
        public static CSColor GreenYellow { get { return _greenYellow.Clone(); } }
        private static readonly CSColor _greenYellow = new CSColor(173, 255, 47);
        public static CSColor HoneyDew { get { return _honeyDew.Clone(); } }
        private static readonly CSColor _honeyDew = new CSColor(240, 255, 240);
        public static CSColor HotPink { get { return _hotPink.Clone(); } }
        private static readonly CSColor _hotPink = new CSColor(255, 105, 180);
        public static CSColor IndianRed { get { return _indianRed.Clone(); } }
        private static readonly CSColor _indianRed = new CSColor(205, 92, 92);
        public static CSColor Indigo { get { return _indigo.Clone(); } }
        private static readonly CSColor _indigo = new CSColor(75, 0, 130);
        public static CSColor Ivory { get { return _ivory.Clone(); } }
        private static readonly CSColor _ivory = new CSColor(255, 255, 240);
        public static CSColor Khaki { get { return _khaki.Clone(); } }
        private static readonly CSColor _khaki = new CSColor(240, 230, 140);
        public static CSColor Lavender { get { return _lavender.Clone(); } }
        private static readonly CSColor _lavender = new CSColor(230, 230, 250);
        public static CSColor LavenderBlush { get { return _lavenderBlush.Clone(); } }
        private static readonly CSColor _lavenderBlush = new CSColor(255, 240, 245);
        public static CSColor LawnGreen { get { return _lawnGreen.Clone(); } }
        private static readonly CSColor _lawnGreen = new CSColor(124, 252, 0);
        public static CSColor LemonChiffon { get { return _lemonChiffon.Clone(); } }
        private static readonly CSColor _lemonChiffon = new CSColor(255, 250, 205);
        public static CSColor LightBlue { get { return _lightBlue.Clone(); } }
        private static readonly CSColor _lightBlue = new CSColor(173, 216, 230);
        public static CSColor LightCoral { get { return _lightCoral.Clone(); } }
        private static readonly CSColor _lightCoral = new CSColor(240, 128, 128);
        public static CSColor LightCyan { get { return _lightCyan.Clone(); } }
        private static readonly CSColor _lightCyan = new CSColor(224, 255, 255);
        public static CSColor LightGoldenRodYellow { get { return _lightGoldenRodYellow.Clone(); } }
        private static readonly CSColor _lightGoldenRodYellow = new CSColor(250, 250, 210);
        public static CSColor LightGray { get { return _lightGray.Clone(); } }
        private static readonly CSColor _lightGray = new CSColor(211, 211, 211);
        public static CSColor LightGrey { get { return _lightGrey.Clone(); } }
        private static readonly CSColor _lightGrey = new CSColor(211, 211, 211);
        public static CSColor LightGreen { get { return _lightGreen.Clone(); } }
        private static readonly CSColor _lightGreen = new CSColor(144, 238, 144);
        public static CSColor LightPink { get { return _lightPink.Clone(); } }
        private static readonly CSColor _lightPink = new CSColor(255, 182, 193);
        public static CSColor LightSalmon { get { return _lightSalmon.Clone(); } }
        private static readonly CSColor _lightSalmon = new CSColor(255, 160, 122);
        public static CSColor LightSeaGreen { get { return _lightSeaGreen.Clone(); } }
        private static readonly CSColor _lightSeaGreen = new CSColor(32, 178, 170);
        public static CSColor LightSkyBlue { get { return _lightSkyBlue.Clone(); } }
        private static readonly CSColor _lightSkyBlue = new CSColor(135, 206, 250);
        public static CSColor LightSlateGray { get { return _lightSlateGray.Clone(); } }
        private static readonly CSColor _lightSlateGray = new CSColor(119, 136, 153);
        public static CSColor LightSlateGrey { get { return _lightSlateGrey.Clone(); } }
        private static readonly CSColor _lightSlateGrey = new CSColor(119, 136, 153);
        public static CSColor LightSteelBlue { get { return _lightSteelBlue.Clone(); } }
        private static readonly CSColor _lightSteelBlue = new CSColor(176, 196, 222);
        public static CSColor LightYellow { get { return _lightYellow.Clone(); } }
        private static readonly CSColor _lightYellow = new CSColor(255, 255, 224);
        public static CSColor Lime { get { return _lime.Clone(); } }
        private static readonly CSColor _lime = new CSColor(0, 255, 0);
        public static CSColor LimeGreen { get { return _limeGreen.Clone(); } }
        private static readonly CSColor _limeGreen = new CSColor(50, 205, 50);
        public static CSColor Linen { get { return _linen.Clone(); } }
        private static readonly CSColor _linen = new CSColor(250, 240, 230);
        public static CSColor Magenta { get { return _magenta.Clone(); } }
        private static readonly CSColor _magenta = new CSColor(255, 0, 255);
        public static CSColor Maroon { get { return _maroon.Clone(); } }
        private static readonly CSColor _maroon = new CSColor(128, 0, 0);
        public static CSColor MediumAquaMarine { get { return _mediumAquaMarine.Clone(); } }
        private static readonly CSColor _mediumAquaMarine = new CSColor(102, 205, 170);
        public static CSColor MediumBlue { get { return _mediumBlue.Clone(); } }
        private static readonly CSColor _mediumBlue = new CSColor(0, 0, 205);
        public static CSColor MediumOrchid { get { return _mediumOrchid.Clone(); } }
        private static readonly CSColor _mediumOrchid = new CSColor(186, 85, 211);
        public static CSColor MediumPurple { get { return _mediumPurple.Clone(); } }
        private static readonly CSColor _mediumPurple = new CSColor(147, 112, 219);
        public static CSColor MediumSeaGreen { get { return _mediumSeaGreen.Clone(); } }
        private static readonly CSColor _mediumSeaGreen = new CSColor(60, 179, 113);
        public static CSColor MediumSlateBlue { get { return _mediumSlateBlue.Clone(); } }
        private static readonly CSColor _mediumSlateBlue = new CSColor(123, 104, 238);
        public static CSColor MediumSpringGreen { get { return _mediumSpringGreen.Clone(); } }
        private static readonly CSColor _mediumSpringGreen = new CSColor(0, 250, 154);
        public static CSColor MediumTurquoise { get { return _mediumTurquoise.Clone(); } }
        private static readonly CSColor _mediumTurquoise = new CSColor(72, 209, 204);
        public static CSColor MediumVioletRed { get { return _mediumVioletRed.Clone(); } }
        private static readonly CSColor _mediumVioletRed = new CSColor(199, 21, 133);
        public static CSColor MidnightBlue { get { return _midnightBlue.Clone(); } }
        private static readonly CSColor _midnightBlue = new CSColor(25, 25, 112);
        public static CSColor MintCream { get { return _mintCream.Clone(); } }
        private static readonly CSColor _mintCream = new CSColor(245, 255, 250);
        public static CSColor MistyRose { get { return _mistyRose.Clone(); } }
        private static readonly CSColor _mistyRose = new CSColor(255, 228, 225);
        public static CSColor Moccasin { get { return _moccasin.Clone(); } }
        private static readonly CSColor _moccasin = new CSColor(255, 228, 181);
        public static CSColor NavajoWhite { get { return _navajoWhite.Clone(); } }
        private static readonly CSColor _navajoWhite = new CSColor(255, 222, 173);
        public static CSColor Navy { get { return _navy.Clone(); } }
        private static readonly CSColor _navy = new CSColor(0, 0, 128);
        public static CSColor OldLace { get { return _oldLace.Clone(); } }
        private static readonly CSColor _oldLace = new CSColor(253, 245, 230);
        public static CSColor Olive { get { return _olive.Clone(); } }
        private static readonly CSColor _olive = new CSColor(128, 128, 0);
        public static CSColor OliveDrab { get { return _oliveDrab.Clone(); } }
        private static readonly CSColor _oliveDrab = new CSColor(107, 142, 35);
        public static CSColor Orange { get { return _orange.Clone(); } }
        private static readonly CSColor _orange = new CSColor(255, 165, 0);
        public static CSColor OrangeRed { get { return _orangeRed.Clone(); } }
        private static readonly CSColor _orangeRed = new CSColor(255, 69, 0);
        public static CSColor Orchid { get { return _orchid.Clone(); } }
        private static readonly CSColor _orchid = new CSColor(218, 112, 214);
        public static CSColor PaleGoldenRod { get { return _paleGoldenRod.Clone(); } }
        private static readonly CSColor _paleGoldenRod = new CSColor(238, 232, 170);
        public static CSColor PaleGreen { get { return _paleGreen.Clone(); } }
        private static readonly CSColor _paleGreen = new CSColor(152, 251, 152);
        public static CSColor PaleTurquoise { get { return _paleTurquoise.Clone(); } }
        private static readonly CSColor _paleTurquoise = new CSColor(175, 238, 238);
        public static CSColor PaleVioletRed { get { return _paleVioletRed.Clone(); } }
        private static readonly CSColor _paleVioletRed = new CSColor(219, 112, 147);
        public static CSColor PapayaWhip { get { return _papayaWhip.Clone(); } }
        private static readonly CSColor _papayaWhip = new CSColor(255, 239, 213);
        public static CSColor PeachPuff { get { return _peachPuff.Clone(); } }
        private static readonly CSColor _peachPuff = new CSColor(255, 218, 185);
        public static CSColor Peru { get { return _peru.Clone(); } }
        private static readonly CSColor _peru = new CSColor(205, 133, 63);
        public static CSColor Pink { get { return _pink.Clone(); } }
        private static readonly CSColor _pink = new CSColor(255, 192, 203);
        public static CSColor Plum { get { return _plum.Clone(); } }
        private static readonly CSColor _plum = new CSColor(221, 160, 221);
        public static CSColor PowderBlue { get { return _powderBlue.Clone(); } }
        private static readonly CSColor _powderBlue = new CSColor(176, 224, 230);
        public static CSColor Purple { get { return _purple.Clone(); } }
        private static readonly CSColor _purple = new CSColor(128, 0, 128);
        public static CSColor RebeccaPurple { get { return _rebeccaPurple.Clone(); } }
        private static readonly CSColor _rebeccaPurple = new CSColor(102, 51, 153);
        public static CSColor Red { get { return _red.Clone(); } }
        private static readonly CSColor _red = new CSColor(255, 0, 0);
        public static CSColor RosyBrown { get { return _rosyBrown.Clone(); } }
        private static readonly CSColor _rosyBrown = new CSColor(188, 143, 143);
        public static CSColor RoyalBlue { get { return _royalBlue.Clone(); } }
        private static readonly CSColor _royalBlue = new CSColor(65, 105, 225);
        public static CSColor SaddleBrown { get { return _saddleBrown.Clone(); } }
        private static readonly CSColor _saddleBrown = new CSColor(139, 69, 19);
        public static CSColor Salmon { get { return _salmon.Clone(); } }
        private static readonly CSColor _salmon = new CSColor(250, 128, 114);
        public static CSColor SandyBrown { get { return _sandyBrown.Clone(); } }
        private static readonly CSColor _sandyBrown = new CSColor(244, 164, 96);
        public static CSColor SeaGreen { get { return _seaGreen.Clone(); } }
        private static readonly CSColor _seaGreen = new CSColor(46, 139, 87);
        public static CSColor SeaShell { get { return _seaShell.Clone(); } }
        private static readonly CSColor _seaShell = new CSColor(255, 245, 238);
        public static CSColor Sienna { get { return _sienna.Clone(); } }
        private static readonly CSColor _sienna = new CSColor(160, 82, 45);
        public static CSColor Silver { get { return _silver.Clone(); } }
        private static readonly CSColor _silver = new CSColor(192, 192, 192);
        public static CSColor SkyBlue { get { return _skyBlue.Clone(); } }
        private static readonly CSColor _skyBlue = new CSColor(135, 206, 235);
        public static CSColor SlateBlue { get { return _slateBlue.Clone(); } }
        private static readonly CSColor _slateBlue = new CSColor(106, 90, 205);
        public static CSColor SlateGray { get { return _slateGray.Clone(); } }
        private static readonly CSColor _slateGray = new CSColor(112, 128, 144);
        public static CSColor SlateGrey { get { return _slateGrey.Clone(); } }
        private static readonly CSColor _slateGrey = new CSColor(112, 128, 144);
        public static CSColor Snow { get { return _snow.Clone(); } }
        private static readonly CSColor _snow = new CSColor(255, 250, 250);
        public static CSColor SpringGreen { get { return _springGreen.Clone(); } }
        private static readonly CSColor _springGreen = new CSColor(0, 255, 127);
        public static CSColor SteelBlue { get { return _steelBlue.Clone(); } }
        private static readonly CSColor _steelBlue = new CSColor(70, 130, 180);
        public static CSColor Tan { get { return _tan.Clone(); } }
        private static readonly CSColor _tan = new CSColor(210, 180, 140);
        public static CSColor Teal { get { return _teal.Clone(); } }
        private static readonly CSColor _teal = new CSColor(0, 128, 128);
        public static CSColor Thistle { get { return _thistle.Clone(); } }
        private static readonly CSColor _thistle = new CSColor(216, 191, 216);
        public static CSColor Tomato { get { return _tomato.Clone(); } }
        private static readonly CSColor _tomato = new CSColor(255, 99, 71);
        public static CSColor Turquoise { get { return _turquoise.Clone(); } }
        private static readonly CSColor _turquoise = new CSColor(64, 224, 208);
        public static CSColor Violet { get { return _violet.Clone(); } }
        private static readonly CSColor _violet = new CSColor(238, 130, 238);
        public static CSColor Wheat { get { return _wheat.Clone(); } }
        private static readonly CSColor _wheat = new CSColor(245, 222, 179);
        public static CSColor White { get { return _white.Clone(); } }
        private static readonly CSColor _white = new CSColor(255, 255, 255);
        public static CSColor WhiteSmoke { get { return _whiteSmoke.Clone(); } }
        private static readonly CSColor _whiteSmoke = new CSColor(245, 245, 245);
        public static CSColor Yellow { get { return _yellow.Clone(); } }
        private static readonly CSColor _yellow = new CSColor(255, 255, 0);
        public static CSColor YellowGreen { get { return _yellowGreen.Clone(); } }
        private static readonly CSColor _yellowGreen = new CSColor(154, 205, 50);
    }
}