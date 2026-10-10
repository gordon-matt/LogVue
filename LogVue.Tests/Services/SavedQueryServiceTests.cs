using Ardalis.Result;
using LogVue.Models;

namespace LogVue.Tests.Services;

public class SavedQueryServiceTests : IDisposable
{
    private readonly TestDatabase database = TestDatabase.CreateSqlite();

    public void Dispose() => database.Dispose();

    [Fact]
    public async Task SaveAsync_SameName_ReplacesTheQuery()
    {
        var service = database.CreateSavedQueryService();

        await service.SaveAsync(new SaveQueryRequest { Name = "Errors", Query = "level = error" });
        await service.SaveAsync(new SaveQueryRequest { Name = "Errors", Query = "level = fatal" });
        await service.SaveAsync(new SaveQueryRequest { Name = "Api", Query = "SourceContext like '%Api%'" });

        var all = (await service.GetAllAsync()).Value;

        Assert.Equal(["Api", "Errors"], all.Select(q => q.Name));
        Assert.Equal("level = fatal", all.Single(q => q.Name == "Errors").Query);
    }

    [Fact]
    public async Task SaveAsync_UnparseableQuery_IsInvalid()
    {
        var result = await database.CreateSavedQueryService().SaveAsync(new SaveQueryRequest { Name = "Broken", Query = "a = " });

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task SaveAndDelete_WhenReadOnly_AreForbidden()
    {
        database.Options.ReadOnly = true;
        var service = database.CreateSavedQueryService();

        Assert.Equal(ResultStatus.Forbidden, (await service.SaveAsync(new SaveQueryRequest { Name = "x", Query = "x" })).Status);
        Assert.Equal(ResultStatus.Forbidden, (await service.DeleteAsync(1)).Status);
    }
}