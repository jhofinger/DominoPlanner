﻿using System;
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

        public bool Expanded
        {
            get { return GetValue(ExpandedProperty); }
            set { SetValue(ExpandedProperty, value); }
        }

        public static readonly StyledProperty<bool> ExpandedProperty = AvaloniaProperty.Register<ProjectCanvas, bool>(nameof(Expanded));

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

        public AvaloniaList<int> SelectedDominoes
        {
            get { return (AvaloniaList<int>)GetValue(SelectedDominoesProperty); }
            set { SetValue(SelectedDominoesProperty, value); }
        }
        public static readonly StyledProperty<AvaloniaList<int>> SelectedDominoesProperty = AvaloniaProperty.Register<ProjectCanvas, AvaloniaList<int>>(nameof(SelectedDominoes));

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


        public double ProjectHeight { get; set; }

        public double ProjectWidth { get; set; }

        public SKBitmap OriginalImage;

        public ProjectCanvas()
        {
            AffectsRender<ProjectCanvas>(ShiftXProperty);
            AffectsRender<ProjectCanvas>(ShiftYProperty);
            AffectsRender<ProjectCanvas>(ZoomProperty);
            //Stones = new List<DominoInCanvas>();
            //Stones.Add(new DominoInCanvas(50, 50, 50, 50, Colors.AliceBlue));
            DataContextProperty.Changed.AddClassHandler<ProjectCanvas>((o, e) => SubscribeEvents(e));
            AffectsRender<ProjectCanvas>(ProjectProperty);
            AffectsRender<ProjectCanvas>(ExpandedProperty);
            AffectsRender<ProjectCanvas>(SelectionDomainProperty);
            AffectsRender<ProjectCanvas>(SelectionDomainProperty);
            AffectsRender<ProjectCanvas>(SelectionDomainVisibleProperty);
            AffectsRender<ProjectCanvas>(SelectionDomainColorProperty);
            AffectsRender<ProjectCanvas>(SourceImageOpacityProperty);
            AffectsRender<ProjectCanvas>(SourceImageProperty);
            AffectsRender<ProjectCanvas>(SourceImageAboveProperty);
            ProjectProperty.Changed.AddClassHandler<ProjectCanvas>((o, e) => ProjectChanged());
            SourceImageProperty.Changed.AddClassHandler<ProjectCanvas>((o, e) => SourceImageChanged());
            SourceImageOpacityProperty.Changed.AddClassHandler<ProjectCanvas>((o, e) => SourceImageChanged());
            SourceImageAboveProperty.Changed.AddClassHandler<ProjectCanvas>((o, e) => SourceImageChanged());
            AffectsRender<ProjectCanvas>(BackgroundColorProperty);
            AffectsRender<ProjectCanvas>(UnselectedBorderColorProperty);
            AffectsRender<ProjectCanvas>(BorderSizeProperty);
        }

        private void SourceImageChanged()
        {
            if (SourceImage!= null)
                OriginalImage = SourceImage.Copy();
        }

        private void ProjectChanged()
        {
            if (Project != null && Project.Count > 0)
            {
                this.ProjectHeight = Project.Max(x => x.CanvasPoints.Length > 0 ? x.CanvasPoints.Max(y => y.Y) : 0);
                this.ProjectWidth = Project.Max(x => x.CanvasPoints.Length > 0 ? x.CanvasPoints.Max(y => y.X) : 0);
            }
        }

        private void SubscribeEvents(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.OldValue is EditProjectVM oldv)
            {
                this.KeyDown -= oldv.PressedKey;
            }
            if (e.NewValue is EditProjectVM newv)
            {
                this.KeyDown += newv.PressedKey;
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
            var oldx = ShiftX;
            var oldy = ShiftY;
            double newx = ShiftX, newy = ShiftY;

            base.OnPointerWheelChanged(e);
            var delta = e.Delta.Y;
            Debug.WriteLine("Delta = " + e.Delta);
            if (e.KeyModifiers == KeyModifiers.Control)
            {
                Debug.WriteLine("Raw position: " + e.GetPosition(this));
                // get the screen coordinate of the current point.
                var p = ScreenPointToDominoCoordinates(e.GetPosition(this));
                Debug.WriteLine("Computed position in domino coordinates: " + oldx + ", " + oldy);
                if (delta > 0)
                    Zoom *= 1.1;
                else
                    Zoom *= 1 / 1.1;
                
                newx = (p.X - e.GetPosition(this).X / Zoom);
                
                newy = (p.Y - e.GetPosition(this).Y / Zoom);
            }
            else if (e.KeyModifiers == KeyModifiers.Shift || e.Delta.X != 0)
            {
                newx = oldx - 100 * (e.Delta.X + e.Delta.Y);
            }
            else
            {
                newy = oldy - 100 * delta;
            }
            ShiftX = newx;
            ShiftY = newy;

        }


        public override void Render(DrawingContext context)
        {             
            context.Custom(new DominoRenderer(this));
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
        private readonly SKPath selectionPath;
        private readonly bool selectionVisible;
        private readonly SKColor selectionColor;
        private readonly AvaloniaList<EditingDominoVM> project;
        private readonly SKBitmap bitmap;
        private readonly float ProjectHeight;
        private readonly float ProjectWidth;
        private readonly byte bitmapopacity;
        private readonly bool above;
        private readonly SKColor background;
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
            unselectedBorderColor = new SKColor(pc.UnselectedBorderColor.R, pc.UnselectedBorderColor.G, pc.UnselectedBorderColor.B, pc.UnselectedBorderColor.A);
            selectedBorderColor = new SKColor(pc.SelectedBorderColor.R, pc.SelectedBorderColor.G, pc.SelectedBorderColor.B, pc.SelectedBorderColor.A);
            selectionColor = new SKColor(pc.SelectionDomainColor.R, pc.SelectionDomainColor.G, pc.SelectionDomainColor.B, 255);
            selectionPath = pc.SelectionDomain.Clone();
            this.project = pc.Project;
            // Transform the selection path into screen coordinates
            var transform = SKMatrix.CreateScaleTranslation(zoom, zoom, -shift_x * zoom, -shift_y * zoom);
            this.ProjectHeight = (float)pc.ProjectHeight;
            this.ProjectWidth = (float)pc.ProjectWidth;
            this.above = pc.SourceImageAbove;
            this.background = new SKColor(pc.BackgroundColor.R, pc.BackgroundColor.G, pc.BackgroundColor.B, pc.BackgroundColor.A);

            selectionPath?.Transform(transform);
            selectionVisible = pc.SelectionDomainVisible;
            bitmap = pc.OriginalImage;
            bitmapopacity = (byte)(pc.SourceImageOpacity * 255);
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
            if (selectionVisible && selectionPath != null)
            {
                canvas.DrawPath(selectionPath, new SKPaint() { Color = new SKColor(0, 0, 0, 255), IsStroke = true, StrokeWidth = 4, IsAntialias = true });
                canvas.DrawPath(selectionPath, new SKPaint() { Color = selectionColor, IsStroke = true, StrokeWidth = 2, IsAntialias = true });
            }


            if (above) DrawImage(canvas);
                    
            canvas.Restore();
            rendered += 1;


        }
        private void DrawImage(SKCanvas canvas)
        {
            if (bitmap == null)
                return;
            var height = Bounds.Height / ProjectHeight / zoom * bitmap.Height;
            var width = Bounds.Width / ProjectWidth / zoom * bitmap.Width;
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

                canvas.DrawPath(path, new SKPaint() { Color = new SKColor(c.R, c.G, c.B, c.A), IsAntialias = true, IsStroke = false });
                if (vm.IsSelected)
                {
                    canvas.DrawPath(path, new SKPaint() { Color = selectedBorderColor, IsAntialias = true, IsStroke = true, StrokeWidth = BorderSize * zoom, PathEffect = SKPathEffect.CreateDash(new float[] { 8 * zoom, 2 * zoom}, 10 * zoom) });
                }
                else
                {

                    canvas.DrawPath(path, new SKPaint() { Color = unselectedBorderColor, IsAntialias = true, IsStroke = true, StrokeWidth = BorderSize * zoom });
                }
            }
        }
    }
}
