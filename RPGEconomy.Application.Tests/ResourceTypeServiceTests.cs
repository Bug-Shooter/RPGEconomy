using FluentAssertions;
using RPGEconomy.Application.Abstractions.Repositories;
using RPGEconomy.Application.Services;
using RPGEconomy.Domain.Resources;

namespace RPGEconomy.Application.Tests;

public class ResourceTypeServiceTests
{
    [Fact]
    public async Task SearchByNameAsync_Should_Return_Matching_ResourceTypes()
    {
        var repository = new ResourceTypeRepositoryFake(
            new ResourceType(1, "Wood", "Desc", true, 1),
            new ResourceType(2, "Stone", "Desc", false, 0))
        {
            SearchResults =
            [
                new ResourceType(1, "Wood", "Desc", true, 1)
            ]
        };
        var service = new ResourceTypeService(repository);

        var result = await service.SearchByNameAsync("woo");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(x => x.Name == "Wood");
        repository.SearchCalls.Should().Be(1);
    }

    [Fact]
    public async Task SearchByNameAsync_Should_Fallback_To_GetAll_For_Whitespace()
    {
        var repository = new ResourceTypeRepositoryFake(
            new ResourceType(1, "Wood", "Desc", true, 1),
            new ResourceType(2, "Stone", "Desc", false, 0));
        var service = new ResourceTypeService(repository);

        var result = await service.SearchByNameAsync("   ");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        repository.GetAllCalls.Should().Be(1);
        repository.SearchCalls.Should().Be(0);
    }

    private sealed class ResourceTypeRepositoryFake : IResourceTypeRepository
    {
        private readonly Dictionary<int, ResourceType> _items = [];

        public IReadOnlyList<ResourceType> SearchResults { get; set; } = [];
        public int GetAllCalls { get; private set; }
        public int SearchCalls { get; private set; }

        public ResourceTypeRepositoryFake(params ResourceType[] items)
        {
            foreach (var item in items)
                _items[item.Id] = item;
        }

        public Task<ResourceType?> GetByIdAsync(int id) => Task.FromResult(_items.GetValueOrDefault(id));

        public Task<IReadOnlyList<ResourceType>> GetAllAsync() =>
            Task.FromResult((IReadOnlyList<ResourceType>)GetAll());

        public Task<IReadOnlyList<ResourceType>> SearchByNameAsync(string search)
        {
            SearchCalls++;
            if (SearchResults.Count > 0)
                return Task.FromResult(SearchResults);

            return Task.FromResult((IReadOnlyList<ResourceType>)_items.Values
                .Where(x => x.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList()
                .AsReadOnly());
        }

        public Task<ResourceType?> GetByNameAsync(string name) =>
            Task.FromResult(_items.Values.FirstOrDefault(x => x.Name == name));

        public Task<int> SaveAsync(ResourceType entity)
        {
            _items[entity.Id == 0 ? 1 : entity.Id] = entity;
            return Task.FromResult(entity.Id == 0 ? 1 : entity.Id);
        }

        public Task DeleteAsync(int id)
        {
            _items.Remove(id);
            return Task.CompletedTask;
        }

        private IReadOnlyList<ResourceType> GetAll()
        {
            GetAllCalls++;
            return _items.Values.ToList().AsReadOnly();
        }
    }
}
