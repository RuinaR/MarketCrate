namespace MarketCrate.App.ViewModels;
public sealed partial class SteamWorkspaceViewModel
{
    private int workPage;
    private bool gridMode;
    public bool GridMode{get=>gridMode;set=>Set(ref gridMode,value);}
    private static readonly int[] navigationOrder=[1,2,3,0,4,5];
    public int NavigationIndex{get=>Array.IndexOf(navigationOrder,WorkPage);set{if(value>=0&&value<navigationOrder.Length)WorkPage=navigationOrder[value];}}
    public Command OpenInventory{get;private set;}=null!;
    public Command OpenGameSettings{get;private set;}=null!;
    private void InitializeNavigation(){OpenInventory=new(()=>WorkPage=1,()=>!Busy&&!AnyReview);OpenGameSettings=new(()=>WorkPage=4,()=>!Busy&&!AnyReview);}
    public int WorkPage{get=>workPage;set{
#if MARKETCRATE_DEMO
        if(value is >=0 and <=5&&Set(ref workPage,value))Raise(nameof(NavigationIndex));
#else
        if(value is >=0 and <=4&&Set(ref workPage,value))Raise(nameof(NavigationIndex));
#endif
    }}
    partial void InitializeDevelopmentTools();
    partial void ResetDevelopmentTools();
    partial void RefreshDevelopmentTools();
}
