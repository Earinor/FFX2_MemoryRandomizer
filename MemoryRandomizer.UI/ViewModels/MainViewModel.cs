using MemoryRandomizer.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace MemoryRandomizer.UI
{
    public class MainViewModel : IRandomizerViewModel
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly GameManager gameManager;

        public string Errors { get; set; }
        public string Path { get; set; }

        public bool Randomize
        {
            get
            {
                return this.DresssphereViewModel.Randomize;// && this.GarmentGridViewModel.Randomize;
            }
            set
            {
                this.DresssphereViewModel.Randomize = value;
                //this.GarmentGridViewModel.Randomize = value;
            }
        }
        public bool LoadSave
        {
            get
            {
                return this.DresssphereViewModel.LoadSave;// && this.GarmentGridViewModel.LoadSave;
            }
            set
            {
                this.DresssphereViewModel.LoadSave = value;
                //this.GarmentGridViewModel.LoadSave = value;
            }
        }

        public DresssphereViewModel DresssphereViewModel { get; set; }
        public GarmentGridViewModel GarmentGridViewModel { get; set; }

        public ICommand AttachCommand { get; private set; }
        public ICommand DeleteCommand { get; private set; }

        public MainViewModel()
        {
            this.OpenPathDialog();

            this.gameManager = new GameManager();
            this.DresssphereViewModel = new DresssphereViewModel();
            this.GarmentGridViewModel = new GarmentGridViewModel();

            this.DresssphereViewModel.PropertyChanged += (o, e) => { this.PropertyChanged?.Invoke(this, e); };
            this.GarmentGridViewModel.PropertyChanged += (o, e) => { this.PropertyChanged?.Invoke(this, e); };

            this.AttachCommand = new AsyncDelegateCommand(this.Attach, _ => true);
            this.DeleteCommand = new AsyncDelegateCommand(this.DeleteSave, _ => true);
        }

        #region Command Implementations
        public async Task Attach(object _)
        {
            try
            {
                await Task.Run(() => this.gameManager.Startup(
                    this.DresssphereViewModel.Randomize,
                    false, //this.GarmentGridViewModel.Randomize,
                    false,
                    this.DresssphereViewModel.ChaosMode,
                    this.DresssphereViewModel.LoadSave,
                    false, //this.GarmentGridViewModel.LoadSave,
                    false
                ));
            }
            catch (Exception exc)
            {
                this.Errors = exc.Message;
            }
        }

        public async Task DeleteSave(object _)
        {
            if (this.OpenDeletionDialog())
            {
                await this.DresssphereViewModel.DeleteSave(_);
                await this.GarmentGridViewModel.DeleteSave(_);
            }
        }
        #endregion

        protected bool OpenDeletionDialog()
        {
            MessageBoxResult result = MessageBox.Show("Are you sure?", "Delete save files", MessageBoxButton.YesNo);
            return result == MessageBoxResult.Yes;
        }

        protected void OpenPathDialog()
        {
            using (System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "Select the path to your FFX-2 installation directory.";
                dialog.UseDescriptionForTitle = true;
                dialog.RootFolder = Environment.SpecialFolder.ProgramFilesX86;

                System.Windows.Forms.DialogResult result = dialog.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    this.Path = dialog.SelectedPath;
                }
                else
                {
                    Environment.Exit(0);
                }
            }
        }
    }
}
