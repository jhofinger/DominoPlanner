﻿using Avalonia.Media;
using DominoPlanner.Core;
using System.IO;
using System.Windows.Input;

namespace DominoPlanner.Usage
{
    internal class ExportOptionVM : ModelBase
    {
        #region CTOR
        public ExportOptionVM()
        {
            ShowImageSize = false;
            Cancel = new RelayCommand((o) => { result = false; Close = true; });
            OK = new RelayCommand((o) => { result = true; Close = true; });
            DrawBorders = true;
            Expandable = true;
        }
        #endregion

        #region PROPERTIES

        public bool result;
        private bool _Close;
        public bool Close
        {
            get { return _Close; }
            set
            {
                if (_Close != value)
                {
                    _Close = value;
                    RaisePropertyChanged();
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

        private bool _draw_borders;
        public bool DrawBorders
        {
            get { return _draw_borders; }
            set
            {
                if (_draw_borders != value)
                {
                    _draw_borders = value;
                    RaisePropertyChanged();
                }
            }
        }
        private Color _backgroundColor;
        public Color BackgroundColor
        {
            get { return _backgroundColor; }
            set
            {
                if (_backgroundColor != value)
                {
                    _backgroundColor = value;
                    RaisePropertyChanged();
                }
            }
        }
        private bool _collapsed;
        public bool Collapsed
        {
            get { return _collapsed; }
            set
            {
                if (_collapsed != value)
                {
                    _collapsed = value;

                    RaisePropertyChanged();
                    if (Collapsed)
                    { //jojo?? hier noch irgendwie reagiserne?
                        //MaxSize = expanded_height > expanded_width ? expanded_height : expanded_width;
                    }
                    else
                    {
                        //MaxSize = height > width ? height : width;
                    }
                }
            }
        }

        private bool _ShowImageSize;
        public bool ShowImageSize
        {
            get { return _ShowImageSize; }
            set
            {
                if (_ShowImageSize != value)
                {
                    _ShowImageSize = value;
                    RaisePropertyChanged();
                }
            }
        }
        #endregion

        #region command
        private ICommand _Cancel;
        public ICommand Cancel { get { return _Cancel; } set { if (value != _Cancel) { _Cancel = value; } } }

        private ICommand _OK;
        public ICommand OK { get { return _OK; } set { if (value != _OK) { _OK = value; } } }

        #endregion
    }

    internal class ProjectExportOptionsVM : ExportOptionVM
    {
        private int width;
        private int height;
        private int expanded_width;
        private int expanded_height;
        public ProjectExportOptionsVM(IDominoProvider provider) : base()
        {
            ShowImageSize = true;
            height = provider.Last.PhysicalHeight;
            width = provider.Last.PhysicalLength;
            expanded_width = provider.Last.PhysicalExpandedHeight;
            expanded_height = provider.Last.PhysicalExpandedLength;
            Expandable = provider is FieldParameters;
            if (provider is FieldParameters fp)
            {
                Collapsed = true;
            }
            MaxSize = 10000; // height > width ? height : width;
            ImageSize = height > width ? height : width;
            ImageSize = ImageSize > MaxSize ? MaxSize : ImageSize;
            Filename = Path.GetFileName(Workspace.Find(provider));
        }
        #region properties
        
        private string _filename;
        public string Filename
        {
            get => _filename;
            set
            {
                if (value != _filename)
                {
                    _filename = value;
                    RaisePropertyChanged();
                }
            }
        }
        private int _imageSize;
        public int ImageSize
        {
            get => _imageSize;
            set
            {
                if (value != _imageSize)
                {
                    _imageSize = value;
                    RaisePropertyChanged();
                }
            }
        }
        

        

        public int MaxSize { get; set; }
        #endregion
    }
}