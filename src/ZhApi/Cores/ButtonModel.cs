using System.Windows.Media;
using System.Windows;

namespace ZhApi.WpfApp.Cores;
public partial class ButtonModel : ObservableObject
{
    [ObservableProperty]
    private bool enabled = true;

    [ObservableProperty]
    private Brush foreground = App.GetResource<Brush>("TextE6");

    [ObservableProperty]
    private Visibility visibility = Visibility.Visible;

    partial void OnEnabledChanged(bool value)
    {
        if (value)
        {
            Foreground = App.GetResource<Brush>("TextE6");
        }
        else
        {
            Foreground = App.GetResource<Brush>("Textdim");  
        }
    }

    public ButtonModel Set(Action<ButtonModel> action)
    {
        action(this);
        return this;
    }

}