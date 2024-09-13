using StratoDomainDDNSChanger.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StratoDomainDDNSChanger.MVVM.ViewModel
{
    class MainViewModel : ObservableObject
    {
        public static MainViewModel Instance;
        public RelayCommand HomeViewCommand { get; set; }
        public RelayCommand ConfigViewCommand { get; set; }

        public ObservableObject HomeVm { get; set; }
        public ObservableObject ConfigVm { get; set; }
        protected object _currentView;

        public object CurrentView {  get { return _currentView; } set { _currentView = value; OnPropertyChanged(); } }

        public MainViewModel() {
            Instance = this;
            HomeVm = new HomeViewModel();
            ConfigVm = new ConfigViewModel();

            CurrentView = HomeVm;

            HomeViewCommand = new RelayCommand(x =>
            {
                CurrentView = HomeVm;
            });

            ConfigViewCommand = new RelayCommand(x =>
            {
                CurrentView = ConfigVm;
            });
        }  
    }
}
