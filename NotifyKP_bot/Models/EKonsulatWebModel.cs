using Newtonsoft.Json;

namespace BezKolejki_bot.Models
{
    public class FirstPostRequestModel
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("iloscZnakow")]
        public int IloscZnakow { get; set; }

        [JsonProperty("image")]
        public string Image { get; set; } = string.Empty;

        [JsonProperty("kod")]
        public string? Kod { get; set; }
    }

    public class SecondPostResponseModel
    {
        [JsonProperty("ok")]
        public bool Ok { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; } = string.Empty;
    }

    public class ThirdPostResponseModel
    {
        [JsonProperty("listaTerminow")]
        public List<ListaTerminow>? ListaTerminow { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; } = string.Empty;
    }

    public class ListaTerminow
    {
        [JsonProperty("idTerminu")]
        public int IdTerminu { get; set; } = 0;

        [JsonProperty("data")]
        public string Data { get; set; } = string.Empty;

        [JsonProperty("godzina")]
        public string Godzina { get; set; } = string.Empty;
    }

    public class FourthRezerwacjePostPayloadModel
    {
        [JsonProperty("id_terminu")]
        public int IdTerminu { get; set; } = 0;

        [JsonProperty("id_wersji_jezykowej")]
        public int IdWersjiJezykowej { get; set; } = 1;

        [JsonProperty("liczba_osob")]
        public int LiczbaOsob { get; set; } = 1;

        [JsonProperty("token")]
        public string Token { get; set; } = string.Empty;
    }

    public class FourthRezerwacjePostResponseModel
    {
        [JsonProperty("bilet")]
        public string Bilet { get; set; } = string.Empty;

        [JsonProperty("listaBiletow")]
        public List<FourthRezerwacjeListaBiletowPostResponseModel> ListaBiletow { get; set; } = new List<FourthRezerwacjeListaBiletowPostResponseModel>();
    }

    public class FourthRezerwacjeListaBiletowPostResponseModel
    {
        [JsonProperty("bilet")]
        public string Bilet { get; set; } = string.Empty;

        [JsonProperty("data")]
        public string Data { get; set; } = string.Empty;

        [JsonProperty("godzina")]
        public string Godzina { get; set; } = string.Empty;
    }

    public class FivePostDaneKartaPolakaPayLoadModel
    {
        [JsonProperty("bilet")]
        public string Bilet { get; set; } = string.Empty;

        [JsonProperty("idWersjiJezykowej")]
        public int IdWersjiJezykowej { get; set; } = 1;

        [JsonProperty("daneFormularza")]
        public FivePostDaneKartaPolakaDaneFormularzaPayLoadModel DaneFormularza { get; set; } = new FivePostDaneKartaPolakaDaneFormularzaPayLoadModel();
    }

    public class FivePostDaneKartaPolakaDaneFormularzaPayLoadModel
    {
        [JsonProperty("imie1")]
        public string Imie1 { get; set; } = string.Empty;

        [JsonProperty("nazwisko1")]
        public string Nazwisko1 { get; set; } = string.Empty;

        [JsonProperty("dataUrodzenia")]
        public string DataUrodzenia { get; set; } = string.Empty;

        [JsonProperty("obywatelstwo")]
        public string Obywatelstwo { get; set; } = string.Empty;

        [JsonProperty("obywatelstwoICAO")]
        public string ObywatelstwoICAO { get; set; } = string.Empty;

        [JsonProperty("plec")]
        public string Plec { get; set; } = string.Empty;

        [JsonProperty("numerPaszportu")]
        public string NumerPaszportu { get; set; } = string.Empty;

        [JsonProperty("numerIdentyfikacyjny")]
        public string NumerIdentyfikacyjny { get; set; } = string.Empty;

        [JsonProperty("ulica")]
        public string Ulica { get; set; } = string.Empty;

        [JsonProperty("nrDomu")]
        public string NrDomu { get; set; } = string.Empty;

        [JsonProperty("kodPocztowy")]
        public string KodPocztowy { get; set; } = string.Empty;

        [JsonProperty("miejscowosc")]
        public string Miejscowosc { get; set; } = string.Empty;

        [JsonProperty("telefon")]
        public string Telefon { get; set; } = string.Empty;

        [JsonProperty("email")]
        public string Email { get; set; } = string.Empty;

        [JsonProperty("opisSprawy")]
        public string OpisSprawy { get; set; } = string.Empty;
    }

    public class FivePostDaneKartaPolakaDaneFormularzaResponseModel
    {
        [JsonProperty("guid")]
        public string Guid { get; set; } = string.Empty;

        [JsonProperty("numerFormularza")]
        public string NumerFormularza { get; set; } = string.Empty;

        [JsonProperty("kod")]
        public string Kod { get; set; } = string.Empty;

        [JsonProperty("wynik")]
        public string Wynik { get; set; } = string.Empty;
    }

    public class PdfResponse
    {
        [JsonProperty("numerFormularza")]
        public string NumerFormularza { get; set; } = string.Empty;

        [JsonProperty("pdf")]
        public string Pdf { get; set; } = string.Empty;

        [JsonProperty("haslo")]
        public string Haslo { get; set; } = string.Empty;
    }
}
