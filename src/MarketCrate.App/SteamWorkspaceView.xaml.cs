using System.Windows.Controls;
namespace MarketCrate.App;
public partial class SteamWorkspaceView : UserControl
{
    private readonly System.Windows.Threading.DispatcherTimer holdTimer=new(){Interval=TimeSpan.FromMinutes(1)};
    public SteamWorkspaceView()
    {
        InitializeComponent();
        holdTimer.Tick+=(_,_)=>{if(DataContext is MarketCrate.App.ViewModels.SteamWorkspaceViewModel vm&&vm.WorkPage==1&&!vm.Busy)foreach(var row in vm.Assets)row.RefreshHoldTime();};
        Loaded+=(_,_)=>holdTimer.Start();Unloaded+=(_,_)=>holdTimer.Stop();
#if MARKETCRATE_DEMO
        var checks=new DevelopmentReadCheckView();
        SteamPages.Items.Add(new TabItem{Header="연결 자가검증",Content=checks});
        RegisterName("SelfCheckScopes",checks.FindName("SelfCheckScopes"));
        RegisterName("SelfCheckConsent",checks.FindName("SelfCheckConsent"));
#endif
    }
}
