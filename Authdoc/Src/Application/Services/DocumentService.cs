using Authdoc.Application.DTOs;
using Authdoc.Data;
using Authdoc.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Authdoc.Application.Services;

public class DocumentService
{
    private readonly ManagerDbContext _context;

    public DocumentService(ManagerDbContext context)
    {
        _context = context;
    }

    public async Task<DocumentResponse> CreateAsync(Guid ownerId, CreateDocumentRequest request)
    {
        var now = DateTime.UtcNow;
        var document = new Document
        {
            OwnerId = ownerId,
            Type = request.Type,
            Name = request.Name,
            Number = request.Number,
            Description = request.Description,
            IssuedAt = request.IssuedAt,
            ExpiresAt = request.ExpiresAt,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        return DocumentResponse.From(document);
    }

    public async Task<PagedResponse<DocumentResponse>> ListAsync(Guid ownerId, int page, int pageSize)
    {
        var ownerDocuments = _context.Documents.Where(document => document.OwnerId == ownerId);

        var totalCount = await ownerDocuments.CountAsync();

        var documents = await ownerDocuments
            .OrderBy(document => document.CreatedAt)
            .ThenBy(document => document.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResponse<DocumentResponse>
        {
            Items = documents.Select(DocumentResponse.From).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<DocumentResponse?> GetByIdAsync(Guid ownerId, Guid id)
    {
        var document = await FindOwnedAsync(ownerId, id);
        if (document is null)
        {
            return null;
        }

        return DocumentResponse.From(document);
    }

    public async Task<DocumentResponse?> UpdateAsync(Guid ownerId, Guid id, UpdateDocumentRequest request)
    {
        var document = await FindOwnedAsync(ownerId, id);
        if (document is null)
        {
            return null;
        }

        document.Type = request.Type;
        document.Name = request.Name;
        document.Number = request.Number;
        document.Description = request.Description;
        document.IssuedAt = request.IssuedAt;
        document.ExpiresAt = request.ExpiresAt;
        document.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return DocumentResponse.From(document);
    }

    public async Task<bool> DeleteAsync(Guid ownerId, Guid id)
    {
        var document = await FindOwnedAsync(ownerId, id);
        if (document is null)
        {
            return false;
        }

        _context.Documents.Remove(document);
        await _context.SaveChangesAsync();

        return true;
    }

    // A document of another user is treated as if it did not exist.
    private Task<Document?> FindOwnedAsync(Guid ownerId, Guid id)
    {
        return _context.Documents
            .FirstOrDefaultAsync(document => document.Id == id && document.OwnerId == ownerId);
    }
}
