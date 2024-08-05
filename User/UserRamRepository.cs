
using System.Collections.Concurrent;

namespace TelegramAIBot.User;

class UserRamRepository : IUserRepository
{
    private readonly ConcurrentDictionary<long, UserProfile> _profiles = [];

	public Task<bool> ContainsAsync(long id)
	{
		return Task.FromResult(_profiles.ContainsKey(id));
	}


	public Task<UserProfile> GetAsync(long id)
	{
		return Task.FromResult(_profiles.GetOrAdd(id, (id) => new UserProfile(id))); 
	}

	public Task SetAsync(UserProfile profile)
	{
        _profiles.AddOrUpdate(profile.Id, profile, (id, current) => profile);
        return Task.CompletedTask;
	}
}