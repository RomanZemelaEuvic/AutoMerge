using System.Windows.Controls;
using Microsoft.TeamFoundation.Controls.MVVM;

namespace AutoMerge
{
	/// <summary>
	/// Interaction logic for RecentChangesetsView.xaml
	/// </summary>
	public partial class RecentChangesetsView : UserControl, IFocusService
	{
		public RecentChangesetsView()
		{
			InitializeComponent();
		}

		public void SetFocus(string id, params object[] args)
		{
			switch (id)
			{
                case RecentChangesetFocusableControlNames.AddBranchToMergeFrom:
                    addBranchToMergeFrom.Focus();
                    break;
                case RecentChangesetFocusableControlNames.AddChangesetByIdLink:
					addChangesetByIdLink.Focus();
					break;
                case RecentChangesetFocusableControlNames.AddChangesetByTeamIdLink:
                    addChangesetByTeamIdLink.Focus();
                    break;
                case RecentChangesetFocusableControlNames.BranchNameTextBox:
                    branchNameTextBox.FocusTextBox();
                    branchNameTextBox.TextBoxControl.SelectionStart = branchNameTextBox.TextBoxControl.Text.Length;
                    break;
                case RecentChangesetFocusableControlNames.ChangesetIdTextBox:
					changesetIdTextBox.FocusTextBox();
					changesetIdTextBox.TextBoxControl.SelectionStart = changesetIdTextBox.TextBoxControl.Text.Length;
					break;
                case RecentChangesetFocusableControlNames.ChangesetTeamIdTextBox:
                    teamIdTextBox.FocusTextBox();
                    teamIdTextBox.TextBoxControl.SelectionStart = teamIdTextBox.TextBoxControl.Text.Length;
                    break;
                case RecentChangesetFocusableControlNames.ChangesetList:
					if (changesetList.SelectedItem != null)
					{
						changesetList.UpdateLayout();
						var item = changesetList.ItemContainerGenerator.ContainerFromIndex(changesetList.SelectedIndex);
						((ListBoxItem) item).Focus();
					}
					else
					{
						changesetList.Focus();
					}
					break;
			}
		}
	}
}
