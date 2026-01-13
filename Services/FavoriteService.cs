using Newtonsoft.Json;
using System.IO;

namespace PromptRunner.Services
{
    public class FavoriteService : IFavoriteService
    {
        private readonly string _favoriteFilePath;

        public FavoriteService()
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "PromptRunner"
            );

            _favoriteFilePath = Path.Combine(directory, "favorites.json");

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        public void SaveFavorites(List<string> itemIds)
        {
            var json = JsonConvert.SerializeObject(itemIds, Formatting.Indented);
            File.WriteAllText(_favoriteFilePath, json);
        }

        public List<string> GetFavorites()
        {
            List<string> favorites;

            if (!File.Exists(_favoriteFilePath))
            {
                // 初回起動時：デフォルトのお気に入りを設定
                favorites = new List<string>
                {
                    "translation_word_meaning", // 文言の意味を調べる
                    "writing_refine"            // 文章構成
                };
                SaveFavorites(favorites);
                return favorites;
            }

            try
            {
                var json = File.ReadAllText(_favoriteFilePath);
                favorites = JsonConvert.DeserializeObject<List<string>>(json) ?? new List<string>();

                // お気に入りが空の場合もデフォルトを設定
                if (favorites.Count == 0)
                {
                    favorites = new List<string>
                    {
                        "translation_word_meaning", // 文言の意味を調べる
                        "writing_refine"            // 文章構成
                    };
                    SaveFavorites(favorites);
                }

                return favorites;
            }
            catch
            {
                return new List<string>();
            }
        }
    }
}
