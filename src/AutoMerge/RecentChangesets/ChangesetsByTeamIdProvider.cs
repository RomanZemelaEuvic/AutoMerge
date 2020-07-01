using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoMerge.RecentChangesets
{
    public class ChangesetsByTeamIdProvider : ChangesetProviderBase
    {
        private readonly IEnumerable<int> _teamIds;
        WorkItemStore _workItemStore;
        VersionControlArtifactProvider _artifactProvider;


        public ChangesetsByTeamIdProvider(IServiceProvider serviceProvider, IEnumerable<int> teamIds, WorkItemStore workItemStore, VersionControlArtifactProvider artifactProvider)
            : base(serviceProvider)
        {
            if (teamIds == null)
                throw new ArgumentNullException("teamIds");
            _workItemStore = workItemStore;
            _teamIds = teamIds;
            _artifactProvider = artifactProvider;
        }

        protected override List<ChangesetViewModel> GetChangesetsInternal(string userLogin)
        {
            var changesetService = GetChangesetService();
            var changesets = new List<ChangesetViewModel>();
            foreach (int id in _teamIds)
            {                
                var workItem = _workItemStore.GetWorkItem(id); //Here we take the workItem from the tfs database
                foreach (var changeset in
                    workItem.Links
                            .OfType<ExternalLink>()
                            .Select(link => _artifactProvider
                            .GetChangeset(new Uri(link.LinkedArtifactUri))))
                {
                    //Here do smth with the changesets
                    changesets.Add(ToChangesetViewModel(changeset, changesetService)); //This changes the tfschangeset into a viewmodel

                    

                }
            }     
           


            //if (changesetService != null)
            //{
            //    changesets = _teamIds
            //        .Select(changesetService.GetChangeset)
            //        .Where(c => c != null)
            //        .Select(tfsChangeset => ToChangesetViewModel(tfsChangeset, changesetService))
            //        .ToList();
            //}

            return changesets;
        }
    }
}

