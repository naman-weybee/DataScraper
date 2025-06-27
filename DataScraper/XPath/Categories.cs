namespace DataScraper.XPath
{
    public static class Categories
    {
        public static string CategoryNodes { get; } = "//table[@id=\"tagstable\"]//tr";
        public static string CategoryName { get; } = "//td[not(@class)]//a";
        public static string CategoryUrl { get; } = "//td[not(@class)]//a";
        public static string TotalPagesXPath { get; } = "(//ul[@id=\"pages\"]//a)[last()-1]";
    }
}