using Kafka.Ksql.Linq.Core.Attributes;

namespace DailyComparisonLib.Models;

[ScheduleRange(nameof(OpenTime), nameof(CloseTime))]
public class MarketSchedule
{
    public string Broker { get; set; } = null!;
    public string Symbol { get; set; } = null!;
    public DateTime Date { get; set; }
    public DateTime OpenTime { get; set; }
    public DateTime CloseTime { get; set; }
}
