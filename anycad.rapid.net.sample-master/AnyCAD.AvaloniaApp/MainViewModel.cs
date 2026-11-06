using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace AnyCAD.AvaloniaApp
{
    internal class MainViewModel : ObservableObject
    {
        public ObservableCollection<TreeViewItem> BasicItems { get; set; }
        public ObservableCollection<TreeViewItem> SimulateItems { get; set; }
        public ObservableCollection<TreeViewItem> MfgItems { get; set; }
        public ObservableCollection<TreeViewItem> PtcItems { get; set; }
        public ObservableCollection<TreeViewItem> QuickSolidItems { get; set; }

        public MainViewModel() 
        {
            BasicItems = TestCaseLoader.LoadBasic();
            SimulateItems = TestCaseLoader.LoadSimulate();
            MfgItems = TestCaseLoader.LoadMfg();
            PtcItems = TestCaseLoader.LoadPtc();
            QuickSolidItems = TestCaseLoader.LoadQuickSolid();
        }  
    }
}
