using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace MarketCrate.App.Controls;

// Uniform tiles, bounded realization, pixel scrolling. Uses the WPF item generator and keyboard BringIntoView.
public sealed class VirtualizingTilePanel : VirtualizingPanel, IScrollInfo
{
    public double TileWidth { get; set; } = 258;
    public double TileHeight { get; set; } = 312;
    private int columns = 1;
    public int RealizedCount => InternalChildren.Count;
    protected override Size MeasureOverride(Size available)
    {
        var owner = ItemsControl.GetItemsOwner(this);
        if (owner == null) return new();
        var width = double.IsFinite(available.Width) ? Math.Max(1, available.Width) : TileWidth;
        var height = double.IsFinite(available.Height) ? Math.Max(1, available.Height) : TileHeight * 2;
        columns = Math.Max(1, (int)(width / TileWidth));
        ExtentWidth = width; ExtentHeight = Math.Ceiling(owner.Items.Count / (double)columns) * TileHeight;
        ViewportWidth = width; ViewportHeight = height;
        VerticalOffset = Math.Clamp(VerticalOffset, 0, Math.Max(0, ExtentHeight - height));
        var first = Math.Max(0, ((int)(VerticalOffset / TileHeight) - 1) * columns);
        var last = Math.Min(owner.Items.Count - 1, ((int)Math.Ceiling((VerticalOffset + height) / TileHeight) + 1) * columns - 1);
        // Access InternalChildren first: WPF initializes the panel's generator lazily here.
        _ = InternalChildren;
        var generator = ItemContainerGenerator;
        for (var i = InternalChildren.Count - 1; i >= 0; i--)
        {
            var index = owner.ItemContainerGenerator.IndexFromContainer(InternalChildren[i]);
            if (index >= first && index <= last) continue;
            if (generator is IRecyclingItemContainerGenerator recycling && GetVirtualizationMode(owner) == VirtualizationMode.Recycling)
                recycling.Recycle(new GeneratorPosition(i, 0), 1);
            else generator.Remove(new GeneratorPosition(i, 0), 1);
            RemoveInternalChildRange(i, 1);
        }
        if (last >= first)
        {
            var position = generator.GeneratorPositionFromIndex(first);
            var childIndex = position.Offset == 0 ? position.Index : position.Index + 1;
            using (generator.StartAt(position, GeneratorDirection.Forward, true))
                for (var index = first; index <= last; index++, childIndex++)
                {
                    var child = (UIElement)generator.GenerateNext(out var created);
                    // Recycled containers can be returned with created=false but are no longer in this visual tree.
                    if (created || !InternalChildren.Contains(child))
                    {
                        if (childIndex >= InternalChildren.Count) AddInternalChild(child); else InsertInternalChild(childIndex, child);
                        generator.PrepareItemContainer(child);
                    }
                    child.Measure(new(TileWidth, TileHeight));
                }
        }
        ScrollOwner?.InvalidateScrollInfo(); return new(width, Math.Min(height, ExtentHeight));
    }
    protected override Size ArrangeOverride(Size size)
    {
        var owner = ItemsControl.GetItemsOwner(this);
        foreach (UIElement child in InternalChildren)
        {
            var index = owner.ItemContainerGenerator.IndexFromContainer(child);
            child.Arrange(new Rect(index % columns * TileWidth, index / columns * TileHeight - VerticalOffset, TileWidth, TileHeight));
        }
        return size;
    }
    protected override void OnItemsChanged(object sender, ItemsChangedEventArgs args)
    {
        base.OnItemsChanged(sender, args);
        if (args.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset) RemoveInternalChildRange(0, InternalChildren.Count);
        else if (args.ItemUICount > 0 && args.Position.Index >= 0 && args.Action is (System.Collections.Specialized.NotifyCollectionChangedAction.Remove or System.Collections.Specialized.NotifyCollectionChangedAction.Replace))
            RemoveInternalChildRange(args.Position.Index, args.ItemUICount);
        else if (args.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Move) RemoveInternalChildRange(args.OldPosition.Index, args.ItemUICount);
        InvalidateMeasure();
    }
    protected override void BringIndexIntoView(int index) { SetVerticalOffset(index / columns * TileHeight); }
    public bool CanHorizontallyScroll { get; set; }
    public bool CanVerticallyScroll { get; set; } = true;
    public double ExtentWidth { get; private set; }
    public double ExtentHeight { get; private set; }
    public double ViewportWidth { get; private set; }
    public double ViewportHeight { get; private set; }
    public double HorizontalOffset => 0;
    public double VerticalOffset { get; private set; }
    public ScrollViewer? ScrollOwner { get; set; }
    public void SetVerticalOffset(double offset) { if (double.IsNaN(offset)) return; VerticalOffset = Math.Clamp(offset, 0, Math.Max(0, ExtentHeight - ViewportHeight)); InvalidateMeasure(); ScrollOwner?.InvalidateScrollInfo(); }
    public void SetHorizontalOffset(double offset) { }
    public void LineUp() => SetVerticalOffset(VerticalOffset - 32);
    public void LineDown() => SetVerticalOffset(VerticalOffset + 32);
    public void MouseWheelUp() => SetVerticalOffset(VerticalOffset - 96);
    public void MouseWheelDown() => SetVerticalOffset(VerticalOffset + 96);
    public void PageUp() => SetVerticalOffset(VerticalOffset - ViewportHeight);
    public void PageDown() => SetVerticalOffset(VerticalOffset + ViewportHeight);
    public void LineLeft() { }
    public void LineRight() { }
    public void MouseWheelLeft() { }
    public void MouseWheelRight() { }
    public void PageLeft() { }
    public void PageRight() { }
    public Rect MakeVisible(Visual visual, Rect rectangle)
    {
        for (DependencyObject? item = visual; item != null && item != this; item = VisualTreeHelper.GetParent(item))
            if (VisualTreeHelper.GetParent(item) == this)
            {
                var index = ItemsControl.GetItemsOwner(this).ItemContainerGenerator.IndexFromContainer(item);
                var top = index / columns * TileHeight;
                if (top < VerticalOffset) SetVerticalOffset(top); else if (top + TileHeight > VerticalOffset + ViewportHeight) SetVerticalOffset(top + TileHeight - ViewportHeight);
                break;
            }
        return rectangle;
    }
}
