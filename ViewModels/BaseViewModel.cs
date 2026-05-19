using CommunityToolkit.Mvvm.ComponentModel;

namespace Plantify.ViewModels
{
    public abstract partial class BaseViewModel : ObservableValidator, ITitledViewModel
    {
        public virtual string Title => string.Empty;
    }
}
