using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using Avalonia;
using System.Linq;
using System.Collections.ObjectModel;
using System.IO;
using System.Diagnostics;
using DominoPlanner.Core;
using Avalonia.Media;
using Avalonia.Input;
using Avalonia.Controls.Shapes;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.VisualTree;
using System.Threading.Tasks;
using SkiaSharp;
using Avalonia.Data.Converters;
using DominoPlanner.Core.RTree;
using Avalonia.Collections;

namespace DominoPlanner.Usage.UserControls.ViewModel
{
    using static Localizer;
    public class EditingToolVM : ModelBase
    {
        public EditProjectVM parent;

        public EditingToolVM(EditProjectVM parent)
        {
            this.parent = parent;
        }
        public string Name { get; internal set; }
        private string image;

        public string Image
        {
            get { return image; }
            set { image = value;
                Application.Current.TryFindResource(value, out object temp);
                Img = (DrawingImage)temp;
            }
        }
        public string HelpToolTip { get; protected set; }

        public DrawingImage Img { get; private set; }

        public virtual void MouseMove(Avalonia.Point dominoPoint, PointerEventArgs e) { }

        public virtual void MouseDown(Avalonia.Point dominoPoint, PointerPressedEventArgs e) { }

        public virtual void MouseUp(Avalonia.Point dominoPoint, PointerReleasedEventArgs e) { }

        public virtual void KeyPressed(KeyEventArgs key) { }

        public virtual void MouseWheel(Avalonia.Point dominoPoint, PointerWheelEventArgs e) { }

        public virtual void OnUndo() { }
        public virtual void OnRedo() { }


        public virtual void LeaveTool() { }
        public virtual void EnterTool() { }
    }
    public enum SelectionMode
    {
        Add,
        Neutral,
        Remove
    }
    public class SelectionOperation : PostFilter
    {
        readonly SelectionToolVM reference;
        readonly IList<int> ToSelect;
        readonly bool positiveselect;
        readonly bool[] oldState;
        public SelectionOperation(SelectionToolVM reference, IList<int> ToSelect, bool select )
        {
            this.reference = reference;
            this.ToSelect = ToSelect.ToArray();
            this.positiveselect = select;
            oldState = new bool[ToSelect.Count];
        }
        public override void Apply()
        {
            for (int i = 0; i < ToSelect.Count; i++)
            {
                if (ToSelect[i] < reference.parent.CurrentProject.Last.Length)
                {
                    oldState[i] = reference.parent.IsSelected(ToSelect[i]);
                    if (positiveselect)
                        reference.parent.AddToSelectedDominoes(ToSelect[i]);
                    else
                        reference.parent.RemoveFromSelectedDominoes(ToSelect[i]);
                }
            }
        }

        public override void Undo()
        {
            for (int i = 0; i < ToSelect.Count; i++)
            {
                if (ToSelect[i] < reference.parent.CurrentProject.Last.Length)
                {
                    if (oldState[i])
                    {
                        reference.parent.AddToSelectedDominoes(ToSelect[i]);
                    }
                    else
                    {
                        reference.parent.RemoveFromSelectedDominoes(ToSelect[i]);
                    }
                }
            }
        }
    }
    public class SelectionToolVM : EditingToolVM
    {
        public SelectionToolVM(EditProjectVM parent) : base(parent)
        {
            Image = "rect_selectDrawingImage";
            Name = _("Select");
            HelpToolTip = _("Esc: clear selection\nq: quick replace\n+/./-: toggle selection modes\nr/c/p/f/b: switch between selection tools");
            SelectionTools = new ObservableCollection<SelectionDomain>() {
                new RectangleSelection(parent), new CircleSelectionDomain(parent),
                new PolygonSelectionDomain(parent), new FreehandSelectionDomain(parent), new FillBucketDomain(parent)};
            CurrentSelectionDomain = SelectionTools[0];
            UndoSelectionOperation = new RelayCommand((o) => {
                parent.UndoInternal(true);
            });
            RedoSelectionOperation = new RelayCommand((o) => {
                parent.RedoInternal(true);
            });
            InvertSelection = new RelayCommand((o) => InvertSelectionOperation());
        }

        private SelectionDomain currentSelectionDomain;

        public SelectionDomain CurrentSelectionDomain
        {
            get { return currentSelectionDomain; }
            set {
                if (value != null)
                {
                    if (currentSelectionDomain != null)
                    {
                        value.IncludeBoundary = currentSelectionDomain.IncludeBoundary;
                        value.SelectionMode = currentSelectionDomain.SelectionMode;
                    }
                    currentSelectionDomain = value; 
                }
                RaisePropertyChanged();
            }
        }
        private bool quickReplacePopupOpen;
        public bool QuickReplacePopupOpen
        {
            get { return quickReplacePopupOpen; }
            set { quickReplacePopupOpen = value; RaisePropertyChanged(); }

        }

        private ObservableCollection<SelectionDomain> selectionTools;

        public ObservableCollection<SelectionDomain> SelectionTools
        {
            get { return selectionTools; }
            set { selectionTools = value; RaisePropertyChanged();  }
        }

        public override void MouseDown(Avalonia.Point dominoPoint, PointerPressedEventArgs e)
        {
            CurrentSelectionDomain?.MouseDown(dominoPoint, e);   
        }
        public override void MouseMove(Avalonia.Point dominoPoint, PointerEventArgs e)
        {
            CurrentSelectionDomain?.MouseMove(dominoPoint, e);
        }
        public override void MouseUp(Avalonia.Point dominoPoint, PointerReleasedEventArgs e)
        {
            var result = CurrentSelectionDomain?.MouseUp(dominoPoint, e);
            if (result.Count == 0) return;
            if (CurrentSelectionDomain?.CurrentSelectionMode == SelectionMode.Add)
            {
                Select(result, true);
            }
            else if (CurrentSelectionDomain?.CurrentSelectionMode == SelectionMode.Remove)
            {
                Select(result, false);
            }
            parent.UpdateUIElements();
        }
        public override void KeyPressed(KeyEventArgs key)
        {
            if (key.Key == Key.Q)
            {
                QuickReplacePopupOpen = true;
            }
            if (key.Key == Key.Escape)
            {
                if (QuickReplacePopupOpen)
                    QuickReplacePopupOpen = false;
                else
                parent.ClearFullSelection(true);
            }
            if (CurrentSelectionDomain != null)
            {
                // These keys are for touchpad users
                if (key.Key == Key.OemPlus || key.Key == Key.Add)
                    CurrentSelectionDomain.SelectionMode = SelectionMode.Add;
                else if (key.Key == Key.OemMinus || key.Key == Key.Subtract)
                    CurrentSelectionDomain.SelectionMode = SelectionMode.Remove;
                else if (key.Key == Key.OemPeriod)
                    CurrentSelectionDomain.SelectionMode = SelectionMode.Neutral;
            }
            switch (key.Key)
            {
                case Key.C:
                    CurrentSelectionDomain = SelectionTools.OfType<CircleSelectionDomain>().FirstOrDefault();
                    break;
                case Key.R:
                    CurrentSelectionDomain = SelectionTools.OfType<RectangleSelection>().FirstOrDefault();
                    break;
                case Key.P:
                    CurrentSelectionDomain = SelectionTools.OfType<PolygonSelectionDomain>().FirstOrDefault();
                    break;
                case Key.F:
                    CurrentSelectionDomain = SelectionTools.OfType<FreehandSelectionDomain>().FirstOrDefault();
                    break;
                case Key.B:
                    CurrentSelectionDomain = SelectionTools.OfType<FillBucketDomain>().FirstOrDefault();
                    break;
            }
        }
        public void Select(IList<int> toSelect, bool select)
        {
            parent.ExecuteOperation(new SelectionOperation(this, toSelect, select));
        }
        public void InvertSelectionOperation()
        {
            var current = parent.GetSelectedDominoes();
            var n = Enumerable.Range(0, parent.dominoTransfer.Length).Except(current).ToList();
            Select(current, false);
            Select(n, true);
            parent.UpdateUIElements();
        }
        public override void LeaveTool()
        {
            CurrentSelectionDomain.RemoveSelectionDomain();
        }
        public override void EnterTool()
        {
            CurrentSelectionDomain.ResetSelectionArea();
        }
        private ICommand _UndoSelectionOperation;
        public ICommand UndoSelectionOperation { get { return _UndoSelectionOperation; } set { if (value != _UndoSelectionOperation) { _UndoSelectionOperation= value; } } }

        private ICommand _RedoSelectionOperation;
        public ICommand RedoSelectionOperation { get { return _RedoSelectionOperation; } set { if (value != _RedoSelectionOperation) { _RedoSelectionOperation = value; } } }

        private ICommand _InvertSelection;
        public ICommand InvertSelection { get { return _InvertSelection; } set { if (value != _InvertSelection) { _InvertSelection = value; } } }

    }
    public class SelectionModeColorConverter : IValueConverter
    {
        static readonly Color AddColor = Colors.LightBlue;
        static readonly Color RemoveColor = Colors.IndianRed;
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SelectionMode sm)
            {
                if (sm == SelectionMode.Add)
                {
                    return AddColor;
                }
                else return RemoveColor;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public abstract class SelectionDomain : ModelBase
    {
        internal EditProjectVM parent;
        internal SelectionDomain(EditProjectVM parent)
        {
            this.parent = parent;
        }
        public string Name { get; internal set; }
        public DrawingImage Img { get; private set; }

        public char Shortcut { get; internal set; }
        private string image;

        public string Image
        {
            get { return image; }
            set { image = value; Application.Current.TryFindResource(value, out object temp);
                Img = (DrawingImage)temp;
            }
        }
        private bool includeBoundary = true;

        public bool IncludeBoundary
        {
            get { return includeBoundary; }
            set { includeBoundary = value; RaisePropertyChanged(); }
        }
        private SelectionMode selectionMode = SelectionMode.Neutral;

        public SelectionMode SelectionMode
        {
            get { return selectionMode; }
            set
            {
                selectionMode = value;
                RaisePropertyChanged();
            }
        }
        protected bool ResetFlag = false;

        private SelectionMode _currentSelectionMode;
        public SelectionMode CurrentSelectionMode
        {
         get { return _currentSelectionMode; }
            set { _currentSelectionMode = value; RaisePropertyChanged(); }
        }
        private SKPath selectionshape;
        public SKPath SelectionShape
        {
            get { return selectionshape; }
            set
            {
                selectionshape = value;
                RaisePropertyChanged();
            }
        }
        private bool _selectionVisible;

        public bool SelectionPathVisible
        {
            get { return _selectionVisible; }
            set { _selectionVisible = value; RaisePropertyChanged(); }
        }
        private Color selectionFill;

        public Color  SelectionFillColor
        {
            get { return selectionFill; }
            set { selectionFill = value; RaisePropertyChanged(); }
        }


        public void RemoveSelectionDomain()
        {
            SelectionPathVisible = false;
        }
        public void AddSelectionDomain()
        {
            SelectionPathVisible = true;
        }
        public abstract void MouseMove(Avalonia.Point dominoPoint, PointerEventArgs e);

        public abstract void MouseDown(Avalonia.Point dominoPoint, PointerPressedEventArgs e);

        public virtual List<int> MouseUp(Avalonia.Point dominoPoint, PointerReleasedEventArgs e)
        {
            return new List<int>();
        }
        public Rect GetBoundingBox()
        {
            SelectionShape.GetBounds(out SKRect rect);
            return new Rect(rect.Left, rect.Top, rect.Width, rect.Height);
        }

        public static bool IsInsideBoundingBox(Rect BoundingBox, EditingDominoVM dic, bool includeBoundary)
        {
            var points = dic.domino.GetPath(expanded: dic.expanded).points;
            if (includeBoundary)
            {
                return points.Max(x => x.X) > BoundingBox.Left &&
                    points.Min(x => x.X) < BoundingBox.Right &&
                    points.Min(x => x.Y) < BoundingBox.Bottom &&
                    points.Max(x => x.Y) > BoundingBox.Top;
            }
            else
            {
                return points.Min(x => x.X) > BoundingBox.Left &&
                    points.Max(x => x.X) < BoundingBox.Right &&
                    points.Max(x => x.Y) < BoundingBox.Bottom &&
                    points.Min(x => x.Y) > BoundingBox.Top;
                
            }
        }

        public abstract bool IsInside(EditingDominoVM dic, Rect boundingBox, bool includeBoundary);

        public void ResetSelectionArea()
        {
            ResetFlag = true;
        }
        public bool CheckBoundary(EditingDominoVM dic, int pointsInsideSelection)
        {
            if (IncludeBoundary)
            {
                if (pointsInsideSelection > 0)
                    return true;
            }
            else
            {
                if (pointsInsideSelection == dic.domino.GetPath().points.Length)
                    return true;
            }
            return false;
        }
    }
    public abstract class TwoClickSelection : SelectionDomain
    {
        public TwoClickSelection(EditProjectVM parent) : base(parent)
        {
            MouseDownPoint = new Avalonia.Point(-1, -1);
        }
        protected Avalonia.Point MouseDownPoint;
        protected Avalonia.Point currentPoint;
        public void UpdateSelectionMode(PointerPressedEventArgs e) // called on Mouse down
        {
            var update = e.GetCurrentPoint(null).Properties.PointerUpdateKind;
            if (update == PointerUpdateKind.LeftButtonPressed || update == PointerUpdateKind.RightButtonPressed)
            {
                if (SelectionMode == SelectionMode.Add || (SelectionMode == SelectionMode.Neutral && update == PointerUpdateKind.LeftButtonPressed))
                {
                    CurrentSelectionMode = SelectionMode.Add;
                }
                else if (SelectionMode == SelectionMode.Remove || (SelectionMode == SelectionMode.Neutral && update == PointerUpdateKind.RightButtonPressed))
                {
                    CurrentSelectionMode = SelectionMode.Remove;
                }
                else
                {
                    CurrentSelectionMode = SelectionMode.Neutral;
                }
            }
        }
        public abstract void UpdateShapeProperties(Avalonia.Point pos);
        public abstract void Initialize();

        public override void MouseDown(Avalonia.Point dominoPoint, PointerPressedEventArgs e)
        {
            UpdateSelectionMode(e);

            MouseDownPoint = dominoPoint;
            Initialize();
            ResetFlag = false;
            
            AddSelectionDomain();
        }
        public override void MouseMove(Avalonia.Point dominoPoint, PointerEventArgs e)
        {
            var props = e.GetCurrentPoint(null).Properties;
            if (!props.IsLeftButtonPressed && !props.IsRightButtonPressed)
            {
                RemoveSelectionDomain();
                return;
            }
            if (SelectionShape == null) return;
            
            UpdateShapeProperties(dominoPoint);

            
        }
        public override List<int> MouseUp(Avalonia.Point dominoPoint, PointerReleasedEventArgs e)
        {
            List<int> result = new List<int>();
            var props = e.GetCurrentPoint(null).Properties;
            if ((props.IsLeftButtonPressed || props.IsRightButtonPressed)
                || (MouseDownPoint.X == -1 && MouseDownPoint.Y == -1))
            {
                return result;
            }
            Rect boundingBox = GetBoundingBox();
            var pos = dominoPoint;
            if ((pos.X - MouseDownPoint.X) * (pos.X - MouseDownPoint.X) + (pos.Y - MouseDownPoint.Y) * (pos.Y - MouseDownPoint.Y) < 5)
            {
                // single click 
                boundingBox = new Rect(dominoPoint.X, dominoPoint.Y, 0, 0);
                var r = parent.FindDominoAtPosition(pos, 3); // we give the user a few pixels of tolerance (relative to domino size)
                if (r != null) result.Add(r.idx);
                ResetFlag = true;
            }
            if (!ResetFlag)
            {
                for (int i = 0; i < parent.Dominoes.Count; i++)
                {
                    if (parent.Dominoes[i] is EditingDominoVM dic && IsInside(dic, boundingBox, IncludeBoundary))
                    {
                        result.Add(i);
                    }
                }
            }
            ResetFlag = false;
            RemoveSelectionDomain();
            MouseDownPoint = new Avalonia.Point(-1, -1);
            return result;
        }
        
        
    }
    public abstract class WidthHeightSelection : TwoClickSelection
    {
        internal WidthHeightSelection(EditProjectVM parent) : base(parent) { }
        
        public override void Initialize()
        {
            SelectionShape = new SKPath();
        }
        public abstract Rect GetCurrentDimensions(Avalonia.Point pos);

        public override void UpdateShapeProperties(Avalonia.Point pos)
        {
            currentPoint = pos;
            var dims = GetCurrentDimensions(pos);
            if (dims.Width > 10 || dims.Height > 10)
                SelectionPathVisible = true;
            else
                SelectionPathVisible = false;

        }
    }
    public class RectangleSelection : WidthHeightSelection
    {
        
        public RectangleSelection(EditProjectVM parent) : base(parent)
        {
            Image = "rect_selectDrawingImage";
            Name = _("Rectangle selection");
            Shortcut = 'r';
        }
        public override void UpdateShapeProperties(Avalonia.Point pos)
        {
            var p = new SKPath();
            var dims = GetCurrentDimensions(pos);
            p.AddRect(new SKRect() { Left = (float)dims.X, Top = (float)dims.Y, Size = new SKSize() { Width = (float)dims.Width, Height = (float)dims.Height } });
            SelectionShape = p;
            base.UpdateShapeProperties(pos);
        }
        public override Rect GetCurrentDimensions(Avalonia.Point pos)
        {
            var x = Math.Min(pos.X, MouseDownPoint.X);
            var y = Math.Min(pos.Y, MouseDownPoint.Y);

            var w = Math.Max(pos.X, MouseDownPoint.X) - x;
            var h = Math.Max(pos.Y, MouseDownPoint.Y) - y;
            return new Rect(x, y, w, h);

        }
        public override bool IsInside(EditingDominoVM dic, Rect boundingBox, bool includeBoundary)
        {
            return IsInsideBoundingBox(boundingBox, dic, includeBoundary);
        }
    }
    public class CircleSelectionDomain : WidthHeightSelection
    {
        public CircleSelectionDomain(EditProjectVM parent) : base(parent)
        {
            Image = "round_selectDrawingImage";
            Name = _("Circle selection");
            Shortcut = 'c';
        }
        public override void UpdateShapeProperties(Avalonia.Point pos)
        {
            base.UpdateShapeProperties(pos);
            var p = new SKPath();
            var dims = GetCurrentDimensions(pos);
            p.AddCircle((float)MouseDownPoint.X, (float)MouseDownPoint.Y, (float)dims.Width/2);
            SelectionShape = p;
        }
        public override Rect GetCurrentDimensions(Avalonia.Point pos)
        {
            var radius = Math.Sqrt(Math.Pow(pos.X - MouseDownPoint.X, 2) + Math.Pow(pos.Y - MouseDownPoint.Y, 2));
            return new Rect(MouseDownPoint.X - radius, MouseDownPoint.Y - radius, 2 * radius, 2*radius);
        }

        public override bool IsInside(EditingDominoVM dic, Rect boundingBox, bool includeBoundary)
        {
            var radius = boundingBox.Width / 2;
            var center = new Avalonia.Point(boundingBox.X + radius, boundingBox.Y + radius);

            if (IsInsideBoundingBox(boundingBox, dic, includeBoundary))
            {
                var insideCircle = dic.CanvasPoints.Count(x =>
                Math.Sqrt((x.X - center.X) * (x.X - center.X) + (x.Y - center.Y) * (x.Y - center.Y)) < radius);
                return CheckBoundary(dic, insideCircle);
            }
            return false;
        }
        
    }
    public class PolygonSelectionDomain : SelectionDomain
    {
        public List<Avalonia.Point> points;
        public PointerUpdateKind? firstButton;
        public PolygonSelectionDomain(EditProjectVM parent) : base(parent)
        {
            Image = "poly_selectDrawingImage";
            Name = _("Polygon selection");
            Shortcut = 's';
            points = new List<Avalonia.Point>();
            SelectionFillColor = Color.FromArgb(50, 100, 100, 100);
            
        }

        public override bool IsInside(EditingDominoVM dic, Rect boundingBox, bool includeBoundary)
        {
            if (IsInsideBoundingBox(boundingBox, dic, includeBoundary))
            {
                var pts = dic.domino.GetPath(expanded: dic.expanded).points;
                var insidePoly = pts.Count(x => SelectionShape.Contains((float)x.X, (float)x.Y));
                return CheckBoundary(dic, insidePoly);
            }
            return false;
        }
        bool DoubleClickFlag;
        public override void MouseDown(Avalonia.Point dominoPoint, PointerPressedEventArgs e)
        {
            var props = e.GetCurrentPoint(null).Properties;
            if (!((props.PointerUpdateKind ==  PointerUpdateKind.LeftButtonPressed || props.PointerUpdateKind == PointerUpdateKind.RightButtonPressed) &&
                (props.IsLeftButtonPressed ^ props.IsRightButtonPressed)))
                return;
            Debug.WriteLine(points.Count);
            if (points.Count == 0)
            {
                firstButton = props.PointerUpdateKind;
                if (SelectionMode == SelectionMode.Neutral)
                {
                    CurrentSelectionMode = props.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed ? SelectionMode.Add : SelectionMode.Remove;
                }
                else
                {
                    CurrentSelectionMode = SelectionMode;
                }
                AddSelectionDomain();
                points.Add(dominoPoint);
            }
            else if (e.ClickCount == 2)
            {
                DoubleClickFlag = true;
            }
            if (props.PointerUpdateKind != firstButton)
            {
                // selection canceled, clear polygon
                points.Clear();
                RemoveSelectionDomain();
                return;
            }
            points.Add(dominoPoint);
            UpdatePath();
            
        }
        private void UpdatePath()
        {
            var p = new SKPath();
            if (points != null && points.Count > 0)
            {
                p.MoveTo((float)points[0].X, (float)points[0].Y);
                foreach (Avalonia.Point point in points.Skip(1))
                    p.LineTo((float)point.X, (float)point.Y);
            }
            SelectionShape = p;
        }
        public override void MouseMove(Avalonia.Point dominoPoint, PointerEventArgs e)
        {
            if (SelectionShape == null) return;
            if (points.Count >= SelectionShape.PointCount - 1 && points.Count > 0)
                points[SelectionShape.PointCount - 1] = dominoPoint;
            UpdatePath();
        }

        public override List<int> MouseUp(Avalonia.Point dominoPoint, PointerReleasedEventArgs e)
        {
            var result = new List<int>();
            if (!DoubleClickFlag)
                return result;
            DoubleClickFlag = false;
            var boundingBox = GetBoundingBox();
            for (int i = 0; i < parent.Dominoes.Count; i++)
            {
                if (parent.Dominoes[i] is EditingDominoVM dic && IsInside(dic, boundingBox, IncludeBoundary))
                {
                    result.Add(i);
                }
            }
            RemoveSelectionDomain();
            points = new List<Avalonia.Point>();
            firstButton = null;
            return result;
        }
    }
    public class FreehandSelectionDomain : TwoClickSelection
    {
        readonly List<Avalonia.Point> points;
        public FreehandSelectionDomain(EditProjectVM parent) : base(parent)
        {
            Image = "freehand_selectDrawingImage";
            Name = _("Freehand selection");
            Shortcut = 'f'; 
            SelectionShape = new SKPath();
            MouseDownPoint = new Avalonia.Point(-1, -1);
            SelectionFillColor = Color.FromArgb(50, 100, 100, 100);
            points = new List<Avalonia.Point>();

        }
        public override void Initialize()
        {
            points.Clear();
            points.Add(MouseDownPoint);
            UpdatePath();
        }
        private void UpdatePath()
        {
            var p = new SKPath();
            if (points != null && points.Count > 1)
            {
                p.MoveTo((float)points[0].X, (float)points[0].Y);
                foreach (Avalonia.Point point in points.Skip(1))
                    p.LineTo((float)point.X, (float)point.Y);
            }
            SelectionShape = p;
        }

        public override bool IsInside(EditingDominoVM dic, Rect boundingBox, bool includeBoundary)
        {
            if (IsInsideBoundingBox(boundingBox, dic, includeBoundary))
            {
                var insidePoly = dic.CanvasPoints.Count(x => SelectionShape.Contains((float)x.X, (float)x.Y));
                return CheckBoundary(dic, insidePoly);
            }
            return false;
        }

        public override void UpdateShapeProperties(Avalonia.Point pos)
        {
            var last = points.Last();
            if ((last.X - pos.X) * (last.X - pos.X) + (last.Y - pos.Y) * (last.Y - pos.Y) > 3)
            {
                points.Add(pos);
                UpdatePath();
            }
        }
    }
    public class FillBucketDomain : SelectionDomain
    {

        public bool IncludeDiagonals
        {
            get { return nl.EightNeighbor; }
            set { nl.EightNeighbor = value; RaisePropertyChanged(); }
        }

        private readonly NeighborLocator nl;
        private AvaloniaList<EditingDominoVM> dominoes;
        public FillBucketDomain(EditProjectVM parent) : base(parent)
        {
            if (parent.CurrentProject is FieldParameters)
            {
                nl = new FieldNeighborLocator();
            }
            else
            {
                nl = new GeneralNeighborLocator();
            }
            this.parent = parent;
            Image = "fill_bucketDrawingImage";
            Name = _("Select connected area");
            Shortcut = 'b';
        }

        public override void MouseDown(Avalonia.Point dominoPoint, PointerPressedEventArgs e)
        {
            var props = e.GetCurrentPoint(null).Properties;
            if (props.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed || props.PointerUpdateKind == PointerUpdateKind.RightButtonPressed)
            {
                if (SelectionMode == SelectionMode.Add || (SelectionMode == SelectionMode.Neutral && props.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed))
                {
                    CurrentSelectionMode = SelectionMode.Add;
                }
                else if (SelectionMode == SelectionMode.Remove || (SelectionMode == SelectionMode.Neutral && props.PointerUpdateKind == PointerUpdateKind.RightButtonPressed))
                {
                    CurrentSelectionMode = SelectionMode.Remove;
                }
                else
                {
                    CurrentSelectionMode = SelectionMode.Neutral;
                }
            }
        }
        public override List<int> MouseUp(Avalonia.Point dominoPoint, PointerReleasedEventArgs e)
        {
            // If the canvas has been reset (i.e. after inserting / removing a line), reset the locator (= recalculate R tree)
            if (dominoes != this.parent.Dominoes)
            {
                dominoes = this.parent.Dominoes;
                nl.ResetLocator(parent, dominoes);
            }
            var props = e.GetCurrentPoint(null).Properties;
            if (!(!props.IsLeftButtonPressed && !props.IsRightButtonPressed))
                return new List<int>();

            List<int> neighbors = new List<int>();

            var start = parent.FindDominoAtPosition(dominoPoint, int.MaxValue); // in this case we don't care if we didn't directly hit a domino since we want to fill the region anyway
            if (start == null) return neighbors; // no domino was clicked

            RecursiveSearch(start.idx, neighbors);
            return neighbors;
        }

        private void RecursiveSearch(int dc, List<int> list)
        {
            var neighbors = nl.FindNeighbors(dc);
            foreach (var n in neighbors)
            {
                if (list.Contains(n)) continue;
                if (parent.dominoTransfer[n].Color == parent.dominoTransfer[dc].Color)
                {
                    list.Add(n);
                    RecursiveSearch(n, list);
                }

            }
        }
        public override bool IsInside(EditingDominoVM dic, Rect boundingBox, bool includeBoundary)
        {
            throw new NotImplementedException();
        }
        public override void MouseMove(Avalonia.Point dominoPoint, PointerEventArgs e)
        {

        }
    }
    public abstract class NeighborLocator
    {
        public bool EightNeighbor = false;
        public NeighborLocator()
        {
        }
        public virtual void ResetLocator(EditProjectVM e, AvaloniaList<EditingDominoVM> pc) { }
        public abstract List<int> FindNeighbors(int dc);
    }
    public class FieldNeighborLocator : NeighborLocator
    {
        private AvaloniaList<EditingDominoVM> dominoes;
        private FieldParameters fp;
        private readonly int[] positions = new int[] { -1, 0, 1 };
        public FieldNeighborLocator()
        {

        }
        public override void ResetLocator(EditProjectVM e, AvaloniaList<EditingDominoVM> dominoes)
        {
            this.dominoes = dominoes;
            this.fp = (e.CurrentProject as FieldParameters);
        }

        public override List<int> FindNeighbors(int dc)
        {
            var result = new List<int>();
            var pos = fp.getPositionFromIndex(dc);

            foreach (int i in positions)
            {
                foreach (int j in positions)
                {
                    if (Math.Abs(i) + Math.Abs(j) <= 1 || EightNeighbor)
                    {
                        CheckCandidateAndAddToList(pos.X + i, pos.Y + j, result);
                    }
                }
            }
            return result;
        }
        private void CheckCandidateAndAddToList(int x, int y, List<int> list)
        {
            if (x >= 0 && x < fp.current_width && y >= 0 && y < fp.current_height)
            {
                list.Add(fp.getIndexFromPosition(y, x, 0));
                //var w = fp.getIndexFromPosition(y, x, 0);
            }
        }
    }

    public class GeneralNeighborLocator : NeighborLocator
    {
        private RTree<EditingDominoVM> tree;
        private EditProjectVM parent;
        private AvaloniaList<EditingDominoVM> dominoes;
        public GeneralNeighborLocator() : base() { }

        public override List<int> FindNeighbors(int dc)
        {
            double cl = parent.CurrentProject.charLength;
            var rect = parent.dominoTransfer[dc].GetBoundingRectangle();
            var roi = new DominoRectangle()
            {
                height = cl * 2,
                width = cl * 2,
                x = rect.xc - cl,
                y = rect.yc - cl
            };
            var results = tree.Search(roi);

            // Todo: replace with real distance between polygons
            var ordered_distances = results.Select(r => {
                var current = r.domino.GetBoundingRectangle();
                var dx = Math.Abs(current.xc - rect.xc) - (current.width + rect.width) / 2;
                var dy = Math.Abs(current.yc - rect.yc) - (current.height + rect.height) / 2;
                return new Tuple<EditingDominoVM, double>(r, EightNeighbor ? Math.Min(dx, dy) : Math.Max(dx, dy));
            }).OrderByDescending(x => x.Item2);

            return ordered_distances.Where(x => x.Item2 < cl / 10).Select(x => x.Item1.idx).ToList();
        }
        public override void ResetLocator(EditProjectVM e, AvaloniaList<EditingDominoVM> dominoes)
        {
            if(dominoes != this.dominoes)
            {
                parent = e;
                this.dominoes = dominoes;
                tree = new RTree<EditingDominoVM>(9, new GuttmannQuadraticSplit<EditingDominoVM>());
                var list = parent.Dominoes.OrderByDescending(x =>
                {
                    var container = x.domino.GetContainer();
                    return container.y + container.height / 2;
                }).ThenBy(x =>
                {
                    var container = x.domino.GetContainer();
                    return container.x + container.width / 2;
                }).ToList();

                for (int i = 0; i < parent.Dominoes.Count; i++)
                {
                    // todo: list not used?
                    tree.Insert(parent.Dominoes[i]);
                }
            }
        }
    }
    public class DisplaySettingsToolVM : EditingToolVM
    {
        private double visibleWidth = 0;
        private double visibleHeight = 0;
        private readonly double largestX = 0;
        private readonly double largestY = 0;

        public DisplaySettingsToolVM(EditProjectVM parent) : base(parent)
        {
            Image = "display_settingsDrawingImage";
            Name = GetParticularString("DisplaySettingsTool", "View Properties");
            ShowImageClick = new RelayCommand(o => { ShowImage(); });

            if (parent.CurrentProject != null && parent.CurrentProject.PrimaryImageTreatment != null)
            {
                if (parent.CurrentProject.PrimaryImageTreatment.FilteredImage != null)
                {
                    FilteredImage = parent.CurrentProject.PrimaryImageTreatment.FilteredImage;
                }
                else
                {
                    Core.BlendFileFilter bff = parent.CurrentProject.PrimaryImageTreatment.ImageFilters.OfType<Core.BlendFileFilter>().FirstOrDefault();
                    if (bff != null)
                    {
                        string relativePath = bff.FilePath;
                        string absolutePath = Core.Workspace.AbsolutePathFromReference(ref relativePath, parent.CurrentProject);
                        if (File.Exists(absolutePath))
                        {
                            FilteredImage = SKBitmap.Decode(absolutePath);
                            bff.FilePath = relativePath;
                        }
                    }
                }
            }
            SliceImage();
            Expandable = parent.CurrentProject is FieldParameters;
        }
        public void SliceImage()
        {
            
            if (parent.CurrentProject is IRowColumnAddableDeletable irc && FilteredImage != null)
            {   
                // The basic idea here is that we save the history which columns are "original" and which have been inserted. 
                // Whenever the Width or Height of a structure is changed, the ColumnHistory is resetted to be a list: Range(0, Height). 
                // Note: Border Columns / Rows are not in the history, as they are never deleted.
                // When we 
                //  - delete a row, we delete the corresponding index in the history. That way we notice that there is a part of the image we don't have to draw.
                //    - undo deletion, we reinsert the history definition, so the image gets drawn as before.
                //  - add a row, we add an entry to the history with OriginalIndex = null, indicating that for this row, nothing has to be drawn
                //    - undo a deletion, we remove the inserted history definition.


                int OriginalNumberOfColumns = irc.getOriginalWidth();
                int OriginalNumberOfRows = irc.getOriginalHeight();

                // This is basically the same as Last.GetPhysicalExpandedWidth / Height. We need this to know the scaling factor of the image. The image will not be scaled proportionally, but will be fitted to
                // the structure.
                double totalOriginalWidth = Enumerable.Range(-1, OriginalNumberOfColumns + 2).Select(x => irc.GetColumnPhysicalWidth(x, OriginalNumberOfColumns)).Sum();
                double totalOriginalHeight = Enumerable.Range(-1, OriginalNumberOfRows + 2).Select(x => irc.GetRowPhysicalHeight(x, OriginalNumberOfRows)).Sum();

                // These functions serve as shortcuts to get the Width/Height of a Column/Row index, in the original / current state. 
                // Again the indices are -1 for the left / top border, 0..Height-1 for the "normal" rows and Height for the last row.  
                double original_colwidth(int i)  => irc.GetColumnPhysicalWidth(i, OriginalNumberOfColumns) / totalOriginalWidth * FilteredImage.Width;
                double original_rowheight(int i)  => irc.GetRowPhysicalHeight(i, OriginalNumberOfRows) / totalOriginalHeight * FilteredImage.Height;

                double current_colwidth(int i)  => irc.GetColumnPhysicalWidth(i, irc.current_width) / totalOriginalWidth * FilteredImage.Width;
                double current_rowheight(int i)  => irc.GetRowPhysicalHeight(i, irc.current_height) / totalOriginalHeight * FilteredImage.Height;

                // create a copy of the insertion history, but with first and last row/column added
                // This way we don't have to give special treatment to the borders of walls
                List<RowColumnHistoryDefinition> CloneAndPad(IEnumerable<RowColumnHistoryDefinition> hist, int LastIndex)
                {
                    var list = hist.ToList();
                    list.Add(new RowColumnHistoryDefinition() {OriginalPosition = LastIndex});
                    list.Insert(0, new RowColumnHistoryDefinition() { OriginalPosition= -1});
                    return list;
                }
                // Note that we insert the right/bottom border with the Original index -> If there is a column inserted at the last possible position (directly before the border), 
                // it is correctly classified as "inserted" that way! (Indices in the History = ALWAYS original index)
                var ColHistory = CloneAndPad(irc.ColumnHistory, OriginalNumberOfColumns);
                var RowHistory = CloneAndPad(irc.RowHistory, OriginalNumberOfRows);
                // This function takes a history and an index i and returns the last subsequent index where History[j + i] = History[i] + j.
                // For inserted rows, we will see 7- 8- 9- null - 10 and stop at 9, and for deleted rows, we will see 7- 8- 9- 11-12 and stop at 9 as well.
                int GetLastSubsequent(List<RowColumnHistoryDefinition> history, int index)
                {
                    int lastvalue;
                    if (history[index].OriginalPosition == null)
                        return index;
                    else
                        lastvalue = (int) history[index].OriginalPosition ;
                    while (true)
                    {
                        if (index+1 == history.Count || history[index+1].OriginalPosition == null || history[index+1].OriginalPosition != lastvalue +1)
                            return index;
                        else
                            index += 1;
                            lastvalue += 1;
                    }
                }
                // Initialize the target picture
                int TotalTargetPixelWidth = (int) Enumerable.Range(-1, irc.current_width + 2).Select(x => current_colwidth(x)).Sum();
                int TotalTargetPixelHeight = (int) Enumerable.Range(-1, irc.current_height + 2).Select(x => current_rowheight(x)).Sum();
                Console.WriteLine($"Target size: ({TotalTargetPixelWidth}, {TotalTargetPixelHeight})");
                SKBitmap result = new SKBitmap(TotalTargetPixelWidth, TotalTargetPixelHeight);
                SKCanvas canvas = new SKCanvas(result);
                // This is where the actual magic happens. 
                // Depending on the start index "index", We basically have 3 cases to handle:
                //  - Index is an inserted row. Increase the target coordinate, but don't change the source coordinate. We indicate with draw=false that we don't want to draw this region.
                //  - Index is an original row. So we definitely have to draw it. 
                //     - First, figure out up until which index we have to draw (GetLastSubsequent method). 
                //     - Accordingly, increase the source and target coordinate by the same amount.
                //     - Now check whether after the last drawn row, there are rows which have been deleted. In that case, we have to increase the source coordinate even further, 
                //       without modifying the target coordinate. We check this by searching the next original row, and if the difference is larger than 1, the rows in between have been deleted.
                
                (double, double, int, bool, double) HandleIndex(List<RowColumnHistoryDefinition> history, int index, bool column)
                {
                    double increase_source_coord_by = 0;
                    double increase_target_coord_by = 0;
                    double additional_source_increase_by = 0;
                    int increase_index_by = 0;
                    bool draw = false;
                    var orig_index = index-1;
                    if (history[index].OriginalPosition == null)
                    {
                        // this row has been inserted. Increase the target coordinate, and leave the source coordinate be.
                        increase_index_by = 1;
                        increase_target_coord_by = column? current_colwidth(index) : current_rowheight(index);
                    }
                    else
                    {
                        // this is an original row, so we need to draw it. 
                        draw = true;
                        var until = GetLastSubsequent(history, index);
                        increase_index_by = until - index + 1;
                        increase_source_coord_by = Enumerable.Range(index, increase_index_by).Select(x => column ? original_colwidth((int)history[x].OriginalPosition) : original_rowheight((int)history[x].OriginalPosition)).Sum();
                        increase_target_coord_by = increase_source_coord_by;
                        // Has the next row(s) been deleted? In this case we need to increase the source coordinate further by the amount of deleted rows. 
                        var nextOriginal = history.FindIndex(index + increase_index_by, x => x.OriginalPosition != null);
                        if (nextOriginal != -1)
                        {
                            int count = (int)history[nextOriginal].OriginalPosition - (int)history[until].OriginalPosition - 1;
                            if (count >= 0) // otherwise something went horribly wrong
                                additional_source_increase_by = Enumerable.Range(index, (int)history[nextOriginal].OriginalPosition - (int)history[until].OriginalPosition - 1).Select(x => column ? original_colwidth(x) : original_rowheight(x)).Sum();
                        }
                    }
                    
                    return (increase_source_coord_by, increase_target_coord_by, increase_index_by, draw, additional_source_increase_by);
                }
                // Now lets get to the actual drawing code. As we insert Rows/Columns, we can partition the image and treat rows / columns in a nested fashion.
                double x_source = 0;
                double x_target = 0; 
                for (int i = 0; i < ColHistory.Count; )
                {
                    double y_source = 0;
                    double y_target = 0;
                    var (delta_source_x, delta_target_x, delta_i, draw, add_source_x) = HandleIndex(ColHistory, i, true);
                    if (draw)
                    {
                        for (int j = 0; j < RowHistory.Count; )
                        {
                            var (delta_source_y, delta_target_y, delta_j, draw2, add_source_y) = HandleIndex(RowHistory, j, false);
                            if (draw2)
                            {
                                canvas.DrawBitmap(FilteredImage, 
                                                 new SKRect((float)x_source, (float) y_source, (float) (x_source + delta_source_x), (float) (y_source + delta_source_y)), 
                                                 new SKRect((float)x_target, (float) y_target, (float) (x_target + delta_target_x), (float) (y_target + delta_target_y)));
                                Console.WriteLine($"Draw, Source=({x_source}, {y_source}, w={delta_source_x}, w={delta_source_y}), Target = ({x_target}, {y_target}, w={delta_target_x}, w={delta_target_y})");
                                 
                            }
                            j += delta_j;
                            y_source += delta_source_y + add_source_y;
                            y_target += delta_target_y;
                        }
                    }
                    x_source += delta_source_x + add_source_x;
                    x_target += delta_target_x;
                    i += delta_i;
                }
                SlicedImage = result.Copy();
            }
            else
            {
                // for Spirals / Circles, we don't have do deal with this at all :)
                SlicedImage = FilteredImage;
            }
        }
        private SKBitmap FilteredImage;
        private SKBitmap _SlicedImage;

        public SKBitmap SlicedImage
        {
            get { return _SlicedImage; }
            set
            {
                if (_SlicedImage != value)
                {
                    
                    _SlicedImage = value;
                    
                    RaisePropertyChanged();
                }
            }
        }

        private bool _Expanded = true;
        public bool Expanded
        {
            get => _Expanded;
            set
            {
                if (_Expanded != value)
                {
                    _Expanded = value;
                    RaisePropertyChanged();
                    parent.RecreateCanvasViewModel();
                    ForceRedraw = true;
                }
            }
        }
        private bool _Expandable;
        public bool Expandable
        {
            get { return _Expandable; }
            set
            {
                if (_Expandable != value)
                {
                    _Expandable = value;
                    RaisePropertyChanged();
                }
            }
        }
        private Color backgroundColor = Color.FromArgb(0, 255, 255, 255);
        public Color BackgroundColor
        {
            get => backgroundColor;
            set
            {
                backgroundColor = value;
                RaisePropertyChanged();
            }
        }
        private Color borderColor = Color.FromArgb(127, 0, 0, 255);
        public Color BorderColor
        {
            get => borderColor;
            set
            {
                borderColor = value;
                RaisePropertyChanged();
            }
        }
        private Color selectedColor = Colors.Blue;
        public Color SelectedColor
        {
            get => selectedColor;
            set
            {
                selectedColor = value;
                RaisePropertyChanged();
            }
        }

        private double borderSize = 0.5;

        public double BorderSize
        {
            get { return borderSize; }
            set
            {
                borderSize = value;
                RaisePropertyChanged();
            }
        }

        private double opacity = 0;
        public double ImageOpacity
        {
            get { return opacity; }
            set
            {
                opacity = value;
                RaisePropertyChanged();
            }
        }

        private double dominoopacity = 1;
        public double DominoOpacity
        {
            get { return dominoopacity; }
            set
            {
                dominoopacity = value;
                RaisePropertyChanged();
            }
        }

        private bool above = false;
        public bool Above
        {
            get { return above; }
            set
            {
                above = value;
                RaisePropertyChanged();
            }
        }
        private double _ZoomValue = 1;
        public double ZoomValue
        {
            get { return _ZoomValue; }
            set
            {
                if (_ZoomValue != value)
                {
                    _ZoomValue = value;
                    RaisePropertyChanged();
                }
            }
        }
        private double horizontalOffset;

        public double HorizontalOffset
        {
            get { return horizontalOffset; }
            set { horizontalOffset = value; RaisePropertyChanged(); }
        }

        private double verticalOffset;

        public double VerticalOffset
        {
            get { return verticalOffset; }
            set { verticalOffset = value; RaisePropertyChanged(); }
        }

        private string PreviewPath
        {
            get
            {
                string imagepath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                imagepath = System.IO.Path.Combine(imagepath, parent.name + "_prev.png");
                return imagepath;
            }
        }
        private bool forceRedraw;

        public bool ForceRedraw
        {
            get { return forceRedraw; }
            set { forceRedraw = value; RaisePropertyChanged(); }
        }

        internal void SizeChanged(double width, double height)
        {
            visibleWidth = width;
            visibleHeight = height;
        }
       
        private void ShowImage()
        {
            try
            {
                if (!File.Exists(PreviewPath))
                {
                    using var image = SKImage.FromBitmap(FilteredImage);
                    using var data = image.Encode(SKEncodedImageFormat.Png, 80);
                    // save the data to a stream
                    using var stream = File.OpenWrite(PreviewPath);
                    data.SaveTo(stream);
                }
                Process.Start(PreviewPath);
            }
            catch (Exception)
            {

            }
        }
        internal void DeleteImage()
        {
            try
            {
                if (File.Exists(PreviewPath)) File.Delete(PreviewPath);
            }
            catch (Exception) { }
        }
        private ICommand _ShowImageClick;
        public ICommand ShowImageClick { get { return _ShowImageClick; } set { if (value != _ShowImageClick) { _ShowImageClick = value; } } }
    }
    public class RulerToolVM : EditingToolVM
    {
        private double _length;

        public double Length
        {
            get { return _length; }
            set { _length = value; RaisePropertyChanged(); }
        }
        private bool _snapping;

        public bool Snapping
        {
            get { return _snapping; }
            set { _snapping = value; RaisePropertyChanged(); }
        }

        private Avalonia.Point start;
        private Avalonia.Point end;
        private double linewidth = 8;
        private int dragging;
        public RulerToolVM(EditProjectVM parent) : base(parent)
        {
            Image = "ruler2DrawingImage";
            Name = _("Measure distance");
            MakeInvisible();
        }
        public override void KeyPressed(KeyEventArgs keyArgs)
        {
            var key = keyArgs.Key;
            if (key == Key.LeftCtrl || key == Key.RightCtrl)
            {
                Snapping = !Snapping;
                keyArgs.Handled = true;
            }
                
        }
        public override void MouseUp(Avalonia.Point pos, PointerReleasedEventArgs e)
        {
            if (e.GetCurrentPoint(null).Properties.PointerUpdateKind != PointerUpdateKind.LeftButtonPressed) return;

            dragging = 0;
        }
        public override void MouseDown(Avalonia.Point pos, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(null).Properties.PointerUpdateKind != PointerUpdateKind.LeftButtonPressed) return;

            if (parent.AdditionalDrawables.Count == 4)
            {
                if (Distance(pos, start) < (4 * linewidth))
                    dragging = 1;
                else if (Distance(pos, end) < (4 * linewidth))
                    dragging = 2;
                else
                    dragging = 0;
            }
            else // no ruler there yet
            {
                dragging = 0;
                MakeVisible();
            }
            if (dragging == 0)
            {
                start = pos;
                end = pos;
                UpdateShapes();
                dragging = 2;
            }


        }
        public double Distance(Avalonia.Point a, Avalonia.Point b)
        {
            return Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
        }
        public override void MouseMove(Avalonia.Point pos, PointerEventArgs e)
        {
            if (!e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
            {
                dragging = 0;
                return;
            }
            var X = pos.X;
            var Y = pos.Y;
            if (pos.X < 0) X = 0;
            if (pos.Y < 0) Y = 0;
            if (pos.X > parent.PhysicalLength) X = parent.PhysicalLength;
            if (pos.Y > parent.PhysicalHeight) Y = parent.PhysicalHeight;

            if (Snapping)
            {
                // get angle between stationary and dragged point
                var stationary = dragging == 1 ? end : start;
                var angle = Math.Atan2(pos.X - stationary.X, pos.Y - stationary.Y);
                // round to multiples of Pi/8 (22.5 deg)
                var steps = 360 / 5;
                angle = Math.Round(angle * steps / Math.PI) * Math.PI / steps;
                var distance = Distance(pos, stationary);
                X = Math.Sin(angle) * distance + stationary.X;
                Y = Math.Cos(angle) * distance + stationary.Y;
            }
            if (dragging == 1)
                start = new Avalonia.Point(X, Y);
            else if (dragging == 2)
                end = new Avalonia.Point(X, Y);
            Length = Distance(start, end);
            UpdateShapes();
        }
        public override void EnterTool()
        {
            Length = 0;
        }
        public override void LeaveTool()
        {
            MakeInvisible();
        }
        public void MakeVisible()
        {
            parent.AdditionalDrawables = new AvaloniaList<CanvasDrawable>();
        }
        public void MakeInvisible()
        {
            parent.AdditionalDrawables = new AvaloniaList<CanvasDrawable>();
        }
        public void UpdateShapes()
        {
            linewidth = 10 / parent.DisplaySettingsTool.ZoomValue;
            var p1 = new SKPath();
            p1.MoveTo((float)start.X,(float) start.Y);
            p1.Close();
            var p2 = new SKPath();
            p2.MoveTo((float)end.X,(float) end.Y);
            p2.Close();

            var circlepaint = new SKPaint() { Color = SKColors.Blue, IsAntialias = true, StrokeCap = SKStrokeCap.Round, StrokeWidth = 10, IsStroke = true};

            var line = new SKPath();
            line.MoveTo((float)start.X,(float) start.Y);
            line.LineTo((float)end.X,(float) end.Y);

            parent.AdditionalDrawables = new AvaloniaList<CanvasDrawable>()
            {
                
                new CanvasDrawable { Path = line, Paint = new SKPaint() {Color = SKColors.White, StrokeWidth = (float) 3, IsStroke = true, IsAntialias=true}},
                new CanvasDrawable { Path = line, Paint = new SKPaint() {Color = SKColors.Black, StrokeWidth = (float) 1, IsStroke = true, IsAntialias=true}},
                new CanvasDrawable { Path = p1, Paint = circlepaint},
                new CanvasDrawable { Path = p2, Paint = circlepaint},
            };
            
        }
        
    }
    public class ZoomToolVM : EditingToolVM
    {
        public ZoomToolVM(EditProjectVM parent) : base(parent)
        {
            Image = "zoomDrawingImage";
            Name = _("Zoom");
            // todo: zoom relative to center? see ProjectCanvas
            ZoomIn = new RelayCommand((o) => parent.DisplaySettingsTool.ZoomValue = Math.Min(parent.DisplaySettingsTool.ZoomValue * 1.1, MaxZoomValue));
            ZoomOut = new RelayCommand((o) => parent.DisplaySettingsTool.ZoomValue = Math.Max(parent.DisplaySettingsTool.ZoomValue / 1.1, MinZoomValue));
            Zoom1To1 = new RelayCommand((o) => parent.DisplaySettingsTool.ZoomValue = 1);
            ZoomToFit = new RelayCommand((o) =>
            {
                parent.DisplaySettingsTool.ZoomValue = FitAllZoomValue;
                parent.DisplaySettingsTool.HorizontalOffset = 0; parent.DisplaySettingsTool.VerticalOffset = 0;
            });
        }
        private ICommand _ZoomIn;
        public ICommand ZoomIn { get { return _ZoomIn; } set { if (value != _ZoomIn) { _ZoomIn = value; } } }

        private ICommand _ZoomOut;
        public ICommand ZoomOut { get { return _ZoomOut; } set { if (value != _ZoomOut) { _ZoomOut = value; } } }

        private double fitAllZoomValue;

        public double FitAllZoomValue
        {
            get { return fitAllZoomValue; }
            set { fitAllZoomValue = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(MinZoomValue)); }
        }

        public double MinZoomValue
        {
            get {return fitAllZoomValue/4; }
        }
        public double MaxZoomValue {
            get {return 4; }
        }

        private ICommand _Zoom1To1;

        public ICommand Zoom1To1
        {
            get { return _Zoom1To1; }
            set { _Zoom1To1 = value; RaisePropertyChanged(); }
        }
        private ICommand _ZoomToFit;

        public ICommand ZoomToFit
        {
            get { return _ZoomToFit; }
            set { _ZoomToFit = value; RaisePropertyChanged(); }
        }
    }
    public class RowColumnInsertionVM : EditingToolVM
    {
        public override void EnterTool()
        {
            parent.ClearFullSelection();
            UpdateInsertionPositions();
            DrawPositions();
        }
        public override void OnRedo()
        {
            base.OnRedo();
            UpdateInsertionPositions();
            DrawPositions();
        }
        public override void OnUndo()
        {
            base.OnUndo();
            UpdateInsertionPositions();
            DrawPositions();
        }
        public override void LeaveTool()
        {
            parent.AdditionalDrawables.Clear();
            foreach (var i in parent.Dominoes)
                i.State &= ~EditingDominoStates.DeletionHighlight;
        }
        private void UpdateInsertionPositions()
        {
            if (parent.CurrentProject is IRowColumnAddableDeletable irc)
            {
                column_positions = AddDeleteHelper.GetInsertionPositions(irc, true, parent.DisplaySettingsTool.Expanded);
                row_positions = AddDeleteHelper.GetInsertionPositions(irc, false, parent.DisplaySettingsTool.Expanded);
            }
        }
        public RowColumnInsertionVM(EditProjectVM parent) : base(parent)
        {
            Image = "add_delete_rowDrawingImage";
            Name = _("Add/Delete Rows/Columns");
            
        }
        private bool insertionMode = true;

        public bool InsertionMode
        {
            get { return insertionMode; }
            set
            {
                insertionMode = value; RaisePropertyChanged();
                if (!insertionMode)
                {
                    UpdateInsertionPositions();
                    DrawPositions();
                }
                if (insertionMode)
                {
                    foreach (var i in parent.Dominoes)
                        i.State &= ~EditingDominoStates.DeletionHighlight;
                    parent.DisplaySettingsTool.ForceRedraw = true;
                }
            }
        }

        List<InsertionHelper> column_positions;
        List<InsertionHelper> row_positions;

        private bool direction = true;

        public bool Direction
        {
            get { return direction; }
            set
            {
                direction = value; RaisePropertyChanged();
                UpdateInsertionPositions();
                DrawPositions();
            }
        }

        private bool livePreview = true;

        public bool LivePreviewEnabled
        {
            get { return livePreview; }
            set { livePreview = value; RaisePropertyChanged();  }
        }
        public override void MouseMove(Avalonia.Point dominoPoint, PointerEventArgs e)
        {
            base.MouseMove(dominoPoint, e);

            if (InsertionMode)
            {
                PreviewInsertion(dominoPoint);
            }
            else 
            {
                PreviewRemoval(dominoPoint);
            }

        }
        
        public override void MouseUp(Avalonia.Point dominoPoint, PointerReleasedEventArgs e)
        {
            var closest_domino = parent.FindDominoAtPosition(dominoPoint, int.MaxValue);
            if (InsertionMode)
            {
                parent.ClearFullSelection();
                var closest_line = GetClosestLine(dominoPoint);
                if (Direction)
                {
                    parent.AddRow(!closest_line.Before, closest_line.Index, closest_domino.domino);
                }
                if (!Direction)
                {
                    parent.AddColumn(!closest_line.Before, closest_line.Index, closest_domino.domino);
                }
            }
            else
            {
                if (parent.CurrentProject is IRowColumnAddableDeletable rc)
                {
                    var pos = rc.getPositionFromIndex(closest_domino.idx);
                    if (Direction)
                    {
                        if (pos.Y >= 0 && pos.Y < rc.current_height)
                        {
                            parent.RemoveSelRows(closest_domino.idx);
                        }
                    }
                    else
                    {
                        if (pos.X >= 0 && pos.X < rc.current_width)
                        {
                            parent.RemoveSelColumns(closest_domino.idx);
                        }
                    }
                }
            }
            UpdateInsertionPositions();
            DrawPositions();
        }
        CanvasDrawable PreviewLine1;
        CanvasDrawable PreviewLine2;
        private InsertionHelper GetClosestLine(Avalonia.Point dominoPoint)
        {
            return Direction ? row_positions.OrderBy(x => Math.Abs(x.DrawPosition - dominoPoint.Y)).First() : column_positions.OrderBy(x => Math.Abs(x.DrawPosition - dominoPoint.X)).First();
        }
        private void PreviewInsertion(Avalonia.Point dominoPoint)
        {
            var closest_line = GetClosestLine(dominoPoint);
            var closest_domino = parent.FindDominoAtPosition(dominoPoint, int.MaxValue);

            var path = closest_line.GetPath(parent.PhysicalLength, parent.PhysicalHeight, Direction);
            parent.AdditionalDrawables.Remove(PreviewLine1);
            parent.AdditionalDrawables.Remove(PreviewLine2);
            PreviewLine1 = new CanvasDrawable()
            {
                Paint = new SKPaint()
                {
                    Color = new SKColor(closest_domino.StoneColor.R, closest_domino.StoneColor.G, closest_domino.StoneColor.B, closest_domino.StoneColor.A),
                    IsStroke = true,
                    StrokeWidth = 2
                },
                Path = path,
            };
            PreviewLine2 = new CanvasDrawable() { Paint = new SKPaint() { Color = SKColors.Gray, IsStroke = true, StrokeWidth = 4 }, Path = path };
            parent.AdditionalDrawables.Add(PreviewLine2);
            parent.AdditionalDrawables.Add(PreviewLine1);

        }
        private void PreviewRemoval(Avalonia.Point dominoPoint)
        {
            foreach (var i in parent.Dominoes)
                i.State &= ~EditingDominoStates.DeletionHighlight;
            
            parent.ClearFullSelection();
            var closest_domino = parent.FindDominoAtPosition(dominoPoint, int.MaxValue);
            int[] indices = null;
            if (parent.CurrentProject is IRowColumnAddableDeletable rc)
            {
                var pos = rc.getPositionFromIndex(closest_domino.idx);
                if (Direction)
                {
                    if (pos.Y >= 0 && pos.Y < rc.current_height)
                    {
                        indices = AddDeleteHelper.getAllIndicesInRowColumn(rc, pos.Y, false, parent.CurrentProject.Last.Length, rc.current_width, rc.current_height);
                    }
                }
                else
                {
                    if (pos.X >= 0 && pos.X < rc.current_width)
                    {
                        indices = AddDeleteHelper.getAllIndicesInRowColumn(rc, pos.X, true, parent.CurrentProject.Last.Length, rc.current_width, rc.current_height);
                    }
                }
            }
            if (indices != null)
            {
                foreach (int i in indices)
                {
                    parent.Dominoes[i].State |= EditingDominoStates.DeletionHighlight;
                }
            }
        }
        private void DrawPositions()
        {
            parent.AdditionalDrawables.Clear();
            if (column_positions == null || row_positions == null)
                UpdateInsertionPositions();
            foreach (var pos in Direction ? row_positions : column_positions )
            {
                var path = pos.GetPath(parent.PhysicalLength, parent.PhysicalHeight, Direction);
                parent.AdditionalDrawables.Add(new CanvasDrawable() { Paint = new SKPaint() { Color = new SKColor(0, 0, 0, 128), IsStroke = true, StrokeWidth = 2 }, Path = path, BeforeBorders = true });
            }
        }
    }
    public static class InsertionHelperExtension
    {
        public static SKPath GetPath(this InsertionHelper pos, int width, int height, bool Direction)
        {
            SKPath path = new SKPath();
            if (Direction)
            {
                path.MoveTo(0, (float)pos.DrawPosition);
                path.LineTo(width, (float)pos.DrawPosition);
            }
            else
            {
                path.MoveTo((float)pos.DrawPosition, 0);
                path.LineTo((float)pos.DrawPosition, height);
            }
            return path;
        }
    }

}
