using RPGEconomy.Domain.Population;
using RPGEconomy.Domain.Production;
using RPGEconomy.Domain.Resources;
using RPGEconomy.Simulation.Engine;

namespace RPGEconomy.Simulation.Services;

public class SettlementEconomySimulationService
{
    public void ConsumeHouseholdStocks(SimulationContext ctx)
    {
        ctx.ResetConsumptionDemand();

        foreach (var settlement in ctx.Settlements)
        {
            if (!ctx.PopulationGroups.TryGetValue(settlement.Id, out var groups))
                continue;

            var activeEffects = ctx.GetActiveEffects(settlement.Id);
            foreach (var group in groups)
            {
                var consumptionDemand = group.CalculateConsumptionDemand(
                    ResolveMultipliers(
                        activeEffects,
                        group.Id,
                        group.ConsumptionProfile.Select(item => item.ProductTypeId),
                        Domain.Events.EconomicEffectType.ConsumptionMultiplier));

                var unmetDemand = group.ConsumeFromStock(consumptionDemand);
                foreach (var request in unmetDemand)
                {
                    var demandMultiplier = ResolveMultiplier(
                        activeEffects,
                        group.Id,
                        request.Key,
                        Domain.Events.EconomicEffectType.DemandMultiplier);

                    ctx.AddConsumptionDemandRequest(
                        settlement.Id,
                        group.Id,
                        request.Key,
                        request.Value * demandMultiplier);
                }
            }
        }
    }

    public void ReplenishReservesAndUpdateMarket(SimulationContext ctx)
    {
        foreach (var settlement in ctx.Settlements)
        {
            if (!ctx.Markets.TryGetValue(settlement.Id, out var market))
                continue;

            var activeEffects = ctx.GetActiveEffects(settlement.Id);

            var householdReserveRequests = BuildHouseholdReserveRequests(ctx, settlement.Id, activeEffects);
            var producerReserveRequests = BuildProducerReserveRequests(ctx, settlement.Id, activeEffects);

            Warehouse? settlementWarehouse = null;
            if (ctx.Warehouses.TryGetValue(settlement.Id, out var warehouse))
            {
                settlementWarehouse = warehouse;
                FulfillWarehouseRequests(warehouse, ctx.GetConsumptionDemandRequests(settlement.Id));
                FulfillWarehouseRequests(warehouse, householdReserveRequests);
                FulfillWarehouseRequests(warehouse, producerReserveRequests);
            }

            var supplyByProduct = settlementWarehouse is not null
                ? settlementWarehouse.Items
                    .GroupBy(item => item.ProductTypeId)
                    .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity))
                : new Dictionary<int, decimal>();

            var demandByProduct = ctx.GetConsumptionDemand(settlement.Id).ToDictionary(x => x.Key, x => x.Value);

            foreach (var productionDemand in ctx.GetProductionDemand(settlement.Id))
            {
                demandByProduct[productionDemand.Key] =
                    demandByProduct.GetValueOrDefault(productionDemand.Key, 0m) + productionDemand.Value;
            }

            foreach (var reserveRequest in householdReserveRequests.Concat(producerReserveRequests))
            {
                demandByProduct[reserveRequest.ProductTypeId] =
                    demandByProduct.GetValueOrDefault(reserveRequest.ProductTypeId, 0m) + reserveRequest.Quantity;
            }

            foreach (var offer in market.Offers)
            {
                var supply = supplyByProduct.GetValueOrDefault(offer.ProductTypeId, 0m);
                var demand = demandByProduct.GetValueOrDefault(offer.ProductTypeId, 0m);
                market.UpdateProductState(offer.ProductTypeId, supply, demand);
            }
        }
    }

    private static IReadOnlyList<TransferRequest> BuildHouseholdReserveRequests(
        SimulationContext ctx,
        int settlementId,
        IReadOnlyList<Domain.Events.EconomicEffect> activeEffects)
    {
        if (!ctx.PopulationGroups.TryGetValue(settlementId, out var groups))
            return [];

        var result = new List<TransferRequest>();
        foreach (var group in groups)
        {
            var productIds = group.ConsumptionProfile.Select(item => item.ProductTypeId).Distinct().ToList();
            var consumptionDemand = group.CalculateConsumptionDemand(
                ResolveMultipliers(
                    activeEffects,
                    group.Id,
                    productIds,
                    Domain.Events.EconomicEffectType.ConsumptionMultiplier));

            var desiredReserve = group.CalculateDesiredReserve(
                consumptionDemand,
                ResolveMultipliers(
                    activeEffects,
                    group.Id,
                    productIds,
                    Domain.Events.EconomicEffectType.DesiredReserveCoverageMultiplier));

            var reserveDemand = group.CalculateReserveDemand(desiredReserve);
            foreach (var demand in reserveDemand)
            {
                var finalQuantity = demand.Value * ResolveMultiplier(
                    activeEffects,
                    group.Id,
                    demand.Key,
                    Domain.Events.EconomicEffectType.DemandMultiplier);

                if (finalQuantity <= 0m)
                    continue;

                result.Add(new TransferRequest(
                    group.Id,
                    demand.Key,
                    finalQuantity,
                    quantity =>
                    {
                        var receiveResult = group.ReceiveReserveStock(demand.Key, quantity);
                        if (!receiveResult.IsSuccess)
                            throw new InvalidOperationException(receiveResult.Error);
                    }));
            }
        }

        return result;
    }

    private static IReadOnlyList<TransferRequest> BuildProducerReserveRequests(
        SimulationContext ctx,
        int settlementId,
        IReadOnlyList<Domain.Events.EconomicEffect> activeEffects)
    {
        if (!ctx.Buildings.TryGetValue(settlementId, out var buildings))
            return [];

        var result = new List<TransferRequest>();
        foreach (var building in buildings.Where(item => item.IsActive))
        {
            if (!ctx.Recipes.TryGetValue(building.RecipeId, out var recipe))
                continue;

            var reserveDemand = building.CalculateReserveDemand(
                recipe,
                ResolveMultipliers(
                    activeEffects,
                    null,
                    recipe.Inputs.Select(item => item.ProductTypeId),
                    Domain.Events.EconomicEffectType.ProducerReserveCoverageMultiplier));

            foreach (var demand in reserveDemand)
            {
                var finalQuantity = demand.Value * ResolveMultiplier(
                    activeEffects,
                    null,
                    demand.Key,
                    Domain.Events.EconomicEffectType.DemandMultiplier);

                if (finalQuantity <= 0m)
                    continue;

                result.Add(new TransferRequest(
                    building.Id,
                    demand.Key,
                    finalQuantity,
                    quantity =>
                    {
                        var receiveResult = building.ReceiveInputReserve(demand.Key, quantity);
                        if (!receiveResult.IsSuccess)
                            throw new InvalidOperationException(receiveResult.Error);
                    }));
            }
        }

        return result;
    }

    private static IReadOnlyDictionary<int, decimal> ResolveMultipliers(
        IReadOnlyList<Domain.Events.EconomicEffect> activeEffects,
        int? populationGroupId,
        IEnumerable<int> productIds,
        Domain.Events.EconomicEffectType effectType)
    {
        return productIds
            .Distinct()
            .ToDictionary(
                productTypeId => productTypeId,
                productTypeId => ResolveMultiplier(activeEffects, populationGroupId, productTypeId, effectType));
    }

    private static decimal ResolveMultiplier(
        IReadOnlyList<Domain.Events.EconomicEffect> activeEffects,
        int? populationGroupId,
        int productTypeId,
        Domain.Events.EconomicEffectType effectType)
    {
        var multiplier = 1m;
        foreach (var effect in activeEffects.Where(effect => effect.EffectType == effectType))
        {
            if (effect.PopulationGroupId.HasValue && effect.PopulationGroupId != populationGroupId)
                continue;

            if (effect.ProductTypeId.HasValue && effect.ProductTypeId != productTypeId)
                continue;

            multiplier *= effect.Value;
        }

        return multiplier;
    }

    private static void FulfillWarehouseRequests(Warehouse warehouse, IEnumerable<DemandRequest> requests)
    {
        var transferRequests = requests
            .Where(request => request.Quantity > 0m)
            .Select(request => new TransferRequest(request.OwnerId, request.ProductTypeId, request.Quantity, _ => { }))
            .ToList();

        FulfillWarehouseRequests(warehouse, transferRequests);
    }

    private static void FulfillWarehouseRequests(Warehouse warehouse, IReadOnlyList<TransferRequest> requests)
    {
        foreach (var productGroup in requests.GroupBy(request => request.ProductTypeId))
        {
            var orderedRequests = productGroup.OrderBy(request => request.OwnerId).ToList();
            var available = warehouse.GetAvailableQuantity(productGroup.Key, QualityGrade.Normal);
            if (available <= 0m)
                continue;

            var totalRequested = orderedRequests.Sum(request => request.Quantity);
            if (totalRequested <= 0m)
                continue;

            var remaining = decimal.Min(available, totalRequested);
            for (var index = 0; index < orderedRequests.Count; index++)
            {
                if (remaining <= 0m)
                    break;

                var request = orderedRequests[index];
                var allocatedQuantity = index == orderedRequests.Count - 1
                    ? decimal.Min(request.Quantity, remaining)
                    : decimal.Min(request.Quantity, remaining * request.Quantity / totalRequested);

                if (allocatedQuantity <= 0m)
                    continue;

                var withdrawal = warehouse.Withdraw(productGroup.Key, allocatedQuantity, QualityGrade.Normal);
                if (!withdrawal.IsSuccess)
                    throw new InvalidOperationException(withdrawal.Error);

                request.Apply(allocatedQuantity);
                remaining -= allocatedQuantity;
            }
        }
    }

    private sealed record TransferRequest(
        int OwnerId,
        int ProductTypeId,
        decimal Quantity,
        Action<decimal> Apply);
}
