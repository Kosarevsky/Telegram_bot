namespace Services.Models
{
    public class SiteMapping
    {
        public string SiteIdentifier { get; set; }
        public string SiteName { get; set; }
        public Dictionary<string, string> CodeMapping { get; set; }
    }

    public static class CodeMapping
    {
        private static readonly List<SiteMapping> SiteList = new List<SiteMapping>
        {
            new SiteMapping
            {
                SiteIdentifier = "Biala",
                SiteName = "Lubelski Urząd Wojewódzki - Delegatura w Białej Podlaskiej",
                CodeMapping = new Dictionary<string, string>
                {
                    { "Karta Polaka - dorośli", "/Biala01" },
                    { "Karta Polaka - dzieci", "/Biala02" },
                    { "Pobyt czasowy - wniosek", "/Biala03" },
                    { "Pobyt czasowy - braki formalne", "/Biala04" },
                    { "Pobyt czasowy - odbiór karty", "/Biala05" },
                    { "Pobyt stały i rezydent - wniosek", "/Biala06" },
                    { "Pobyt stały i rezydent - braki formalne", "/Biala07" },
                    { "Pobyt stały i rezydent - odbiór karty", "/Biala08" },
                    { "Obywatele Unii Europejskiej + Polski Dokument Podróży", "/Biala09" }
                }
            },
            new SiteMapping
            {
                SiteIdentifier = "Opole",
                SiteName = "Rezerwacja kolejki w Opolskim Urzędzie Wojewódzkim",
                CodeMapping = new Dictionary<string, string>
                {
                    { "Wydawanie dokumentów (karty pobytu, zaproszenia)", "/Opole01" },
                    { "Złożenie wniosku: przez ob. UE i członków ich rodzin/na zaproszenie/o wymianę karty pobytu (w przypadku: zmiany danych umieszczonych w posiadanej karcie pobytu, zmiany wizerunku twarzy, utraty, uszkodzenia) oraz uzupełnianie braków formalnych w tych sprawach", "/Opole02" },
                    { "Karta Polaka - złożenie wniosku o przyznanie Karty Polaka", "/Opole03" },
                    { "Karta Polaka - złożenie wniosku o wymianę / przedłużenie / wydanie duplikatu / odbiór", "/Opole04" }
                }
            }
        };

        public static readonly Dictionary<string, SiteMapping> Sites = SiteList.ToDictionary(site => site.SiteIdentifier);

        public static string GetButtonCode(string siteIdentifier, string buttonText)
        {
            if (Sites.TryGetValue(siteIdentifier, out var site) &&
                site.CodeMapping.TryGetValue(buttonText, out var code))
            {
                return code;
            }
            return string.Empty;
        }

        public static string GetSiteNameBySiteIdentifier(string siteIdentifier)
        {
            return Sites.TryGetValue(siteIdentifier, out var site) ? site.SiteName : string.Empty;
        }

        public static string GetSiteIdentifierBySiteName(string siteName)
        {
            return Sites.FirstOrDefault(el => string.Equals(el.Value, siteName)).Key ?? string.Empty;
        }

        public static string GetSiteNameByCode(string code)
        {
            foreach (var site in Sites.Values)
            {
                if (site.CodeMapping.ContainsValue(code))
                {
                    return site.SiteName;
                }
            }
            return string.Empty; 
        }

        public static string GetKeyByCode(string code)
        {
            foreach (var site in Sites.Values)
            {
                var entry = site.CodeMapping.FirstOrDefault(x => x.Value.Equals(code));
                if (!string.IsNullOrEmpty(entry.Key))
                {
                    return entry.Key; 
                }
            }
            return string.Empty; 
        }
        public static string GetValueByKey(string key)
        {
            foreach (var site in Sites.Values)
            {
                if (site.CodeMapping.TryGetValue(key, out var result))
                {
                    return result;
                }
            }
            return string.Empty;
        }

        public static string GetSiteIdentifierByCode(string code)
        {
            foreach (var site in Sites.Values)
            {
                if (site.CodeMapping.ContainsValue(code))
                {
                    return site.SiteIdentifier;
                }
            }
            return string.Empty; 
        }
    }
}
