using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using AutoMerge.Events;
using AutoMerge.Prism.Command;
using AutoMerge.Prism.Events;
using AutoMerge.RecentChangesets;
using Microsoft.TeamFoundation;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Controls;
using Microsoft.TeamFoundation.Controls.WPF.TeamExplorer;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Task = System.Threading.Tasks.Task;

using TeamExplorerSectionViewModelBase = AutoMerge.Base.TeamExplorerSectionViewModelBase;

namespace AutoMerge
{
    public sealed class RecentChangesetsViewModel : TeamExplorerSectionViewModelBase
    {
        private readonly string _baseTitle;
        private readonly IEventAggregator _eventAggregator;

        public RecentChangesetsViewModel(ILogger logger)
            : base(logger)
        {
            Title = Resources.RecentChangesetSectionName;
            IsVisible = true;
            IsExpanded = true;
            IsBusy = false;
            Changesets = new ObservableCollection<ChangesetViewModel>();
            _baseTitle = Title;

            _eventAggregator = EventAggregatorFactory.Get();
            _eventAggregator.GetEvent<MergeCompleteEvent>()
                .Subscribe(OnMergeComplete);


            ViewChangesetDetailsCommand = new DelegateCommand(ViewChangesetDetailsExecute, ViewChangesetDetailsCanExecute);

            //Branch to merge from
            ToggleAddBranchToMergeFromCommand = new DelegateCommand(ToggleAddBranchToMergeFromExecute, ToggleAddBranchToMergeFromCanExecute);
            ResetAddBranchToMergeFromCommand = new DelegateCommand(ResetAddBranchToMergeFromExecute);
            AddBranchToMergeFromCommand = new DelegateCommand(AddBranchToMergeFromExecute, AddBranchToMergeFromCanExecute);
            branchesToMergeFrom = new List<string>();

            //By Changeset ID
            ToggleAddByIdCommand = new DelegateCommand(ToggleAddByIdExecute, ToggleAddByIdCanExecute);
            CancelAddChangesetByIdCommand = new DelegateCommand(CancelAddByIdExecute);
            AddChangesetByIdCommand = new DelegateCommand(AddChangesetByIdExecute, AddChangesetByIdCanExecute);

            //By Team ID            
            ToggleAddByTeamIdCommand = new DelegateCommand(ToggleAddByTeamIdExecute, ToggleAddByTeamIdCanExecute);
            CancelAddChangesetByTeamIdCommand = new DelegateCommand(CancelAddByTeamIdExecute);
            AddChangesetByTeamIdCommand = new DelegateCommand(AddChangesetByTeamIdExecute, AddChangesetByTeamIdCanExecute);
        }

     

        public ChangesetViewModel SelectedChangeset
        {
            get
            {
                return _selectedChangeset;
            }
            set
            {
                _selectedChangeset = value;
                RaisePropertyChanged("SelectedChangeset");
                _eventAggregator.GetEvent<SelectChangesetEvent>().Publish(value);
            }
        }
        private ChangesetViewModel _selectedChangeset;

        public ObservableCollection<ChangesetViewModel> Changesets
        {
            get
            {
                return _changesets;
            }
            private set
            {
                _changesets = value;
                RaisePropertyChanged("Changesets");
            }
        }
        private ObservableCollection<ChangesetViewModel> _changesets;

        public bool ShowAddBranchToMergeFrom
        {
            get
            {
                return _showAddBranchToMergeFrom;
            }
            set
            {
                _showAddBranchToMergeFrom = value;
                RaisePropertyChanged("ShowAddBranchToMergeFrom");
            }
        }
        private bool _showAddBranchToMergeFrom;

        public bool ShowAddByIdChangeset
        {
            get
            {
                return _showAddByIdChangeset;
            }
            set
            {
                _showAddByIdChangeset = value;
                RaisePropertyChanged("ShowAddByIdChangeset");
            }
        }
        private bool _showAddByIdChangeset;

        public bool ShowAddByTeamIdChangeset
        {
            get
            {
                return _showAddByTeamIdChangeset;
            }
            set
            {
                _showAddByTeamIdChangeset = value;
                RaisePropertyChanged("ShowAddByTeamIdChangeset");
            }
        }
        private bool _showAddByTeamIdChangeset;

        public string BranchNameText
        {
            get
            {
                return _branchNameText;
            }
            set
            {
                _branchNameText = value;
                RaisePropertyChanged("BranchNameText");
                InvalidateCommands();
            }
        }
        private string _branchNameText;

        public string ChangesetIdsText
        {
            get
            {
                return _changesetIdsText;
            }
            set
            {
                _changesetIdsText = value;
                RaisePropertyChanged("ChangesetIdsText");
                InvalidateCommands();
            }
        }
        private string _changesetIdsText;

        public string ChangesetTeamIdsText
        {
            get
            {
                return _changesetTeamIdsText;
            }
            set
            {
                _changesetTeamIdsText = value;
                RaisePropertyChanged("ChangesetTeamIdsText");
                InvalidateCommands();
            }
        }
        private string _changesetTeamIdsText;

        public DelegateCommand ViewChangesetDetailsCommand { get; private set; }

        public DelegateCommand ToggleAddBranchToMergeFromCommand { get; private set; }

        public DelegateCommand ResetAddBranchToMergeFromCommand { get; private set; }

        public DelegateCommand AddBranchToMergeFromCommand { get; private set; }

        public DelegateCommand ToggleAddByIdCommand { get; private set; }

        public DelegateCommand ToggleAddByTeamIdCommand { get; private set; }

        public DelegateCommand AddChangesetByIdCommand { get; private set; }

        public DelegateCommand CancelAddChangesetByIdCommand { get; private set; }

        public DelegateCommand AddChangesetByTeamIdCommand { get; private set; } 

        public DelegateCommand CancelAddChangesetByTeamIdCommand { get; private set; } 

        private void ViewChangesetDetailsExecute()
        {
            var changesetId = SelectedChangeset.ChangesetId;
            TeamExplorerUtils.Instance.NavigateToPage(TeamExplorerPageIds.ChangesetDetails, ServiceProvider, changesetId);
        }

        private bool ViewChangesetDetailsCanExecute()
        {
            return SelectedChangeset != null;
        }

        private async void OnMergeComplete(bool obj)
        {
            await RefreshAsync();
        }

        protected override async Task InitializeAsync(object sender, SectionInitializeEventArgs e)
        {
            if (e.Context == null)
            {
                await RefreshAsync();
            }
            else
            {
                RestoreContext(e);
            }
        }

        protected override async Task RefreshAsync()
        {
            Changesets.Clear();

            var changesetProvider = new MyChangesetChangesetProvider(ServiceProvider, Settings.Instance.ChangesetCount);
            var userLogin = VersionControlNavigationHelper.GetAuthorizedUser(ServiceProvider);

            Logger.Info("Getting changesets ...");
            var changesets = await changesetProvider.GetChangesets(userLogin);
            Logger.Info("Getting changesets end");

            Changesets = new ObservableCollection<ChangesetViewModel>(changesets);
            UpdateTitle();

            if (Changesets.Count > 0)
            {
                if (SelectedChangeset == null || SelectedChangeset.ChangesetId != Changesets[0].ChangesetId)
                    SelectedChangeset = Changesets[0];
            }
        }

        private void UpdateTitle()
        {
            Title = Changesets.Count > 0
                ? string.Format("{0} ({1})", _baseTitle, Changesets.Count)
                : _baseTitle;
        }

        private void ToggleAddBranchToMergeFromExecute()
        {
            try
            {
                ShowAddBranchToMergeFrom = true;
                InvalidateCommands();
                ResetAddById();
                SetMvvmFocus(RecentChangesetFocusableControlNames.BranchNameTextBox);
            }
            catch (Exception ex)
            {
                ShowException(ex);
                throw;
            }
        }

        private void ToggleAddByIdExecute()
        {
            try
            {
                ShowAddByIdChangeset = true;
                InvalidateCommands();
                ResetAddById();
                SetMvvmFocus(RecentChangesetFocusableControlNames.ChangesetIdTextBox);
            }
            catch (Exception ex)
            {
                ShowException(ex);
                throw;
            }
        }

        private void ToggleAddByTeamIdExecute()
        {
            try
            {
                ShowAddByTeamIdChangeset = true;
                InvalidateCommands();
                ResetAddByTeamId();
                SetMvvmFocus(RecentChangesetFocusableControlNames.ChangesetTeamIdTextBox);
            }
            catch (Exception ex)
            {
                ShowException(ex);
                throw;
            }
        }

        private bool ToggleAddBranchToMergeFromCanExecute()
        {
            return !ShowAddBranchToMergeFrom;
        }

        private bool ToggleAddByIdCanExecute()
        {
            return !ShowAddByIdChangeset;
        }

        private bool ToggleAddByTeamIdCanExecute()
        {
            return !ShowAddByTeamIdChangeset;
        }

        private void ResetAddBranchToMergeFromExecute()
        {
            try
            {
                ShowAddBranchToMergeFrom = false;
                InvalidateCommands(); 
                SetMvvmFocus(RecentChangesetFocusableControlNames.BranchNameTextBox);
                ResetAddBranchToMergeFrom();
                branchesToMergeFrom.Clear();
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
        }

        private void CancelAddByIdExecute()
        {
            try
            {
                ShowAddByIdChangeset = false;
                InvalidateCommands();
                SetMvvmFocus(RecentChangesetFocusableControlNames.AddChangesetByIdLink);
                ResetAddById();
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
        }     

        private void CancelAddByTeamIdExecute()
        {
            try
            {
                ShowAddByTeamIdChangeset = false;
                InvalidateCommands();
                SetMvvmFocus(RecentChangesetFocusableControlNames.AddChangesetByTeamIdLink);
                ResetAddByTeamId();
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
        }

        private void ResetAddBranchToMergeFrom()
        {
            BranchNameText = string.Empty;
        }
        private void ResetAddById()
        {
            ChangesetIdsText = string.Empty;
        }

        private void ResetAddByTeamId()
        {
            ChangesetTeamIdsText = string.Empty;
        }

        private async void AddBranchToMergeFromExecute()
        {
            ShowBusy();
            try
            {
                var branchNames = GetBranchesToMergeFrom(BranchNameText);
                branchesToMergeFrom = branchNames;                    
                ShowAddBranchToMergeFrom = false;                
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
            HideBusy();
        }
        private List<string> branchesToMergeFrom;

        private async void AddChangesetByIdExecute()
        {
            ShowBusy();
            try
            {
                var changesetIds = GetChangesetIdsToAdd(ChangesetIdsText);
                if (changesetIds.Count > 0)
                {
                    var changesetProvider = new ChangesetByIdChangesetProvider(ServiceProvider, changesetIds);
                    var changesets = await changesetProvider.GetChangesets(null);

                    if (changesets.Count > 0)
                    {
                        foreach (ChangesetViewModel changeset in changesets)
                        {
                            Changesets.Add(changeset);
                        }         
                        SelectedChangeset = changesets[0];
                        SetMvvmFocus(RecentChangesetFocusableControlNames.ChangesetList);
                        UpdateTitle();
                    }
                    ShowAddByIdChangeset = false;
                }
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
            HideBusy();
        }

        private async void AddChangesetByTeamIdExecute()
        {
            ShowBusy();
            try
            {
                var TeamIds = GetTeamIdsFromText(ChangesetTeamIdsText);                
                
                if (TeamIds.Count > 0)
                {
                    var context = Context;
                    var tfs = context.TeamProjectCollection;
                    var versionControl = tfs.GetService<VersionControlServer>();
                    var artifactProvider = versionControl.ArtifactProvider;
                    var workItemStore = tfs.GetService<WorkItemStore>();
                    var changesetProvider = new ChangesetsByTeamIdProvider(ServiceProvider, TeamIds, workItemStore, artifactProvider);
                    var changesets = await changesetProvider.GetChangesets(null);

                    if (changesets.Count > 0)
                    {
                        CheckIfChangesetIsFromDesiredBranchAndAddToResults(changesets);
                        SelectedChangeset = Changesets[0];
                        SetMvvmFocus(RecentChangesetFocusableControlNames.ChangesetList);
                        UpdateTitle();
                    }
                    ShowAddByTeamIdChangeset = false;
                }
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
            HideBusy();
        }

        private void CheckIfChangesetIsFromDesiredBranchAndAddToResults(List<ChangesetViewModel> changesets)
        {
            bool restrictBranches = false;
            if (branchesToMergeFrom.Count > 0)
            {
                restrictBranches = true;
            }
            bool branchFound = false;
            foreach (ChangesetViewModel changeset in changesets)
            {
                branchFound = false;
                if (restrictBranches)
                {
                    foreach (string branch in changeset.Branches)
                    {
                        if (branchesToMergeFrom.Contains(branch))
                        {
                            branchFound = true;
                            break;
                        }
                    }
                }
                else branchFound = true;
                if (branchFound) Changesets.Add(changeset);
            }
        }


        //Takes Team Ids from the Frontend TextBox
        private static List<int> GetTeamIdsFromText(string text)
        {
            var list = new List<int>();
            var teamIdsStrArray = string.IsNullOrEmpty(text) ? new string[0] : text.Split(new[] { ',', ';' });
            if (teamIdsStrArray.Length > 0)
            {
                foreach (var idStr in teamIdsStrArray)
                {
                    int result;
                    if (int.TryParse(idStr.Trim(), out result) && result > 0)
                        list.Add(result);
                }
            }
            return list;
        }

        private bool AddChangesetByIdCanExecute()
        {
            try
            {
                return GetChangesetIdsToAdd(ChangesetIdsText).Count > 0;
            }
            catch (Exception ex)
            {
                ShowException(ex);
                TeamFoundationTrace.TraceException(ex);
            }
            return false;
        }

        private bool AddBranchToMergeFromCanExecute()
        {
            try
            {
                return GetBranchesToMergeFrom(BranchNameText).Count > 0;
            }
            catch (Exception ex)
            {
                ShowException(ex);
                TeamFoundationTrace.TraceException(ex);
            }
            return false;
        }

        private bool AddChangesetByTeamIdCanExecute()
        {
            try
            {
                return GetChangesetTeamIdsToAdd(ChangesetTeamIdsText).Count > 0;
            }
            catch (Exception ex)
            {
                ShowException(ex);
                TeamFoundationTrace.TraceException(ex);
            }
            return false;
        }

        private static List<string> GetBranchesToMergeFrom(string text)
        {
            var branchNamesArray = string.IsNullOrEmpty(text) ? new string[0] : text.Split(new[] { ',', ';' });
            var resultList = new List<string>(branchNamesArray);
            return resultList;
        }

        private static List<int> GetChangesetIdsToAdd(string text)
        {
            var list = new List<int>();
            var idsStrArray = string.IsNullOrEmpty(text) ? new string[0] : text.Split(new[] { ',', ';' });
            if (idsStrArray.Length > 0)
            {
                ; foreach (var idStr in idsStrArray)
                {
                    int result;
                    if (int.TryParse(idStr.Trim(), out result) && result > 0)
                        list.Add(result);
                }
            }
            return list;
        }

        
        private static List<int> GetChangesetTeamIdsToAdd(string text) 
        {
            var list = new List<int>();
            var idsStrArray = string.IsNullOrEmpty(text) ? new string[0] : text.Split(new[] { ',', ';' });
            if (idsStrArray.Length > 0)
            {
                foreach (var idStr in idsStrArray)
                {
                    int result;
                    if (int.TryParse(idStr.Trim(), out result) && result > 0)
                        list.Add(result);
                }
            }
            return list;
        }

        private void InvalidateCommands()
        {
            ViewChangesetDetailsCommand.RaiseCanExecuteChanged();
            ToggleAddBranchToMergeFromCommand.RaiseCanExecuteChanged();
            ResetAddBranchToMergeFromCommand.RaiseCanExecuteChanged();
            AddBranchToMergeFromCommand.RaiseCanExecuteChanged();
            ToggleAddByIdCommand.RaiseCanExecuteChanged();
            CancelAddChangesetByIdCommand.RaiseCanExecuteChanged();
            AddChangesetByIdCommand.RaiseCanExecuteChanged();
            ToggleAddByTeamIdCommand.RaiseCanExecuteChanged();
            CancelAddChangesetByTeamIdCommand.RaiseCanExecuteChanged();
            AddChangesetByTeamIdCommand.RaiseCanExecuteChanged();
        }

        public override void Dispose()
        {
            base.Dispose();
            _eventAggregator.GetEvent<MergeCompleteEvent>().Unsubscribe(OnMergeComplete);
        }

        public override void SaveContext(object sender, SectionSaveContextEventArgs e)
        {
            base.SaveContext(sender, e);
            var context = new RecentChangesetsViewModelContext
            {
                BranchNameText = BranchNameText,
                ChangesetIdsText = ChangesetIdsText,
                ChangesetTeamIdsText = ChangesetTeamIdsText,
                Changesets = Changesets,
                SelectedChangeset = SelectedChangeset,
                ShowAddBranchToMergeFrom = ShowAddBranchToMergeFrom,
                ShowAddByIdChangeset = ShowAddByIdChangeset,
                ShowAddByTeamIdChangeset = ShowAddByTeamIdChangeset,
                Title = Title
            };

            e.Context = context;
        }

        private void RestoreContext(SectionInitializeEventArgs e)
        {
            var context = (RecentChangesetsViewModelContext)e.Context;
            BranchNameText = context.BranchNameText;
            ChangesetIdsText = context.ChangesetIdsText;
            ChangesetTeamIdsText = context.ChangesetTeamIdsText;
            Changesets = context.Changesets;
            SelectedChangeset = context.SelectedChangeset;
            ShowAddBranchToMergeFrom = context.ShowAddBranchToMergeFrom;
            ShowAddByIdChangeset = context.ShowAddByIdChangeset;
            ShowAddByTeamIdChangeset = context.ShowAddByTeamIdChangeset;
            Title = context.Title;
        }

        protected override void OnContextChanged(object sender, ContextChangedEventArgs e)
        {
            Refresh();
        }
    }
}
