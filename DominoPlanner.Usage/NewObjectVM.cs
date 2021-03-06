﻿using DominoPlanner.Core;
using DominoPlanner.Usage.Serializer;
using DominoPlanner.Usage.UserControls.ViewModel;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using System.Windows.Input;
using Avalonia.Media;
using System.Collections.Generic;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;
using System.Threading.Tasks;


namespace DominoPlanner.Usage
{
    using static Localizer;
    public class NewObjectVM : ModelBase
    {
        #region CTOR
        public NewObjectVM(string folderpath, DominoAssembly parentProject)
        {

            this.parentProject = parentProject;
            FillObjects();
            ProjectPath = folderpath;
            CreateIt = new RelayCommand(o => { MCreateIt(); });
            SelectedType = 0;
        }
        #endregion

        #region fields
        private readonly DominoAssembly parentProject;
        #endregion

        #region prop
        public IDominoWrapper ResultNode { get; private set; }
        public string ProjectPath { get; private set; }
        public string ObjectPath { get { return string.Format("{0}\\Planner Files\\{1}{2}", ProjectPath, _filename, _endung); } }

        private ObservableCollection<NewObjectEntry> _ViewModels;
        public ObservableCollection<NewObjectEntry> ViewModels
        {
            get => _ViewModels;
            set
            {
                if (_ViewModels != value)
                {
                    _ViewModels = value;
                    RaisePropertyChanged();
                }
            }
        }
        private object _CurrentViewModel;
        public object CurrentViewModel
        {
            get => _CurrentViewModel;
            set
            {
                if (_CurrentViewModel != value)
                {
                    _CurrentViewModel = value;
                    // if we change object type, but never change anything on the right side, the target size won't update.
                    if (CurrentImageInformation is SingleImageInformation si && CurrentViewModel is DominoProviderVM c)
                        si.UpdateProvider(c);
                    RaisePropertyChanged();
                }
            }
        }
        private ImageInformation _CurrentImageInformation;
        public ImageInformation CurrentImageInformation
        {
            get => _CurrentImageInformation;
            set
            {
                if (_CurrentImageInformation != value)
                {
                    _CurrentImageInformation = value;
                    RaisePropertyChanged();
                }
            }
        }
        private int _selectedType = -1;
        public int SelectedType
        {
            get { return _selectedType; }
            set
            {
                if (_selectedType != value)
                {
                    _selectedType = value;
                    Extension = ViewModels[value].Extension;
                    CurrentViewModel = ViewModels[value].ViewModel;
                    CurrentImageInformation = ViewModels[value].ImageInformation;
                    RaisePropertyChanged();
                }
            }
        }
        private string _filename;
        public string Filename
        {
            get { return _filename; }
            set
            {
                if (_filename != value)
                {
                    _filename = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string _endung = "." + Declares.ObjectExtension;
        public string Extension
        {
            get { return _endung; }
            set
            {
                if (_endung != value)
                {
                    _endung = value;
                    RaisePropertyChanged();
                }
            }
        }
        private bool _Close;
        public bool Close
        {
            get { return _Close; }
            set
            {
                if (_Close != value)
                {
                    _Close = value;
                    CloseChanged(this, EventArgs.Empty);
                    RaisePropertyChanged();
                }
            }
        }

        public event EventHandler CloseChanged;

        #endregion

        #region command

        private ICommand _CreateIt;
        public ICommand CreateIt { get { return _CreateIt; } set { if (value != _CreateIt) { _CreateIt = value; } } }


        #endregion
        #region Methods

        private void FillObjects()
        {
            ViewModels = new ObservableCollection<NewObjectEntry>();
            CurrentImageInformation = new SingleImageInformation(this);
            string AbsoluteColorPath = Workspace.AbsolutePathFromReferenceLoseUpdate(parentProject.ColorPath, parentProject);
            ViewModels.Add(new DominoProviderObjectEntry()
            {
                Name = GetParticularString("The domino structure type", "Field"),
                Description = _("Add a field based on an image"),
                Icon = "/Icons/insert_table.ico",
                ImageInformation = CurrentImageInformation,
                Provider = new CreateFieldVM(
                    new FieldParameters(50, 50, Colors.Transparent, AbsoluteColorPath,
                    8, 8, 24, 8, 5000, SkiaSharp.SKFilterQuality.High, new CieDe2000Comparison(), new Dithering(), new NoColorRestriction()), null)
                { BindSize = true }
            });
            ViewModels.Add(new DominoProviderObjectEntry()
            {
                Name = GetParticularString("The domino structure type", "Structure"),
                Description = _("Add a structure (e.g. Wall) based on an image"),
                Icon = "/icons/insert_table.ico",
                ImageInformation = CurrentImageInformation,
                Provider = new CreateRectangularStructureVM(
                    new StructureParameters(5, 5, Colors.Transparent, CreateRectangularStructureVM.StuctureTypes().Item1[0],
                    2000, AbsoluteColorPath, new CieDe2000Comparison(), new Dithering(), AverageMode.Corner, new NoColorRestriction()), null)
            });
            ViewModels.Add(new DominoProviderObjectEntry()
            {
                Name = GetParticularString("The domino structure type", "Circle"),
                Description = _("Add a circle bomb based on an image"),
                Icon = "/Icons/insert_table.ico",
                ImageInformation = CurrentImageInformation,
                Provider = new CreateCircleVM(
                    new CircleParameters(5, 5, Colors.Transparent, 10,
                    AbsoluteColorPath, new CieDe2000Comparison(), new Dithering(), AverageMode.Corner, new NoColorRestriction()), null)
            });
            ViewModels.Add(new DominoProviderObjectEntry()
            {
                Name = GetParticularString("The domino structure type", "Spiral"),
                Description = _("Add a spiral based on an image"),
                Icon = "/Icons/insert_table.ico",
                ImageInformation = CurrentImageInformation,
                Provider = new CreateSpiralVM(
                    new SpiralParameters(5, 5, Colors.Transparent, 10,
                    AbsoluteColorPath, new CieDe2000Comparison(), new Dithering(), AverageMode.Corner, new NoColorRestriction()), false)
            });
            ViewModels.Add(new NewAssemblyEntry(parentProject)
            {
                Name = _("Subproject"),
                Description = _("Add a subproject with the same color list"),
                Icon = "/Icons/folder_txt.ico",
                ImageInformation = new NoImageInformation()
            });
        }
        private async void MCreateIt()
        {
            ResultNode = await ViewModels[SelectedType].CreateIt(parentProject, Filename, ProjectPath);
            if (ResultNode != null)
            {
                Close = true;
            }
        }
        #endregion
    }
    public class ImageInformation : ModelBase
    {
        public virtual void UpdateProvider(DominoProviderVM provider) { }
#pragma warning disable CS1998 // Bei der asynchronen Methode fehlen "await"-Operatoren. Die Methode wird synchron ausgeführt.
        public virtual async Task<bool> FinalizeProvider(IDominoProvider provider, string filename)
#pragma warning restore CS1998 
        { return true; }
        public virtual void RevertChangesToFileSystem() { }
    }
    public class NoImageInformation : ImageInformation
    {

    }
    public class SingleImageInformation : ImageInformation, IFileDragDropTarget
    {
        NewObjectVM parent;
        public SingleImageInformation(NewObjectVM parent)
        {
            this.parent = parent;
            LoadNewImage = new RelayCommand((o) => OpenNewImage() );
        }
        private string DefaultPictureName { get; set; } = "/Icons/add.ico";
        private string internPictureName = "";

        public DominoProviderVM CurrentProvider
        {
            get 
            { 
                if (parent.CurrentViewModel is DominoProviderVM vM) 
                    return vM;
                else
                    return null;
            }
        }
        
        public string InternPictureName
        {
            get { return internPictureName; }
            set
            {
                internPictureName = value;
                RaisePropertyChanged();
            }
        }
        private string finalImagePath;

        #region Command
        private ICommand _LoadNewImage;
        public ICommand LoadNewImage { get { return _LoadNewImage; } set { if (value != _LoadNewImage) { _LoadNewImage = value; } } }
        #endregion

        #region Methods
        private async void SetNewImage(string filename)
        {
            string oldfilename = InternPictureName;
            if (CurrentProvider == null) return;
            if (File.Exists(filename))
            {
                InternPictureName = filename;
            }
            try
            {
                UpdateProvider(CurrentProvider);
            }
            catch
            {
                await Errorhandler.RaiseMessageWithParent<NewObject>(_("The image file is not readable, please select another file"), _("Invalid file"), Errorhandler.MessageType.Error);
                InternPictureName = oldfilename;
            }
        }
        private async void OpenNewImage()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filters.Add(new FileDialogFilter() { Extensions = new List<string> { "jpg", "jpeg", "jpe", "jfif", "png" }, Name = _("Image files") });
            openFileDialog.Filters.Add(new FileDialogFilter() { Extensions = new List<string> { "*" }, Name = _("All Files") });
            openFileDialog.AllowMultiple = false;
            var result = await openFileDialog.ShowAsyncWithParent<NewObject>();
            if (result != null && result.Length != 0)
                SetNewImage(result[0]);
        }
        public override void UpdateProvider(DominoProviderVM provider)
        {
            if (!string.IsNullOrEmpty(InternPictureName))
            {
                provider.CurrentProject.PrimaryImageTreatment = new NormalReadout(provider.CurrentProject, InternPictureName, AverageMode.Corner, true);
                if (provider.DominoCount > 0)
                {
                    provider.DominoCount += 1; // if equal, setter would reject changes
                }
            }

        }
        public override async Task<bool> FinalizeProvider(IDominoProvider provider, string savepath)
        {
            finalImagePath = InternPictureName;

            if (string.IsNullOrEmpty(finalImagePath) || string.IsNullOrWhiteSpace(finalImagePath))
            {
                await Errorhandler.RaiseMessageWithParent<NewObject>(_("Please choose an image"), _("Missing Values"), Errorhandler.MessageType.Error);
                InternPictureName = finalImagePath;
                return false;
            }

            finalImagePath = string.Format("{0}{1}", Path.GetFileNameWithoutExtension(savepath), Path.GetExtension(finalImagePath));
            int counter = 1;
            while (File.Exists(Path.Combine(Path.GetDirectoryName(savepath), @"../Source Image", finalImagePath)))
            {
                finalImagePath = $"{Path.GetFileName(savepath)} ({counter}){Path.GetExtension(finalImagePath)}";
                counter++;
            }
            try
            {
                File.Copy(InternPictureName, Path.Combine(Path.GetDirectoryName(savepath), @"../Source Image", finalImagePath));
            }
            catch (IOException)
            {
                await Errorhandler.RaiseMessageWithParent<NewObject>(_("Copying the image into the project folder failed.\nPlease check the permissions to this file."), "", Errorhandler.MessageType.Warning);
                InternPictureName = finalImagePath;
                return false;
            }
            string relPicturePath = $@"..\Source Image\{finalImagePath}";
            if (provider is FieldParameters f)
            {
                provider.PrimaryImageTreatment = new FieldReadout(f, relPicturePath, SkiaSharp.SKFilterQuality.High);
            }
            else
            {
                provider.PrimaryImageTreatment = new NormalReadout(provider, relPicturePath, AverageMode.Corner, true);
            }
            return true;
        }
        public override void RevertChangesToFileSystem()
        {
            if (File.Exists(finalImagePath)) File.Delete(finalImagePath);
        }

        public void OnFileDrop(string[] filepaths)
        {
            if (filepaths != null && filepaths.Length != 0)
                SetNewImage(filepaths[0]);
        }
        #endregion
    }

    public abstract class NewObjectEntry
    {
        internal DominoAssembly parentProject;
        public ImageInformation ImageInformation { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public abstract string Extension { get; }
        public abstract Task<IDominoWrapper> CreateIt(DominoAssembly parentProject, string filename, string ProjectPath);
        public abstract object ViewModel { get; }
        public abstract void UpdateImageInformation();
    }
    public class DominoProviderObjectEntry : NewObjectEntry
    {
        public override void UpdateImageInformation()
        {
            ImageInformation.UpdateProvider(Provider);
        }
        public override object ViewModel => Provider;
        public DominoProviderVM Provider { get; set; }

        public override string Extension => "." + Declares.ObjectExtension;

        public async Task<bool> Finalize(string filepath, DominoAssembly parentProject)
        {
            Provider.CurrentProject.Save(filepath);
            string colorlist = parentProject.ColorPath;
            Provider.CurrentProject.ColorPath = $@"..\{colorlist}";
            return await ImageInformation.FinalizeProvider(Provider.CurrentProject, filepath);
        }
        public override async Task<IDominoWrapper> CreateIt(DominoAssembly parentProject, string filename, string ProjectPath)
        {
            try
            {
                string resultPath = Path.Combine(ProjectPath, Path.Combine("Planner Files", string.Format("{0}{1}", filename, Extension)));

                if (File.Exists(resultPath))
                {
                    await Errorhandler.RaiseMessageWithParent<NewObject>(_("This name is already in use in this project.\n Please choose a different Name."), _("Error!"), Errorhandler.MessageType.Error);
                    return null;
                }
                if (string.IsNullOrEmpty(filename) || string.IsNullOrWhiteSpace(filename))
                {
                    await Errorhandler.RaiseMessageWithParent<NewObject>(_("You forgot to choose a name."), _("Missing Values"), Errorhandler.MessageType.Error);
                    return null;
                }
                try
                {
                    bool finalizeResult = await Finalize(resultPath, parentProject);

                    if (!finalizeResult)
                    {
                        throw new OperationCanceledException(_("Finalize failed"));
                    }
                }
                catch (Exception ex)
                {
                    if (File.Exists(resultPath)) File.Delete(resultPath);
                    Workspace.CloseFile(Provider.CurrentProject);
                    ImageInformation.RevertChangesToFileSystem();
                    if (!(ex is OperationCanceledException))
                        await Errorhandler.RaiseMessageWithParent<NewObject>(string.Format(_("Project creation failed. Error mesage: \n{0}\n The created files have been deleted"), ex.Message), _("Error"), Errorhandler.MessageType.Error);
                    return null;
                }
                Provider.CurrentProject.Save();
                var ResultNode = (DocumentNode)IDominoWrapper.CreateNodeFromPath(parentProject, resultPath);
                parentProject.Save();
                return ResultNode;
            }
            catch (Exception es)
            {
                await Errorhandler.RaiseMessageWithParent<NewObject>(_("Could not create a new Project!") + "\n" + es + "\n" + es.InnerException + "\n" + es.StackTrace, _("Error"), Errorhandler.MessageType.Error);
                return null;
            }
        }
    }
    public class NewAssemblyEntry : NewObjectEntry
    {
        public override string Extension => "." + Declares.ProjectExtension;
        public override object ViewModel => this;
        public string ColorPath { get; set; }

        public NewAssemblyEntry(DominoAssembly parentProject)
        {
            this.parentProject = parentProject;
            ColorPath = Workspace.AbsolutePathFromReferenceLoseUpdate(parentProject.ColorPath, parentProject);
        }
        public override async Task<IDominoWrapper> CreateIt(DominoAssembly parentProject, string filename, string ProjectPath)
        {
            if (string.IsNullOrEmpty(filename) || string.IsNullOrWhiteSpace(filename))
            {
                await Errorhandler.RaiseMessageWithParent<NewObject>(_("You forgot to choose a name."), _("Missing Values"), Errorhandler.MessageType.Error);
                return null;
            }
            string newProject = Path.Combine(ProjectPath, "Planner Files", filename);
            string PlannerFilesPath = Path.Combine(newProject, "Planner Files");
            string SourceImagesPath = Path.Combine(newProject, "Source Image");
            string assemblyPath = Path.Combine(newProject, filename + Extension);
            try
            {

                if (Directory.Exists(ProjectPath))
                {
                    if (Directory.Exists(newProject))
                    {
                        await Errorhandler.RaiseMessageWithParent<NewObject>(_("A subassembly with this name already exists. Please choose a different name"), _("Error"), Errorhandler.MessageType.Error);
                        return null;
                    }
                    Directory.CreateDirectory(newProject);
                    Directory.CreateDirectory(PlannerFilesPath);
                    Directory.CreateDirectory(SourceImagesPath);

                    DominoAssembly dominoAssembly = new DominoAssembly() { };
                    dominoAssembly.Save(assemblyPath);
                    dominoAssembly.ColorPath = Workspace.MakeRelativePath(assemblyPath,
                        Workspace.AbsolutePathFromReferenceLoseUpdate(parentProject.ColorPath, parentProject));
                    dominoAssembly.Save();
                    AssemblyNode asn = new AssemblyNode(Workspace.MakeRelativePath(ProjectPath + "/", assemblyPath), parentProject);
                    parentProject.Save();
                    return asn;
                }
            }
            catch
            {
                if (Directory.Exists(newProject)) Directory.Delete(newProject, true);
                await Errorhandler.RaiseMessageWithParent<NewObject>(_("Project creation failed. The file system changes have been reverted"), _("Error"), Errorhandler.MessageType.Error);
            }
            return null;
        }

        public override void UpdateImageInformation()
        {

        }
    }
    public class ImageTemplateSelector : IDataTemplate
    {
        public bool SupportsRecycling => false;

        public DataTemplate EmptyImageTemplate { get; set; }
        public DataTemplate ImageTemplate { get; set; }

        public bool Match(object data)
        {
            return data is string;
        }

        public IControl Build(object param)
        {
            if (string.IsNullOrEmpty(param.ToString()))
                return EmptyImageTemplate.Build(param);
            else
                return ImageTemplate.Build(param);

            //return Templates[string.IsNullOrEmpty((param as SingleImageInformation).InternPictureName) ? "EmptyImageTemplate" : "ImageTemplate"].Build(param);
        }

    }
    public class EmptyImageTemplate : DataTemplate, IDataTemplate
    {
        bool IDataTemplate.Match(object data)
        {
            if (string.IsNullOrEmpty(data?.ToString()))
                return true;
            return false;
        }
    }
    public class ImageTemplate : DataTemplate, IDataTemplate
    {
        bool IDataTemplate.Match(object data)
        {
            if (string.IsNullOrEmpty(data?.ToString()))
                return false;
            return true;
        }
    }
    /*public class ImageSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (string.IsNullOrEmpty(item?.ToString()))
                return EmptyImageTemplate;
            return ImageTemplate;
        }
        public DataTemplate EmptyImageTemplate { get; set; }
        public DataTemplate ImageTemplate { get; set; }
    }*/
}