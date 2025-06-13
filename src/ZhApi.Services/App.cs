namespace ZhApi.Services;

public class App
{
    public required IServiceProvider Service { get; init; }

    public required IConfiguration Config { get; init; }
}