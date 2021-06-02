using System.Windows.Controls;
using System.Windows.Input;

namespace SprayPaintImage
{
    public abstract class DrawStrokeBase
    {
        public abstract void OnMouseDown(InkCanvas inkCanvas, MouseButtonEventArgs e);

        public abstract void OnMouseMove(InkCanvas inkCanvas, MouseEventArgs e);

        public StrokeBase StrokeResult { get; set; }

    }
}