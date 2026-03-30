using Dapper;
using RPGEconomy.Application.Abstractions.Repositories;
using RPGEconomy.Domain.Markets;
using RPGEconomy.Infrastructure.Persistence.Queries;
using System.Data;
using System.Transactions;

namespace RPGEconomy.Infrastructure.Persistence.Repositories;

public class MarketRepository : IMarketRepository
{
    private readonly IDbConnectionFactory _factory;

    public MarketRepository(IDbConnectionFactory factory)
        => _factory = factory;

    public async Task<Market?> GetByIdAsync(int id)
    {
        using var conn = _factory.Create();

        var market = await conn.QueryFirstOrDefaultAsync<Market>(MarketQueries.GetById, new { Id = id });
        if (market is null)
            return null;

        await LoadOffersAsync(conn, market);
        return market;
    }

    public async Task<Market?> GetBySettlementIdAsync(int settlementId)
    {
        using var conn = _factory.Create();

        var market = await conn.QueryFirstOrDefaultAsync<Market>(
            MarketQueries.GetBySettlementId,
            new { SettlementId = settlementId });
        if (market is null)
            return null;

        await LoadOffersAsync(conn, market);
        return market;
    }

    public async Task<int> SaveAsync(Market market)
    {
        using var conn = _factory.Create();
        if (conn.State != ConnectionState.Open)
            conn.Open();

        var useLocalTransaction = Transaction.Current is null;
        using var tx = useLocalTransaction ? conn.BeginTransaction() : null;

        int marketId;
        if (market.IsNew)
        {
            marketId = await conn.ExecuteScalarAsync<int>(MarketQueries.Insert, new { market.SettlementId }, tx);
        }
        else
        {
            marketId = market.Id;
        }

        await conn.ExecuteAsync(MarketQueries.DeleteOffers, new { MarketId = marketId }, tx);

        if (market.Offers.Any())
        {
            await conn.ExecuteAsync(
                MarketQueries.InsertOffer,
                market.Offers.Select(offer => new
                {
                    MarketId = marketId,
                    offer.ProductTypeId,
                    offer.CurrentPrice,
                    offer.SupplyVolume,
                    offer.DemandVolume
                }),
                tx);
        }

        tx?.Commit();
        return marketId;
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _factory.Create();
        if (conn.State != ConnectionState.Open)
            conn.Open();

        var useLocalTransaction = Transaction.Current is null;
        using var tx = useLocalTransaction ? conn.BeginTransaction() : null;
        await conn.ExecuteAsync(MarketQueries.Delete, new { Id = id }, tx);
        tx?.Commit();
    }

    private static async Task LoadOffersAsync(IDbConnection conn, Market market)
    {
        var offers = await conn.QueryAsync<MarketOffer>(MarketQueries.GetOffers, new { MarketId = market.Id });
        market.LoadOffers(offers);
    }
}
