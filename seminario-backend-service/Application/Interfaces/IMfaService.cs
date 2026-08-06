using Seminario.Domain.Entities;

namespace Seminario.Application.Interfaces;

public interface IMfaService
{
    Task<string> GenerateAsync(int userId);
    Task<User> VerifyAsync(string mfaToken, string code);
}
