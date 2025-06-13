using System.Windows.Controls;

namespace ZhApi.WpfApp.LogItems;
/// <summary>
/// FileProgressControl.xaml 的交互逻辑
/// </summary>
[AddService( ServiceLifetime.Transient)]
public partial class FileProgressControl : UserControl
{
    public FileProgressControl()
    {
        InitializeComponent();
    }

}
