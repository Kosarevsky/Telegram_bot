
namespace Services.Models
{
    public static class BialaCodeMapping
    {
        public static readonly Dictionary<string, string> buttonCodeMapping = new Dictionary<string, string>()
        {
            {"Karta Polaka - dorośli", "/Biala01" },
            {"Karta Polaka - dzieci", "/Biala02" },
            {"Pobyt czasowy - wniosek", "/Biala03" },
            {"Pobyt czasowy - braki formalne", "/Biala04" },
            {"Pobyt czasowy - odbiór karty", "/Biala05" },
            {"Pobyt stały i rezydent - wniosek", "/Biala06" },
            {"Pobyt stały i rezydent - braki formalne", "/Biala07" },
            {"Pobyt stały i rezydent - odbiór karty", "/Biala08" },
            {"Obywatele Unii Europejskiej + Polski Dokument Podróży", "/Biala09" }
        };
    }
}
