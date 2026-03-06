using System;
using Contracts;
using Entities.Models;
using Microsoft.EntityFrameworkCore;

namespace Repository;

public class UserRepository : RepositoryBase<User>, IUserRepository
{
    public UserRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }

    public void CreateUser(User user) => Create(user);

    public async Task<IEnumerable<User>> GetAllUsersAsync(bool trackChanges) =>
        await FindAll(trackChanges)
        .ToListAsync();


    public async Task<User> GetUserByEmailAsync(string email, bool trackChanges) =>
        await FindByCondition(u => u.Email.Equals(email), trackChanges)
        .SingleOrDefaultAsync();

    public async Task<User> GetUserByIdAsync(string id, bool trackChanges) =>
        await FindByCondition(u => u.Id.Equals(id), trackChanges)
        .SingleOrDefaultAsync();
}
