using MemoryRandomizer.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MemoryRandomizer.UI
{
    public class DresssphereViewModel : NotifyPropertyChanged, IRandomizerViewModel
    {
        private string errors;
        private bool randomize;
        private bool chaosMode;
        private bool loadSave;

        public string Errors { get => errors; private set => this.Set(ref errors, value); }
        public bool Randomize { get => randomize; set => this.Set(ref randomize, value); }
        public bool ChaosMode { get => chaosMode; set => this.Set(ref chaosMode, value); }
        public bool LoadSave { get => loadSave; set => this.Set(ref loadSave, value); }

        public ICommand DeleteCommand { get; private set; }

        public MainViewModel Parent { get; set; }

        public DresssphereViewModel()
        {
            this.DeleteCommand = new AsyncDelegateCommand(this.DeleteSave, _ => true);
        }

        public Task DeleteSave(object _)
        {
            File.Delete(this.Parent.Path + SaveManager.DresssphereSaveFileName);
            return Task.CompletedTask;
        }
    }
}
