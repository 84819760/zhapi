using System.Windows.Controls;

namespace ZhApi.WpfApp.LogItems;
/// <summary>
/// LogControl.xaml 的交互逻辑
/// </summary>
[AddService(ServiceLifetime.Transient)]

public partial class LogControl : UserControl
{
    public LogControl()
    {
        InitializeComponent();
    }

}
