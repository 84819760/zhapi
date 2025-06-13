using System.Windows.Controls;

namespace ZhApi.WpfApp;

public interface IControlProvider
{
    Control Control { get; }
}