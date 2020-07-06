using System.Collections.ObjectModel;

namespace AutoMerge
{
    public class RecentChangesetsViewModelContext
    {
        public ChangesetViewModel SelectedChangeset { get; set; }

        public ObservableCollection<ChangesetViewModel> Changesets { get; set; }

        public bool ShowAddBranchToMergeFrom { get; set; }

        public bool ShowAddByIdChangeset { get; set; }

        public bool ShowAddByTeamIdChangeset { get; set; }

        public string BranchNameText { get; set; }

        public string ChangesetIdsText { get; set; }

        public string ChangesetTeamIdsText { get; set; }

        public string Title { get; set; }
    }
}
