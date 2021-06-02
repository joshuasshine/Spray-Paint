using System.Reflection;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace SprayPaintImage
{
    public class StrokeBase : Stroke
    {

        protected PathGeometry pathGeomery = null;

        public StrokeBase(StylusPointCollection pts, DrawingAttributes da)
            : base(pts, da)
        {
            this.StylusPoints = pts;
            this.DrawingAttributes = da;
        }

        public StrokeBase()
            : base(null, null)
        {

        }

        public bool IsSelected
        {
            get
            {
                PropertyInfo property = typeof(Stroke).GetProperty("IsSelected",
                   System.Reflection.BindingFlags.Instance |
                   System.Reflection.BindingFlags.GetProperty |
                   System.Reflection.BindingFlags.NonPublic);
                return (bool)property.GetValue(this, null);
            }
        }
    }
}