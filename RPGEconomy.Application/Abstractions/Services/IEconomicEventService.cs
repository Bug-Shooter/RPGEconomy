using RPGEconomy.Application.DTOs;
using RPGEconomy.Domain.Common;

namespace RPGEconomy.Application.Abstractions.Services;

public interface IEconomicEventService
{
    Task<Result<IReadOnlyList<EconomicEventDto>>> GetBySettlementIdAsync(int settlementId);
    Task<Result<EconomicEventDto>> GetByIdAsync(int id);
    Task<Result<EconomicEventDto>> CreateAsync(
        int settlementId,
        string name,
        bool isEnabled,
        int startDay,
        int? endDay,
        IReadOnlyList<EconomicEffectDto> effects);
    Task<Result<EconomicEventDto>> UpdateAsync(
        int id,
        string name,
        bool isEnabled,
        int startDay,
        int? endDay,
        IReadOnlyList<EconomicEffectDto> effects);
    Task<Result> ActivateAsync(int id);
    Task<Result> DeactivateAsync(int id);
}
