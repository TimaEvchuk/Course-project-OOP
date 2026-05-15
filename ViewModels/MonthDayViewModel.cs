using CommunityToolkit.Mvvm.ComponentModel;

namespace Plantify.ViewModels
{
    public partial class MonthDayViewModel : BaseViewModel
    {
        [ObservableProperty]
        private DateTime _fullDate;

        [ObservableProperty]
        private int _wateringTaskCount;

        [ObservableProperty]
        private int _fertilizingTaskCount;

        public bool IsOutOfMonth { get; set; }
        public bool HasTasks => WateringTaskCount > 0 || FertilizingTaskCount > 0;
    }
}
