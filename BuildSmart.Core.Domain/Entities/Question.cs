using BuildSmart.Core.Domain.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace BuildSmart.Core.Domain.Entities;

public class Question : BaseEntity
{
    public string QuestionCode { get; set; } = string.Empty; // e.g. "global_total_sqm"
    public string Text { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // e.g. choice, number, boolean, multiselect
    public bool IsRequired { get; set; }
    
    // JSON representation of options (if choice or multiselect)
    public string? OptionsJson { get; set; }
    public string? HintText { get; set; }

    public string? EnglishText { get; set; }

    public string? EnglishHint { get; set; }

    public string? EnglishOptionsJson { get; set; }

    public Guid? ServiceCategoryId { get; set; }
    public ServiceCategory? ServiceCategory { get; set; }

    // Spider-net relationships
    public Guid? ParentQuestionId { get; set; }
    public Question? ParentQuestion { get; set; }
    public ICollection<Question> NextQuestions { get; set; } = new List<Question>();

    public int DisplayOrder { get; set; }
    public string? VisibilityCondition { get; set; } // JSON-logic or simple expression

    // Link arrays stored directly for easy serialization
    public List<Guid> SkuIds { get; set; } = new();
    public List<Guid> FormulaIds { get; set; } = new();

    // EF Core navigations
    public ICollection<ServiceSku> Skus { get; set; } = new List<ServiceSku>();
    public ICollection<Formula> Formulas { get; set; } = new List<Formula>();
}
