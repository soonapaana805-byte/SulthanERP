using Sulthan.Core.Entities;

namespace Sulthan.Core.Interfaces;

public interface IJwtService
{
    string GenerateToken(User user);
}