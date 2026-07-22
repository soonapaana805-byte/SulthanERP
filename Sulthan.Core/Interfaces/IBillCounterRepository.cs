using Sulthan.Core.Entities;

namespace Sulthan.Core.Interfaces;

public interface IBillCounterRepository
{
    Task<string> GetNextBillNumberAsync();
}