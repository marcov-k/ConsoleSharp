using ConsoleSharp;

var display = new Display(dimensions: new Size(1920, 1080));
display.Print(@"\ln\tb<tc: 255, 0, 0, 255; bc: 0, 0, 255, 255; ft: 100, (Bold); ef: TypeWriter, 100;>Embedded styling test.../tb/ln");

namespace ConsoleSharp
{
    using Microsoft.VisualBasic.Devices;
    using System.Text.RegularExpressions;
    using System.Windows.Forms;
    using System.Reflection;

    public class Display
    {
        public static Size DefaultDims { get; set; } = new Size(500, 500);
        public Window Window { get; private set; } = new Window();
        readonly Thread UIThread;

        public void Print(List<Line> lines)
        {
            foreach (var line in lines)
            {
                line.PrintText(display: this);
            }
        }

        public void Print(Line line)
        {
            line.PrintText(display: this);
        }

        public void Print(List<TextBlock> texts)
        {
            foreach (var text in texts)
            {
                text.PrintText(display: this);
            }
        }

        public void Print(TextBlock text)
        {
            text.PrintText(display: this);
        }

        public void Print(string text)
        {
            var lines = Utils.BuildFromString(text);
            foreach (var line in lines)
            {
                line.PrintText(display: this);
            }
        }

        public void Print(int? fontSize = null)
        {
            Point? pos = null;
            if (Window.Labels.Count > 0)
            {
                var prevField = Window.Labels.Last();
                pos = new Point(0, prevField.Location.Y + prevField.Height);
            }
            AddLabel(pos, fontSize);
        }

        public void PrintDivider(CSFont? font = null, CSColor? textColor = null, CSColor? bgColor = null, Effect? effect = null)
        {
            font = font ?? new CSFont();
            textColor = textColor ?? Colors.White;
            bgColor = bgColor ?? Colors.Black;
            effect = effect ?? new NoEffect();

            var sizingLabel = new Label();
            sizingLabel.AutoSize = true;
            sizingLabel.Font = font.FontData;
            sizingLabel.Text += "-";
            sizingLabel.AutoSize = false;
            sizingLabel.Width = Window.Width;

            string fillString = "";
            bool maxxed = false;
            while (!maxxed)
            {
                fillString += "-";
                var textSize = TextRenderer.MeasureText(text: fillString, font: font.FontData);
                if (textSize.Width > sizingLabel.Width)
                {
                    fillString.Remove(fillString.Length - 1);
                    maxxed = true;
                }
            }

            Line fillLine = new Line(new TextBlock(text: fillString, textColor: textColor, bgColor: bgColor, font: font, effect: effect));
            Print(fillLine);
        }

        public void PlayWAV(string file)
        {
            Window.BeginInvoke(() => Window.Audio.Play(file));
        }

        public Label AddLabel(Point? pos = null, int? fontSize = null)
        {
            CSFont font = new CSFont(fontSize: fontSize, fontStyle: null);
            var label = new Label();
            label.AutoSize = true;
            if (pos != null)
            {
                label.Location = pos.Value;
            }
            label.Font = font.FontData;
            Window.Invoke(() =>
            {
                Window.Labels.Add(label);
                Window.Controls.Add(label);
            });
            return Window.Labels.Last();
        }

        public Display(string name = "New Display", Size? dimensions = null, Point? position = null, CSColor? bgColor = null)
        {
            var formReady = new AutoResetEvent(false);
            Size dims = dimensions ?? DefaultDims;
            Point pos = position ?? new Point(Screen.PrimaryScreen.Bounds.Width / 2 - dims.Width / 2, Screen.PrimaryScreen.Bounds.Height / 2 - dims.Height / 2);
            bgColor = bgColor ?? Colors.Black;

            UIThread = new Thread(() =>
            {
                Window = new Window();
                Window.HandleCreated += (_, __) => formReady.Set();
                Window.StartPosition = FormStartPosition.Manual;
                Window.AutoScroll = true;
                Window.Text = name;
                Window.BackColor = bgColor.ColorData;
                Window.Size = dims;
                Window.Location = pos;
                Application.Run(Window);
            });

            UIThread.SetApartmentState(ApartmentState.STA);
            UIThread.Start();
            formReady.WaitOne();
        }
    }

    public class TextCont
    {
        public virtual void PrintText(Display display, Label? field = null)
        {
            throw new NotImplementedException();
        }
    }

    public class Line : TextCont
    {
        public List<TextBlock> TextBlocks { get; set; } = new List<TextBlock>();

        public override void PrintText(Display display, Label? field = null)
        {
            Point? pos = null;
            if (display.Window.Labels.Count > 0)
            {
                var prevField = display.Window.Labels.Last();
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

    public class TextBlock : TextCont
    {
        public static Effect DefaultEffect { get; set; } = new NoEffect();
        public string Text { get; set; } = "";
        public CSColor TextColor { get; set; } = Colors.White;
        public CSColor BGColor { get; set; } = Colors.Black;
        public CSFont Font { get; set; } = new CSFont();
        public Effect Effect { get; set; }

        public override void PrintText(Display display, Label? field = null)
        {
            if (field == null)
            {
                Point? pos = null;
                if (display.Window.Labels.Count > 0)
                {
                    var prevField = display.Window.Labels.Last();
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

        public TextBlock(string? text = null, CSColor? textColor = null, CSColor? bgColor = null, CSFont? font = null, Effect? effect = null)
        {
            Text = text ?? Text;
            TextColor = textColor ?? TextColor;
            BGColor = bgColor ?? BGColor;
            Font = font ?? Font;
            Effect = effect ?? DefaultEffect;
        }
    }

    public class CSColor
    {
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

        private int _r = 0;
        private int _g = 0;
        private int _b = 0;
        private int _a = 0;

        public Color ColorData { get; private set; }

        void UpdateColorData()
        {
            ColorData = Color.FromArgb(_a, _r, _g, _b);
        }

        public CSColor Clone()
        {
            return new CSColor(_r, _g, _b, _a);
        }

        public CSColor()
        {
            (R, G, B, A) = (0, 0, 0, 255);
        }

        public CSColor(int? r = null, int? g = null, int? b = null, int? a = null)
        {
            R = (r != null) ? r.Value : 0;
            G = (g != null) ? g.Value : 0;
            B = (b != null) ? b.Value : 0;
            A = (a != null) ? a.Value : 255;
        }

        public CSColor(string? hex = null, int? a = null)
        {
            hex ??= "000000";
            (R, G, B) = Utils.HexToRGB(hex);
            A = (a != null) ? a.Value : 255;
        }
    }

    public class CSFont
    {
        public static FontFamily DefaultFontFamily { get; set; } = FontFamily.GenericSansSerif;
        public static int DefaultFontSize { get; set; } = 12;
        public static FontStyle DefaultFontStyle { get; set; } = FontStyle.Regular;
        public Font FontData { get; private set; }

        public CSFont()
        {
            FontData = new Font(DefaultFontFamily, DefaultFontSize, DefaultFontStyle);
        }

        public CSFont(FontFamily? fontFamily = null, int? fontSize = null)
        {
            FontFamily family = fontFamily ?? DefaultFontFamily;
            int size = fontSize ?? DefaultFontSize;
            FontData = new Font(family, size);
        }

        public CSFont(FontFamily? fontFamily = null, int? fontSize = null, FontStyle? fontStyle = null) : this(fontFamily, fontSize)
        {
            FontStyle style = fontStyle ?? DefaultFontStyle;
            FontData = new Font(FontData.FontFamily, FontData.SizeInPoints, style);
        }

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

    public class Effect
    {
        public Type? ParamType { get; private set; } = null;

        public virtual void PrintEffect(string text, Label field)
        {
            throw new NotImplementedException();
        }

        public virtual void SetParam(dynamic newParam)
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
        private readonly int DefaultDelay = 200;
        public new Type? ParamType { get; private set; }

        public override void PrintEffect(string text, Label field)
        {
            var chars = Utils.ParseString(text);
            foreach (var chara in chars)
            {
                field.BeginInvoke(() => field.Text += chara);
                Thread.Sleep(Delay);
            }
        }

        public override void SetParam(dynamic newParam)
        {
            Delay = newParam;
        }

        public TypeWriter()
        {
            Delay = DefaultDelay;
            ParamType = Delay.GetType();
        }

        public TypeWriter(int delay = 500)
        {
            Delay = delay;
            ParamType = Delay.GetType();
        }
    }

    public class Window : Form
    {
        public List<Label> Labels { get; private set; } = new List<Label>();
        public Audio Audio { get; private set; } = new Audio();
    }

    public static partial class Utils
    {
        // String to int conversions for hexadecimal to decimal conversion.
        static readonly Dictionary<string, int> Convs = new()
        {
            {"0",0}, {"1",1}, {"2",2}, {"3",3}, {"4",4}, {"5",5}, {"6",6}, {"7",7}, {"8",8}, {"9",9}, {"a",10}, {"b",11}, {"c",12}, {"d",13}, {"e",14}, {"f",15}
        };

        static Dictionary<string, FontFamily>? Families = null;

        static readonly Dictionary<string, FontStyle> Styles = new()
        {
            {"Regular",FontStyle.Regular}, {"Italic",FontStyle.Italic}, {"Bold",FontStyle.Bold}, {"Strikeout",FontStyle.Strikeout}, {"Underline",FontStyle.Underline}
        };

        static Dictionary<string, Type>? Effects = null;

        // Regex's for reading of embedded styling in strings plus other utilities.
        [GeneratedRegex(@"(?<r>[0-9a-f]{2})(?<g>[0-9a-f]{2})(?<b>[0-9a-f]{2})")]
        private static partial Regex HexRegex();

        [GeneratedRegex(@"(?:(?:\\ln(?<line>.*?)/ln)|(?<line>(?:(?:[^\\])|(?:\\(?!ln)))+))|(?<line>(?<!.)(?!.))")]
        private static partial Regex LineRegex();

        [GeneratedRegex(@"(?:(?:\\tb(?: *< *(?:tc: *(?:(?:(?:(?<tc_r>[0-9]{1,3})(?:, *(?<tc_g>[0-9]{1,3}))(?:, *(?<tc_b>[0-9]{1,3})))|(?<tc_hex>[0-9a-f]{6}))(?:, *(?<tc_a>[0-9]{1,3}))?)?; *)?(?:bc: *(?:(?:(?:(?<bc_r>[0-9]{1,3}), *(?<bc_g>[0-9]{1,3}), *(?<bc_b>[0-9]{1,3}))|(?<bc_hex>[0-9a-f]{6}))(?:, *(?<bc_a>[0-9]{1,3}))?)?; *)?(?:ft: *(?:(?<fam>[A-Za-z]+)?(?:(?:(?:(?<=ft: *))|(?:(?<!ft: *), *))(?<size>[0-9]+))?(?:(?:(?:(?<=ft: *))|(?:(?<!ft: *), *))\((?<style>[A-Za-z]+(?:, *[A-Za-z]+)?)\))?)?; *)?(?:ef: *(?:(?<ef_name>[A-Za-z]+)(?:, *(?<ef_param>[\w]+))?)?; *)? *>)?(?<body>.*?)/tb)|(?<body>(?:(?:[^\\])|(?:\\(?!tb)))+))|(?<body>(?<!.)(?!.))")]
        private static partial Regex TextBlockRegex();

        [GeneratedRegex(@"(?<style>[A-Za-z]+)")]
        private static partial Regex StyleRegex();

        public static List<TextCont> BuildFromString(string text)
        {
            var output = new List<TextCont>();
            var lines = SplitToLines(text);
            foreach (var line in lines)
            {
                var texts = SplitToTextBlocks(line);
                output.Add(new Line(texts));
            }
            return output;
        }

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
                    if (Families == null)
                    {
                        Families = [];
                        foreach (var family in FontFamily.Families)
                        {
                            Families.Add(family.Name, family);
                        }
                    }

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

        static Effect ExtractEffect(string? ef_name, string? ef_param)
        {
            Effect effect = new NoEffect();

            if (Effects == null)
            {
                Effects = [];
                var efSubclasses = GetInheritedClasses(typeof(Effect));
                foreach (var efSubclass in efSubclasses)
                {
                    Effects.Add(efSubclass.Name, efSubclass);
                }
            }

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

        static List<Type> GetInheritedClasses(Type baseType)
        {
            return [.. Assembly.GetAssembly(baseType).GetTypes().Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(baseType))];
        }

        public static void AddEffectSubclass(Type newEffect)
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
            else Effects.Add(newEffect.Name, newEffect);
        }
        
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

        public static List<string> ParseString(string input)
        {
            List<string> chars = new List<string>();
            foreach (var chara in input.ToCharArray())
            {
                chars.Add(chara.ToString());
            }
            return chars.ToList();
        }

        private class TBDataCont
        {
            List<TBStringData> Data = new List<TBStringData>();

            public TBStringData? GetData(int index)
            {
                var data = Data.Find((x) => x.Index == index);
                return data;
            }

            public int GetMaxIndex()
            {
                var max = -1;
                foreach (var tb in Data)
                {
                    if (tb.Index > max) max = tb.Index;
                }
                return max;
            }

            public void AddData(TBStringData data)
            {
                Data.Add(data);
            }

            public void AddData(List<TBStringData> data)
            {
                Data.AddRange(data);
            }

            public TBDataCont(List<TBStringData> data)
            {
                Data.AddRange(data);
            }
        }

        private class TBStringData(int index, string head, string body)
        {
            public int Index { get; private set; } = index;
            public string Head { get; private set; } = head;
            public string Body { get; private set; } = body;
        }
    }

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