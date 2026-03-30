using FluentAssertions;
using RPGEconomy.Domain.Production;
using RPGEconomy.Domain.Resources;

namespace RPGEconomy.Domain.Tests;

public class WarehouseTests
{
    [Fact]
    public void AddItem_Should_Aggregate_Items_With_Same_Product_And_Quality()
    {
        // Тест агрегации одинаковых товаров
        var warehouse = Warehouse.Create(1);

        warehouse.AddItem(10, 5, QualityGrade.Normal);
        warehouse.AddItem(10, 3, QualityGrade.Normal);

        warehouse.Items.Should().ContainSingle();
        warehouse.Items[0].Quantity.Should().Be(8);
    }

    [Fact]
    public void AddItem_Should_Fail_For_NonPositive_Quantity()
    {
        // Тест добавления 0 товаров
        var warehouse = Warehouse.Create(1);

        var result = warehouse.AddItem(10, 0, QualityGrade.Normal);

        result.IsSuccess.Should().BeFalse();
        warehouse.Items.Should().BeEmpty();
    }

    [Fact]
    public void Withdraw_Should_Remove_Item_When_Quantity_Reaches_Zero()
    {
        // Удаление товара, когда его количество достигает 0
        var warehouse = Warehouse.Create(1);
        warehouse.AddItem(10, 5, QualityGrade.Normal);

        var result = warehouse.Withdraw(10, 5, QualityGrade.Normal);

        result.IsSuccess.Should().BeTrue();
        warehouse.Items.Should().BeEmpty();
    }

    [Fact]
    public void CanFulfill_Should_Only_Use_Normal_Quality_Stock()
    {
        var warehouse = Warehouse.Create(1);
        warehouse.AddItem(10, 5, QualityGrade.High);

        var result = warehouse.CanFulfill([new RecipeIngredient(10, 5)]);

        result.Should().BeFalse();
    }

    [Fact]
    public void AddItem_Should_Preserve_Fractional_Quantity()
    {
        var warehouse = Warehouse.Create(1);

        warehouse.AddItem(10, 1.25m, QualityGrade.Normal);
        warehouse.AddItem(10, 0.75m, QualityGrade.Normal);

        warehouse.Items.Should().ContainSingle();
        warehouse.Items[0].Quantity.Should().Be(2m);
    }

    [Fact]
    public void SetItemQuantity_Should_Create_Item_When_Missing()
    {
        var warehouse = Warehouse.Create(1);

        var result = warehouse.SetItemQuantity(10, 4m, QualityGrade.Normal);

        result.IsSuccess.Should().BeTrue();
        warehouse.Items.Should().ContainSingle(item =>
            item.ProductTypeId == 10 &&
            item.Quantity == 4m &&
            item.Quality == QualityGrade.Normal.Name);
    }

    [Fact]
    public void SetItemQuantity_Should_Update_Existing_Item_Without_Duplicating()
    {
        var warehouse = Warehouse.Create(1);
        warehouse.AddItem(10, 2m, QualityGrade.Normal);

        var result = warehouse.SetItemQuantity(10, 9m, QualityGrade.Normal);

        result.IsSuccess.Should().BeTrue();
        warehouse.Items.Should().ContainSingle();
        warehouse.Items[0].Quantity.Should().Be(9m);
    }

    [Fact]
    public void SetItemQuantity_Should_Remove_Item_When_Quantity_Is_Zero()
    {
        var warehouse = Warehouse.Create(1);
        warehouse.AddItem(10, 2m, QualityGrade.Normal);

        var result = warehouse.SetItemQuantity(10, 0m, QualityGrade.Normal);

        result.IsSuccess.Should().BeTrue();
        warehouse.Items.Should().BeEmpty();
    }

    [Fact]
    public void SetItemQuantity_Should_Fail_For_Negative_Quantity()
    {
        var warehouse = Warehouse.Create(1);

        var result = warehouse.SetItemQuantity(10, -1m, QualityGrade.Normal);

        result.IsSuccess.Should().BeFalse();
        warehouse.Items.Should().BeEmpty();
    }
}
