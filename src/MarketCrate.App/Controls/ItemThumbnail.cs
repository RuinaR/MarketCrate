using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MarketCrate.Core;
using MarketCrate.Infrastructure.Steam;

namespace MarketCrate.App.Controls;

// Only realized controls subscribe. Recycling/unload cancels the subscriber and clears decoded pixels.
public sealed class ItemThumbnail:UserControl
{
    public static readonly DependencyProperty ReferenceProperty=DependencyProperty.Register(nameof(Reference),typeof(ItemImageReference),typeof(ItemThumbnail),new PropertyMetadata(null,Changed));
    public static readonly DependencyProperty LoaderProperty=DependencyProperty.Register(nameof(Loader),typeof(SteamImageCache),typeof(ItemThumbnail),new PropertyMetadata(null,Changed));
    public static readonly DependencyProperty OperationIdProperty=DependencyProperty.Register(nameof(OperationId),typeof(Guid?),typeof(ItemThumbnail));
    public ItemImageReference? Reference{get=>(ItemImageReference?)GetValue(ReferenceProperty);set=>SetValue(ReferenceProperty,value);}
    public SteamImageCache? Loader{get=>(SteamImageCache?)GetValue(LoaderProperty);set=>SetValue(LoaderProperty,value);}
    public Guid? OperationId{get=>(Guid?)GetValue(OperationIdProperty);set=>SetValue(OperationIdProperty,value);}
    private readonly Image image=new(){MaxWidth=64,MaxHeight=64,Stretch=Stretch.Uniform,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};
    private readonly TextBlock label=new(){Text="이미지 없음",TextWrapping=TextWrapping.Wrap,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,FontSize=10};
    private readonly Button retry=new(){Content="다시",Padding=new Thickness(2),Visibility=Visibility.Collapsed};
    private CancellationTokenSource? loading;
    private int revision;
    public string ImageState{get;private set;}="Missing";
    public bool HasPixels=>image.Source is not null;
    public ItemThumbnail()
    {
        Width=76;Height=84;Focusable=false;var grid=new Grid();grid.Children.Add(image);grid.Children.Add(label);retry.VerticalAlignment=VerticalAlignment.Bottom;grid.Children.Add(retry);var frame=new Border{Child=grid,CornerRadius=new CornerRadius(6),Padding=new Thickness(4),BorderThickness=new Thickness(1)};frame.SetResourceReference(Border.BackgroundProperty,"Surface");frame.SetResourceReference(Border.BorderBrushProperty,"Edge");Content=frame;
        Loaded+=async(_,_)=>await RefreshAsync();Unloaded+=(_,_)=>Release();retry.Click+=async(_,_)=>await RefreshAsync();
        System.Windows.Automation.AutomationProperties.SetName(this,"아이템 이미지와 로딩 상태");
    }
    private static void Changed(DependencyObject d,DependencyPropertyChangedEventArgs e){var control=(ItemThumbnail)d;control.Release();if(control.IsLoaded)_=control.RefreshAsync();else control.State(control.Reference?.Invalid==true?"Invalid":control.Reference?.Token is null?"Missing":"Disabled");}
    public void Release(){revision++;loading?.Cancel();loading?.Dispose();loading=null;image.Source=null;}
    private void State(string value)
    {
        ImageState=value;label.Text=value switch{"Loading"=>"이미지 로딩…","Ready"=>"","Failed"=>"이미지 오류","Invalid"=>"이미지 미지원","Disabled"=>"이미지 꺼짐",_=>"이미지 없음"};
        label.Visibility=value=="Ready"?Visibility.Collapsed:Visibility.Visible;retry.Visibility=value=="Failed"?Visibility.Visible:Visibility.Collapsed;
        ToolTip=label.Text;System.Windows.Automation.AutomationProperties.SetHelpText(this,label.Text);
    }
    public async Task RefreshAsync()
    {
        Release();var version=revision;var reference=Reference;var cache=Loader;
        if(reference?.Invalid==true){State("Invalid");return;}if(reference?.Token is null){State("Missing");return;}if(cache is null){State("Disabled");return;}
        loading=new();var token=loading.Token;State("Loading");
        using var scope=DiagnosticHub.Scope(operation:OperationId);
        try
        {
            var bytes=await cache.GetAsync(reference.Token,token);
            var bitmap=await Task.Run(()=>{token.ThrowIfCancellationRequested();SteamImageCache.ValidateDimensions(bytes);using var stream=new MemoryStream(bytes,false);var decoded=new BitmapImage();decoded.BeginInit();decoded.CacheOption=BitmapCacheOption.OnLoad;decoded.DecodePixelWidth=128;decoded.StreamSource=stream;decoded.EndInit();decoded.Freeze();return decoded;},token);
            if(version!=revision||token.IsCancellationRequested)return;image.Source=bitmap;State("Ready");
        }
        catch(OperationCanceledException){if(version==revision)State("Disabled");}
        catch(Exception error)
        {
            if(version!=revision)return;cache.Invalidate(reference.Token);State("Failed");
            DiagnosticHub.Emit(DiagnosticEvent.ImageLoad,DiagnosticArea.UI,DiagnosticOutcome.Failed,LogLevel.Warning,DiagnosticError.Parsing,requirement:"B04",reason:"ImageFailed",exception:error);
        }
    }
}
