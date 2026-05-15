using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace Plantify.ViewModels
{
    public partial class DayTasksViewModel : BaseViewModel
    {
        [ObservableProperty] 
        private string _dayName = string.Empty;
        
        [ObservableProperty] 
        private string _date = string.Empty;
        
        [ObservableProperty]
        private ObservableCollection<CareTaskViewModel> _tasks = new();
    }
}
