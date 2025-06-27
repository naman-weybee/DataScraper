using DataScraper.Entities;
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
                CollectCategories($"{URL}/{currentPage}");
        }

        private void CollectCategories(string url)
        {
            var categoryList = new List<KeyValuePair<string, string>>();

            var results = HttpClientLoader.Run(url);
            var categoryNodes = results.DocumentNode.SelectNodes(XPath.Categories.CategoryNodes);

            foreach (var categoryNode in categoryNodes!)
            {
                var name = categoryNode.SelectSingleNode($".{XPath.Categories.CategoryName}")?.InnerText?.Trim();
                var categoryUrl = categoryNode.SelectSingleNode($".{XPath.Categories.CategoryUrl}")?.GetAttributeValue("href", string.Empty);

                if (string.IsNullOrWhiteSpace(categoryUrl))
                    continue;

                categoryUrl = string.Format(BaseURL, categoryUrl);
                categoryList.Add(new KeyValuePair<string, string>(name!, categoryUrl));
            }

            foreach (var category in categoryList)
            {
                var item = new Category()
                {
                    Name = category.Key,
                    Description = $"This is {category.Key}"
                };

                UpsertCategory(item);
            }
        }

        public static void UpsertCategory(Category category)
        {
            var existingCategory = GetCategoryById(category.Name);

            using var connection = new SqlConnection(ConnectionString);
            connection.Open();

            if (category.IsDeleted)
            {
                Console.WriteLine($"Category: {category.Name} is Deleted");

                return;
            }

            if (existingCategory == null)
            {
                var insertSql = "INSERT INTO Categories (Id, ParentCategoryId, Name, Description, CreatedDate, UpdatedDate, DeletedDate, IsDeleted) VALUES (@Id, @ParentCategoryId, @Name, @Description, @CreatedDate, @UpdatedDate, @DeletedDate, @IsDeleted)";
                using var insertCommand = new SqlCommand(insertSql, connection);
                insertCommand.Parameters.Add("@Id", SqlDbType.UniqueIdentifier, 100).Value = category.Id;
                insertCommand.Parameters.Add("@ParentCategoryId", SqlDbType.UniqueIdentifier, 100).Value = category.ParentCategoryId ?? (object)DBNull.Value;
                insertCommand.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = category.Name;
                insertCommand.Parameters.Add("@Description", SqlDbType.NVarChar, 500).Value = category.Description;
                insertCommand.Parameters.Add("@CreatedDate", SqlDbType.SmallDateTime, 100).Value = category.CreatedDate;
                insertCommand.Parameters.Add("@UpdatedDate", SqlDbType.SmallDateTime, 100).Value = category.UpdatedDate;
                insertCommand.Parameters.Add("@DeletedDate", SqlDbType.SmallDateTime, 100).Value = category.DeletedDate ?? (object)DBNull.Value;
                insertCommand.Parameters.Add("@IsDeleted", SqlDbType.Bit, 100).Value = category.IsDeleted;

                int rowsInserted = insertCommand.ExecuteNonQuery();
                if (rowsInserted != 1)
                    Console.WriteLine("Insert failed.");
            }
            else
            {
                var updateSql = "UPDATE Categories SET Id = @Id, ParentCategoryId = @ParentCategoryId, Name = @Name, Description = @Description, CreatedDate = GETDATE(), UpdatedDate = GETDATE(), DeletedDate = NULL, IsDeleted = 0 WHERE Name = @Name";
                using var updateCommand = new SqlCommand(updateSql, connection);
                updateCommand.Parameters.Add("@Id", SqlDbType.UniqueIdentifier, 100).Value = category.Id;
                updateCommand.Parameters.Add("@ParentCategoryId", SqlDbType.UniqueIdentifier, 100).Value = category.ParentCategoryId ?? (object)DBNull.Value;
                updateCommand.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = category.Name;
                updateCommand.Parameters.Add("@Description", SqlDbType.NVarChar, 500).Value = category.Description;
                updateCommand.Parameters.Add("@CreatedDate", SqlDbType.SmallDateTime, 100).Value = category.CreatedDate;
                updateCommand.Parameters.Add("@UpdatedDate", SqlDbType.SmallDateTime, 100).Value = category.UpdatedDate;
                updateCommand.Parameters.Add("@DeletedDate", SqlDbType.SmallDateTime, 100).Value = category.DeletedDate ?? (object)DBNull.Value;
                updateCommand.Parameters.Add("@IsDeleted", SqlDbType.Bit, 100).Value = category.IsDeleted;

                int rowsUpdated = updateCommand.ExecuteNonQuery();
                if (rowsUpdated != 1)
                    Console.WriteLine("Update failed.");
            }
        }

        public static Category? GetCategoryById(string name)
        {
            using var connection = new SqlConnection(ConnectionString);
            connection.Open();

            var sql = "SELECT * FROM Categories WHERE Name = @Name";
            using var command = new SqlCommand(sql, connection);
            command.Parameters.Add("@Name", SqlDbType.NVarChar).Value = name;

            using var reader = command.ExecuteReader();

            if (reader.Read())
            {
                return new Category
                {
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Description = reader.GetString(reader.GetOrdinal("Description")),
                };
            }

            return null;
        }
    }
}