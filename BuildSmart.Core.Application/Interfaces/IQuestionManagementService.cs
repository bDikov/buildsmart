using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BuildSmart.Core.Application.Interfaces;

public interface IQuestionManagementService
{
    // Questions
    Task<Question> CreateQuestionAsync(Question question, CancellationToken cancellationToken = default);
    Task<Question> UpdateQuestionAsync(Question question, CancellationToken cancellationToken = default);
    Task<Question> UpdateQuestionLinksAsync(Guid questionId, List<Guid> skuIds, List<Guid> formulaIds, CancellationToken cancellationToken = default);
    Task DeleteQuestionAsync(Guid questionId, CancellationToken cancellationToken = default);
    Task<Question?> GetQuestionByIdAsync(Guid questionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Question>> GetAllQuestionsAsync(CancellationToken cancellationToken = default);

    // SKUs
    Task<ServiceSku> CreateSkuAsync(ServiceSku sku, CancellationToken cancellationToken = default);
    Task<ServiceSku> UpdateSkuAsync(ServiceSku sku, CancellationToken cancellationToken = default);
    Task DeleteSkuAsync(Guid skuId, CancellationToken cancellationToken = default);

    // Formulas
    Task<Formula> CreateFormulaAsync(Formula formula, CancellationToken cancellationToken = default);
    Task<Formula> UpdateFormulaAsync(Formula formula, CancellationToken cancellationToken = default);
    Task DeleteFormulaAsync(Guid formulaId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Formula>> GetAllFormulasAsync(CancellationToken cancellationToken = default);

    // Graph Data
    Task<(IEnumerable<GraphNodeDto> Nodes, IEnumerable<GraphEdgeDto> Edges)> GetGraphDataAsync(CancellationToken cancellationToken = default);

    // Import/Export
    Task<string> ExportSpiderNetAsync(CancellationToken cancellationToken = default);
    Task ImportSpiderNetAsync(string jsonContent, CancellationToken cancellationToken = default);

    // Run Offer Simulation
    Task<OfferSimulationResultDto> ExecuteOfferSimulationAsync(List<Guid> selectedQuestionIds, string jobDetailsJson, decimal adminMarkupPercentage = 20.0m, CancellationToken cancellationToken = default);
}
