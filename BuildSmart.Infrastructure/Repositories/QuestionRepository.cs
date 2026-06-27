using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BuildSmart.Infrastructure.Repositories;

public class QuestionRepository : IQuestionRepository
{
    private readonly AppDbContext _context;

    public QuestionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Question?> GetByIdAsync(Guid id)
    {
        return await _context.Questions
            .Include(q => q.Skus)
            .Include(q => q.Formulas)
            .Include(q => q.NextQuestions)
            .Include(q => q.ParentQuestion)
            .FirstOrDefaultAsync(q => q.Id == id);
    }

    public async Task<Question?> GetByCodeAsync(string questionCode)
    {
        return await _context.Questions
            .Include(q => q.Skus)
            .Include(q => q.Formulas)
            .Include(q => q.NextQuestions)
            .Include(q => q.ParentQuestion)
            .FirstOrDefaultAsync(q => q.QuestionCode == questionCode);
    }

    public async Task<IEnumerable<Question>> GetAllAsync()
    {
        return await _context.Questions
            .Include(q => q.Skus)
            .Include(q => q.Formulas)
            .Include(q => q.NextQuestions)
            .Include(q => q.ParentQuestion)
            .ToListAsync();
    }

    public async Task AddAsync(Question question)
    {
        await _context.Questions.AddAsync(question);
    }

    public void Update(Question question)
    {
        _context.Questions.Update(question);
    }

    public void Delete(Question question)
    {
        _context.Questions.Remove(question);
    }
}
