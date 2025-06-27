using DataScraper.Entities;
using DataScraper.XPath;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DataScraper
{
    public class OperationCategories
    {
        public static string BaseURL { get; } = "https://world.openfoodfacts.org{0}";
        public string URL { get; set; }
        public string TotalPagesXPath { get; set; }

        private const string ConnectionString = "Server=DESKTOP-B8PQOC6;Database=ECommerce;Encrypt=True;TrustServerCertificate=True;Integrated Security=True;";

        public OperationCategories(string url, string totalPagesXPath)
        {
            URL = url;
            TotalPagesXPath = totalPagesXPath;
        }

        public void Run()
        {
            var page = HttpClientLoader.Run(URL);
            var totalPages = Convert.ToInt32(page.DocumentNode.SelectSingleNode(TotalPagesXPath)?.InnerText?.Trim());

            Console.WriteLine($"Total Pages Found: {totalPages}");

            for (int currentPage = 1; currentPage <= totalPages; currentPage++)
                SaveParentCategories($"{URL}/{currentPage}");
        }

        private List<KeyValuePair<string, string>> CollectCategories(string url, string nodesXPath, string nameXPath, string urlXPath)
        {
            var categoryies = new List<KeyValuePair<string, string>>();

            var results = HttpClientLoader.Run(url);
            var categoryNodes = results.DocumentNode.SelectNodes(nodesXPath);

            if (categoryNodes == null || categoryNodes.Count == 0)
            {
                Console.WriteLine($"No Categories Found on URL: {url}");
                return null!;
            }

            foreach (var categoryNode in categoryNodes)
            {
                var name = categoryNode.SelectSingleNode($".{nameXPath}")?.InnerText?.Trim();
                var categoryUrl = categoryNode.SelectSingleNode($".{urlXPath}")?.GetAttributeValue("href", string.Empty);

                if (string.IsNullOrWhiteSpace(categoryUrl))
                    continue;

                categoryUrl = string.Format(BaseURL, categoryUrl);
                categoryies.Add(new KeyValuePair<string, string>(name!, categoryUrl));
            }

            return categoryies;
        }

        private void SaveParentCategories(string url)
        {
            var categories = CollectCategories(url, Categories.CategoryNodes, Categories.CategoryName, Categories.CategoryUrl);

            if (categories == null || categories.Count == 0)
            {
                Console.WriteLine($"No Parent Categories Found on URL: {url}");
                return;
            }

            foreach (var category in categories)
            {
                var id = Guid.NewGuid();
                var item = new Category()
                {
                    Id = id,
                    Name = category.Key,
                    Description = $"This is {category.Key}"
                };

                UpsertCategory(item);

                var childCategories = CollectCategories(category.Value, Categories.ChildCategoryNodes, Categories.ChildCategoryName, Categories.ChildCategoryUrl);
                var parentCategory = GetCategory(item.Name, null);

                if (childCategories == null || childCategories.Count == 0)
                {
                    Console.WriteLine($"No Child Categories Found on URL: {url}");
                    continue;
                }

                foreach (var childCategory in childCategories)
                {
                    var childItem = new Category()
                    {
                        Id = Guid.NewGuid(),
                        ParentCategoryId = parentCategory?.Id,
                        Name = childCategory.Key,
                        Description = $"This is {childCategory.Key}"
                    };

                    UpsertCategory(childItem);
                }
            }
        }

        public static void UpsertCategory(Category category)
        {
            var existingCategory = GetCategory(category.Name, category.ParentCategoryId);

            using var connection = new SqlConnection(ConnectionString);
            connection.Open();

            if (category.IsDeleted)
            {
                Console.WriteLine($"Category: {category.Name} is Deleted");
                return;
            }

            if (existingCategory == null)
            {
                var insertSql = "INSERT INTO Categories (Id, ParentCategoryId, Name, Description, CreatedDate, UpdatedDate, DeletedDate, IsDeleted) VALUES (@Id, @ParentCategoryId, @Name, @Description, @CreatedDate, @UpdatedDate, @DeletedDate, @IsDeleted);";
                using var insertCommand = new SqlCommand(insertSql, connection);
                insertCommand.Parameters.Add("@Id", SqlDbType.UniqueIdentifier, 100).Value = category.Id;
                insertCommand.Parameters.Add("@ParentCategoryId", SqlDbType.UniqueIdentifier, 100).Value = category.ParentCategoryId ?? (object)DBNull.Value;
                insertCommand.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = category.Name;
                insertCommand.Parameters.Add("@Description", SqlDbType.NVarChar, 500).Value = category.Description;
                insertCommand.Parameters.Add("@CreatedDate", SqlDbType.SmallDateTime, 100).Value = category.CreatedDate;
                insertCommand.Parameters.Add("@UpdatedDate", SqlDbType.SmallDateTime, 100).Value = category.UpdatedDate;
                insertCommand.Parameters.Add("@DeletedDate", SqlDbType.SmallDateTime, 100).Value = category.DeletedDate ?? (object)DBNull.Value;
                insertCommand.Parameters.Add("@IsDeleted", SqlDbType.Bit, 100).Value = category.IsDeleted;

                if (insertCommand.ExecuteNonQuery() == 0)
                    Console.WriteLine("Insert failed.");
            }
            else
            {
                var updateSql = @"UPDATE Categories SET ParentCategoryId = @ParentCategoryId, Name = @Name, Description = @Description, CreatedDate = @CreatedDate, UpdatedDate = @UpdatedDate, DeletedDate = @DeletedDate, IsDeleted = @IsDeleted WHERE Name = @Name AND ( (ParentCategoryId IS NULL AND @ParentCategoryId IS NULL) OR (ParentCategoryId = @ParentCategoryId) );";
                using var updateCommand = new SqlCommand(updateSql, connection);
                updateCommand.Parameters.Add("@ParentCategoryId", SqlDbType.UniqueIdentifier, 100).Value = category.ParentCategoryId ?? (object)DBNull.Value;
                updateCommand.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = category.Name;
                updateCommand.Parameters.Add("@Description", SqlDbType.NVarChar, 500).Value = category.Description;
                updateCommand.Parameters.Add("@CreatedDate", SqlDbType.SmallDateTime, 100).Value = category.CreatedDate;
                updateCommand.Parameters.Add("@UpdatedDate", SqlDbType.SmallDateTime, 100).Value = category.UpdatedDate;
                updateCommand.Parameters.Add("@DeletedDate", SqlDbType.SmallDateTime, 100).Value = category.DeletedDate ?? (object)DBNull.Value;
                updateCommand.Parameters.Add("@IsDeleted", SqlDbType.Bit, 100).Value = category.IsDeleted;

                if (updateCommand.ExecuteNonQuery() == 0)
                    Console.WriteLine("Update failed.");
            }
        }

        public static Category? GetCategory(string name, Guid? parentCategoryId)
        {
            using var connection = new SqlConnection(ConnectionString);
            connection.Open();

            var sql = "SELECT * FROM Categories WHERE Name = @Name AND ( (ParentCategoryId IS NULL AND @ParentCategoryId IS NULL) OR (ParentCategoryId = @ParentCategoryId) )";
            using var command = new SqlCommand(sql, connection);
            command.Parameters.Add("@ParentCategoryId", SqlDbType.UniqueIdentifier).Value = parentCategoryId ?? (object)DBNull.Value;
            command.Parameters.Add("@Name", SqlDbType.NVarChar).Value = name;

            using var reader = command.ExecuteReader();

            if (reader.Read())
            {
                return new Category
                {
                    Id = reader.GetGuid(reader.GetOrdinal("Id")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Description = reader.GetString(reader.GetOrdinal("Description")),
                };
            }

            return null;
        }
    }
}