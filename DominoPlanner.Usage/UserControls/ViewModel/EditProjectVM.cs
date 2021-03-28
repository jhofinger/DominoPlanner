using DominoPlanner.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Avalonia.Input;
using Avalonia.Collections;
using Avalonia.Controls;
using static DominoPlanner.Usage.Localizer;

namespace DominoPlanner.Usage.UserControls.ViewModel
{
    public class EditProjectVM : DominoProviderTabItem
    {
        #region CTOR
        public EditProjectVM(IDominoProvider dominoProvider) : base()
        {
            HaveBuildtools = dominoProvider.HasProtocolDefinition;

            UICursor = null;
            Dominoes = new AvaloniaList<EditingDominoVM>();
            PossiblePastePositions = new List<int>();
            UnsavedChanges = false;
            CurrentProject = dominoProvider;

            _DominoList = new ObservableCollection<ColorListEntry>();

            _DominoList.Clear();
            CurrentProject.colors.Anzeigeindizes.CollectionChanged += Anzeigeindizes_CollectionChanged;
            RefreshList();
            selectedColors = new int[CurrentProject.colors.Length];
            SaveField = new RelayCommand(o => { Save(); });
            RestoreBasicSettings = new RelayCommand(o => { redoStack = new Stack<PostFilter>(); Editing = false; });
            
            SelectColor = new RelayCommand(o => { SelectAllStonesWithColor(); });
            MouseClickCommand = new RelayCommand(o => { ChangeColor(); });
            ClearSelection = new RelayCommand(o => { ClearFullSelection(true); });
            CopyCom = new RelayCommand(o => { Copy(); });

            AddRowAbove = new RelayCommand(o => { AddRow(false); });
            AddRowBelow = new RelayCommand(o => { AddRow(true); });
            AddColumnRight = new RelayCommand(o => { AddColumn(true); });
            AddColumnLeft = new RelayCommand(o => { AddColumn(false); });
            RemoveRows = new RelayCommand(o => { RemoveSelRows(); });
            RemoveColumns = new RelayCommand(o => { RemoveSelColumns(); });
            FlipHorizontallyCom = new RelayCommand(o => { System.Diagnostics.Debug.WriteLine("asdf"); ; });
            FlipVerticallyCom = new RelayCommand(o => { System.Diagnostics.Debug.WriteLine("asdf"); ; });
            MouseInPicture = new RelayCommand(o => { UICursor = new Cursor(StandardCursorType.Hand); });
            MouseOutPicture = new RelayCommand(o => { UICursor = null; });
            SelectAllCom = new RelayCommand(o => { SelectAll(); });
            ChangeColorCom = new RelayCommand(o => { if (o is IDominoColor dc) ChangeColor(dc); });
            UnsavedChanges = false;
            SelectionTool = new SelectionToolVM(this);
            DisplaySettingsTool = new DisplaySettingsToolVM(this);
            ZoomTool = new ZoomToolVM(this);
            RulerTool = new RulerToolVM(this);
            EditingTools = new ObservableCollection<EditingToolVM>() {
                SelectionTool,
                RulerTool,
                new EditingToolVM(this) { Image = "textDrawingImage", Name="Write text"},
                ZoomTool,
                DisplaySettingsTool
            };
            if (this.CurrentProject is IRowColumnAddableDeletable)
            {
                RowColumnTool = new RowColumnInsertionVM(this);
                EditingTools.Insert(2, RowColumnTool);
            }
            SelectedTool = SelectionTool;
            UpdateUIElements();
        }
        #endregion

        #region fields


        internal DominoTransfer dominoTransfer => CurrentProject.Last;

        public List<int> GetSelectedDominoes()
        {
            List<int> selected = new List<int>();
            foreach (var domino in Dominoes)
            {
                if (domino.State == EditingDominoStates.Selected)
                {
                    selected.Add(domino.idx);
                }
            }
            return selected;
        }
        private int[] selectedColors;
        private int startindex;

        private AvaloniaList<EditingDominoVM> dominoes;

        public AvaloniaList<EditingDominoVM> Dominoes
        {
            get { return dominoes; }
            set { dominoes = value; RaisePropertyChanged(); }
        }
        private AvaloniaList<CanvasDrawable> additionalDrawables;
        public AvaloniaList<CanvasDrawable> AdditionalDrawables
        {
            get { return additionalDrawables; }
            set { additionalDrawables = value; RaisePropertyChanged(); }
        }

        public SelectionToolVM SelectionTool { get; set; }
        public DisplaySettingsToolVM DisplaySettingsTool { get; set; }
        public ZoomToolVM ZoomTool { get; set; }
        public RulerToolVM RulerTool;
        public RowColumnInsertionVM RowColumnTool;
        #endregion

        #region events
        internal event EventHandler RefreshSize;
        #endregion

        #region prope

        private ObservableCollection<EditingToolVM> editingTools;

        public ObservableCollection<EditingToolVM> EditingTools
        {
            get { return editingTools; }
            set { editingTools = value; TabPropertyChanged(ProducesUnsavedChanges: false); }
        }
        private EditingToolVM selectedTool;

        public EditingToolVM SelectedTool
        {
            get { return selectedTool; }
            set
            {
                if (value != null && value != selectedTool)
                {
                    selectedTool?.LeaveTool();
                    selectedTool = value;
                    selectedTool.EnterTool();
                }
                TabPropertyChanged(ProducesUnsavedChanges: false);

            }
        }
        private bool _HaveBuildtools;
        public bool HaveBuildtools
        {
            get { return _HaveBuildtools; }
            set
            {
                if (_HaveBuildtools != value)
                {
                    _HaveBuildtools = value;
                    TabPropertyChanged(ProducesUnsavedChanges: false);
                }
            }
        }
        

        private Cursor _UICursor;
        public Cursor UICursor
        {
            get { return _UICursor; }
            set
            {
                if (_UICursor != value)
                {
                    _UICursor = value;
                    TabPropertyChanged(ProducesUnsavedChanges: false);
                }
            }
        }
        
        public override TabItemType tabType
        {
            get
            {
                return TabItemType.EditProject;
            }
        }
        private ObservableCollection<ColorListEntry> _DominoList;
        public ObservableCollection<ColorListEntry> DominoList
        {
            get { return new ObservableCollection<ColorListEntry>(_DominoList.OrderBy(x => x.SortIndex)); }
            set
            {
                if (_DominoList != value)
                {
                    _DominoList = value;
                    RaisePropertyChanged();
                }
            }
        }

        private ColorListEntry _SelectedColor;
        public ColorListEntry SelectedColor
        {
            get { return _SelectedColor; }
            set
            {
                if (_SelectedColor != value)
                {
                    _SelectedColor = value;
                    TabPropertyChanged(ProducesUnsavedChanges: false);
                }
            }
        }
        private string _ProjectName;
        public string ProjectName
        {
            get { return _ProjectName; }
            set
            {
                if (_ProjectName != value)
                {
                    _ProjectName = value;
                    TabPropertyChanged(ProducesUnsavedChanges: false);
                }
            }
        }

        private string _ProjectAmount;
        public string ProjectAmount
        {
            get { return _ProjectAmount; }
            set
            {
                if (_ProjectAmount != value)
                {
                    _ProjectAmount = value;
                    TabPropertyChanged(ProducesUnsavedChanges: false);
                }
            }
        }

        private string _ProjectHeight;
        public string ProjectHeight
        {
            get { return _ProjectHeight; }
            set
            {
                if (_ProjectHeight != value)
                {
                    _ProjectHeight = value;
                    TabPropertyChanged(ProducesUnsavedChanges: false);
                }
            }
        }

        private string _ProjectWidth;
        public string ProjectWidth
        {
            get { return _ProjectWidth; }
            set
            {
                if (_ProjectWidth != value)
                {
                    _ProjectWidth = value;
                    TabPropertyChanged(ProducesUnsavedChanges: false);
                }
            }
        }

        private AvaloniaList<ColorControl.Column> _colorColumnConfig;

        public AvaloniaList<ColorControl.Column> ColorColumnConfig
        {
            get { return _colorColumnConfig; }
            set { _colorColumnConfig = value; }
        }

        #endregion

        #region Methods
        internal override void Close()
        {
            base.Close();

            DisplaySettingsTool.DeleteImage();

            ClearCanvas();
        }

        internal void ClearCanvas(bool ClearSelection = true)
        {
            ClearPastePositions();
            if (ClearSelection) ClearFullSelection(true);
            /*if (DisplaySettingsTool.DominoProject != null)
                DisplaySettingsTool.RemoveStones();*/
        }

        private void Anzeigeindizes_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Replace)
            {
                var filtered = _DominoList.Where(x => x.DominoColor is DominoColor);
                if (e.NewStartingIndex < filtered.Count())
                    filtered.ElementAt(e.NewStartingIndex).SortIndex = (int)e.NewItems[0];
            }
            UnsavedChanges = false;
            RaisePropertyChanged(nameof(DominoList));
        }
        public void UpdateUIElements()
        {
            RefreshColorAmount();
            Redraw();
            RefreshSizeLabels();
        }
        private void RefreshColorAmount()
        {
            var projectCounts = CurrentProject.Counts;
            for (int i = 0; i < _DominoList.Count(); i++)
            {
                if (_DominoList[i].ProjectCount.Count != 2)
                {
                    _DominoList[i].ProjectCount.Clear();
                    // add dummy entries
                    _DominoList[i].ProjectCount.Add(0);
                    _DominoList[i].ProjectCount.Add(0);
                }

                if (projectCounts.Length > i + 1)
                {
                    _DominoList[i].ProjectCount[0] = projectCounts[i + 1];
                    _DominoList[i].ProjectCount[1] = selectedColors[i + 1];
                }
                else
                {
                    _DominoList[i].ProjectCount[0] = projectCounts[0];
                    _DominoList[i].ProjectCount[1] = selectedColors[0];
                }
            }
        }
        private void RefreshList()
        {
            // Setup Columns
            ColorColumnConfig = new AvaloniaList<ColorControl.Column>
            {
                new ColorControl.Column() { DataField = "DominoColor.mediaColor", Header = "", Class = "Color" },
                new ColorControl.Column() { DataField = "DominoColor.name", Header = _("Name"), Width = new GridLength(100), CanResize = true },
                new ColorControl.Column() { DataField = "DominoColor.count", Header = GetParticularString("Number of stones available", "Total"), Class="Count", Width = new GridLength(70), CanResize=true },
                new ColorControl.Column() { DataField = "ProjectCount[0]", Header = GetParticularString("Number of stones used in current field", "Used"), HighlightDataField = "DominoColor.count" },
                new ColorControl.Column() { DataField = "ProjectCount[1]", Header = GetParticularString("Number of stones currently selected", "Selected") }
            };

            _DominoList.Clear();
            int counter = 0;

            foreach (DominoColor domino in CurrentProject.colors.RepresentionForCalculation.OfType<DominoColor>())
            {
                _DominoList.Add(new ColorListEntry() { DominoColor = domino, SortIndex = CurrentProject.colors.Anzeigeindizes[counter] });
                counter++;
            }

            if (CurrentProject.colors.RepresentionForCalculation.OfType<EmptyDomino>().Count() == 1)
            {
                _DominoList.Add(new ColorListEntry() { DominoColor = CurrentProject.colors.RepresentionForCalculation.OfType<EmptyDomino>().First(), SortIndex = -1 });
            }
        }
        private void SelectAll()
        {
            SelectionTool.Select(Enumerable.Range(0, dominoTransfer.Length).ToList(), true);
            UpdateUIElements();
        }
        List<int> toCopy = new List<int>();

        bool iscopying = false;
        private async void Copy()
        {
            if (!(CurrentProject is ICopyPasteable)) await Errorhandler.RaiseMessage(_("Copy/Paste is not supported in this project."), "Copy", Errorhandler.MessageType.Warning);
            ClearPastePositions();
            var selected = GetSelectedDominoes();
            if (selected.Count <= 0)
            {
                await Errorhandler.RaiseMessage(_("Nothing to copy!"), _("No selection"), Errorhandler.MessageType.Error);
                return;
            }
            iscopying = true;
            toCopy = new List<int>(selected);
            startindex = selected.Min();
            ClearFullSelection(true);
            try
            {
                int[] validPositions = ((ICopyPasteable)this.CurrentProject).GetValidPastePositions(startindex);
                HighlightPastePositions(validPositions);
            }
            catch (InvalidOperationException ex)
            {
                await Errorhandler.RaiseMessage(ex.Message, _("Error"), Errorhandler.MessageType.Error);
                FinalizePaste(true);
            }
            UpdateUIElements();
        }

        private async void Paste(Avalonia.Point dominoPoint, PointerReleasedEventArgs e)
        {
            bool pasteFailed = true;
            try
            {
                if (!(CurrentProject is ICopyPasteable)) await Errorhandler.RaiseMessage(_("Copy/Paste is not supported in this project."), _("Paste"), Errorhandler.MessageType.Warning);
                // find closest domino
                int domino = FindDominoAtPosition(dominoPoint, int.MaxValue).idx;
                if (PossiblePastePositions.Contains(domino))
                {
                    PasteFilter paste = new PasteFilter(CurrentProject as ICopyPasteable, startindex, toCopy.ToArray(), domino);
                    paste.Apply();
                    undoStack.Push(paste);
                    pasteFailed = false;
                    if (e.KeyModifiers != KeyModifiers.Control)
                    {
                        SelectionTool.Select(paste.paste_target, true);
                    }
                    
                }
            }
            catch (InvalidOperationException ex)
            {
                await Errorhandler.RaiseMessage(ex.Message, _("Error"), Errorhandler.MessageType.Error);
            }
            finally
            {
                FinalizePaste(e.KeyModifiers != KeyModifiers.Control && !pasteFailed);
            }
        }
        private void FinalizePaste(bool clearall)
        {
            if (clearall)
            {
                iscopying = false;
                ClearPastePositions();
            }
            // refresh the preview in case we copied to the source domain
            AdditionalDrawables.RemoveAll(PasteOverlays);
            PasteOverlays = new List<CanvasDrawable>();
            UpdateUIElements();
        }
        List<CanvasDrawable> PasteOverlays = new List<CanvasDrawable>();
        Avalonia.Point lastreference;

        private void DrawPasteOverlay(Avalonia.Point dominoPoint, PointerEventArgs e)
        {
            // display the dominoes to paste as a half-transparent overlay.
            // reference point:
            var reference_point = Dominoes[toCopy.Min()].CenterPoint - dominoPoint;
            if (PasteOverlays.Count == 0)
            {
                // create the overlays
                foreach (int i in toCopy)
                {
                    var p = new SkiaSharp.SKPath();
                    var points = Dominoes[i].CanvasPoints;
                    if (points.Length > 1)
                    {
                        p.MoveTo(new SkiaSharp.SKPoint((float)points[0].X - (float)reference_point.X, (float)points[0].Y - (float)reference_point.Y));
                        foreach (var point in points.Skip(1))
                        {
                            p.LineTo(new SkiaSharp.SKPoint((float)point.X - (float)reference_point.X, (float)point.Y - (float)reference_point.Y));
                        }
                    }
                    var color = Dominoes[i].StoneColor.ToSKColor().WithAlpha(128);
                    PasteOverlays.Add(new CanvasDrawable() { BeforeBorders = false, Paint = new SkiaSharp.SKPaint() { Color = color }, Path = p });
                    }
                AdditionalDrawables.AddRange(PasteOverlays);
            }
            else
            {
                // find out difference between current and last reference point
                var shift = lastreference - reference_point;
                var transform = SkiaSharp.SKMatrix.CreateTranslation((float)shift.X, (float)shift.Y);
                System.Diagnostics.Debug.WriteLine(shift);
                foreach (var stone in PasteOverlays)
                {
                    stone.Path.Transform(transform);
                }
            }
            lastreference = reference_point;
            Redraw();
        }
        public void ExecuteOperation(PostFilter pf)
        {
            pf.Apply();
            undoStack.Push(pf);
            redoStack.Clear();
        }

        public override void Undo()
        {
            UndoInternal(false);
        }
        public void UndoInternal(bool IncludeSelectionOperation = false)
        {
            if (undoStack.Count == 0) return;
            PostFilter undoFilter;
            do
            {
                undoFilter = undoStack.Pop();
                redoStack.Push(undoFilter);
                undoFilter.Undo();
            } while ((!IncludeSelectionOperation && (undoFilter is SelectionOperation)) && (undoStack.Count != 0));

            if (!(undoFilter is EditingActivatedOperation || undoFilter is SelectionOperation || undoFilter is SetColorOperation) )
            {
                ClearCanvas(false);
                RecreateCanvasViewModel();
                UpdateUIElements();
                DisplaySettingsTool.SliceImage();
                if (undoStack.Count == 0) UnsavedChanges = false;
            }
            else
            {
                UpdateUIElements();
            }
            SelectedTool.OnUndo();
        }
            
        public override void Redo()
        {
            RedoInternal(false);
        }
        public void RedoInternal(bool IncludeSelectionOperation = false)
        {
            if (redoStack.Count == 0) return;
            PostFilter redoFilter;
            do
            {
                redoFilter = redoStack.Pop();
                undoStack.Push(redoFilter);
                redoFilter.Apply();
            } while ((!IncludeSelectionOperation && redoFilter is SelectionOperation) && redoStack.Count != 0);
          
            if (!(redoFilter is EditingDeactivatedOperation || redoFilter is SelectionOperation || redoFilter is SetColorOperation))
            {
                ClearCanvas(false);
                RecreateCanvasViewModel();
                UpdateUIElements();
                DisplaySettingsTool.SliceImage();
            }
            else
            {
                UpdateUIElements();
            }
            SelectedTool.OnRedo();
        }
        internal override void KeyPressed(object sender, KeyEventArgs args)
        {
            if (iscopying && args.Key == Key.Escape)
            {
                FinalizePaste(true);
                args.Handled = true;
            }
            if (!args.Handled)
                SelectedTool.KeyPressed(args);
        }
        internal override void ResetContent()
        {
            base.ResetContent();
            RefreshSize?.Invoke(this, EventArgs.Empty);
        }
        private void ChangeColor(IDominoColor color = null)
        {
            if (color == null)
            {
                color = SelectedColor.DominoColor;
            }
            SetColorOperation sco = new SetColorOperation(CurrentProject, GetSelectedDominoes().ToArray(), CurrentProject.colors.RepresentionForCalculation.ToList().IndexOf(color));
            ClearFullSelection(true);
            ExecuteOperation(sco);
            UnsavedChanges = true;
            UpdateUIElements();
        }

        public async void AddRow(bool addBelow, int index = -1, IDominoShape colorReference = null)
        {
            var selected = GetSelectedDominoes();
            try
            {
                if (selected.Count > 0 || index != -1)
                {
                    int selDomino = selected.Count > 0 ? selected.First() : index;
                    int color = (colorReference ?? dominoTransfer[selDomino]).Color;
                    if (CurrentProject is IRowColumnAddableDeletable)
                    {
                        AddRows addRows = new AddRows((CurrentProject as IRowColumnAddableDeletable), selDomino, 1, color, addBelow);
                        ClearCanvas();
                        ExecuteOperation(addRows);
                        
                        RecreateCanvasViewModel();
                        SelectionTool.Select(addRows.added_indizes, true);
                        UpdateUIElements();
                        DisplaySettingsTool.SliceImage();
                    }
                    else
                    {
                        await Errorhandler.RaiseMessage(_("Adding rows is not supported in this project."), _("Add Row"), Errorhandler.MessageType.Warning);
                    }
                }
            }
            catch (InvalidOperationException ex)
            {
                await Errorhandler.RaiseMessage(ex.Message, _("Error"), Errorhandler.MessageType.Error);
            }
        }

        public async void AddColumn(bool addRight, int index = -1, IDominoShape colorReference  =null)
        {
            var selected = GetSelectedDominoes();
            try
            {
                if (selected.Count > 0 || index != -1)
                {
                    int selDomino = selected.Count > 0 ? selected.First() : index;
                    int color = (colorReference ?? dominoTransfer[selDomino]).Color;
                    if (CurrentProject is IRowColumnAddableDeletable)
                    {
                        AddColumns addRows = new AddColumns((CurrentProject as IRowColumnAddableDeletable), selDomino, 1, color, addRight);
                        ClearCanvas();
                        ExecuteOperation(addRows);
                       
                        RecreateCanvasViewModel();
                        SelectionTool.Select(addRows.added_indizes, true);
                        UpdateUIElements();
                        DisplaySettingsTool.SliceImage();
                    }
                    else
                    {
                        await Errorhandler.RaiseMessage(_("Adding columns is not supported in this project."), _("Add Row"), Errorhandler.MessageType.Warning);
                    }
                }
            }
            catch (InvalidOperationException ex)
            {
                await Errorhandler.RaiseMessage(ex.Message, _("Error"), Errorhandler.MessageType.Error);
            }
        }

        public async void RemoveSelRows(int index = -1)
        {
            var selected = GetSelectedDominoes();
            try
            {
                if (CurrentProject is IRowColumnAddableDeletable)
                {
                    int[] deletionIndices = null;
                    if (index != -1)
                    {
                        deletionIndices = new int[] { index };
                    }
                    if (selected.Count > 0)
                    {
                        deletionIndices = selected.ToArray();
                    }
                    if (deletionIndices != null)
                    {
                        DeleteRows deleteRows = new DeleteRows((CurrentProject as IRowColumnAddableDeletable), deletionIndices);
                        ClearCanvas();
                        ExecuteOperation(deleteRows);
                        RecreateCanvasViewModel();
                        DisplaySettingsTool.SliceImage();
                    }
                }
                else
                {
                    await Errorhandler.RaiseMessage(_("Removing rows is not supported in this project."), _("Remove Row"), Errorhandler.MessageType.Warning);
                }
            }
            catch (InvalidOperationException ex)
            {
                await Errorhandler.RaiseMessage(ex.Message, _("Error"), Errorhandler.MessageType.Error);
            }
        }

        public async void RemoveSelColumns(int index = -1)
        {
            var selected = GetSelectedDominoes();
            try
            {
                if (CurrentProject is IRowColumnAddableDeletable)
                {
                    int[] deletionIndices = null;
                    if (index != -1)
                    {
                        deletionIndices = new int[] { index };
                    }
                    if (selected.Count > 0)
                    {
                        deletionIndices = selected.ToArray();
                    }
                    if (deletionIndices != null)
                    {
                        DeleteColumns deleteColumns = new DeleteColumns((CurrentProject as IRowColumnAddableDeletable), deletionIndices);
                        ClearCanvas();
                        ExecuteOperation(deleteColumns);
                        RecreateCanvasViewModel();
                        DisplaySettingsTool.SliceImage();
                    }
                }
                else
                {
                    await Errorhandler.RaiseMessage(_("Removing columns is not supported in this project."), _("Remove Column"), Errorhandler.MessageType.Warning);
                }
            }
            catch (InvalidOperationException ex)
            {
                await Errorhandler.RaiseMessage(ex.Message, _("Error"), Errorhandler.MessageType.Error);
            }
        }
        internal void ClearFullSelection(bool undoable = false)
        {
            var selected = GetSelectedDominoes();
            if (undoable)
            {
                SelectionTool.Select(selected, false);

            }
            else
            {
                foreach (int i in selected.ToArray())
                {
                    RemoveFromSelectedDominoes(i);
                }
                while (undoStack.Count > 0 && undoStack.Peek() is SelectionOperation)
                {
                    undoStack.Pop();
                }
            }
            selectedColors = new int[CurrentProject.colors.Length];
            UpdateUIElements();
            SelectionTool.CurrentSelectionDomain.ResetSelectionArea();
            RefreshColorAmount();
        }

        private void RefreshSizeLabels()
        {
            if (dominoTransfer != null)
            {
                ProjectHeight = dominoTransfer.FieldPlanHeight.ToString();
                ProjectWidth = dominoTransfer.FieldPlanLength.ToString();
                ProjectAmount = dominoTransfer.shapes.Count().ToString();
                PhysicalLength = dominoTransfer.PhysicalLength;
                PhysicalHeight = dominoTransfer.PhysicalHeight;
            }
        }
        private void SelectAllStonesWithColor()
        {
            if (SelectedColor == null) return;
            var selectedIndex = CurrentProject.colors.RepresentionForCalculation.ToList().IndexOf(SelectedColor.DominoColor);
            bool isAnySelected = false;
            List<int> AllWithColor = new List<int>();
            List<int> Deselect = new List<int>();
            foreach (var d in Dominoes)
            {
                if (d.State == EditingDominoStates.Selected)
                {
                    isAnySelected = true;
                    if (d.domino.Color != selectedIndex)
                        Deselect.Add(d.idx);
                } 
                if (d.domino.Color == selectedIndex)
                        AllWithColor.Add(d.idx);
            }
            if (isAnySelected)
                SelectionTool.Select(Deselect, false);
            else
                SelectionTool.Select(AllWithColor, true);
            UpdateUIElements();
        }

        internal void Canvas_MouseDown(Avalonia.Point dominoPoint, PointerPressedEventArgs e)
        {
            SelectedTool?.MouseDown(dominoPoint, e);
        }

        internal void Canvas_MouseMove(Avalonia.Point dominoPoint, PointerEventArgs e)
        {
            if (iscopying)
            { 
                DrawPasteOverlay(dominoPoint, e);
            }
            else
            {
                SelectedTool?.MouseMove(dominoPoint, e);
            }
        }
        

        internal void Canvas_MouseUp(Avalonia.Point dominoPoint, PointerReleasedEventArgs e)
        {
            if (iscopying)
            {
                Paste(dominoPoint, e);
                if (e.InitialPressMouseButton == MouseButton.Right)
                {
                    FinalizePaste(true);
                }
            }
            else
            {
                SelectedTool?.MouseUp(dominoPoint, e);
                UpdateUIElements();
            }
        }
        internal void Canvas_MouseWheel(Avalonia.Point dominoPoint, PointerWheelEventArgs e)
        {
            SelectedTool?.MouseWheel(dominoPoint, e);
            
        }
        public void AddToSelectedDominoes(int i)
        {
            if (SelectDominoVisual(i))
            { 
                selectedColors[dominoTransfer[i].Color]++;
            }
        }
        public void RemoveFromSelectedDominoes(int i)
        {
            if (DeSelectDominoVisual(i))
            {
                selectedColors[dominoTransfer[i].Color]--;
            }
        }
        public bool IsSelected(int i)
        {
            return Dominoes[i].State.HasFlag(EditingDominoStates.Selected);
        }
        internal void RecreateCanvasViewModel()
        {
            if (CurrentProject.Last == null)
            {
                return;
            }
            Dominoes.Clear();
            for (int i = 0; i < CurrentProject.Last.shapes.Count(); i++)
            {
                EditingDominoVM dic = new EditingDominoVM(i, CurrentProject.Last[i], CurrentProject.colors, DisplaySettingsTool.Expanded);
                Dominoes.Add(dic);
            }
        }
        internal void Redraw()
        {
            DisplaySettingsTool.ForceRedraw = true;
        }
        private List<int> PossiblePastePositions;
        public void HighlightPastePositions(int[] validPositions)
        {
            PossiblePastePositions = new List<int>();
            foreach (int i in validPositions)
            {
                var dic = Dominoes[i];
                dic.State |= EditingDominoStates.PasteHighlight;
                PossiblePastePositions.Add(i);
            }
            Redraw();
        }
        public void ClearPastePositions()
        {
            foreach (int i in PossiblePastePositions)
            {
                Dominoes[i].State &= ~EditingDominoStates.PasteHighlight;
            }
            PossiblePastePositions.Clear();
            Redraw();
        }
        public bool SelectDominoVisual(int position)
        {
            var dic = Dominoes[position];
            if (!dic.State.HasFlag(EditingDominoStates.Selected))
            {
                dic.State |= EditingDominoStates.Selected;
                return true;
            }
            return false;
        }
        public bool DeSelectDominoVisual(int position)
        {
            var dic = Dominoes[position];
            if (dic.State.HasFlag(EditingDominoStates.Selected))
            {
                dic.State &= ~EditingDominoStates.Selected;
                return true;
            }
            return false;
        }
        public EditingDominoVM FindDominoAtPosition(Avalonia.Point pos, int tolerance = 0)
        {
            double min_dist = int.MaxValue;
            EditingDominoVM result = null;
            foreach (var shape in Dominoes)
            {
                if (shape.domino.IsInside(new Core.Point(pos.X, pos.Y), expanded: DisplaySettingsTool.Expanded)) return shape;
                var rect = shape.domino.GetContainer();
                double dist = Math.Pow((rect.x + rect.width / 2) - pos.X, 2) + Math.Pow(rect.y + rect.height / 2 - pos.Y, 2);
                if (min_dist > dist && dist < tolerance)
                {
                    min_dist = dist;
                    result = shape;
                }
            }
            return result;
        }
        #endregion

        #region Command
        private ICommand _ClearSelection;
        public ICommand ClearSelection { get { return _ClearSelection; } set { if (value != _ClearSelection) { _ClearSelection = value; } } }

        private ICommand _SelectColor;
        public ICommand SelectColor { get { return _SelectColor; } set { if (value != _SelectColor) { _SelectColor = value; } } }

        private ICommand _SaveField;
        public ICommand SaveField { get { return _SaveField; } set { if (value != _SaveField) { _SaveField = value; } } }

        private ICommand _RestoreBasicSettings;
        public ICommand RestoreBasicSettings { get { return _RestoreBasicSettings; } set { if (value != _RestoreBasicSettings) { _RestoreBasicSettings = value; } } }

        
        private ICommand _MouseClickCommand;
        public ICommand MouseClickCommand { get { return _MouseClickCommand; } set { if (value != _MouseClickCommand) { _MouseClickCommand = value; } } }

        private ICommand _GridSizeChanged;
        public ICommand GridSizeChanged { get { return _GridSizeChanged; } set { if (value != _GridSizeChanged) { _GridSizeChanged = value; } } }

        private ICommand _AddRowAbove;
        public ICommand AddRowAbove { get { return _AddRowAbove; } set { if (value != _AddRowAbove) { _AddRowAbove = value; } } }

        private ICommand _AddRowBelow;
        public ICommand AddRowBelow { get { return _AddRowBelow; } set { if (value != _AddRowBelow) { _AddRowBelow = value; } } }

        private ICommand _AddColumnRight;
        public ICommand AddColumnRight { get { return _AddColumnRight; } set { if (value != _AddColumnRight) { _AddColumnRight = value; } } }

        private ICommand _AddColumnLeft;
        public ICommand AddColumnLeft { get { return _AddColumnLeft; } set { if (value != _AddColumnLeft) { _AddColumnLeft = value; } } }

        private ICommand _RemoveRows;
        public ICommand RemoveRows { get { return _RemoveRows; } set { if (value != _RemoveRows) { _RemoveRows = value; } } }

        private ICommand _RemoveColumns;
        public ICommand RemoveColumns { get { return _RemoveColumns; } set { if (value != _RemoveColumns) { _RemoveColumns = value; } } }

        private ICommand _CopyCom;
        public ICommand CopyCom { get { return _CopyCom; } set { if (value != _CopyCom) { _CopyCom = value; } } }

        private ICommand _PasteCom;
        public ICommand PasteCom { get { return _PasteCom; } set { if (value != _PasteCom) { _PasteCom = value; } } }

        private ICommand _FlipHorizontallyCom;
        public ICommand FlipHorizontallyCom { get { return _FlipHorizontallyCom; } set { if (value != _FlipHorizontallyCom) { _FlipHorizontallyCom = value; } } }

        private ICommand _FlipVerticallyCom;
        public ICommand FlipVerticallyCom { get { return _FlipVerticallyCom; } set { if (value != _FlipVerticallyCom) { _FlipVerticallyCom = value; } } }

        private ICommand _MouseInPicture;
        public ICommand MouseInPicture { get { return _MouseInPicture; } set { if (value != _MouseInPicture) { _MouseInPicture = value; } } }

        private ICommand _MouseOutPicture;
        public ICommand MouseOutPicture { get { return _MouseOutPicture; } set { if (value != _MouseOutPicture) { _MouseOutPicture = value; } } }

        private ICommand _SelectAllCom;

        public ICommand SelectAllCom { get { return _SelectAllCom; } set { if (value != _SelectAllCom) { _SelectAllCom = value; } } }

        private ICommand _ChangeColorCom;

        public ICommand ChangeColorCom
        {
            get { return _ChangeColorCom; }
            set { _ChangeColorCom = value; RaisePropertyChanged(); }
        }


        #endregion
    }
}

