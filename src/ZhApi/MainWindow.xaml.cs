namespace ZhApi.WpfApp;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
[AddService(ServiceLifetime.Singleton)]
public partial class MainWindow : Window
{
    static MainWindow()
    {
        Application.Current.DispatcherUnhandledException += (s, e) =>
        App.ErrorHandler(e.Exception, "DispatcherUnhandledException");
    }

    public MainWindow(MainWindowViewModel viewModel)
    {
        DataContext = ViewModel = viewModel;
        App.Taskbar.TaskbarItemInfo = TaskbarItemInfo;
        InitializeComponent();
    }

    public MainWindowViewModel ViewModel { get; set; }

    private void Window_Closed(object sender, EventArgs e) { }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var screenWidth = SystemParameters.WorkArea.Width;
        var screenHeight = SystemParameters.WorkArea.Height;
        Left = (screenWidth - Width) / 2;
        Top = (screenHeight - Height) / 2;
        WindowState = WindowState.Normal;
    }
}