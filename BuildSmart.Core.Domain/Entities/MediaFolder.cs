using System;
using System.Collections.Generic;

namespace BuildSmart.Core.Domain.Entities;

/// <summary>
/// Represents a logical folder in the Cloudflare R2 media management system.
/// </summary>
public class MediaFolder
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? ParentId { get; set; }
    public MediaFolder? Parent { get; set; }

    public ICollection<MediaFolder> SubFolders { get; set; } = new List<MediaFolder>();
    public ICollection<MediaAsset> Assets { get; set; } = new List<MediaAsset>();

    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;

    public bool IsSystem { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
