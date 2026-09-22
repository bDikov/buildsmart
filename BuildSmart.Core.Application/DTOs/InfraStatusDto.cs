using System;

namespace BuildSmart.Core.Application.DTOs;

public class InfraStatusDto
{
    public bool DatabaseConnected { get; set; }
    public string DatabaseLatencyMs { get; set; } = "N/A";
    public double MemoryUsageMb { get; set; }
    public double TotalAllocatedMb { get; set; }
    public TimeSpan Uptime { get; set; }
    public string Environment { get; set; } = "Production";
    public string OsVersion { get; set; } = string.Empty;
    public int ProcessorCount { get; set; }
    public int ActiveHangfireJobs { get; set; }
    public DateTime ServerTimeUtc { get; set; } = DateTime.UtcNow;
}
