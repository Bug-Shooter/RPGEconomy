using RPGEconomy.Domain.Events;
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
                        EconomicEffectType.ConsumptionMultiplier));

                var unmetDemand = group.ConsumeFromStock(consumptionDemand);
                foreach (var request in unmetDemand)
                {
                    var demandMultiplier = ResolveMultiplier(
                        activeEffects,
                        group.Id,
                        request.Key,
                        EconomicEffectType.DemandMultiplier);

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

            if (!ctx.Warehouses.TryGetValue(settlement.Id, out var warehouse))
                continue;

            var activeEffects = ctx.GetActiveEffects(settlement.Id);
            var startingSupplyByProduct = CaptureStartingSupply(warehouse);
            var householdReserveRequests = BuildHouseholdReserveRequests(ctx, settlement.Id, activeEffects);
            var producerReserveRequests = BuildProducerReserveRequests(ctx, settlement.Id, activeEffects);
            var demandByProduct = BuildGrossDemand(
                ctx,
                settlement.Id,
                householdReserveRequests,
                producerReserveRequests);

            foreach (var offer in market.Offers)
            {
                var supply = startingSupplyByProduct.GetValueOrDefault(offer.ProductTypeId, 0m);
                var demand = demandByProduct.GetValueOrDefault(offer.ProductTypeId, 0m);
                market.UpdateProductState(offer.ProductTypeId, supply, demand);
            }

            FulfillWarehouseRequests(warehouse, ctx.GetConsumptionDemandRequests(settlement.Id));
            FulfillWarehouseRequests(warehouse, householdReserveRequests);
            FulfillWarehouseRequests(warehouse, producerReserveRequests);
        }
    }

    private static IReadOnlyDictionary<int, decimal> CaptureStartingSupply(Warehouse warehouse) =>
        warehouse.Items
            .GroupBy(item => item.ProductTypeId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));

    private static IReadOnlyDictionary<int, decimal> BuildGrossDemand(
        SimulationContext ctx,
        int settlementId,
        IReadOnlyList<TransferRequest> householdReserveRequests,
        IReadOnlyList<TransferRequest> producerReserveRequests)
    {
        var demandByProduct = ctx.GetConsumptionDemand(settlementId)
            .ToDictionary(item => item.Key, item => item.Value);

        foreach (var productionDemand in ctx.GetProductionDemand(settlementId))
        {
            demandByProduct[productionDemand.Key] =
                demandByProduct.GetValueOrDefault(productionDemand.Key, 0m) + productionDemand.Value;
        }

        foreach (var reserveRequest in householdReserveRequests.Concat(producerReserveRequests))
        {
            demandByProduct[reserveRequest.ProductTypeId] =
                demandByProduct.GetValueOrDefault(reserveRequest.ProductTypeId, 0m) + reserveRequest.Quantity;
        }

        return demandByProduct;
    }

    private static IReadOnlyList<TransferRequest> BuildHouseholdReserveRequests(
        SimulationContext ctx,
        int settlementId,
        IReadOnlyList<EconomicEffect> activeEffects)
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
                    EconomicEffectType.ConsumptionMultiplier));

            var desiredReserve = group.CalculateDesiredReserve(
                consumptionDemand,
                ResolveMultipliers(
                    activeEffects,
                    group.Id,
                    productIds,
                    EconomicEffectType.DesiredReserveCoverageMultiplier));

            var reserveDemand = group.CalculateReserveDemand(desiredReserve);
            foreach (var demand in reserveDemand)
            {
                var finalQuantity = demand.Value * ResolveMultiplier(
                    activeEffects,
                    group.Id,
                    demand.Key,
                    EconomicEffectType.DemandMultiplier);

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
        IReadOnlyList<EconomicEffect> activeEffects)
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
                    EconomicEffectType.ProducerReserveCoverageMultiplier));

            foreach (var demand in reserveDemand)
            {
                var finalQuantity = demand.Value * ResolveMultiplier(
                    activeEffects,
                    null,
                    demand.Key,
                    EconomicEffectType.DemandMultiplier);

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
        IReadOnlyList<EconomicEffect> activeEffects,
        int? populationGroupId,
        IEnumerable<int> productIds,
        EconomicEffectType effectType)
    {
        return productIds
            .Distinct()
            .ToDictionary(
                productTypeId => productTypeId,
                productTypeId => ResolveMultiplier(activeEffects, populationGroupId, productTypeId, effectType));
    }

    private static decimal ResolveMultiplier(
        IReadOnlyList<EconomicEffect> activeEffects,
        int? populationGroupId,
        int productTypeId,
        EconomicEffectType effectType)
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
            var orderedRequests = productGroup
                .Where(request => request.Quantity > 0m)
                .OrderBy(request => request.OwnerId)
                .ToList();

            if (orderedRequests.Count == 0)
                continue;

            var available = warehouse.GetAvailableQuantity(productGroup.Key, QualityGrade.Normal);
            if (available <= 0m)
                continue;

            var totalRequested = orderedRequests.Sum(request => request.Quantity);
            if (totalRequested <= 0m)
                continue;

            foreach (var allocation in AllocateProRata(orderedRequests, available, totalRequested))
            {
                if (allocation.Quantity <= 0m)
                    continue;

                var withdrawal = warehouse.Withdraw(productGroup.Key, allocation.Quantity, QualityGrade.Normal);
                if (!withdrawal.IsSuccess)
                    throw new InvalidOperationException(withdrawal.Error);

                allocation.Request.Apply(allocation.Quantity);
            }
        }
    }

    private static IReadOnlyList<AllocatedTransfer> AllocateProRata(
        IReadOnlyList<TransferRequest> requests,
        decimal available,
        decimal totalRequested)
    {
        if (available >= totalRequested)
        {
            return requests
                .Select(request => new AllocatedTransfer(request, request.Quantity))
                .ToArray();
        }

        var allocations = requests
            .Select(request => new AllocationState(
                request,
                decimal.Min(request.Quantity, available * request.Quantity / totalRequested)))
            .ToList();

        var allocatedTotal = allocations.Sum(item => item.AllocatedQuantity);
        var remainder = decimal.Max(available - allocatedTotal, 0m);

        if (remainder > 0m)
        {
            foreach (var allocation in allocations
                         .Where(item => item.AllocatedQuantity < item.Request.Quantity)
                         .OrderByDescending(item => item.Request.Quantity - item.AllocatedQuantity)
                         .ThenBy(item => item.Request.OwnerId))
            {
                if (remainder <= 0m)
                    break;

                var additional = decimal.Min(allocation.Request.Quantity - allocation.AllocatedQuantity, remainder);
                if (additional <= 0m)
                    continue;

                allocation.AllocatedQuantity += additional;
                remainder -= additional;
            }
        }

        return allocations
            .Where(item => item.AllocatedQuantity > 0m)
            .Select(item => new AllocatedTransfer(item.Request, item.AllocatedQuantity))
            .ToArray();
    }

    private sealed record TransferRequest(
        int OwnerId,
        int ProductTypeId,
        decimal Quantity,
        Action<decimal> Apply);

    private sealed record AllocatedTransfer(TransferRequest Request, decimal Quantity);

    private sealed class AllocationState
    {
        public AllocationState(TransferRequest request, decimal allocatedQuantity)
        {
            Request = request;
            AllocatedQuantity = allocatedQuantity;
        }

        public TransferRequest Request { get; }
        public decimal AllocatedQuantity { get; set; }
    }
}
