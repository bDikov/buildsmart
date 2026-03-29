using System;
using System.Collections.Generic;

namespace BuildSmart.Api.DTOs;

public class UpdateJobTasksInput
{
    public Guid JobPostId { get; set; }
    public List<JobTaskInput> Tasks { get; set; } = new();
}

public class JobTaskInput
{
    public Guid? Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int SequenceOrder { get; set; }
    public List<TaskAcceptanceCriteriaInput> Criteria { get; set; } = new();
}

public class TaskAcceptanceCriteriaInput
{
    public Guid? Id { get; set; }
    public string Description { get; set; } = string.Empty;
}