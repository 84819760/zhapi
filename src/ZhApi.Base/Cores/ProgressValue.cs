namespace ZhApi.Cores;

public readonly record struct ProgressValue
{
    public required int Value { get; init; }

    public required int Max { get; init; }

    public double Progress => (double)Value / Max;

}

public readonly record struct ProgressValue<T>
{
    public required int Value { get; init; }

    public required int Max { get; init; }

    public required T Data { get; init; }

    public double Progress => (double)Value / Max;
}