namespace StockTvBlazor.Components.Services
{
    public record LogMessage(
      DateTime Timestamp,
      LogLevel Level,
      string Category,
      string Message,
      Exception? Exception
  );

}
