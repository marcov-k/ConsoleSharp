using ConsoleSharp;

var display = new Display(dimensions: [1920, 1080]);
List<Line> lines = [new Line(new TextBlock(text: "Hello World", effect: new TypeWriter(delay: 100))), 
    new Line(new TextBlock(text: "Goodbye World", textColor: new CSColor(0, 255, 0), effect: new TypeWriter(delay: 100)))];
display.Print(lines);
display.Print();
display.Print(lines);

namespace ConsoleSharp
{
    using System.Windows.Forms;

    public class Display
    {
        public static int[] DefaultDims { get; set; } = [500, 500];
        public Window Window { get; private set; } = new Window();
        readonly Thread UIThread;

        public void Print(List<Line> lines)
        {
            foreach (var line in lines)
            {
                line.PrintText(this);
            }
        }

        public void Print(Line line)
        {
            line.PrintText(this);
        }

        public void Print(TextBlock text)
        {
            text.PrintText(this);
        }

        public void Print()
        {
            int[]? pos = null;
            if (Window.Labels.Count > 0)
            {
                var prevField = Window.Labels.Last();
                pos = [0, prevField.Location.Y + prevField.Height];
            }
            AddLabel(pos);
        }

        public Label AddLabel(int[]? pos = null)
        {
            var label = new Label();
            label.AutoSize = true;
            if (pos != null)
            {
                label.Location = new Point(pos[0], pos[1]);
            }
            Window.Invoke(() =>
            {
                Window.Labels.Add(label);
                Window.Controls.Add(label);
            });
            return Window.Labels.Last();
        }

        public Display(string name = "New Display", int[]? dimensions = null, int[]? position = null, CSColor? bgColor = null)
        {
            if (dimensions != null && dimensions.Length != 2)
            {
                throw new ArgumentException("Invalid display dimensions.");
            }
            if (position != null && position.Length != 2)
            {
                throw new ArgumentException("Invalid display position.");
            }
            var formReady = new AutoResetEvent(false);
            int[] dims = dimensions ?? DefaultDims;
            int[] pos = position ?? [Screen.PrimaryScreen.Bounds.Width / 2 - dims[0] / 2, Screen.PrimaryScreen.Bounds.Height / 2 - dims[1] / 2];
            bgColor = bgColor ?? new CSColor();

            UIThread = new Thread(() =>
            {
                Window = new Window();
                Window.HandleCreated += (_, __) => formReady.Set();
                Window.StartPosition = FormStartPosition.Manual;
                Window.AutoScroll = true;
                Window.Text = name;
                Window.BackColor = bgColor.ColorData;
                Utils.SetDims(Window, dims);
                Utils.SetPos(Window, pos);
                Application.Run(Window);
            });

            UIThread.SetApartmentState(ApartmentState.STA);
            UIThread.Start();
            formReady.WaitOne();
        }
    }

    public class Line
    {
        public List<TextBlock> TextBlocks { get; set; } = new List<TextBlock>();

        public void PrintText(Display display)
        {
            int[]? pos = null;
            if (display.Window.Labels.Count > 0)
            {
                var prevField = display.Window.Labels.Last();
                pos = [0, prevField.Location.Y + prevField.Height];
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

        public Line() { }

        public Line(TextBlock text)
        {
            TextBlocks.Add(text);
        }

        public Line(List<TextBlock> textBlocks)
        {
            TextBlocks.AddRange(textBlocks);
        }
    }

    public class TextBlock
    {
        public static Effect DefaultEffect { get; set; } = new NoEffect();
        public string Text { get; set; } = "";
        public CSColor TextColor { get; set; } = new CSColor(255, 255, 255);
        public CSColor BGColor { get; set; } = new CSColor();
        public Effect Effect { get; set; }

        public void PrintText(Display display, Label? field = null)
        {
            if (field == null)
            {
                int[]? pos = null;
                if (display.Window.Labels.Count > 0)
                {
                    var prevField = display.Window.Labels.Last();
                    pos = [prevField.Location.X + prevField.Width, prevField.Location.Y];
                }
                field = display.AddLabel(pos);
            }
            field.BeginInvoke(() =>
            {
                field.BackColor = BGColor.ColorData;
                field.ForeColor = TextColor.ColorData;
            });
            Effect.PrintEffect(Text, field);
        }

        public TextBlock(string? text = null, CSColor? textColor = null, CSColor? bgColor = null, Effect? effect = null)
        {
            Text = text ?? Text;
            TextColor = textColor ?? TextColor;
            BGColor = bgColor ?? BGColor;
            Effect = effect ?? DefaultEffect;
        }
    }

    public class CSColor
    {
        public Color ColorData { get; private set; }

        public CSColor(int r = 0, int g = 0, int b = 0, int a = 255)
        {
            ColorData = Color.FromArgb(a, r, g, b);
        }
    }

    public class Effect
    {
        public virtual void PrintEffect(string text, Label field)
        {
            throw new NotImplementedException();
        }
    }

    public class NoEffect : Effect
    {
        public override void PrintEffect(string text, Label field)
        {
            field.BeginInvoke(() => field.Text += text);
        }
    }

    public class TypeWriter : Effect
    {
        public int Delay { get; set; }

        public override void PrintEffect(string text, Label field)
        {
            var chars = Utils.ParseString(text);
            foreach (var chara in chars)
            {
                field.BeginInvoke(() => field.Text += chara);
                Thread.Sleep(Delay);
            }
        }

        public TypeWriter(int delay = 500)
        {
            Delay = delay;
        }
    }

    public class Window : Form
    {
        public List<Label> Labels { get; private set; } = new List<Label>();

        public Window() { }
    }

    public static class Utils
    {
        public static void SetDims(Form window, int[] dims)
        {
            window.Size = new Size(dims[0], dims[1]);
        }

        public static void SetPos(Form window, int[] pos)
        {
            window.Location = new Point(pos[0], pos[1]);
        }

        public static List<string> ParseString(string input)
        {
            List<string> chars = new List<string>();
            foreach (var chara in input.ToCharArray())
            {
                chars.Add(chara.ToString());
            }
            return chars;
        }
    }
}