namespace BezKolejki_bot.Models
{
        public class FirstPostRequestModel
        {
            public string id { get; set; } = string.Empty;
            public int iloscZnakow { get; set; }
            public string image { get; set; } = string.Empty;
            public string? kod { get; set; }
        }
        public class SecondPostResponseModel
        {
            public bool ok { get; set; }
            public string token { get; set; } = string.Empty;
        }

        public class ThirdPostResponseModel
        {
            public List<ListaTerminow>? listaTerminow { get; set; }
            public string token { get; set; } = string.Empty;
        }

        public class ListaTerminow
        {
            public int idTerminu { get; set; } = 0;
            public string data { get; set; } = string.Empty;
            public string godzina { get; set; } = string.Empty;
        }

        public class FourthRezerwacjePostPayloadModel
        {
            public int id_terminu { get; set; } = 0;
            public int id_wersji_jezykowej { get; set; } = 1;
            public int liczba_osob { get; set; } = 1;
            public string token { get; set; } = string.Empty;
        }

        public class FourthRezerwacjePostResponseModel
        {
            public string bilet { get; set; } = string.Empty;

            public List<FourthRezerwacjeListaBiletowPostResponseModel> listaBiletow { get; set; }

        }

        public class FourthRezerwacjeListaBiletowPostResponseModel
        {
            public string bilet { get; set; } = string.Empty;
            public string data { get; set; } = string.Empty;
            public string godzina { get; set; } = string.Empty;
        }

        public class FivePostDaneKartaPolakaPayLoadModel
        {
            public string bilet { get; set; } = string.Empty;
            public int idWersjiJezykowej { get; set; } = 1;
            public FivePostDaneKartaPolakaDaneFormularzaPayLoadModel daneFormularza { get; set; }
        }

        public class FivePostDaneKartaPolakaDaneFormularzaPayLoadModel
        {
            public string imie1 { get; set; } = string.Empty;
            public string nazwisko1 { get; set; } = string.Empty;
            public string dataUrodzenia { get; set; } = string.Empty;
            public string obywatelstwo { get; set; } = string.Empty;
            public string obywatelstwoICAO { get; set; } = string.Empty;
            public string plec { get; set; } = string.Empty;
            public string numerPaszportu { get; set; } = string.Empty;
            public string numerIdentyfikacyjny { get; set; } = string.Empty;
            public string ulica { get; set; } = string.Empty;
            public string nrDomu { get; set; } = string.Empty;
            public string kodPocztowy { get; set; } = string.Empty;
            public string miejscowosc { get; set; } = string.Empty;
            public string telefon { get; set; } = string.Empty;
            public string email { get; set; } = string.Empty;
            public string opisSprawy { get; set; } = string.Empty;
        }

        public class FivePostDaneKartaPolakaDaneFormularzaResponseModel
        {
            public string guid { get; set; } = string.Empty;
            public string numerFormularza { get; set; } = string.Empty;
            public string kod { get; set; } = string.Empty;
            public string wynik { get; set; } = string.Empty;
        }
}
