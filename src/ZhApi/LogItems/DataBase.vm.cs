namespace ZhApi.WpfApp.LogItems;
public partial class DataBaseViewModel : ControlProvider,
    IRecipient<DataBaseMessage>
{
    private readonly static Lock @lock = new();
    public string? Target { get; set; }

    protected override Control CreateControl() => 
        new DataBase() { DataContext = this };

    [ObservableProperty]
    public partial Visibility SymbolVisibility { get; set; } = Visibility.Visible;

    public DataBaseUnitViewModel WriteRow { get; set; } = new()
    {
        Symbol = Wpf.Ui.Controls.SymbolRegular.DatabaseArrowDown20,
        ChangedFromColor = Colors.LightPink,
    };

    public DataBaseUnitViewModel UpdateRow { get; set; } = new()
    {
        Symbol = Wpf.Ui.Controls.SymbolRegular.DatabaseArrowRight20,
        ChangedFromColor = Colors.LightGreen,
    };

    void IRecipient<DataBaseMessage>.Receive(DataBaseMessage message)
    {
        if (message.Target != Target) return;

        SymbolVisibility = Visibility.Collapsed;
        lock (@lock)
        {
            if (message.IsUpdate)
            {
                UpdateRow.Count += message.Count;
            }
            else
            {
                WriteRow.Count += message.Count;
            }
        }
    }
}
