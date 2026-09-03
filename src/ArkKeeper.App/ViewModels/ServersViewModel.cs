using System.Collections.ObjectModel;
using ArkKeeper.Core.Profiles;

namespace ArkKeeper.App.ViewModels;

public sealed class ServersViewModel : ViewModelBase
{
    public ServersViewModel(ObservableCollection<ServerProfile> profiles)
    {
        Profiles = profiles;
    }

    public ObservableCollection<ServerProfile> Profiles { get; }
}
