using Authdoc.Application.DTOs;
using Authdoc.Application.Services;
using Authdoc.Data;
using Authdoc.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Authdoc.Tests;

public class DocumentServiceTests
{
    private static ManagerDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ManagerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ManagerDbContext(options);
    }

    private static Document NewDocument(Guid ownerId, string name = "Passport")
    {
        return new Document
        {
            OwnerId = ownerId,
            Type = "Passport",
            Name = name,
            Number = "FZ123456",
            IssuedAt = new DateOnly(2024, 1, 10),
            ExpiresAt = new DateOnly(2034, 1, 10)
        };
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_SavesDocumentForOwner()
    {
        // Arrange
        using var context = CreateContext();
        var service = new DocumentService(context);
        var ownerId = Guid.NewGuid();
        var request = new CreateDocumentRequest
        {
            Type = "Passport",
            Name = "My passport",
            Number = "FZ123456",
            IssuedAt = new DateOnly(2024, 1, 10)
        };

        // Act
        var result = await service.CreateAsync(ownerId, request);

        // Assert
        var saved = await context.Documents.SingleAsync();
        Assert.Equal(saved.Id, result.Id);
        Assert.Equal(ownerId, saved.OwnerId);
        Assert.Equal("FZ123456", saved.Number);
        Assert.Null(saved.ExpiresAt);
    }

    [Fact]
    public async Task GetByIdAsync_WhenDocumentBelongsToOwner_ReturnsDocument()
    {
        // Arrange
        using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        var document = NewDocument(ownerId);
        context.Documents.Add(document);
        await context.SaveChangesAsync();
        var service = new DocumentService(context);

        // Act
        var result = await service.GetByIdAsync(ownerId, document.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(document.Id, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenDocumentBelongsToAnotherUser_ReturnsNull()
    {
        // Arrange
        using var context = CreateContext();
        var document = NewDocument(Guid.NewGuid());
        context.Documents.Add(document);
        await context.SaveChangesAsync();
        var service = new DocumentService(context);

        // Act
        var result = await service.GetByIdAsync(Guid.NewGuid(), document.Id);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ListAsync_WithDocumentsOfTwoUsers_ReturnsOnlyOwnerDocuments()
    {
        // Arrange
        using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        context.Documents.AddRange(
            NewDocument(ownerId, "Passport"),
            NewDocument(ownerId, "Visa"),
            NewDocument(Guid.NewGuid(), "Other user passport"));
        await context.SaveChangesAsync();
        var service = new DocumentService(context);

        // Act
        var result = await service.ListAsync(ownerId, page: 1, pageSize: 10);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.DoesNotContain(result.Items, item => item.Name == "Other user passport");
    }

    [Fact]
    public async Task UpdateAsync_WhenDocumentBelongsToOwner_UpdatesDocument()
    {
        // Arrange
        using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        var document = NewDocument(ownerId);
        context.Documents.Add(document);
        await context.SaveChangesAsync();
        var service = new DocumentService(context);
        var request = new UpdateDocumentRequest
        {
            Type = "Visa",
            Name = "Work visa",
            Number = "V-0042",
            IssuedAt = new DateOnly(2025, 3, 1)
        };

        // Act
        var result = await service.UpdateAsync(ownerId, document.Id, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Work visa", result.Name);
        Assert.Equal("V-0042", document.Number);
        Assert.Null(document.ExpiresAt);
    }

    [Fact]
    public async Task UpdateAsync_WhenDocumentBelongsToAnotherUser_ReturnsNullAndKeepsDocument()
    {
        // Arrange
        using var context = CreateContext();
        var document = NewDocument(Guid.NewGuid());
        context.Documents.Add(document);
        await context.SaveChangesAsync();
        var service = new DocumentService(context);
        var request = new UpdateDocumentRequest
        {
            Type = "Visa",
            Name = "Changed by another user",
            Number = "V-0042",
            IssuedAt = new DateOnly(2025, 3, 1)
        };

        // Act
        var result = await service.UpdateAsync(Guid.NewGuid(), document.Id, request);

        // Assert
        Assert.Null(result);
        Assert.Equal("Passport", document.Name);
    }

    [Fact]
    public async Task DeleteAsync_WhenDocumentBelongsToOwner_ReturnsTrueAndRemovesDocument()
    {
        // Arrange
        using var context = CreateContext();
        var ownerId = Guid.NewGuid();
        var document = NewDocument(ownerId);
        context.Documents.Add(document);
        await context.SaveChangesAsync();
        var service = new DocumentService(context);

        // Act
        var result = await service.DeleteAsync(ownerId, document.Id);

        // Assert
        Assert.True(result);
        Assert.Null(await context.Documents.FindAsync(document.Id));
    }

    [Fact]
    public async Task DeleteAsync_WhenDocumentBelongsToAnotherUser_ReturnsFalseAndKeepsDocument()
    {
        // Arrange
        using var context = CreateContext();
        var document = NewDocument(Guid.NewGuid());
        context.Documents.Add(document);
        await context.SaveChangesAsync();
        var service = new DocumentService(context);

        // Act
        var result = await service.DeleteAsync(Guid.NewGuid(), document.Id);

        // Assert
        Assert.False(result);
        Assert.NotNull(await context.Documents.FindAsync(document.Id));
    }
}
