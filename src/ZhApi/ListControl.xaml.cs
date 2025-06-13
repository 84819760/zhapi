using System.Collections.Specialized;

namespace ZhApi.WpfApp;

public partial class ListControl : UserControl
{
    public ListControl(ListControlViewModel viewModel)
    {
        DataContext = viewModel;
        viewModel.Items.CollectionChanged += CollectionChanged;         
        InitializeComponent();       
        ScrollViewer1.ScrollToEnd();
    }

    private void CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        var scroll = ScrollViewer1;
        if (scroll.VerticalOffset == scroll.ExtentHeight - scroll.ViewportHeight)
            ScrollViewer1.ScrollToEnd();
    }    
}