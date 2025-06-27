using DataScraper.XPath;

namespace DataScraper
{
    public static class Program
    {
        public static string BaseURL { get; } = "https://world.openfoodfacts.org{0}";
        public static string CategoryUrl { get; } = "https://world.openfoodfacts.org/facets/categories";

        public static void Main(string[] args)
        {
            try
            {
                var categories = new OperationCategories(CategoryUrl, Categories.TotalPagesXPath);
                categories.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex}");
            }

            Console.WriteLine("---");
            Console.ReadKey();
        }
    }
}