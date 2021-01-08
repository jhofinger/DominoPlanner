﻿using Avalonia.Collections;
using Avalonia.Media;
using DominoPlanner.Core;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.CompilerServices;

namespace DominoPlanner.Usage
{
    // Viewmodel for DominoColor
    public class ColorListEntry : ModelBase
    {
        public Action<object, object, string, bool, Action, Action> ValueChanged;
        protected void PropertyValueChanged(object sender, object value_new, [CallerMemberName]
        string membername = "", bool producesUnsavedChanges = true, Action PostAction = null, Action PostUndoAction = null)
        {
            ValueChanged?.Invoke(sender, value_new, membername, producesUnsavedChanges, PostAction, PostUndoAction);
        }
        public ColorListEntry()
        {
            ProjectCount = new ObservableCollection<int>();
            SumAll = 0;
        }

        private void ProjectCount_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            SumAll = ProjectCount.Sum();
        }
        public static ColorListEntry ColorListEntryFactory(IDominoColor color, ColorRepository repo, int ProjectCountLength)
        {
            ColorListEntry result;
            if (color is ColorMixColor cmc)
            {
                result = new ColorMixEntry(cmc, repo);
            }
            else
            {
                result = new ColorListEntry() { DominoColor = color };
            }
            result.SortIndex = repo.Anzeigeindizes[repo.IndexOf(color)];
            result.ProjectCount = new ObservableCollection<int>(Enumerable.Repeat(0, ProjectCountLength));
            return result;
        }

        #region PROPERTIES
        private IDominoColor _DominoColor;
        public IDominoColor DominoColor
        {
            get { return _DominoColor; }
            set
            {
                if (_DominoColor != value)
                {
                    _DominoColor = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string Name
        {
            get { return DominoColor.name; }
            set { PropertyValueChanged(this, value); DominoColor.name = value; RaisePropertyChanged(); }
        }


        public int Count
        {
            get { return DominoColor.count; }
            set { PropertyValueChanged(this, value); DominoColor.count = value; RaisePropertyChanged(); }
        }

        public Color Color
        {
            get { return DominoColor.mediaColor; }
            set { PropertyValueChanged(this, value); DominoColor.mediaColor = value; RaisePropertyChanged(); }
        }


        private int _SortIndex;
        public int SortIndex
        {
            get { return _SortIndex; }
            set
            {
                if (_SortIndex != value)
                {
                    _SortIndex = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int _SumAll;
        public int SumAll
        {
            get { return _SumAll; }
            set
            {
                if (_SumAll != value)
                {
                    _SumAll = value;
                    RaisePropertyChanged();
                }
            }
        }
        private double _weight;
        public double Weight
        {
            get { return _weight; }
            set
            {
                if (_weight != value)
                {
                    _weight = value;
                    RaisePropertyChanged();
                }
            }
        }

        public bool Deleted
        {
            get { return DominoColor.Deleted; }
            set { DominoColor.Deleted = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<int> _ProjectCount;
        public ObservableCollection<int> ProjectCount
        {
            get { return _ProjectCount; }
            set
            {
                if (_ProjectCount != value)
                {
                    if (_ProjectCount != null)
                        _ProjectCount.CollectionChanged -= ProjectCount_CollectionChanged; 
                    _ProjectCount = value;
                    if(_ProjectCount != null)
                        _ProjectCount.CollectionChanged += ProjectCount_CollectionChanged;
                    RaisePropertyChanged();
                }
            }
        }
        #endregion
    }
    public class ColorMixComponentVM : ModelBase
    {
        public ColorMixComponent model;
        private ColorMixEntry parent;

        public Action<object, object, string, bool, Action, Action> ValueChanged;
        protected void PropertyValueChanged(object sender, object value_new, [CallerMemberName]
        string membername = "", bool producesUnsavedChanges = true, Action PostAction = null, Action PostUndoAction = null)
        {
            ValueChanged?.Invoke(sender, value_new, membername, producesUnsavedChanges, PostAction, PostUndoAction);
        }

        public int Count
        {
            get { return model.count; }
            set { PropertyValueChanged(this, value); model.count = value; RaisePropertyChanged(); parent.UpdatePreview(); }
        }

        public int Index
        {
            get { return model.index; }
            set { PropertyValueChanged(this, value); model.index = value; RaisePropertyChanged(); parent.UpdatePreview();  }
        }
        public ColorMixComponentVM(ColorMixComponent model, ColorMixEntry parent)
        {
            this.model = model;
            this.parent = parent;
            DeleteCommand = new RelayCommand((o) => parent.Delete(this));
        }
        private RelayCommand deleteCommand;
        public RelayCommand DeleteCommand
        {
            get { return deleteCommand; }
            set { deleteCommand = value; RaisePropertyChanged();}
        }
        
    }
    public class ColorMixEntry : ColorListEntry
    {
        public ColorMixColor Model {
            get{ return DominoColor as ColorMixColor; }
        }
        public bool CountsAreAbsolute
        {
            get { return Model.AbsoluteCount; }
            set { Model.AbsoluteCount = value; RaisePropertyChanged(); }
        }

        private AvaloniaList<ColorMixComponentVM> components;
        public AvaloniaList<ColorMixComponentVM> Components
        {
            get { return components; }
            set { 
                if (components != null) 
                    components.CollectionChanged -= ComponentsChanged; 
                components = value;
                value.CollectionChanged += ComponentsChanged; 
            }
        }
        private RelayCommand addCommand;
        public RelayCommand AddCommand
        {
            get { return addCommand; }
            set { addCommand = value; RaisePropertyChanged(); }
        }

        public ColorRepository ColorRepository { get; }
        private DrawingImage preview;

        public DrawingImage Preview
        {
            get { return preview; }
            set { preview = value; RaisePropertyChanged(); }
        }


        private void ComponentsChanged(object o, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (ColorMixComponentVM i in e.NewItems)
                {
                    if (!Model.colors.Contains(i.model))
                        Model.colors.Add(i.model);
                    i.ValueChanged = this.ValueChanged;
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (ColorMixComponentVM i in e.OldItems)
                {
                    Model.colors.Remove(i.model);
                }
            }
            UpdatePreview();
        }
        public void UpdatePreview()
        {
            var di = new DrawingImage();
            var gd = new DrawingGroup();
            di.Drawing = gd;
            double x = 0;
            var sum = Components.Sum(x => x.Count);
            foreach (ColorMixComponentVM component in Components)
            {
                var width = 40.0 / sum * component.Count;
                gd.Children.Add(new GeometryDrawing() { Geometry = new RectangleGeometry(new Avalonia.Rect(x, 0, width, 20)), Brush = new SolidColorBrush(ColorRepository[component.Index].mediaColor) });
                x += width;
            }
            Preview = di;

        }

        public ColorMixEntry(ColorMixColor color, ColorRepository repo)
        {
            ColorRepository = repo;
            DominoColor = color;
            Components = new AvaloniaList<ColorMixComponentVM>();
            foreach (var c in color.colors)
            {
                Components.Add(new ColorMixComponentVM(c, this));
            }
            AddCommand = new RelayCommand((o) => AddColor(o as ColorListEntry));
            UpdatePreview();
        }
        public void AddColor(ColorListEntry reference)
        {
            
            Components.Add(new ColorMixComponentVM(new ColorMixComponent(){ count = 1, index = ColorRepository.IndexOf(reference.DominoColor) + 1 }, this));
            UpdatePreview();
        }

        internal void Delete(ColorMixComponentVM colorMixComponentVM)
        {
            Components.Remove(colorMixComponentVM);
        }
    }
}
