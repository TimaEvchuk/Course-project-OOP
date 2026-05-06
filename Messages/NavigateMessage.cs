using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using Plantify.ViewModels;

namespace Plantify.Messages
{
    /// <summary>
    /// A message that signals a request to navigate to a different view model.
    /// </summary>
    public class NavigateMessage : ValueChangedMessage<Type>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NavigateMessage"/> class.
        /// </summary>
        /// <param name="viewModelType">The type of the view model to navigate to. Must inherit from <see cref="BaseViewModel"/>.</param>
        public NavigateMessage(Type viewModelType) : base(viewModelType)
        {
        }
    }
}
