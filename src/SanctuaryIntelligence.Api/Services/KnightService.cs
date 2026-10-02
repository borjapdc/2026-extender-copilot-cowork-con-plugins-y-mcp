using SanctuaryIntelligence.Api.Data;
using SanctuaryIntelligence.Api.Models;

namespace SanctuaryIntelligence.Api.Services;

public class KnightService
{
    public IReadOnlyList<Knight> GetAll() => SanctuaryData.GetKnights();

    public Knight? GetByName(string name) => SanctuaryData.GetKnightByName(name);
}
