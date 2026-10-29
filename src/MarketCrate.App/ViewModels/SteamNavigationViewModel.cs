namespace MarketCrate.App.ViewModels;
public sealed partial class SteamWorkspaceViewModel
{
    private int workPage;
    public int WorkPage{get=>workPage;set{
#if MARKETCRATE_DEMO
        if(value is >=0 and <=5)Set(ref workPage,value);
#else
        if(value is >=0 and <=4)Set(ref workPage,value);
#endif
    }}
    partial void InitializeDevelopmentTools();
    partial void ResetDevelopmentTools();
    partial void RefreshDevelopmentTools();
}
