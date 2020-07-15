using AutoMerge.Prism.Events;
using System.Collections.ObjectModel;

namespace AutoMerge.Events
{
    class AllChangesetsSelected : PubSubEvent<ObservableCollection<ChangesetViewModel>>
    {

    }
}
