using System;
using Entities.Models;

namespace Contracts;

public interface IUserRepository
{
    Task<IEnumerable<User>> GetAllUsersAsync(bool trackChanges);
    Task<User> GetUserByEmailAsync(string email, bool trackChanges);
    Task<User> GetUserByIdAsync(string id, bool trackChanges);
    void CreateUser(User user);
}
