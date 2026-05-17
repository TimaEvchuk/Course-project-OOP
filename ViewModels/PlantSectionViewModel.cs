using CommunityToolkit.Mvvm.ComponentModel;

namespace Plantify.ViewModels
{
    public partial class PlantSectionViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private string _content = string.Empty;
    }
}
