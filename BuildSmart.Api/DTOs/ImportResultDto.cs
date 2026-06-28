using System.Collections.Generic;

namespace BuildSmart.Api.DTOs
{
    public class ImportResultDto
    {
        public bool Success { get; set; }
        public List<string> LogLines { get; set; } = new List<string>();
        public string? ErrorMessage { get; set; }
    }
}
