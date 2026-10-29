using System.Windows.Controls;
namespace MarketCrate.App;
public partial class SteamWorkspaceView : UserControl
{
    public SteamWorkspaceView()
    {
        InitializeComponent();
#if MARKETCRATE_DEMO
        var checks=new DevelopmentReadCheckView();
        SteamPages.Items.Add(new TabItem{Header="연결 자가검증",Content=checks});
        RegisterName("SelfCheckScopes",checks.FindName("SelfCheckScopes"));
        RegisterName("SelfCheckConsent",checks.FindName("SelfCheckConsent"));
#endif
    }
}
