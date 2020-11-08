﻿using DominoPlanner.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Input;
using System.Windows;
using Avalonia.Controls;
using DominoPlanner.Usage.Serializer;
using Avalonia.Media;

namespace DominoPlanner.Usage.UserControls.ViewModel
{
    public abstract class NodeVM : ModelBase
    {
        
        public ObservableCollection<DominoWrapperNodeVM> Children { get; set; }

        private bool brokenReference;

        public bool BrokenReference
        {
            get { return brokenReference; }
            set { brokenReference = value; RaisePropertyChanged(); }
        }


        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool _IsExpanded;
        public bool IsExpanded
        {
            get { return _IsExpanded; }
            set
            {
                if (_IsExpanded != value)
                {
                    _IsExpanded = value;
                    RaisePropertyChanged();
                }
            }
        }
        public string ImagePath
        {
            get
            {
                if (AbsolutePath == "")
                    return "";
                var result = ImageHelper.GetImageOfFile(AbsolutePath);
                RaisePropertyChanged("AbsolutePath");
                return result;
            }
        }

        public AssemblyNodeVM parent { get; set; }
        public ContextMenu contextMenu { get; set; }

        public static Action<TabItem> openTab;
        public static Func<NodeVM, Task<bool>> closeTab;
        public static Func<NodeVM, TabItem> getTab;
        
        public abstract string RelativePathFromParent { get; set; }

        public virtual bool CheckPath()
        {

            try
            {
                return AbsolutePath != "";
            }
            catch (IOException)
            {
                return false;
            }
        }
        public string PathRoot
        {
            get => Path.GetDirectoryName(AbsolutePath);
        }
        internal string _AbsolutePath;
        public abstract string AbsolutePath { get; set; }

        private ContextMenu _ContextMenu;

        public ContextMenu ContextMenu
        {
            get
            {
                if (_ContextMenu == null)
                {
                    var ContextMenuEntries = BuildContextMenu();
                    if (ContextMenuEntries != null)
                    {
                        _ContextMenu = new ContextMenu();
                        _ContextMenu.Items = ContextMenuEntries;
                    }
                }
                return _ContextMenu;
            }
        }

        public string Name
        {
            get =>
   Path.GetFileNameWithoutExtension(RelativePathFromParent);
        }


        public NodeVM()
        {
            MouseClickCommand = new RelayCommand((o) => OpenInternal());
        }
        private ICommand _MouseClickCommand;
        public ICommand MouseClickCommand
        {
            get => _MouseClickCommand;
            set { _MouseClickCommand = value; RaisePropertyChanged(); }
        }
        public List<MenuItem> BuildContextMenu()
        {
            if (BrokenReference)
                return null;
            return this.GetType().GetMethods()
                      .Select(m => Tuple.Create(m, m.GetCustomAttributes(typeof(ContextMenuAttribute), false)))
                      .Where(tuple => tuple.Item2.Count() > 0)
                      .OrderBy(tuple => (tuple.Item2.First() as ContextMenuAttribute).Index)
                      .Select(tuple => ContextMenuEntry.GenerateMenuItem(tuple.Item2.First() as ContextMenuAttribute, tuple.Item1, this))
                      .ToList();
        }
        public static NodeVM NodeVMFactory(IDominoWrapper node, AssemblyNodeVM parent)
        {
            switch (node)
            {
                case AssemblyNode assy:
                    return new AssemblyNodeVM(assy, parent);
                case DocumentNode dn:
                    return new DocumentNodeVM(dn);
            }
            return null;
        }
        [ContextMenuAttribute("Open Folder", "Icons/folder_tar.ico", Index = 10)]
        public void OpenFolder()
        {
            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{ AbsolutePath}\"");
        }
        public async void OpenInternal()
        {
            if (brokenReference)
            {
                await new ReferenceManager().ShowDialog(MainWindowViewModel.GetWindow());
                parent?.AssemblyModel.Save();
                PostReferenceRestoration();
            }
            Open();
        }

        internal virtual void PostReferenceRestoration()
        {
            _ContextMenu = null;
            RaisePropertyChanged("ContextMenu");
            RaisePropertyChanged("ImagePath");
        }

        public abstract void Open();
        
    }
    public abstract class DominoWrapperNodeVM : NodeVM
    {
        private IDominoWrapper dominoWrapper;

        public IDominoWrapper Model
        {
            get { return dominoWrapper; }
            set { dominoWrapper = value; RaisePropertyChanged(); }
        }
        internal void RelativePathChanged()
        {
            _AbsolutePath = null;
            RaisePropertyChanged("AbsolutePath");
            RaisePropertyChanged("Name");
            Model.parent?.Save();
        }

    }

    public class AssemblyNodeVM : DominoWrapperNodeVM
    {
        public ColorNodeVM colorNode { get; set; }
        public AssemblyNode AssemblyModel
        {
            get => Model as AssemblyNode;
            set
            {
                Model = value;
                RaisePropertyChanged();
            }
        }

        public override string RelativePathFromParent
        {
            get
            {
                try
                {
                    var result = AssemblyModel.Path;
                    if (BrokenReference)
                    {
                        if (File.Exists(_AbsolutePath))
                            BrokenReference = false;
                    }
                    return result;
                }
                catch (FileNotFoundException)
                {
                    BrokenReference = true;
                    return "";
                }
            }
            set
            {
                AssemblyModel.Path = value;
                // Reload all children
                LoadChildren();
            }
        }
        public AssemblyNodeVM(AssemblyNode assembly, AssemblyNodeVM parent)
        {
            AssemblyModel = assembly;
            AssemblyModel.RelativePathChanged += (s, args) => RelativePathChanged();
            colorNode = new ColorNodeVM(this);
            LoadChildren();
        }

        public AssemblyNodeVM(AssemblyNode assembly, Action<TabItem> openTab,
            Func<NodeVM, Task<bool>> closeTab, Func<NodeVM, TabItem> getTab) : this(assembly, null, openTab, closeTab, getTab) { }

        public AssemblyNodeVM(AssemblyNode assembly, AssemblyNodeVM parent, Action<TabItem> openTab,
            Func<NodeVM, Task<bool>> closeTab, Func<NodeVM, TabItem> getTab)
        {
            AssemblyModel = assembly;
            AssemblyModel.RelativePathChanged += (s, args) => RelativePathChanged();
            NodeVM.openTab = openTab;
            NodeVM.closeTab = closeTab;
            NodeVM.getTab = getTab;
            colorNode = new ColorNodeVM(this);
            LoadChildren();
        }
        public void Initialize()
        {
            colorNode = new ColorNodeVM(this);
            LoadChildren();
        }
        public void LoadChildren()
        {
            try
            {
                Children = new ObservableCollection<DominoWrapperNodeVM>();
                Children.CollectionChanged -= ChildrenAddDelegates;
                Children.CollectionChanged += ChildrenAddDelegates;

                for (int i = 0; i < AssemblyModel.obj.children.Count; i++)
                {
                    var node = AssemblyModel.obj.children[i];
                    try
                    {
                        if (node is IDominoWrapper idw && (idw is AssemblyNode || idw is DocumentNode))
                        {
                            var vm = NodeVMFactory(idw, this);
                            vm.parent = this;
                            Children.Add(vm as DominoWrapperNodeVM);
                        }
                    }
                    catch (Exception ex) when (ex is InvalidDataException || ex is ProtoBuf.ProtoException)
                    {
                        // broken Subassembly
                        var restored = RestoreAssembly((node as AssemblyNode).AbsolutePath, colorNode.AbsolutePath);
                        {
                            restored.parent = AssemblyModel.obj;
                            // make color path relative
                            restored.Path = Workspace.MakeRelativePath(AbsolutePath, restored.Path);
                            AssemblyModel.obj.children[i] = restored;
                            Children.Add(NodeVMFactory(restored, this) as AssemblyNodeVM);
                            AssemblyModel.Save();
                        }

                    }
                    catch (FileNotFoundException)
                    {
                        // missing subassembly
                        /*string path = "";
                        if (node is AssemblyNode asn) path = asn.Path;
                        Errorhandler.RaiseMessage($"The project file {path} does not exist at the current location. " +
                            $"It will be removed from the project.", "Error", Errorhandler.MessageType.Error);*/
                        // Remove file from Assembly and decrease counter
                        /*AssemblyModel.obj.children.RemoveAt(i--);
                        AssemblyModel.Save();*/
                    }
                }
                AssemblyModel.obj.children.CollectionChanged += AssemblyModelChildren_CollectionChanged;
                this.BrokenReference = false;
            }
            catch (FileNotFoundException)
            {
                this.BrokenReference = true;
            }
        }
        internal override void PostReferenceRestoration()
        {
            this.LoadChildren();
            RaisePropertyChanged("Children");
            this.AssemblyModel.Save();
            base.PostReferenceRestoration();
        }
        private void ChildrenAddDelegates(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                foreach (NodeVM i in e.NewItems)
                {
                    if (i is DocumentNodeVM dn)
                    {
                        dn.DocumentModel.RelativePathChanged += (s, args) => dn.RelativePathChanged();
                    }
                    else if (i is AssemblyNodeVM an)
                    {
                        an.AssemblyModel.RelativePathChanged += (s, args) => an.RelativePathChanged();
                    }
                }

            }
        }
        private void AssemblyModelChildren_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (IDominoWrapper i in e.OldItems)
                {
                    if (i is AssemblyNode a)
                    {
                        Children.RemoveAll(x => x is AssemblyNodeVM vm && vm.AssemblyModel == a);
                    }
                    if (i is DocumentNode d)
                    {
                        Children.RemoveAll(x => x is DocumentNodeVM vm && vm.DocumentModel == d);
                    }
                }
            }
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    if (item is AssemblyNode || item is DocumentNode)
                    {
                        var newNode = NodeVMFactory(item as IDominoWrapper, this);
                        newNode.parent = this;
                        Children.Add(newNode as DominoWrapperNodeVM);
                    }
                }
            }
            AssemblyModel.Save();
        }
        public void RemoveChild(DominoWrapperNodeVM node)
        {
            AssemblyModel.obj.children.Remove(node.Model);
        }
        [ContextMenuAttribute("Open color list", "Icons/colorLine.ico", index: 0)]
        public void OpenColorList()
        {
            try
            {
                colorNode.Open();
                BrokenReference = false;
            }
            catch (FileNotFoundException)
            {
                BrokenReference = true;
            }
        }
        public override void Open()
        {
            // will be implemented when doing Masterplan
        }
        [ContextMenuAttribute("Add new object", "Icons/add.ico", index: 1)]
        public async void NewFieldStructure()
        {
            NewObjectVM novm = new NewObjectVM(Path.GetDirectoryName(AbsolutePath), AssemblyModel.obj);
            await new NewObject(novm).ShowDialog(MainWindowViewModel.GetWindow()) ;
            if (!novm.Close || novm.ResultNode == null) return;
            Children.Where(x => x.Model == novm.ResultNode).FirstOrDefault()?.Open();
        }
        [ContextMenuAttribute("Add existing object", "Icons/add.ico", index: 2)]
        public async void AddExistingItem()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filters.Add(
                new FileDialogFilter() { Extensions = new List<string> { MainWindow.ReadSetting("ProjectExtension"), MainWindow.ReadSetting("ObjectExtension") }, Name = "All DominoPlanner files" });
            openFileDialog.Filters.Add(
               new FileDialogFilter() { Extensions = new List<string> { MainWindow.ReadSetting("ObjectExtension") }, Name = "Object files" });
            openFileDialog.Filters.Add(
                new FileDialogFilter() { Extensions = new List<string> { MainWindow.ReadSetting("ProjectExtension") }, Name = "Project files" });
            var result = await openFileDialog.ShowAsync(MainWindowViewModel.GetWindow());
            if (result != null && result.Length == 1 && File.Exists(result[0]))
            {
                string extension = Path.GetExtension(result[0]).ToLower();
                if (extension == "." + MainWindow.ReadSetting("ObjectExtension").ToLower())
                {
                    try
                    {
                        IDominoWrapper node = IDominoWrapper.CreateNodeFromPath(AssemblyModel.obj, result[0]);
                        AssemblyModel.Save();
                        Children.Where(x => x.Model == node).FirstOrDefault()?.Open();
                    }
                    catch (FileNotFoundException)
                    {
                        Errorhandler.RaiseMessage("Error loading file", "Error", Errorhandler.MessageType.Error);
                    }
                }
                else if (extension == "." + MainWindow.ReadSetting("ProjectExtension").ToLower())
                {
                    try
                    {
                        string relativePath = Workspace.MakeRelativePath(AbsolutePath, result[0]);
                        var assy = Workspace.Load<DominoAssembly>(result[0]);
                        if (assy == AssemblyModel.obj || relativePath == "" || assy.ContainsReferenceTo(AssemblyModel.obj))
                        {
                            Errorhandler.RaiseMessage("This operation would create a circular dependency between assemblies. This is not supported.", "Circular Reference", Errorhandler.MessageType.Error);
                            return;
                        }
                        IDominoWrapper node = new AssemblyNode(relativePath, AssemblyModel.obj);
                        AssemblyModel.Save();
                    }
                    catch { }
                }

            }
        }
        [ContextMenuAttribute("Rename", "Icons/draw_freehand.ico", index: 3)]
        public async void Rename()
        {
            var dn = (AssemblyNode)Model;
            RenameObject ro = new RenameObject(Path.GetFileName(AbsolutePath));
            if (await ro.ShowDialog<bool>(MainWindowViewModel.GetWindow()) == true)
            {
                Workspace.CloseFile(AbsolutePath);
                var new_path = Path.Combine(Path.GetDirectoryName(AbsolutePath), ((RenameObjectVM)ro.DataContext).NewName);
                File.Move(AbsolutePath, new_path);
                if (parent == null)
                {
                    OpenProjectSerializer.RenameProject(AbsolutePath, new_path);
                    AssemblyModel.Path = new_path;

                }
                else
                {
                    AssemblyModel.Path = Workspace.MakeRelativePath(parent.AbsolutePath, new_path);
                }
            }
        }
        [ContextMenuAttribute("Remove", "Icons/remove.ico", index: 4)]
        public void Remove()
        {
            if (parent != null)
            {
                this.parent.RemoveChild(this);
            }
            else
            {
                MainWindowViewModel._Projects.Remove(this);
                int index = OpenProjectSerializer.GetProjectID(AbsolutePath);
                if (index >= 0) OpenProjectSerializer.RemoveOpenProject(index);
            }
        }
        [ContextMenuAttribute("Properties", "Icons/properties.ico", index: 20)]
        public void ShowProperties()
        {
            PropertiesWindow pw = new PropertiesWindow(Model);
            pw.ShowDialog(MainWindowViewModel.GetWindow());
        }
        public static AssemblyNode RestoreAssembly(string projectpath, string colorlistPath = null)
        {
            string colorpath = Path.Combine(Path.GetDirectoryName(projectpath), "Planner Files");
            var colorres = Directory.EnumerateFiles(colorpath, $"*.{MainWindow.ReadSetting("ColorExtension")}");
            // restore project if colorfile exists
            if (colorlistPath == null && colorres.First() == null)
            {
                throw new InvalidDataException("Color file not found");
            }
            colorlistPath = colorlistPath ?? colorres.First();
            Workspace.CloseFile(projectpath);
            if (File.Exists(projectpath))
                File.Copy(projectpath, Path.Combine(Path.GetDirectoryName(projectpath), $"backup_{DateTime.Now.ToLongTimeString().Replace(":", "_")}.{MainWindow.ReadSetting("ProjectExtension")}"));
            DominoAssembly newMainNode = new DominoAssembly();
            newMainNode.Save(projectpath);
            newMainNode.colorPath = Workspace.MakeRelativePath(projectpath, colorlistPath);
            foreach (string path in Directory.EnumerateDirectories(colorpath))
            {
                try
                {
                    var assembly = Directory.EnumerateFiles(path, $"*.{MainWindow.ReadSetting("ProjectExtension")}").
                        Where(x => Path.GetFileName(x).Contains("backup_")).FirstOrDefault();
                    if (string.IsNullOrEmpty(assembly)) continue;
                    AssemblyNode an = new AssemblyNode(Workspace.MakeRelativePath(projectpath, assembly), newMainNode);
                    newMainNode.children.Add(an);
                }
                catch { } // if error on add assembly, don't add assembly
            }
            foreach (string path in Directory.EnumerateFiles(colorpath, "*." + MainWindow.ReadSetting("ObjectExtension")))
            {
                try
                {
                    var node = (DocumentNode)IDominoWrapper.CreateNodeFromPath(newMainNode, path);
                }
                catch { } // if error on add of file, don't add file 
                
            }
            newMainNode.Save();
            Workspace.CloseFile(projectpath);
            Errorhandler.RaiseMessage($"The project file {projectpath} was damaged. An attempt has been made to restore the file.", "Damaged File", Errorhandler.MessageType.Info);

            return new AssemblyNode(projectpath);
        }
        public override string AbsolutePath
        {
            get
            {
                try
                {
                    _AbsolutePath = AssemblyModel.AbsolutePath;
                    if (BrokenReference)
                    {
                        if (File.Exists(_AbsolutePath))
                            BrokenReference = false;
                    }
                    return _AbsolutePath;
                }
                catch (FileNotFoundException)
                {
                    BrokenReference = true;
                    return "";
                }
            }
            set
            {
                if (parent == null)
                {
                    AssemblyModel.Path = value;
                }
            }
        }
    }
    public class ColorNodeVM : NodeVM
    {
        public override string RelativePathFromParent { get => parent.AssemblyModel.obj.colorPath; set => throw new NotImplementedException(); }

        public ColorNodeVM(AssemblyNodeVM assembly)
        {
            parent = assembly;
        }
        public override void Open()
        {
            TabItem tabItem = NodeVM.getTab(this);
            if (tabItem != null && tabItem.Content is ColorListControlVM c) 
                c.DominoAssembly = parent.AssemblyModel;
            NodeVM.openTab(tabItem ?? new TabItem(this));
        }
        public override string AbsolutePath {
            get => Workspace.AbsolutePathFromReferenceLoseUpdate(RelativePathFromParent, parent.AssemblyModel.obj);
            set => throw new NotImplementedException(); }
    }
    public class DocumentNodeVM : DominoWrapperNodeVM
    {
        public override string RelativePathFromParent
        {
            get => DocumentModel.relativePath;
            set
            {
                DocumentModel.relativePath = value;
                _AbsolutePath = null;
            }
        }
        public DocumentNode DocumentModel
        {
            get => Model as DocumentNode;
            set
            {
                Model = value;
                RaisePropertyChanged();
            }
        }
        public override string AbsolutePath
        {
            get
            {
                try
                {
                    _AbsolutePath = DocumentModel.AbsolutePath;
                    BrokenReference = false;
                    return _AbsolutePath;
                }
                catch (FileNotFoundException)
                {
                    BrokenReference = true;
                    return "";
                }
            }
            set { }
        }
        public DocumentNodeVM(DocumentNode dn)
        {
            DocumentModel = dn;
        }
        public bool HasFieldProtocol()
        {
            try
            {
                var result = Workspace.LoadHasProtocolDefinition<IWorkspaceLoadColorList>(AbsolutePath);
                BrokenReference = false;
                return result;
            }
            catch (FileNotFoundException)
            {
                BrokenReference = true;
                return false;
            }
        }
        [ContextMenuAttribute("Export as Image", "Icons/image.ico", index: 4 )]
        public void ExportImage()
        {
            ExportImage(false);
        }
        [ContextMenuAttribute("Custom Image Export", "Icons/image.ico", index: 5)]
        public void ExportImageCustom()
        {
            ExportImage(true);
        }
        public async void ExportImage(bool userDefinedExport)
        {
            try
            {
                int width = 2000;
                bool collapsed = false;
                bool drawBorders = false;
                Color background = Colors.Transparent;
                if (userDefinedExport)
                {
                    ExportOptions exp = new ExportOptions(DocumentModel.obj);
                    if (await exp.ShowDialog<bool>(MainWindowViewModel.GetWindow()))
                    {
                        var dc = exp.DataContext as ExportOptionsVM;
                        width = dc.ImageSize;
                        collapsed = dc.Collapsed;
                        drawBorders = dc.DrawBorders;
                        background = dc.BackgroundColor;
                    }
                    else
                    {
                        return;
                    }
                }
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                
                saveFileDialog.Filters.Add(new FileDialogFilter() { Extensions = new List<string> { "png" }, Name = "PNG files" });
                //saveFileDialog.RestoreDirectory = true;
                var result = await saveFileDialog.ShowAsync(MainWindowViewModel.GetWindow());
                if (result != "")
                {
                    if (File.Exists(result))
                    {
                        File.Delete(result);
                    }
                    DocumentModel.obj.Generate(new System.Threading.CancellationToken()).GenerateImage(background, width, drawBorders, collapsed).Save(result);

                }
            }
            catch (Exception ex) { Errorhandler.RaiseMessage("Export failed" + ex, "Error", Errorhandler.MessageType.Error); }
        }
        [ContextMenuAttribute("Show protocol", "Icons/file_export.ico", "HasFieldProtocol", true, 6)]
        public void ShowProtocol()
        {
            if (!DocumentModel.obj.HasProtocolDefinition)
            {
                Errorhandler.RaiseMessage("Could not generate a protocol. This structure type has no protocol definition.", "No Protocol", Errorhandler.MessageType.Warning);
                return;
            }
            ProtocolV protocolV = new ProtocolV();
            DocumentModel.obj.Generate(new System.Threading.CancellationToken());
            protocolV.DataContext = new ProtocolVM(DocumentModel.obj, Path.GetFileNameWithoutExtension(DocumentModel.relativePath));
            protocolV.Show();
        }
        [ContextMenuAttribute("Open", "Icons/folder_tar.ico", Index = 0)]
        public override void Open()
        {
            try
            {
                openTab(getTab(this) ?? new TabItem(this));
            }
            catch (FileNotFoundException)
            {
                BrokenReference = true;
            }
            catch (InvalidDataException)
            {
                RemoveNodeFromProject();
                Errorhandler.RaiseMessage($"The file {this.Name} is broken. " +
                    $"It has been removed from the project.", "File not readable", Errorhandler.MessageType.Error);
            }
            
        }
        [ContextMenuAttribute("Remove from project", "Icons/remove.ico", Index = 7)]
        public async void RemoveNodeFromProject()
        {
            try
            {
                var msgbox = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow("Delete?", $"Remove reference to file {Name} from project {parent.Name}?\n" +
                    $"The file won't be permanently deleted.", MessageBox.Avalonia.Enums.ButtonEnum.YesNo, MessageBox.Avalonia.Enums.Icon.Warning);
                if (await closeTab(this) && await msgbox.ShowDialog(MainWindowViewModel.GetWindow())  == MessageBox.Avalonia.Enums.ButtonResult.Yes)
                {
                    parent.RemoveChild(this);
                    Errorhandler.RaiseMessage($"{Name} has been removed!", "Removed", Errorhandler.MessageType.Error);
                }
            }
            catch (Exception)
            {
                Errorhandler.RaiseMessage("Could not remove the object!", "Error", Errorhandler.MessageType.Error);
            }
            
        }
        [ContextMenuAttribute("Rename", "Icons/draw_freehand.ico", index: 3)]
        public async void Rename()
        {
            try
            {
                RenameObject ro = new RenameObject(Path.GetFileName(AbsolutePath));
                if (await closeTab(this) && await ro.ShowDialog<bool>(MainWindowViewModel.GetWindow()))
                {
                    Workspace.CloseFile(DocumentModel.AbsolutePath);
                    string old_path = AbsolutePath;
                    File.Move(old_path, Path.Combine(Path.GetDirectoryName(old_path), ((RenameObjectVM)ro.DataContext).NewName));
                    RelativePathFromParent = Path.Combine(Path.GetDirectoryName(RelativePathFromParent), 
                        ((RenameObjectVM)ro.DataContext).NewName);
                    parent.AssemblyModel.Save();
                    Open();
                }
            }
            catch
            {
                Errorhandler.RaiseMessage("Renaming object failed!", "Error", Errorhandler.MessageType.Error);
            }
        }
        [ContextMenuAttribute("Properties", "Icons/properties.ico", index: 20)]
        public async void ShowProperties()
        {
            PropertiesWindow pw = new PropertiesWindow(Model);
            await pw.ShowDialog(MainWindowViewModel.GetWindow());
        }
    }
}
