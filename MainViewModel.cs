using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Resources;
using static System.Net.Mime.MediaTypeNames;

namespace SprayPaintImage
{
    class MainViewModel: INotifyPropertyChanged
    {
        #region Member

        public InkCanvas inkCanvas { get; set; }     //InkCanvas
        public DrawStrokeBase curDraw { get; set; }  //current draw stroke

        private StrokeCollection lstStrokeClipBoard; //clipboard of strokes

        private DoCommandStack doCmdStack;           //redo undo command stack

        private int editingOperationCount;           //redo undo command count

        private bool isDraw;                         // is drawing stroke

       

        private Brush foreground;

        public Brush Foreground
        {
            get { return foreground; }
            set
            {
                var old_foreground = foreground;

                foreground = value;
                var color = ((SolidColorBrush)foreground).Color;
                inkCanvas.DefaultDrawingAttributes.Color = color;
                var lstStrokes = inkCanvas.GetSelectedStrokes();

                if (lstStrokes.Count > 0)
                {
                    foreach (var stroke in lstStrokes)
                    {
                        stroke.DrawingAttributes.Color = color;
                    }

                    editingOperationCount++;
                    CommandItem item = new SelectionColorOrWidthCI(doCmdStack, lstStrokes, old_foreground, foreground,
                        Background, Background, StrokeWidth, StrokeWidth, editingOperationCount);
                    doCmdStack.Enqueue(item);
                }
                OnPropertyChanged("Foreground");
            }
        }

        private Brush background;

        public Brush Background
        {
            get { return background; }
            set
            {
                var old_background = background;

                background = value;

                inkCanvas.DefaultDrawingAttributes.AddPropertyData(new Guid("6F03AB24-31E5-4152-BF30-7E0F61FD40F7"), background.ToString());

                var lstStrokes = inkCanvas.GetSelectedStrokes();

                if (lstStrokes.Count > 0)
                {
                    foreach (var stroke in lstStrokes)
                    {
                        //get DrawingAttributes and set DrawingAttributes to trigger AttributesChanged event
                        var attr = stroke.DrawingAttributes;
                        attr.AddPropertyData(new Guid("6F03AB24-31E5-4152-BF30-7E0F61FD40F7"), background.ToString());

                        stroke.DrawingAttributes = attr;
                    }
                    editingOperationCount++;
                    CommandItem item = new SelectionColorOrWidthCI(doCmdStack, lstStrokes, Foreground, Foreground,
                       old_background, background, StrokeWidth, StrokeWidth, editingOperationCount);
                    doCmdStack.Enqueue(item);
                }
                OnPropertyChanged("Background");
            }
        }

        private Color selectColor;

        public Color SelectColor
        {
            get { return selectColor; }
            set
            {
                selectColor = value;
                Foreground = new SolidColorBrush(selectColor);               
                OnPropertyChanged("SelectColor");
            }
        }



        private int penWidthIndex;

        public int PenWidthIndex
        {
            get { return penWidthIndex; }
            set
            {
                var old_strokewidth = StrokeWidth;

                penWidthIndex = value;

                inkCanvas.DefaultDrawingAttributes.Height = inkCanvas.DefaultDrawingAttributes.Width = StrokeWidth;

                var lstStrokes = inkCanvas.GetSelectedStrokes();

                if (lstStrokes.Count > 0)
                {
                    foreach (var stroke in lstStrokes)
                    {
                        stroke.DrawingAttributes.Height = stroke.DrawingAttributes.Width = StrokeWidth;
                    }
                    editingOperationCount++;
                    CommandItem item = new SelectionColorOrWidthCI(doCmdStack, lstStrokes, Foreground, Foreground,
                      Background, Background, old_strokewidth, StrokeWidth, editingOperationCount);
                    doCmdStack.Enqueue(item);
                }

                OnPropertyChanged("PenWidthIndex");
            }
        }


        private int StrokeWidth
        {
            get { return (penWidthIndex + 1) * 2; }
        }    

        #endregion

        #region Function

        public MainViewModel(InkCanvas _inkCanvas)
        {
            inkCanvas = _inkCanvas;
            inkCanvas.PreviewMouseLeftButtonDown += CanvasMouseDown;
            inkCanvas.MouseMove += CanvasMouseMove;
            
            inkCanvas.MouseUp += CanvasMouseUp;

            doCmdStack = new DoCommandStack(_inkCanvas.Strokes);
            lstStrokeClipBoard = new StrokeCollection();

            //Init
            PenWidthIndex = 0;
            Foreground = Brushes.Black;
            SelectColor = Colors.Black;
            curDraw = new DrawBrush();
        }


        #region Command
        public void Delete()
        {
            var lstStokes = inkCanvas.GetSelectedStrokes();
            if (lstStokes.Count > 0)
            {
                Strokes_Removed(lstStokes);
                inkCanvas.Strokes.Remove(lstStokes);
                inkCanvas.Strokes.Clear();
            }
        }

        public void Undo()
        {
            doCmdStack.Undo();
        }

        /// <summary>
        /// Redo the last edit.
        /// </summary> 
        public void Redo()
        {
            doCmdStack.Redo();
        }

        #endregion


        #region Event
        public void KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyStates == Keyboard.GetKeyStates(Key.Y) && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Redo();
            }
            else if (e.KeyStates == Keyboard.GetKeyStates(Key.Z) && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Undo();
            }
            e.Handled = true;
        }
        private void Strokes_Added(StrokeCollection lstAdded)
        {
            editingOperationCount++;
            CommandItem item = new StrokesAddedOrRemovedCI(doCmdStack, inkCanvas.EditingMode, lstAdded, new StrokeCollection(), editingOperationCount);
            doCmdStack.Enqueue(item);
        }

        private void Strokes_Removed(StrokeCollection lstRemoved)
        {
            editingOperationCount++;
            CommandItem item = new StrokesAddedOrRemovedCI(doCmdStack, inkCanvas.EditingMode, new StrokeCollection(), lstRemoved, editingOperationCount);
            doCmdStack.Enqueue(item);
        }

       

       

        private void CanvasMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (inkCanvas.EditingMode == InkCanvasEditingMode.Select && inkCanvas.GetSelectedStrokes().Count > 0)
            {
                Rect rect = inkCanvas.GetSelectionBounds();  //bound of strokes
                rect.Inflate(14, 14);                        //bound of resize thumb
                if (!rect.Contains(e.GetPosition(inkCanvas)))
                {
                    inkCanvas.EditingMode = InkCanvasEditingMode.None;
                }
                else
                {
                    return;
                }
            }

            
            if (curDraw != null)
            {
                curDraw.OnMouseDown(inkCanvas, e);
            }
        }
        private void CanvasMouseMove(object sender, MouseEventArgs e)
        {
            var point = e.GetPosition(inkCanvas);           

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (curDraw != null && inkCanvas.GetSelectedStrokes().Count == 0)
                {
                    isDraw = true;
                    inkCanvas.EditingMode = InkCanvasEditingMode.None;
                    curDraw.OnMouseMove(inkCanvas, e);

                    if (curDraw.StrokeResult != null)
                    {
                        Rect rect = curDraw.StrokeResult.GetBounds();
                    }

                }
                else
                {
                    isDraw = false;
                }
            }
        }
       
        private void CanvasMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (curDraw != null && isDraw)
            {
                if (curDraw.StrokeResult != null)
                {
                    var lstStrokes = new StrokeCollection() { curDraw.StrokeResult };
                    Strokes_Added(lstStrokes);
                    isDraw = false;
                }

            }
            else if (inkCanvas.EditingMode == InkCanvasEditingMode.Ink)
            {
                if(inkCanvas.Strokes.Count > 0)
                Strokes_Added(new StrokeCollection() { inkCanvas.Strokes[inkCanvas.Strokes.Count - 1] });
            }

        }

        #endregion

        #endregion

        #region win API

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool GetCursorPos(out POINT pt);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hwnd);
        [DllImport("gdi32.dll")]
        private static extern int GetPixel(IntPtr hdc, int nXPos, int nYPos);


        #endregion

        #region PropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        #endregion
    }
}
