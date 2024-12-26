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
        private static readonly Dictionary<int, string> OperationIdToCodemap = new Dictionary<int, string> {
            { 3074409, "/Biala01"},
            { 3074424, "/Biala02"},
            { 3074423, "/Biala03"},
            { 3074422, "/Biala04"},
            { 3074421, "/Biala05"},
            { 3074420, "/Biala06"},
            { 3074419, "/Biala07"},
            { 3074418, "/Biala08"},
            { 3074417, "/Biala09"},
            { 3213864, "/Opole01" },
            { 3213865, "/Opole02" },
            { 3213866, "/Opole03" },
            { 3213867, "/Opole04" },
            { 8414, "/Rzeszow01" },
            { 3062274, "/Rzeszow04" },
            { 3062276, "/Rzeszow06"}
        };

        public static string GetCodeByOperationId(int operationId)
        {
            return OperationIdToCodemap.TryGetValue(operationId, out var code) ? code : string.Empty;
        }

        private static readonly List<SiteMapping> SiteList = new List<SiteMapping>
        {
            new SiteMapping
            {
                SiteIdentifier = "Gdansk",
                SiteName = "POMORSKI URZĄD WOJEWÓDZKI W GDAŃSKU",
                CodeMapping = new Dictionary<string, string>
                {
                    { "Zezwolenie na pobyt (stały, czasowy), rezydenta, wymiana karty, dokumenty dla cudzoziemców", "/Gdansk01" }
                }
            },
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
                    { "Karta Polaka - złożenie wniosku o wymianę / przedłużenie / wydanie duplikatu / odbiór ", "/Opole04"}
                }
            },

            new SiteMapping
            {
                SiteIdentifier = "Rzeszow",
                SiteName = "Podkarpacki Urząd Wojewódzki w Rzeszowie",
                CodeMapping = new Dictionary<string, string>
                {
                    { "1. Odbiór paszportów)", "/Rzeszow01" },
                    { "4. Składanie wniosków w sprawach obywatelstwa polskiego (nadanie, zrzeczenie, uznanie, potwierdzenie posiadania) - pokój 326, III piętro", "/Rzeszow04" },
                    { "6. Złożenie wniosku przez obywateli UE oraz członków ich rodzin (NIE DOT. OB. POLSKICH I CZŁONKÓW ICH RODZIN); złożenie wniosku o wymianę dokumentu, przedłużenie wizy; zaproszenie", "/Rzeszow06" }
                }
            },

            new SiteMapping
            {
                SiteIdentifier = "Olsztyn",
                SiteName = "Warmińsko-Mazurski Urząd Wojewódzki w Olsztynie",
                CodeMapping = new Dictionary<string, string>
                {
                    { "WMUW Karta Polaka", "/OlsztynKP" }
                }
            }
        };

        public static readonly Dictionary<string, SiteMapping> Sites = SiteList.ToDictionary(site => site.SiteIdentifier);

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

        public static string GetSiteIdentifierByKey(string key)
        {
            foreach (var site in Sites.Values)
            {

                if (site.CodeMapping.TryGetValue(key, out var result))
                {
                    return site.SiteIdentifier;
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

        public static string GetButtonCode(string siteIdentifier, string buttonText)
        {
            if (Sites.TryGetValue(siteIdentifier, out var site) &&
                site.CodeMapping.TryGetValue(buttonText, out var code))
            {
                return code;
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
    }
}
