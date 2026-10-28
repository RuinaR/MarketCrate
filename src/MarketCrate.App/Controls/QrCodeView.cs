using System.Windows;
using System.Windows.Media;

namespace MarketCrate.App.Controls;

// Local modules and a four-module quiet zone. No screen capture or OS input.
public sealed class QrCodeView : FrameworkElement
{
    public static readonly DependencyProperty MatrixProperty = DependencyProperty.Register(nameof(Matrix),typeof(bool[,]),typeof(QrCodeView),new FrameworkPropertyMetadata(null,FrameworkPropertyMetadataOptions.AffectsRender));
    public bool[,]? Matrix {get=>(bool[,]?)GetValue(MatrixProperty);set=>SetValue(MatrixProperty,value);}
    protected override void OnRender(DrawingContext drawing)
    {
        base.OnRender(drawing);drawing.DrawRectangle(Brushes.White,null,new Rect(RenderSize));
        if(Matrix is not { } cells)
        {
            var label=new FormattedText("QR 연결 대기",System.Globalization.CultureInfo.CurrentUICulture,FlowDirection.LeftToRight,new Typeface("Malgun Gothic"),14,Brushes.DimGray,VisualTreeHelper.GetDpi(this).PixelsPerDip);
            drawing.DrawText(label,new Point(Math.Max(0,(ActualWidth-label.Width)/2),Math.Max(0,(ActualHeight-label.Height)/2)));return;
        }
        var size=cells.GetLength(0);if(size!=cells.GetLength(1)||size is <21 or >177)return;
        var module=Math.Floor(Math.Min(ActualWidth,ActualHeight)/(size+8));if(module<1)return;
        var left=Math.Floor((ActualWidth-module*(size+8))/2);var top=Math.Floor((ActualHeight-module*(size+8))/2);
        for(var y=0;y<size;y++)for(var x=0;x<size;x++)if(cells[x,y])drawing.DrawRectangle(Brushes.Black,null,new Rect(left+(x+4)*module,top+(y+4)*module,module,module));
    }
}
