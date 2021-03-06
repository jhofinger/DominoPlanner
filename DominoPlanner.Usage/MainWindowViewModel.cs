﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using DominoPlanner.Usage.Serializer;
using DominoPlanner.Usage.UserControls.ViewModel;
using DominoPlanner.Core;
using MessageBox.Avalonia;
using MessageBox.Avalonia.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Input;
using System.Runtime.InteropServices;
using static DominoPlanner.Usage.Localizer;

namespace DominoPlanner.Usage
{    
    class MainWindowViewModel : ModelBase
    {
        #region CTOR
        public MainWindowViewModel()
        {
            OsType = Environment.OSVersion.Platform;
            ShowWindowMenu = !RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            NewFieldStruct = new RelayCommand(o => { NewFieldStructure(); });
            MenuSetStandard = new RelayCommand(async o => { await new SetStandardV().ShowDialogWithParent<MainWindow>(); });
            AddExistingProject = new RelayCommand(o => { AddProject_Exists(); });
            AddExistingItem = new RelayCommand(o => { AddItem_Exists(); });
            NewProject = new RelayCommand(o => { CreateNewProject(); });
            SaveAll = new RelayCommand(o => { SaveAllOpen(); });
            SaveCurrentOpen = new RelayCommand(async o => { await SaveCurrentOpenProject(); });
            FileListClickCommand = new RelayCommand(o => { OpenItemFromPath(o); });
            OpenAbout = new RelayCommand(o => { OpenAboutDialog(); });
            Tabs = new ObservableCollection<UserControls.ViewModel.TabItem>();
        }
        public static string ShareDirectory
        {
            get
            {
#if DEBUG
                return Environment.CurrentDirectory;
#else
                return UserSettings.AppDataPath;
#endif
            }

        }

        internal async void AfterStartupChecks()
        {

            if (FirstStartup)
            {
                UserSettings.Instance.StandardColorArray = Path.Combine(UserSettings.AppDataPath, "colors.DColor");
                UserSettings.Instance.StandardProjectPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "DominoPlanner");
                UserSettings.Instance.OpenProjectList = Path.Combine(UserSettings.AppDataPath, "OpenProjects.xml");
                if (!File.Exists(UserSettings.Instance.OpenProjectList))
                {
                    File.Create(UserSettings.Instance.OpenProjectList).Close();
                }
                UserSettings.Instance.StructureTemplates = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Structures.xml");
                Directory.CreateDirectory(UserSettings.Instance.StandardProjectPath);
                Directory.CreateDirectory(UserSettings.AppDataPath);
                OpenProjectSerializer.Create();
                FirstStartup = false;
            }

            while (!File.Exists(UserSettings.Instance.StandardColorArray))
            {
               await Errorhandler.RaiseMessage(_("Please create a default color table."), _("Missing Color Table"), Errorhandler.MessageType.Info);
               await new SetStandardV().ShowDialogWithParent<MainWindow>();
            }
            LoadProjectList();
        }

        internal void KeyPressed(object sender, KeyEventArgs args)
        {
            if (!args.Handled)
            {
                SelectedTab?.KeyPressed(sender, args);
            }
        }

        private async void CleanUpTabs(AssemblyNodeVM removedNode)
        {
            foreach (DominoWrapperNodeVM item in removedNode.Children)
            {
                if (item is AssemblyNodeVM assemblyNodeVM)
                {
                    CleanUpTabs(assemblyNodeVM);
                }
                else
                {
                    List<UserControls.ViewModel.TabItem> toClose = Tabs.Where(x => x.Path == item.AbsolutePath).ToList();
                    foreach (var tabItem in toClose)
                    {
                        await RemoveItem(tabItem);
                    }
                }
            }
        }

        internal async Task<bool> CloseAllTabs()
        {
            while (Tabs.Count > 0)
            {
                if (!(await RemoveItem(Tabs.First())))
                    return false;
            }
            return true;
        }
        private void RegisterNewViewModel(DominoProviderTabItem oldViewModel, DominoProviderTabItem newViewModel)
        {
            UserControls.ViewModel.TabItem tabItem = Tabs.Where(x => x.Content == oldViewModel).FirstOrDefault();
            tabItem.Content = newViewModel;
        }
        private void RegisterReplacementViewModel(DominoProviderTabItem oldVM, DominoProviderTabItem newVM)
        {
            UserControls.ViewModel.TabItem tabItem = Tabs.Where(x => x.Content == oldVM).FirstOrDefault();
            tabItem.Content = newVM;
        }
        private DominoProviderTabItem GetNewViewModel(DominoProviderTabItem oldVM)
        {
            UserControls.ViewModel.TabItem tabItem = Tabs.Where(x => x.Content == oldVM).FirstOrDefault();
            return UserControls.ViewModel.TabItem.ViewModelGenerator(((DominoProviderTabItem)tabItem.Content).CurrentProject, tabItem.Path);
        }
        #endregion

        #region prop
        public ObservableCollection<UserControls.ViewModel.TabItem> Tabs { get; set; }
        private UserControls.ViewModel.TabItem _SelectedTab;
        public UserControls.ViewModel.TabItem SelectedTab
        {
            get { return _SelectedTab; }
            set
            {
                if (_SelectedTab != value)
                {
                    if (_SelectedTab != null)
                    {
                        if (value != null && value.Content is ColorListControlVM colorList)
                        {
                            // to fix a weird bug where changing the tab resets the count
                            colorList.SelectedStone = null;
                        }
                    }
                    _SelectedTab = value;
                    if (SelectedTab != null)
                        _SelectedTab.Content.ResetContent();
                    RaisePropertyChanged();
                }
            }
        }
        private DominoWrapperNodeVM _SelectedProject;
        public DominoWrapperNodeVM SelectedProject
        {
            get { return _SelectedProject; }
            set
            {
                if (_SelectedProject != value)
                {
                    _SelectedProject = value;
                    RaisePropertyChanged();
                }
            }
        }
        public static ObservableCollection<AssemblyNodeVM> _Projects;
        public ObservableCollection<AssemblyNodeVM> Projects
        {
            get { return _Projects; }
            set
            {
                if (_Projects != value)
                {
                    if(_Projects != null)
                    {
                        _Projects.CollectionChanged -= Children_CollectionChanged;
                    }
                    _Projects = value;
                    if (_Projects != null)
                    {
                        _Projects.CollectionChanged += Children_CollectionChanged;
                    }
                    RaisePropertyChanged();
                }
            }
        }

        private void AddAssemblyNode(AssemblyNodeVM newAssemblyNode)
        {
            if (newAssemblyNode != null)
            {
                newAssemblyNode.Children.CollectionChanged += Children_CollectionChanged;
                foreach (AssemblyNodeVM subChilds in newAssemblyNode.Children.OfType<AssemblyNodeVM>())
                {
                    AddAssemblyNode(subChilds);
                }
            }
        }

        private void RemovedAssemblyNode(AssemblyNodeVM removedAssemblyNode)
        {
            if (removedAssemblyNode != null)
            {
                removedAssemblyNode.Children.CollectionChanged -= Children_CollectionChanged;
                CleanUpTabs(removedAssemblyNode);
                foreach (AssemblyNodeVM subChilds in removedAssemblyNode.Children.OfType<AssemblyNodeVM>())
                {
                    RemovedAssemblyNode(subChilds);
                }
            }
        }

        private void Children_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if(e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                foreach (AssemblyNodeVM assemblyNode in e.NewItems.OfType<AssemblyNodeVM>())
                {
                    AddAssemblyNode(assemblyNode);
                }
            }
            if(e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                foreach (AssemblyNodeVM oldAssemblynode in e.OldItems.OfType<AssemblyNodeVM>())
                {
                    RemovedAssemblyNode(oldAssemblynode);
                }
            }
        }

        private PlatformID osType;
        public PlatformID OsType
        {
            get { return osType; }
            set
            {
                if(osType != value)
                {
                    osType = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool _ShowWindowMenu;
        public bool ShowWindowMenu
        {
            get { return _ShowWindowMenu; }
            set
            {
                if(_ShowWindowMenu != value)
                {
                    _ShowWindowMenu = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool _FirstStartup = true;
        [SettingsAttribute("MainWindowViewModel", true)]
        public bool FirstStartup
        {
            get { return _FirstStartup; }
            set
            {
                if (_FirstStartup != value)
                {
                    _FirstStartup = value;
                    RaisePropertyChanged();
                }
            }
        }
        #endregion

        #region Command
        private ICommand _AddExistingProject;
        public ICommand AddExistingProject { get { return _AddExistingProject; } set { if (value != _AddExistingProject) { _AddExistingProject = value; } } }

        private ICommand _AddExistingItem;
        public ICommand AddExistingItem { get { return _AddExistingItem; } set { if (value != _AddExistingItem) { _AddExistingItem = value; } } }

        private ICommand _NewProject;
        public ICommand NewProject { get { return _NewProject; } set { if (value != _NewProject) { _NewProject = value; } } }

        private ICommand _NewFieldStruct;
        public ICommand NewFieldStruct { get { return _NewFieldStruct; } set { if (value != _NewFieldStruct) { _NewFieldStruct = value; } } }

        private ICommand _MenuSetStandard;
        public ICommand MenuSetStandard { get { return _MenuSetStandard; } set { if (value != _MenuSetStandard) { _MenuSetStandard = value; } } }

        private ICommand _SaveAll;
        public ICommand SaveAll { get { return _SaveAll; } set { if (value != _SaveAll) { _SaveAll = value; } } }

        private ICommand _SaveCurrentOpen;
        public ICommand SaveCurrentOpen { get { return _SaveCurrentOpen; } set { if (value != _SaveCurrentOpen) { _SaveCurrentOpen = value; } } }

        private ICommand _FileListClickCommand;
        public ICommand FileListClickCommand { get { return _FileListClickCommand; } set { if (value != _FileListClickCommand) { _FileListClickCommand = value; } } }

        private ICommand _OpenAboutCommand;
        public ICommand OpenAbout { get { return _OpenAboutCommand; } set { if (value != _OpenAboutCommand) { _OpenAboutCommand = value; } } }

        #endregion

        #region Methods
        #region Eventmethods
        /// <summary>
        /// Remove selected Project
        /// </summary>
        //private void RemoveMI_Click(object sender, System.Windows.RoutedEventArgs e)
        //{
        //    List<TabItem> removeList = Tabs.Where(x => x.ProjectID == ((ProjectListComposite)((System.Windows.Controls.MenuItem)sender).DataContext).OwnID).ToList<TabItem>();
        //    for (int i = 0; i < removeList.Count; i++)
        //    {
        //        RemoveItem(removeList[0]);
        //    }

        //    if (OpenProjectSerializer.RemoveOpenProject(((ProjectListComposite)((System.Windows.Controls.MenuItem)sender).DataContext).OwnID))
        //    {
        //        Projects.Remove(((ProjectListComposite)((System.Windows.Controls.MenuItem)sender).DataContext));
        //    }
        //    else
        //    {
        //        Errorhandler.RaiseMessage("Could not remove the project!", "Error", Errorhandler.MessageType.Error);
        //    }
        //}

        internal void SaveSettings()
        {
            UserSettingsSerializer.Instance.SaveSettings();
        }
        /// <summary>
        /// Clickevent wenn in der Baumstruktur ein Projektnode geklickt wird
        /// </summary>
        /// <param name="sender">Gecklicktes Node</param>
        /// <param name="e"></param>
        private void Item_IsClicked(object sender, EventArgs e)
        {
            try
            {
                OpenItem(UserControls.ViewModel.TabItem.TabItemGenerator(sender as NodeVM));
                (sender as NodeVM).BrokenReference = false;
            }
            catch (FileNotFoundException)
            {
                (sender as NodeVM).BrokenReference = true;
            }
        }
        private void OpenItemFromPath(object param)
        {
            string path = param.ToString();
            string ex = Path.GetExtension(path).ToLower();
            if (ex == "." + Declares.ColorExtension.ToLower() || ex == "." + Declares.ObjectExtension.ToLower())
            {
                OpenItem(GetTab(path) ?? new UserControls.ViewModel.TabItem(path));
            }
        }
        private UserControls.ViewModel.TabItem GetTab(NodeVM toOpen)
        {
            return GetTab(toOpen.AbsolutePath);
        }
        private UserControls.ViewModel.TabItem GetTab(string toOpen)
        {
            return Tabs.FirstOrDefault(x => Path.GetFullPath(x.Path).Equals(Path.GetFullPath(toOpen), StringComparison.OrdinalIgnoreCase));
        }
        private void OpenItem(UserControls.ViewModel.TabItem toOpen)
        {
            if (!Tabs.Contains(toOpen))
            {
                Tabs.Add(toOpen);
                toOpen.CloseIt = MainWindowViewModel_CloseIt;
                if (toOpen.Content is DominoProviderTabItem ti && ti.CurrentProject != null)
                {
                    ti.GetNewViewModel = GetNewViewModel;
                    ti.RegisterNewViewModel = RegisterNewViewModel;
                }
                if (toOpen.Content is EditProjectVM v)
                {
                    v.RecreateCanvasViewModel();
                }
            }
            SelectedTab = toOpen;
        }
        public void OpenFile(string filename)
        {
            foreach (string s in filename.Split('\n'))
            {
                var fn = s.Trim();
                var ext = Path.GetExtension(fn).ToLower();
                if (ext == "." + Declares.ObjectExtension.ToLower() || ext == "." + Declares.ColorExtension.ToLower())
                {
                    OpenItemFromPath(fn);
                }
                else if (ext == "." + Declares.ProjectExtension.ToLower())
                {
                    AssemblyNodeVM res = null;
                    foreach (AssemblyNodeVM p in Projects)
                    {
                        if (Path.GetFullPath(p.AbsolutePath).Equals(Path.GetFullPath(fn), StringComparison.OrdinalIgnoreCase))
                        {
                            res = p;
                            res.IsExpanded = true;
                            break;
                        }
                    }
                    if (res != null) continue;
                    if (File.Exists(fn))
                    {
                        OpenProject openProject = OpenProjectSerializer.AddOpenProject(Path.GetFileNameWithoutExtension(fn), Path.GetDirectoryName(fn));
                        LoadProject(openProject);
                    }
                }
            }
        }
        /// <summary>
        /// Selection Changed in der Baumstruktur (damit das akteuelle Item refreshed werden kann)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindowViewModel_SelectedEvent(object sender, EventArgs e)
        {
            if (sender is NodeVM node && node.IsSelected)
            {
                if (sender is AssemblyNodeVM assy)
                {
                    SelectedProject = assy;
                }
                else
                {
                    SelectedProject = node.Parent;
                }
            }
        }
        /// <summary>
        /// Aktuelles TabItem schließen
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async Task<bool> MainWindowViewModel_CloseIt(UserControls.ViewModel.TabItem tabItem)
        {
            return await RemoveItem(tabItem);
        }
        private async Task<bool> RemoveFileFromTabs(string path)
        {
            bool result = true;
            foreach (UserControls.ViewModel.TabItem tabItem in Tabs.Where(x => x.Path == path).ToArray())
            {
                result = result && await RemoveItem(tabItem);
            }
            return result;
        }
        private async Task<bool> RemoveNodeFromTabs(NodeVM node)
        {
            return await RemoveFileFromTabs(node.AbsolutePath);
        }
        private async Task<bool> RemoveItem(UserControls.ViewModel.TabItem tabItem)
        {
            bool remove = false;
            if (tabItem.Content.UnsavedChanges)
            {
                var msgbox = MessageBoxManager.GetMessageBoxStandardWindow(_("Warning"), string.Format(_("Save unsaved changes of {0}?"), tabItem.Header.TrimEnd('*')),
                    ButtonEnum.YesNoCancel, Icon.Warning);
                var result = await msgbox.ShowDialogWithParent<MainWindow>();
                if (result == ButtonResult.Yes)
                {
                    remove = await SaveCurrentOpenProject();
                }
                if (result == ButtonResult.No)
                {
                    // we need to close all files and remove all references to the DominoProvider, 
                    // so the file can be reopened again from disk
                    

                    void RecurseProjects(AssemblyNodeVM assembly)
                    {
                        foreach (NodeVM node in assembly.Children)
                        {
                            if (node is AssemblyNodeVM assy)
                                RecurseProjects(assy);
                            else if (node is DocumentNodeVM dn)
                            {
                                try
                                {
                                    if (dn.AbsolutePath == tabItem.Path)
                                        dn.DocumentModel.CloseFile();
                                }
                                catch
                                {
                                    // happens if one reference to another file is broken, in this case
                                    // we simply don't care
                                }
                            }
                        }
                    }
                    foreach (AssemblyNodeVM node in Projects)
                        RecurseProjects(node);
                    remove = true;
                }
            }
            else
            {
                remove = true;
            }
            if (remove) Tabs.Remove(tabItem);
            return remove;
        }

        #endregion
        /// <summary>
        /// Projektliste laden
        /// </summary>
        private async void LoadProjectList()
        {
            Projects = new ObservableCollection<AssemblyNodeVM>();
            List<OpenProject> OpenProjects = OpenProjectSerializer.GetOpenProjects();
            if (OpenProjects != null)
            {
                foreach (OpenProject curOP in OpenProjects)
                {
                    LoadProject(curOP);
                }
            }
            else
            {
                await Errorhandler.RaiseMessage(_("Error loading opened projects!"), _("Error"), Errorhandler.MessageType.Error);
                OpenProjectSerializer.Create();
            }
        }

        private async void LoadProject(OpenProject newProject)
        {
            bool remove = true;
            string projectpath = Path.Combine(newProject.path, $"{newProject.name}.{Declares.ProjectExtension}");
            if (File.Exists(projectpath))
            {
                remove = false;

                AssemblyNodeVM node = null;

                try
                {
                    AssemblyNode mainnode = new AssemblyNode(projectpath);
                    // check if the file can be deserialized properly
                    node = new AssemblyNodeVM(mainnode, OpenItem, RemoveNodeFromTabs, GetTab);
                    if (node.BrokenFile || (node.Model as AssemblyNode).Obj.ColorListBroken)
                        remove = true;
                }
                catch (Exception)
                {
                    try
                    {
                        AssemblyNode restored = await AssemblyNodeVM.RestoreAssembly(projectpath);
                        node = new AssemblyNodeVM(restored, OpenItem, RemoveNodeFromTabs, GetTab);
                    }
                    catch (FileNotFoundException)
                    {
                        remove = true;
                    }
                    catch
                    {
                        await Errorhandler.RaiseMessage(String.Format(_("The main project file of project {0} was damaged. An attempt to restore the file has been unsuccessful. \nThe project will be removed from the list of opened projects."), projectpath), _("Damaged File"), Errorhandler.MessageType.Error);
                        remove = true;
                    }
                }
                if (!remove)
                {
                    Projects.Add(node);
                }
            }
            if (remove)
            {
                await Errorhandler.RaiseMessage(string.Format(_("Unable to load project {0}. It might have been moved or damaged. \nPlease re-add it at its current location.\n\nThe project has been removed from the list of opened projects."), newProject.name), _("Error"), Errorhandler.MessageType.Error);
                OpenProjectSerializer.RemoveOpenProject(newProject.id);
            }
        }
        public async Task<bool> CheckIfParentProjectMissing()
        {
            if (SelectedAssembly == null)
            {
                await Errorhandler.RaiseMessage(_("Objects require a parent project, which contains the color list to be used. Please choose a parent project in the project panel."), _("Select parent project"), Errorhandler.MessageType.Error);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Neues Unterprojekt starten
        /// </summary>
        private async void NewFieldStructure()
        {
            if (!await CheckIfParentProjectMissing())
                SelectedAssembly.NewFieldStructure();
        }
        AssemblyNodeVM SelectedAssembly
        {
            get {
                if (SelectedProject == null)
                {
                    return null;
                }
                if (SelectedProject is AssemblyNodeVM ass)
                    return ass;
                else
                {
                    var cur = SelectedProject.Parent;
                    while (!(cur is AssemblyNodeVM))
                    {
                        cur = cur.Parent;
                    }
                    return cur;
                }
            }
        }

        private async void AddProject_Exists()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filters = new List<FileDialogFilter>() { new FileDialogFilter() { Extensions = new List<string> { Declares.ProjectExtension }, Name = Declares.ProjectExtension } },
                Directory = UserSettings.Instance.StandardProjectPath
            };
            //openFileDialog.RestoreDirectory = true;
            var result = await openFileDialog.ShowAsyncWithParent<MainWindow>();
            if (result != null && result.Length == 1 && File.Exists(result[0]))
            {
                {
                    OpenProject openProject = OpenProjectSerializer.AddOpenProject(Path.GetFileNameWithoutExtension(result[0]), Path.GetDirectoryName(result[0]));
                    LoadProject(openProject);
                }
            }
        }

        private async void AddItem_Exists()
        {
            if (!await CheckIfParentProjectMissing())
                SelectedAssembly.AddExistingItem();
        }
        private async void CreateNewProject()
        {
            NewProjectVM curNPVM = new NewProjectVM();
            await new NewProject(curNPVM).ShowDialogWithParent<MainWindow>();
            if (curNPVM.Close == true)
            {
                OpenProject newProj = OpenProjectSerializer.AddOpenProject(curNPVM.ProjectName, string.Format(@"{0}/{1}", curNPVM.SelectedPath, curNPVM.ProjectName));
                if (newProj == null)
                {
                    await Errorhandler.RaiseMessage(_("Could not create new Project!"), _("Error!"), Errorhandler.MessageType.Error);
                    return;
                }
                LoadProject(newProj);
            }
        }

        /// <summary>
        /// Save all open projects
        /// </summary>
        private async void SaveAllOpen()
        {
            foreach (UserControls.ViewModel.TabItem curTI in Tabs)
            {
                if (curTI.Content.UnsavedChanges)
                {
                    if (!curTI.Content.Save())
                    {
                        await Errorhandler.RaiseMessage(_("Error saving files!"), string.Format(_("Save aborted, unable to save file {0}"), curTI.Header), Errorhandler.MessageType.Error);
                        return;
                    }
                }
            }
            await Errorhandler.RaiseMessage(_("All files saved"), _("Success"), Errorhandler.MessageType.Info);
        }
        /// <summary>
        /// Save current project
        /// </summary>
        private async Task<bool> SaveCurrentOpenProject()
        {
            var result = SelectedTab.Content.Save();
            if (result)
                await Errorhandler.RaiseMessage(_("All changes saved"), _("Success"), Errorhandler.MessageType.Info);
            else
                await Errorhandler.RaiseMessage(_("Error saving changes"), _("Error"), Errorhandler.MessageType.Error);
            return result;
        }
        public async void OpenAboutDialog()
        {
            var aw = new AboutWindow();
            await aw.ShowDialogWithParent<MainWindow>();
        }
        #endregion
    }
    
}