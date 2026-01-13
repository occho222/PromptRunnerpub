namespace PromptRunner.Services
{
    public interface IFavoriteService
    {
        void SaveFavorites(List<string> itemIds);
        List<string> GetFavorites();
    }
}
