namespace SatRural.Application.Modules.Monitoring;

public class DashboardDto
{
    public decimal Temperature { get; set; }

    public decimal Humidity { get; set; }

    public decimal WindSpeed { get; set; }

    public decimal Rainfall { get; set; }

    public decimal RiverLevel { get; set; }

    public string Status { get; set; } = string.Empty;

    public string StatusMessage { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; }
}