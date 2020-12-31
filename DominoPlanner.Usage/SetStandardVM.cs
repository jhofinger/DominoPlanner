﻿using Avalonia.Controls;
using DominoPlanner.Core;
using DominoPlanner.Usage.UserControls.ViewModel;
using System;
using System.IO;
using System.Windows.Input;

namespace DominoPlanner.Usage
{
    class SetStandardVM : ModelBase
    {
        public SetStandardVM()
        {
            var StandardColorPath = Properties.Settings.Default.StandardColorArray;
            SetStandardColor = new RelayCommand(o => { SetColorPath(); });
            SetStandardPath = new RelayCommand(o => { SetStandardPathOpen(); });
            ClearList = new RelayCommand(o => { ClearListMet(); });
            standardpath = Properties.Settings.Default.StandardProjectPath;

            if (!File.Exists(StandardColorPath))
            {
                try
                {
                    var share_path = MainWindowViewModel.ShareDirectory;
                    File.Copy(Path.Combine(share_path, "Resources", "lamping.DColor"), StandardColorPath);
                }
                catch { }
            }

            ColorVM = new ColorListControlVM(StandardColorPath);
        }

        #region prop
        private ColorListControlVM _ColorVM;
        public ColorListControlVM ColorVM
        {
            get { return _ColorVM; }
            set
            {
                if (_ColorVM != value)
                {
                    _ColorVM = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string _standardpath;
        public string standardpath
        {
            get { return _standardpath; }
            set
            {
                if (_standardpath != value)
                {
                    _standardpath = value;
                    Properties.Settings.Default.StandardProjectPath = value;
                    Properties.Settings.Default.Save();
                    RaisePropertyChanged();
                }
            }
        }

        #endregion

        #region Method

        private async void SetStandardPathOpen()
        {
            OpenFolderDialog ofd = new OpenFolderDialog { Directory = standardpath };
            var result = await ofd.ShowAsync(window);
            if (result != null && !string.IsNullOrEmpty(result))
            {
                standardpath = result;
            }
        }

        private async void SetColorPath()
        {
            var StandardColorPath = Properties.Settings.Default.StandardColorArray;
            OpenFileDialog openFileDialog = new OpenFileDialog();
            try
            {
                openFileDialog.Directory = ColorVM.FilePath;
                openFileDialog.Filters = new System.Collections.Generic.List<FileDialogFilter>
                {
                    new FileDialogFilter() { Extensions = new System.Collections.Generic.List<string> { Properties.Settings.Default.ColorExtension,  "clr", "farbe"}, Name = "All color files"},
                    new FileDialogFilter() { Extensions = new System.Collections.Generic.List<string> { Properties.Settings.Default.ColorExtension }, Name = "DominoPlanner 3.x color files"},
                    new FileDialogFilter() { Extensions = new System.Collections.Generic.List<string> {"clr"}, Name = "DominoPlanner 2.x color files"},
                    new FileDialogFilter() { Extensions = new System.Collections.Generic.List<string> {"farbe"}, Name = "Dominorechner color files"},
                };
                var share_path = MainWindowViewModel.ShareDirectory;
                openFileDialog.Directory = Path.Combine(share_path, "Resources");
            }
            catch (Exception) { }
            var result = await openFileDialog.ShowAsync(window);
            if (result != null && result.Length != 0)
            {
                var filename = result[0];
                if (File.Exists(filename))
                {
                    ColorRepository colorList;
                    int colorListVersion;
                    try
                    {
                        colorList = Workspace.Load<ColorRepository>(filename);
                        colorListVersion = 3;
                    }
                    catch
                    {
                        // Colorlist version 1 or 2
                        try
                        {
                            colorList = new ColorRepository(filename);
                            colorListVersion = 1;
                        }
                        catch
                        {
                            // file not readable
                            await Errorhandler.RaiseMessage("Color repository file is invalid", "Error", Errorhandler.MessageType.Error);
                            return;
                        }
                    }
                    File.Delete(StandardColorPath);
                    if (colorListVersion == 3)
                    {
                        File.Copy(filename, StandardColorPath);
                    }
                    else if (colorListVersion != 0)
                    {
                        colorList.Save(StandardColorPath);
                    }
                }
                Workspace.CloseFile(StandardColorPath);
                ColorVM.Reload(StandardColorPath);
            }
        }

        private void ClearListMet()
        {
            ColorVM.ResetList();
        }
        #endregion

        #region Command
        private ICommand _SetStandardColor;
        public ICommand SetStandardColor { get { return _SetStandardColor; } set { if (value != _SetStandardColor) { _SetStandardColor = value; } } }

        private ICommand _SetStandardPath;
        public ICommand SetStandardPath { get { return _SetStandardPath; } set { if (value != _SetStandardPath) { _SetStandardPath = value; } } }

        private ICommand _ClearList;
        internal SetStandardV window;

        public ICommand ClearList { get { return _ClearList; } set { if (value != _ClearList) { _ClearList = value; } } }

        #endregion
    }
}