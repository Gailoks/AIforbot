namespace TelegramAIBot.User;

interface IUserRepository
{
    public Task<UserProfile> GetAsync(long id);
    public Task SetAsync(UserProfile profile);

    public Task<bool> ContainsAsync(long id);
}