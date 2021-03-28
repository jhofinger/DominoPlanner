using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using Avalonia.Controls;
using Avalonia;
using System.Linq;
using System.IO;
using Avalonia.Media;
using DominoPlanner.Usage.UserControls.View;
using Avalonia.Collections;
using DominoPlanner.Usage.UserControls.ViewModel;
using Avalonia.Input;
using SkiaSharp;
using Avalonia.Skia;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using System.Diagnostics;
using DominoPlanner.Core;
using Avalonia.Data.Converters;
using System.Globalization;
using Avalonia.Controls.Shapes;
using Avalonia.VisualTree;
using System.Collections.Specialized;

namespace DominoPlanner.Usage
{
    public class ProjectCanvas : Control
    {


        public double ShiftX
        {
            get { return GetValue(ShiftXProperty); }
            set { SetValue(ShiftXProperty, value); }
        }

        public static readonly StyledProperty<double> ShiftXProperty =  AvaloniaProperty.Register<ProjectCanvas, double>(nameof(ShiftX));

        public double ShiftY
        {
            get { return GetValue(ShiftYProperty); }
            set { SetValue(ShiftYProperty, value); }
        }

        public static readonly StyledProperty<double> ShiftYProperty = AvaloniaProperty.Register<ProjectCanvas, double>(nameof(ShiftY));

        public double Zoom
        {
            get { return GetValue(ZoomProperty); }
            set { SetValue(ZoomProperty, value); }
        }

        public static readonly StyledProperty<double> ZoomProperty = AvaloniaProperty.Register<ProjectCanvas, double>(nameof(Zoom));

        public Color UnselectedBorderColor
        {
            get { return GetValue(UnselectedBorderColorProperty); }
            set { SetValue(UnselectedBorderColorProperty, value); }
        }

        public static readonly StyledProperty<Color> UnselectedBorderColorProperty = AvaloniaProperty.Register<ProjectCanvas, Color>(nameof(UnselectedBorderColor));

        public Color SelectedBorderColor
        {
            get { return GetValue(SelectedBorderColorProperty); }
            set { SetValue(SelectedBorderColorProperty, value); }
        }

        public static readonly StyledProperty<Color> SelectedBorderColorProperty = AvaloniaProperty.Register<ProjectCanvas, Color>(nameof(SelectedBorderColor));

        public float ImageOpacity
        {
            get { return GetValue(ImageOpacityProperty); }
            set { SetValue(ImageOpacityProperty, value); }
        }

        public static readonly StyledProperty<float> ImageOpacityProperty = AvaloniaProperty.Register<ProjectCanvas, float>(nameof(ImageOpacity));

        public AvaloniaList<EditingDominoVM> Project
        {
            get { return GetValue(ProjectProperty); }
            set { SetValue(ProjectProperty, value); }
        }

        public static readonly StyledProperty<AvaloniaList<EditingDominoVM>> ProjectProperty = AvaloniaProperty.Register<ProjectCanvas, AvaloniaList<EditingDominoVM>>(nameof(Project));

        public SKPath SelectionDomain
        {
            get { return GetValue(SelectionDomainProperty); }
            set { SetValue(SelectionDomainProperty, value); }
        }

        public static readonly StyledProperty<SKPath> SelectionDomainProperty = AvaloniaProperty.Register<ProjectCanvas, SKPath>(nameof(SelectionDomain));

        public Color SelectionDomainColor
        {
            get { return GetValue(SelectionDomainColorProperty); }
            set { SetValue(SelectionDomainColorProperty, value); }
        }
        public static readonly StyledProperty<Color> SelectionDomainColorProperty = AvaloniaProperty.Register<ProjectCanvas, Color>(nameof(SelectionDomainColor));

        public bool SelectionDomainVisible
        {
            get { return GetValue(SelectionDomainVisibleProperty); }
            set { SetValue(SelectionDomainVisibleProperty, value); }
        }
        public static readonly StyledProperty<bool> SelectionDomainVisibleProperty = AvaloniaProperty.Register<ProjectCanvas, bool>(nameof(SelectionDomainVisible));

        public SKBitmap SourceImage
        {
            get { return GetValue(SourceImageProperty); }
            set { SetValue(SourceImageProperty, value); }
        }
        public static readonly StyledProperty<SKBitmap> SourceImageProperty = AvaloniaProperty.Register<ProjectCanvas, SKBitmap>(nameof(SourceImage));

        public float SourceImageOpacity
        {
            get { return GetValue(SourceImageOpacityProperty); }
            set { SetValue(SourceImageOpacityProperty, value); }
        }
        public static readonly StyledProperty<float> SourceImageOpacityProperty = AvaloniaProperty.Register<ProjectCanvas, float>(nameof(SourceImageOpacity), 0.2f);

        public bool SourceImageAbove
        {
            get { return GetValue(SourceImageAboveProperty); }
            set { SetValue(SourceImageAboveProperty, value); }
        }
        public static readonly StyledProperty<bool> SourceImageAboveProperty = AvaloniaProperty.Register<ProjectCanvas, bool>(nameof(SourceImageAbove));

        public float DominoOpacity
        {
            get { return GetValue(DominoOpacityProperty); }
            set { SetValue(DominoOpacityProperty, value); }
        }
        public static readonly StyledProperty<float> DominoOpacityProperty = AvaloniaProperty.Register<ProjectCanvas, float>(nameof(DominoOpacity), 0.2f);


        public Color BackgroundColor
        {
            get { return GetValue(BackgroundColorProperty); }
            set { SetValue(BackgroundColorProperty, value); }
        }
        public static readonly StyledProperty<Color> BackgroundColorProperty = AvaloniaProperty.Register<ProjectCanvas, Color>(nameof(BackgroundColor), Colors.Transparent);

        public float BorderSize
        {
            get { return GetValue(BorderSizeProperty); }
            set { SetValue(BorderSizeProperty, value); }
        }

        public static readonly StyledProperty<float> BorderSizeProperty = AvaloniaProperty.Register<ProjectCanvas, float>(nameof(BorderSize));

        public bool ForceRedraw
        {
            get { return GetValue(ForceRedrawProperty); }
            set { SetValue(ForceRedrawProperty, value); }
        }
        public static readonly StyledProperty<bool> ForceRedrawProperty = AvaloniaProperty.Register<ProjectCanvas, bool>(nameof( ForceRedraw), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

        public AvaloniaList<CanvasDrawable> AdditionalDrawables
        {
            get { return GetValue(AdditionalDrawablesProperty); }
            set { SetValue(AdditionalDrawablesProperty, value); }
        }
        public static readonly StyledProperty<AvaloniaList<CanvasDrawable>> AdditionalDrawablesProperty = AvaloniaProperty.Register<ProjectCanvas, AvaloniaList<CanvasDrawable>>(nameof(Project));


        public double VerticalSliderSize
        {
            get { return GetValue(VerticalSliderSizeProperty); }
            set { SetValue(VerticalSliderSizeProperty, value); }
        }
        public static readonly StyledProperty<double> VerticalSliderSizeProperty = AvaloniaProperty.Register<ProjectCanvas, double>(nameof(VerticalSliderSize));

        public double VerticalSliderPos
        {
            get { return GetValue(VerticalSliderPosProperty); }
            set { SetValue(VerticalSliderPosProperty, value); }
        }
        public static readonly StyledProperty<double> VerticalSliderPosProperty = AvaloniaProperty.Register<ProjectCanvas, double>(nameof(VerticalSliderPos));

        public double HorizontalSliderSize
        {
            get { return GetValue(HorizontalSliderSizeProperty); }
            set { SetValue(HorizontalSliderSizeProperty, value); }
        }
        public static readonly StyledProperty<double> HorizontalSliderSizeProperty = AvaloniaProperty.Register<ProjectCanvas, double>(nameof(HorizontalSliderSize));

        public double HorizontalSliderPos
        {
            get { return GetValue(HorizontalSliderPosProperty); }
            set { SetValue(HorizontalSliderPosProperty, value); }
        }
        public static readonly StyledProperty<double> HorizontalSliderPosProperty = AvaloniaProperty.Register<ProjectCanvas, double>(nameof(HorizontalSliderPos));


        public double FitAllZoomValue
        {
            get { return GetValue(FitAllZoomValueProperty); }
            set { SetValue(FitAllZoomValueProperty, value); }
        }
        public static readonly StyledProperty<double> FitAllZoomValueProperty = AvaloniaProperty.Register<ProjectCanvas, double>(nameof(FitAllZoomValue));

        public double ProjectHeight { get; set; }

        public double ProjectWidth { get; set; }

        public SKBitmap OriginalImage;

        private double VirtualVerticalSliderMin;
        private double VirtualVerticalSliderMax;

        private double VirtualHorizontalSliderMin;
        private double VirtualHorizontalSliderMax;

        public ProjectCanvas()
        {
            AffectsRender<ProjectCanvas>(ShiftXProperty);
            AffectsRender<ProjectCanvas>(ShiftYProperty);
            AffectsRender<ProjectCanvas>(ZoomProperty);
            //Stones = new List<DominoInCanvas>();
            //Stones.Add(new DominoInCanvas(50, 50, 50, 50, Colors.AliceBlue));
            //DataContextProperty.Changed.AddClassHandler<ProjectCanvas>((o, e) => SubscribeEvents(e));
            AffectsRender<ProjectCanvas>(ProjectProperty);
            AffectsRender<ProjectCanvas>(SelectionDomainProperty);
            AffectsRender<ProjectCanvas>(SelectionDomainProperty);
            AffectsRender<ProjectCanvas>(SelectionDomainVisibleProperty);
            AffectsRender<ProjectCanvas>(SelectionDomainColorProperty);
            AffectsRender<ProjectCanvas>(SourceImageOpacityProperty);
            AffectsRender<ProjectCanvas>(SourceImageProperty);
            AffectsRender<ProjectCanvas>(SourceImageAboveProperty);
            AffectsRender<ProjectCanvas>(DominoOpacityProperty);
            ProjectProperty.Changed.AddClassHandler<ProjectCanvas>((o, e) => ProjectChanged());
            SourceImageProperty.Changed.AddClassHandler<ProjectCanvas>((o, e) => SourceImageChanged());
            SourceImageOpacityProperty.Changed.AddClassHandler<ProjectCanvas>((o, e) => SourceImageChanged());
            SourceImageAboveProperty.Changed.AddClassHandler<ProjectCanvas>((o, e) => SourceImageChanged());
            AffectsRender<ProjectCanvas>(BackgroundColorProperty);
            AffectsRender<ProjectCanvas>(UnselectedBorderColorProperty);
            AffectsRender<ProjectCanvas>(BorderSizeProperty);
            AffectsRender<ProjectCanvas>(ForceRedrawProperty);
            AffectsRender<ProjectCanvas>(AdditionalDrawablesProperty);
            //ShiftXProperty.Changed.AddClassHandler<ProjectCanvas>((o, e) => UpdateVerticalSlider(o, e));
            ShiftYProperty.Changed.AddClassHandler<ProjectCanvas>((o, e) => UpdateVerticalSlider(o, e));
            ZoomProperty.Changed.AddClassHandler<ProjectCanvas>((o, e) => UpdateVerticalSlider(o, e));
            VerticalSliderPosProperty.Changed.AddClassHandler<ProjectCanvas>((o, e) => UpdateVerticalSlider(o, e));
            ShiftXProperty.Changed.AddClassHandler<ProjectCanvas>((o, e) => UpdateHorizontalSlider(o, e));
            ZoomProperty.Changed.AddClassHandler<ProjectCanvas>((o, e) => UpdateHorizontalSlider(o, e));
            HorizontalSliderPosProperty.Changed.AddClassHandler<ProjectCanvas>((o, e) => UpdateHorizontalSlider(o, e));
            AdditionalDrawablesProperty.Changed.AddClassHandler<ProjectCanvas>((o, e) => AdditionalDrawablesChanged(o, e));
            BoundsProperty.Changed.AddClassHandler<ProjectCanvas>((o, e) => UpdateMinZoomValue());
            
        }

        private void AdditionalDrawablesChanged(ProjectCanvas o, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.OldValue != null && e.OldValue is AvaloniaList<CanvasDrawable> list)
            {
                list.CollectionChanged -= ForceRedrawMethod;
            }
            if (e.NewValue != null && e.NewValue is AvaloniaList<CanvasDrawable> list2)
            {
                list2.CollectionChanged += ForceRedrawMethod;
            }
        }
        private void ForceRedrawMethod(object sender, NotifyCollectionChangedEventArgs e)
        {
            ForceRedraw = true;
        }

        private void UpdateMinZoomValue()
        {
            FitAllZoomValue = Math.Min(Bounds.Width / ProjectWidth, Bounds.Height / ProjectHeight);
        }

        private void UpdateVerticalSlider(ProjectCanvas o, AvaloniaPropertyChangedEventArgs e)
        {
            if (e == null || e.Property == ShiftYProperty || e.Property == ZoomProperty)
            {
                double newval = e == null || e.Property == ZoomProperty ? ShiftY : (double) e.NewValue;
                // Binding to maximum or minimum doesn't work for some reason, so it's fixed
                VirtualVerticalSliderMax = Math.Max(ShiftY, ProjectHeight - Bounds.Height / Zoom);
                VirtualVerticalSliderMin = Math.Min(ShiftY, 0);
                var scaling_factor = 100.0 / (VirtualVerticalSliderMax - VirtualVerticalSliderMin);
                var candidate = newval * scaling_factor - VirtualVerticalSliderMin * scaling_factor;
                if (Math.Abs(candidate - VerticalSliderPos) > 0.01)
                    VerticalSliderPos = candidate;
                VerticalSliderSize = scaling_factor * Bounds.Height;
            }
            else if (e.Property == VerticalSliderPosProperty && !double.IsNaN((double)e.NewValue))
            {
                var scaling_factor = 100.0 / (VirtualVerticalSliderMax - VirtualVerticalSliderMin);
                ShiftY = (double) e.NewValue / scaling_factor +  VirtualVerticalSliderMin; 
            }

        }
        private void UpdateHorizontalSlider(ProjectCanvas o, AvaloniaPropertyChangedEventArgs e)
        {
            if (e == null || e.Property == ShiftXProperty || e.Property == ZoomProperty)
            {
                double newval = e == null || e.Property == ZoomProperty ? ShiftX : (double) e.NewValue;
                // Binding to maximum or minimum doesn't work for some reason, so it's fixed
                VirtualHorizontalSliderMax = Math.Max(ShiftX, ProjectWidth - Bounds.Width / Zoom);
                VirtualHorizontalSliderMin = Math.Min(ShiftX, 0);
                var scaling_factor = 100.0 / (VirtualHorizontalSliderMax - VirtualHorizontalSliderMin);
                var candidate = newval * scaling_factor - VirtualHorizontalSliderMin * scaling_factor;
                if (Math.Abs(candidate - HorizontalSliderPos) > 0.01)
                    HorizontalSliderPos = candidate;
                HorizontalSliderSize = scaling_factor * Bounds.Width;
            }
            else if (e.Property == HorizontalSliderPosProperty && !double.IsNaN((double)e.NewValue))
            {
                var scaling_factor = 100.0 / (VirtualHorizontalSliderMax - VirtualHorizontalSliderMin);
                ShiftX = (double) e.NewValue / scaling_factor +  VirtualHorizontalSliderMin; 
            }

        }

        private void SourceImageChanged()
        {
            if (SourceImage!= null)
                OriginalImage = SourceImage.Copy();
            ProjectChanged();
        }

        private void ProjectChanged()
        {
            if (Project != null && Project.Count > 0)
            {
                this.ProjectHeight = Project.Max(x => x.CanvasPoints.Length > 0 ? x.CanvasPoints.Max(y => y.Y) : 0);
                this.ProjectWidth = Project.Max(x => x.CanvasPoints.Length > 0 ? x.CanvasPoints.Max(y => y.X) : 0);
                UpdateMinZoomValue();
            }
        }
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            
            var p = e.GetCurrentPoint(this);
            if (DataContext is EditProjectVM ed)
                ed.Canvas_MouseDown(ScreenPointToDominoCoordinates(p.Position), e);
        }
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            var p = e.GetCurrentPoint(this);
            if (DataContext is EditProjectVM ed)
                ed.Canvas_MouseUp(ScreenPointToDominoCoordinates(p.Position), e);
        }
        protected override void OnPointerMoved(PointerEventArgs e)
        {
            var p = e.GetCurrentPoint(this);
            if (DataContext is EditProjectVM ed)
                ed.Canvas_MouseMove(ScreenPointToDominoCoordinates(p.Position), e);
        }
        Avalonia.Point ScreenPointToDominoCoordinates(Avalonia.Point p)
        {
            var tempx = p.X / Zoom + ShiftX;
            var tempy = p.Y / Zoom + ShiftY;
            return new Avalonia.Point(tempx, tempy);
        }
        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            if (double.IsNaN(ShiftX))
                ShiftX = 0;
            if (double.IsNaN(ShiftY))
                ShiftY = 0;
            var oldx = ShiftX;
            var oldy = ShiftY;
            double newx = ShiftX, newy = ShiftY;

            base.OnPointerWheelChanged(e);
            var delta = e.Delta.Y;
            Debug.WriteLine("Delta = " + e.Delta);
            Debug.WriteLine(e.KeyModifiers);
            if (e.KeyModifiers == KeyModifiers.Control)
            {
                Debug.WriteLine("Raw position: " + e.GetPosition(this));
                // get the screen coordinate of the current point.
                var p = ScreenPointToDominoCoordinates(e.GetPosition(this));
                Debug.WriteLine("Computed position in domino coordinates: " + oldx + ", " + oldy);
                var newzoom = Zoom;
                if (delta > 0)
                    newzoom *= 1.1;
                else
                    newzoom *= 1 / 1.1;
                if (newzoom > FitAllZoomValue / 4 && newzoom < 4)
                    Zoom = newzoom;

                newx = (p.X - e.GetPosition(this).X / Zoom);

                newy = (p.Y - e.GetPosition(this).Y / Zoom);
            }
            else
            {
                if (e.KeyModifiers == KeyModifiers.Shift)
                {
                    newx = oldx - 100 * (e.Delta.X + e.Delta.Y);
                }
                else
                {
                    newx = oldx - 100 * e.Delta.X;
                    newy = oldy - 100 * delta;
                }
            }
            ShiftX = newx;
            ShiftY = newy;

        }


        public override void Render(DrawingContext context)
        {
            if (this.ProjectWidth == 0  || double.IsInfinity(this.ProjectWidth))
            {
                ProjectChanged();
            }
            UpdateVerticalSlider(this, null);
            UpdateHorizontalSlider(this, null);
            UpdateMinZoomValue();
            context.Custom(new DominoRenderer(this));
            ForceRedraw = false;
        }
    }
    public class CanvasDrawable
    {
        public SKPath Path {get; set;}
        public SKPaint Paint {get; set;}
        public bool BeforeBorders { get; set; }

        public void Render(SKCanvas canvas, SKMatrix transform)
        {
            var p2 = Path.Clone();
            p2.Transform(transform);
            canvas.DrawPath(p2, Paint);
        }

    }
    public class DominoRenderer : ICustomDrawOperation
    {
        private readonly FormattedText _noSkia;
        private readonly float shift_x;
        private readonly float shift_y;
        private readonly float zoom;
        private readonly SKColor unselectedBorderColor;
        private readonly SKColor selectedBorderColor;
        private readonly SKColor pasteHightlightColor;
        private readonly SKColor deletionHighlightColor;
        private readonly SKPath selectionPath;
        private readonly bool selectionVisible;
        private readonly SKColor selectionColor;
        private readonly AvaloniaList<EditingDominoVM> project;
        private readonly SKBitmap bitmap;
        private readonly float ProjectHeight;
        private readonly float ProjectWidth;
        private readonly byte bitmapopacity;
        private readonly bool above;

        private readonly byte dominoopacity;
        private readonly SKColor background;
        private AvaloniaList<CanvasDrawable> AdditionalDrawables;
        private readonly float BorderSize;
        int rendered = 0;

        public Rect Bounds { get; set; }
        public TransformedBounds? TightBounds { get; set; }


        public DominoRenderer(ProjectCanvas pc)
        {
            _noSkia = new FormattedText()
            {
                Text = "Current rendering API is not Skia"
            };
            Bounds = new Rect(0, 0, pc.Bounds.Width, pc.Bounds.Height);
            TightBounds = pc.TransformedBounds;
            shift_x = (float)pc.ShiftX;
            shift_y = (float)pc.ShiftY;
            zoom = (float)pc.Zoom;
            unselectedBorderColor = pc.UnselectedBorderColor.ToSKColor();
            selectedBorderColor = pc.SelectedBorderColor.ToSKColor();
            pasteHightlightColor = Colors.Violet.ToSKColor();
            deletionHighlightColor = SKColors.Red;
            selectionColor = pc.SelectionDomainColor.ToSKColor();
            selectionPath = pc.SelectionDomain.Clone();
            this.project = pc.Project;
            // Transform the selection path into screen coordinates
            var transform = SKMatrix.CreateScaleTranslation(zoom, zoom, -shift_x * zoom, -shift_y * zoom);
            this.ProjectHeight = (float)pc.ProjectHeight;
            this.ProjectWidth = (float)pc.ProjectWidth;
            this.above = pc.SourceImageAbove;
            this.background = pc.BackgroundColor.ToSKColor();
            this.AdditionalDrawables = pc.AdditionalDrawables;

            selectionPath?.Transform(transform);
            selectionVisible = pc.SelectionDomainVisible;
            bitmap = pc.SourceImage;
            bitmapopacity = (byte)(pc.SourceImageOpacity * 255);
            dominoopacity = (byte)(pc.DominoOpacity * 255);
            BorderSize = pc.BorderSize;
        }

        public void Dispose()
        {

        }

        public bool Equals([AllowNull] ICustomDrawOperation other) => false;
        public bool HitTest(Avalonia.Point p) => true;

        public void Render(IDrawingContextImpl context)
        {

            if (rendered > 20)
                return;

            var canvas = (context as ISkiaDrawingContextImpl)?.SkCanvas;
            if (canvas == null)
            {
                context.DrawText(Brushes.Black, new Avalonia.Point(), _noSkia.PlatformImpl);
                return;
            }

            if (project == null)
                return;

            canvas.Save();

            if (background.Alpha != 0)
            {
                canvas.DrawRect(new SKRect(0, 0, (float)Bounds.Width, (float)Bounds.Height), new SKPaint() { Color = background });
            }

            if (!above) DrawImage(canvas);
            for (int i = 0; i < project.Count; i++)
            {
                DrawDomino(canvas, project[i]);
            }
            DrawAdditionals(canvas, true);
            for (int i = 0; i < project.Count; i++)
            {
                DrawDominoBorder(canvas, project[i]);
            }
            if (above) DrawImage(canvas);

            if (selectionVisible && selectionPath != null)
            {
                canvas.DrawPath(selectionPath, new SKPaint() { Color = new SKColor(0, 0, 0, 255), IsStroke = true, StrokeWidth = 4, IsAntialias = true });
                canvas.DrawPath(selectionPath, new SKPaint() { Color = selectionColor, IsStroke = true, StrokeWidth = 2, IsAntialias = true });
            }
            DrawAdditionals(canvas, false);



            canvas.Restore();
            rendered += 1;


        }
        private void DrawAdditionals(SKCanvas canvas, bool beforeBorders)
        {
            try
            {
                foreach (CanvasDrawable d in AdditionalDrawables.ToList())
                {
                    if (d == null)
                        continue;
                    if (d.BeforeBorders == beforeBorders)
                    {
                        var transform = SKMatrix.CreateScaleTranslation(zoom, zoom, -shift_x * zoom, -shift_y * zoom);
                        d.Render(canvas, transform);
                    }
                }
            }
            catch {} // happens mostly when AdditionalDrawables is modified during a render pass. This triggers a redraw anyway, so let's ignore it
        }
        private void DrawImage(SKCanvas canvas)
        {
            if (bitmap == null)
                return;
            var height = Bounds.Height / ProjectHeight / zoom * bitmap.Height;
            var width = Bounds.Width / ProjectWidth / zoom * bitmap.Width;
            Console.WriteLine($"({ProjectHeight}, {ProjectWidth})");
            var x = shift_x / ProjectWidth * bitmap.Width;
            var y = shift_y / ProjectHeight * bitmap.Height;
            canvas.DrawBitmap(bitmap, 
            new SKRect(x, y, (float)(width + x), (float)(height+y)), 
            new SKRect(0, 0, (float)Bounds.Width, (float)Bounds.Height), 
            new SKPaint() { ColorFilter = SKColorFilter.CreateBlendMode(SKColors.White.WithAlpha(bitmapopacity), SKBlendMode.DstIn) });
           

        }
        public SKPoint PointToDisplaySkiaPoint(Avalonia.Point p)
        {
            return new SKPoint((float)((p.X - shift_x) * zoom), (float)((p.Y - shift_y) * zoom));
        }
        public SKPoint PointToDisplaySkiaPoint(Core.Point p)
        {
            return new SKPoint((float)((p.X - shift_x) * zoom), (float)((p.Y - shift_y) * zoom));
        }
        public Avalonia.Point PointToDisplayAvaloniaPoint(Avalonia.Point p)
        {
            return new Avalonia.Point((float)((p.X - shift_x) * zoom), (float)((p.Y - shift_y) * zoom));
        }
        
        private void DrawDomino(SKCanvas canvas, EditingDominoVM vm)
        {
            var shape = vm.domino;
            var c = vm.StoneColor;
            var dp = vm.CanvasPoints;
            // is the domino visible at all?
            var inside = dp.Select(x => new Avalonia.Point(x.X, x.Y)).Sum(x => Bounds.Contains(PointToDisplayAvaloniaPoint(x)) ? 1 : 0);
            if (inside > 0)
            {
                var path = new SKPath();
                path.MoveTo(PointToDisplaySkiaPoint(dp[0]));
                foreach (var line in dp.Skip(0))
                    path.LineTo(PointToDisplaySkiaPoint(line));
                path.Close();

                canvas.DrawPath(path, new SKPaint() { Color = c.ToSKColor(dominoopacity), IsAntialias = true, IsStroke = false }) ;
            }
        }
        private void DrawDominoBorder(SKCanvas canvas, EditingDominoVM vm)
        {
            var shape = vm.domino;
            var c = vm.StoneColor;
            var dp = vm.CanvasPoints;
            // is the domino visible at all?
            // todo: speed up this call
            var inside = dp.Select(x => new Avalonia.Point(x.X, x.Y)).Sum(x => Bounds.Contains(PointToDisplayAvaloniaPoint(x)) ? 1 : 0);
            if (inside > 0)
            {
                var path = new SKPath();
                path.MoveTo(PointToDisplaySkiaPoint(dp[0]));
                foreach (var line in dp.Skip(0))
                    path.LineTo(PointToDisplaySkiaPoint(line));
                path.Close();
                SKColor? borderColor = null;
                if (vm.State.HasFlag(EditingDominoStates.PasteHighlight))
                {
                    borderColor = pasteHightlightColor;
                }
                if (vm.State.HasFlag(EditingDominoStates.Selected))
                {
                    borderColor = selectedBorderColor;
                }
                if (vm.State.HasFlag(EditingDominoStates.DeletionHighlight))
                {
                    borderColor = deletionHighlightColor;
                }
                
                if (borderColor != null)
                {
                    canvas.DrawPath(path, new SKPaint() { Color = (SKColor)borderColor, IsAntialias = true, IsStroke = true, StrokeWidth = Math.Max(BorderSize, 2) * zoom, PathEffect = SKPathEffect.CreateDash(new float[] { 8 * zoom, 2 * zoom }, 10 * zoom) });
                }
                else
                {
                    if (BorderSize > 0)
                        canvas.DrawPath(path, new SKPaint() { Color = unselectedBorderColor, IsAntialias = true, IsStroke = true, StrokeWidth = BorderSize / 2 * zoom });
                }
            }
        }
    }
    public static class ColorExtensions
    {
        public static SKColor ToSKColor(this Avalonia.Media.Color color)
        {
            return new SKColor(color.R, color.G, color.B, color.A);
        }
        public static SKColor ToSKColor(this Avalonia.Media.Color color, byte opacity)
        {
            return new SKColor(color.R, color.G, color.B, (byte)(color.A * opacity / 255d));
        }
    }
}
