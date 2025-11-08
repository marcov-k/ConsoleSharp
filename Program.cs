using ConsoleSharp;

var display = new Display();

namespace ConsoleSharp
{
    using System.Windows.Forms;

    public class Display
    {
        public Display()
        {
            Form window = new Form();
            window.ShowDialog();
            while (window.Created)
            {

            }
        }
    }
}