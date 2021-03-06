﻿using Avalonia.Collections;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using DominoPlanner.Core;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace DominoPlanner.Usage.UserControls.ViewModel
{
    public class CreateRectangularStructureVM : CreateStructureVM
    {
        
        #region CTOR
        public CreateRectangularStructureVM(StructureParameters d, bool? AllowRegenerate) : base(d, AllowRegenerate)
        {
            var result = StuctureTypes();
            list = result.Item2;
            structures = result.Item1;
            for (int i = 0; i < structures.Count; i++)
            {
                if (XNode.DeepEquals(structures[i], XElement.Parse(currentStructure._structureDefinitionXML)))
                {
                    structure_index = i;
                }
            }
            if (structure_index == -1)
            {
                structure_index = 0;
            }
            UnsavedChanges = false;
            TargetSizeAffectedProperties = new string[] { nameof(sLength), nameof(sHeight) };
        }
        #endregion

        #region fields
        public List<XElement> structures;
        #endregion

        #region prop
        private List<string> _list;
        public List<string> list
        {
            get { return _list; }
            set
            {
                if (_list != value)
                {
                    _list = value;
                    RaisePropertyChanged();
                }
            }
        }
        public StructureParameters currentStructure
        {
            get => CurrentProject as StructureParameters;
        }
        
        public int sLength
        {
            get => currentStructure.Length;
            set
            {
                if (currentStructure.Length != value)
                {
                    PropertyValueChanged(this, value);
                    currentStructure.Length = value;
                    RaisePropertyChanged();
                }
            }
        }
        
        public int sHeight
        {
            get { return currentStructure.Height; }
            set
            {
                if (currentStructure.Height != value)
                {
                    PropertyValueChanged(this, value);
                    currentStructure.Height = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int _structure_index = -1;
        public int structure_index
        {
            get { return _structure_index; }
            set
            {
                if (_structure_index != value && value >= 0)
                {
                    var temp_struct_index = _structure_index;
                    // On load, Undo should not be triggered
                    if (_structure_index != -1)
                    {
                        PropertyValueChanged(this, value, ChangesSize: true);
                    }
                    _structure_index = value;
                    SelectedStructureElement = structures.ElementAt(_structure_index);
                    TabPropertyChanged(nameof(VisibleFieldplan), ProducesUnsavedChanges: false);
                    RaisePropertyChanged();
                    RefreshDescriptionImages();
                    if (temp_struct_index != -1)
                    {
                        RefreshTargetSize();
                    }
                    Refresh();
                }
            }
        }
        private AvaloniaList<IImage> _description_imgs;
        public AvaloniaList<IImage> description_imgs
        {
            get { return _description_imgs; }
            set
            {
                _description_imgs = value;
                RaisePropertyChanged();
            }
        }
        protected XElement _selectedStructureElement;

        public XElement SelectedStructureElement
        {
            get
            {
                return _selectedStructureElement;
            }
            set
            {
                _selectedStructureElement = value;
                currentStructure.StructureDefinitionXML = value;
            }
        }
        #endregion

        #region Methods

        protected override void PostCalculationUpdate()
        {
            TabPropertyChanged(nameof(sHeight), ProducesUnsavedChanges: false);
            TabPropertyChanged(nameof(sLength), ProducesUnsavedChanges: false);
        }
        public static Tuple<List<XElement>, List<string>> StuctureTypes()
        {
            var structures = new List<XElement>();
            var names = new List<string>();
            try
            {
                string structurepath = Path.Combine(MainWindowViewModel.ShareDirectory, UserSettings.Instance.StructureTemplates);
                if (!File.Exists(structurepath))
                {
                    Debug.WriteLine("Structure file not found");
                }
                StreamReader fr = new StreamReader(structurepath);
                XElement xElement = XElement.Parse(fr.ReadToEnd());
                structures = xElement.Elements().ToList();

                foreach (var structure in structures)
                {
                    names.Add(structure.FirstAttribute.Value);
                }
            }
            catch (Exception es)
            {
                System.Diagnostics.Debug.WriteLine(es);
            }
            return new Tuple<List<XElement>, List<string>>(structures, names);
        }
        

        public void RefreshDescriptionImages()
        {
            SKSurface[,] previews = StructureParameters.getPreviews(47, SelectedStructureElement);
            description_imgs = new AvaloniaList<IImage>();
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    var bit = previews[j, i].Snapshot();
                    description_imgs.Add(Bitmap.DecodeToWidth(bit.Encode(SKEncodedImageFormat.Png, 100).AsStream(), bit.Width));
                }
            }
        }
        #endregion
    }
}
