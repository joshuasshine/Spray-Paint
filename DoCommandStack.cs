﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media;

namespace SprayPaintImage
{
    sealed class DoCommandStack
    {
        /// <summary>
        /// Initialization.
        /// </summary>
        /// <param name="strokes"></param>
        public DoCommandStack(StrokeCollection strokes)
        {
            if (strokes == null)
            {
                return;
            }
            _strokeCollection = strokes;
            _undoStack = new Stack<CommandItem>();
            _redoStack = new Stack<CommandItem>();
            _disableChangeTracking = false;
        }

        /// <summary>
        /// StrokeCollection to track changes for
        /// </summary>
        public StrokeCollection StrokeCollection
        {
            get
            {
                return _strokeCollection;
            }
        }

        /// <summary>
        /// Only undo if there are more items in the stack to step back into.
        /// </summary>
        public bool CanUndo
        {
            get { return (_undoStack.Count > 0); }
        }

        /// <summary>
        /// Only undo if one or more steps back in the stack.
        /// </summary>
        public bool CanRedo
        {
            get { return (_redoStack.Count > 0); }
        }

        /// <summary>
        /// Add an item to the top of the command stack
        /// </summary>
        public void Undo()
        {
            if (!CanUndo) return;

            CommandItem item = _undoStack.Pop();

            // Invoke the undo operation, with change-tracking temporarily suspended.
            _disableChangeTracking = true;
            try
            {
                item.Undo();
            }
            finally
            {
                _disableChangeTracking = false;
            }

            //place this item on the redo stack
            _redoStack.Push(item);
        }

        /// <summary>
        /// Take the top item off the command stack.
        /// </summary>
        public void Redo()
        {
            if (!CanRedo) return;

            CommandItem item = _redoStack.Pop();

            // Invoke the redo operation, with change-tracking temporarily suspended.
            _disableChangeTracking = true;
            try
            {
                item.Redo();
            }
            finally
            {
                _disableChangeTracking = false;
            }

            //place this item on the undo stack
            _undoStack.Push(item);
        }

        /// <summary>
        /// Add a command item to the stack.
        /// </summary>
        /// <param name="item"></param>
        public void Enqueue(CommandItem item)
        {
            if (item == null)
            {
                return;
            }

            // Ensure we don't enqueue new items if we're being changed programmatically.
            if (_disableChangeTracking)
            {
                return;
            }

            // Check to see if this new item can be merged with previous.
            bool merged = false;
            if (_undoStack.Count > 0)
            {
                CommandItem prev = _undoStack.Peek();
                merged = prev.Merge(item);
            }

            // If not, append the new command item
            if (!merged)
            {
                _undoStack.Push(item);
            }

            //clear the redo stack
            if (_redoStack.Count > 0)
            {
                _redoStack.Clear();
            }
        }

        /// <summary>
        /// Implementation
        /// </summary>
        private StrokeCollection _strokeCollection;

        private Stack<CommandItem> _undoStack;
        private Stack<CommandItem> _redoStack;


        bool _disableChangeTracking; // reentrancy guard: disables tracking of programmatic changes 
        // (eg, in response to undo/redo ops)
    }

    /// <summary>
    /// Derive from this class for every undoable/redoable operation you wish to support.
    /// </summary>
    abstract class CommandItem
    {

        // Interface
        public abstract void Undo();
        public abstract void Redo();


        // Allows multiple subsequent commands of the same type to roll-up into one 
        // logical undoable/redoable command -- return false if newitem is incompatable.
        public abstract bool Merge(CommandItem newitem);

        // Implementation
        protected DoCommandStack _commandStack;

        protected CommandItem(DoCommandStack commandStack)
        {
            _commandStack = commandStack;
        }
    }

    /// <summary>
    /// This operation covers collecting new strokes, stroke-erase, and point-erase.
    /// </summary>
    class StrokesAddedOrRemovedCI : CommandItem
    {
        InkCanvasEditingMode _editingMode;
        StrokeCollection _added, _removed;
        int _editingOperationCount;

        public StrokesAddedOrRemovedCI(DoCommandStack commandStack, InkCanvasEditingMode editingMode, StrokeCollection added, StrokeCollection removed, int editingOperationCount)
            : base(commandStack)
        {
            _editingMode = editingMode;

            _added = added;
            _removed = removed;

            _editingOperationCount = editingOperationCount;
        }

        public override void Undo()
        {
            if (_commandStack.StrokeCollection.Count > 0)
            {
                _commandStack.StrokeCollection.Remove(_added);
                _commandStack.StrokeCollection.Add(_removed);
            }
        }

        public override void Redo()
        {
                _commandStack.StrokeCollection.Add(_added);
                _commandStack.StrokeCollection.Remove(_removed);
        }

        public override bool Merge(CommandItem newitem)
        {
            StrokesAddedOrRemovedCI newitemx = newitem as StrokesAddedOrRemovedCI;

            if (newitemx == null ||
                newitemx._editingMode != _editingMode ||
                newitemx._editingOperationCount != _editingOperationCount)
            {
                return false;
            }

            // We only implement merging for repeated point-erase operations.
            if (_editingMode != InkCanvasEditingMode.EraseByPoint) return false;
            if (newitemx._editingMode != InkCanvasEditingMode.EraseByPoint) return false;

            // Note: possible for point-erase to have hit intersection of >1 strokes!
            // For each newly hit stroke, merge results into this command item.
            foreach (Stroke doomed in newitemx._removed)
            {
                if (_added.Contains(doomed))
                {
                    _added.Remove(doomed);
                }
                else
                {
                    _removed.Add(doomed);
                }
            }
            _added.Add(newitemx._added);

            return true;
        }
    }

   
    /// <summary>
    /// This operation covers new color or width operations.
    /// </summary>
    class SelectionColorOrWidthCI : CommandItem
    {
        StrokeCollection _selection;
        Brush _old_foreground, _new_foreground, _old_background, _new_background;
        int _old_width, _new_width;
        int _editingOperationCount;

        public SelectionColorOrWidthCI(DoCommandStack commandStack, StrokeCollection selection,
            Brush old_foreground, Brush new_foreground, Brush old_background, Brush new_background,
            int old_width, int new_width, int editingOperationCount)
            : base(commandStack)
        {
            _selection = selection;
            _old_foreground = old_foreground;
            _new_foreground = new_foreground;
            _old_background = old_background;
            _new_background = new_background;
            _old_width = old_width;
            _new_width = new_width;

            _editingOperationCount = editingOperationCount;
        }

        public override void Undo()
        {
            foreach (var stroke in _selection)
            {
                var attr = stroke.DrawingAttributes;
                attr.Color = ((SolidColorBrush)_old_foreground).Color;
                attr.AddPropertyData(new Guid("6F03AB24-31E5-4152-BF30-7E0F61FD40F7"), _old_background.ToString());
                attr.Width = _old_width;
                stroke.DrawingAttributes = attr;
            }
        }

        public override void Redo()
        {
            foreach (var stroke in _selection)
            {
                var attr = stroke.DrawingAttributes;
                attr.Color = ((SolidColorBrush)_new_foreground).Color;
                attr.AddPropertyData(new Guid("6F03AB24-31E5-4152-BF30-7E0F61FD40F7"), _new_background.ToString());
                attr.Width = _new_width;
                stroke.DrawingAttributes = attr;
            }
        }

        public override bool Merge(CommandItem newitem)
        {
            SelectionColorOrWidthCI newitemx = newitem as SelectionColorOrWidthCI;

            // Ensure items are of the same type.
            if (newitemx == null ||
                newitemx._editingOperationCount != _editingOperationCount)
            {
                return false;
            }

            _old_foreground = newitemx._old_foreground;
            _new_foreground = newitemx._new_foreground;
            _old_background = newitemx._old_background;
            _new_background = newitemx._new_background;
            _old_width = newitemx._old_width;
            _new_width = newitemx._new_width;

            return true;
        }
    }
}
