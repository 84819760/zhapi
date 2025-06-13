namespace ZhApi.WpfApp;
public partial class MainControl : UserControl
{
    private readonly MainControlViewModel mainModel;

    public MainControl(MainControlViewModel mainModel)
    {
        DataContext = this.mainModel = mainModel;
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        await App.EnsureCreatedTask;
        mainModel.ButtonsVisibility = Visibility.Visible;
        await Task.Run(mainModel.SetRepairCountAsync);
    }
}
