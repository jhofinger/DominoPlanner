﻿using Avalonia.Controls;
using DominoPlanner.Core;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace DominoPlanner.UI
{
    public class NewProjectVM : ModelBase
    {
        #region CTOR
        public NewProjectVM()
        {
            SelectedPath = MainWindow.ReadSetting("StandardProjectPath");
            sPath = MainWindow.ReadSetting("StandardColorArray");
            ProjectName = "New Project";
            rbStandard = true;
            rbCustom = false; //damit die Labels passen
            SelectFolder = new RelayCommand(o => { SelectProjectFolder(); });
            SelectColor = new RelayCommand(o => { SelectColorArray(); });
            StartClick = new RelayCommand(o => { CreateNewProject(); });
        }
        #endregion

        #region Methods
        private void SelectProjectFolder()
        {
            OpenFolderDialog ofd = new OpenFolderDialog();
            var result = ofd.ShowDialog();
            if (result != null && result != "")
            {
                SelectedPath = result;
            }
        }

        private void CreateNewProject()
        {
            try
            {
                if (Directory.Exists(Path.Combine(SelectedPath, ProjectName)))
                {
                    Errorhandler.RaiseMessage("This folder already exists. Please choose another project name.", "Existing Folder", Errorhandler.MessageType.Error);
                    return;
                }

                string projectpath = Path.Combine(SelectedPath, ProjectName);
                Directory.CreateDirectory(projectpath);
                Directory.CreateDirectory(Path.Combine(projectpath, "Source Image"));
                Directory.CreateDirectory(Path.Combine(projectpath, "Planner Files"));

                DominoAssembly main = new DominoAssembly();
                main.Save(Path.Combine(projectpath, ProjectName + MainWindow.ReadSetting("ProjectExtension")));

                if (File.Exists(sPath))
                {
                    string colorPath = Path.Combine(SelectedPath, ProjectName, "Planner Files", $"colors{MainWindow.ReadSetting("ColorExtension")}");
                    File.Copy(sPath, colorPath);
                    main.colorPath = Path.Combine("Planner Files", "colors" + MainWindow.ReadSetting("ColorExtension"));
                }

                main.Save();

                Errorhandler.RaiseMessage($"The project {ProjectName}{MainWindow.ReadSetting("ProjectExtension")} has been created", "Created", Errorhandler.MessageType.Info);
                Close = true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Project creation failed: {0}", e.ToString());
            }
        }

        private void SelectColorArray()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            try
            {
                openFileDialog.Directory = sPath;
                openFileDialog.AllowMultiple = false;
                openFileDialog.Filters.Add(new FileDialogFilter() { Extensions = new List<string> { MainWindow.ReadSetting("ColorExtension") }, Name = "Color files" });
                openFileDialog.Filters.Add(new FileDialogFilter() { Extensions = new List<string> { "*" }, Name = "All files" });
            }
            catch (Exception) { }
            var result = openFileDialog.ShowDialog();
            if (result != null && result.Length == 1 && result[0] != "")
            {
                sPath = result[0];
            }
        }
        #endregion

        #region Prop
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

        private string _sPath;
        public string sPath
        {
            get { return _sPath; }
            set
            {
                if (_sPath != value)
                {
                    _sPath = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string _SelectedPath;
        public string SelectedPath
        {
            get { return _SelectedPath; }
            set
            {
                if (_SelectedPath != value)
                {
                    _SelectedPath = value;
                    RaisePropertyChanged();
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
                    RaisePropertyChanged();
                }
            }
        }

        private bool _rbStandard;
        public bool rbStandard
        {
            get { return _rbStandard; }
            set
            {
                if (_rbStandard != value)
                {
                    _rbStandard = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool _rbCustom;
        public bool rbCustom
        {
            get { return _rbCustom; }
            set
            {
                if (_rbCustom != value)
                {
                    _rbCustom = value;
                    RaisePropertyChanged();
                }
                if (value)
                    ColorVisibility = true;
                else
                    ColorVisibility = false;
            }
        }
        private bool _ColorVisibility;
        public bool ColorVisibility
        {
            get { return _ColorVisibility; }
            set
            {
                if (_ColorVisibility != value)
                {
                    _ColorVisibility = value;
                    RaisePropertyChanged();
                }
            }
        }

        #endregion

        #region Command
        private ICommand _SelectFolder;
        public ICommand SelectFolder { get { return _SelectFolder; } set { if (value != _SelectFolder) { _SelectFolder = value; } } }

        private ICommand _SelectColor;
        public ICommand SelectColor { get { return _SelectColor; } set { if (value != _SelectColor) { _SelectColor = value; } } }

        private ICommand _StartClick;
        public ICommand StartClick { get { return _StartClick; } set { if (value != _StartClick) { _StartClick = value; } } }
        #endregion
    }
}
