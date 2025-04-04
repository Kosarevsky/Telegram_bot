using Newtonsoft.Json;

namespace BezKolejki_bot.Models
{
    public class GdanskWebModel
    {
        public string MSG { get; set; } = string.Empty;
        public string MSG_FULL { get; set; } = string.Empty;
        public IList<string> DATES { get; set; } = new List<string>();
    }

    public class GdanskTimeWebModel
    {
        public int TIMES_DEBT { get; set; }
        public IList<GdanskTimeItemWebModel> TIMES { get; set; } = new List<GdanskTimeItemWebModel>();
    }
    public class GdanskTimeItemWebModel
    {
        public string time { get; set; } = string.Empty;
        public int slots { get; set; }
        public int reservations_count { get; set; }
        public int max_slots { get; set; }
    }

    public class GdanskTakeAppointmentWebModel
    {
        public GdanskTakeAppointmentResponse RESPONSE { get; set; } = new GdanskTakeAppointmentResponse();
    }

    public class GdanskTakeAppointmentResponse
    {
        public TakeAppointmentResult TakeAppointmentResult { get; set; } = new TakeAppointmentResult();

        public AppointmentTicketInfo AppointmentTicketInfo { get; set; } = new AppointmentTicketInfo();

        [JsonProperty("reservation_id")]
        public int ReservationId { get; set; }
    }

    public class TakeAppointmentResult
    {
        public int Code { get; set; }

        public string Description { get; set; } = string.Empty;
    }

    public class AppointmentTicketInfo
    {
        public string AppointmentDay { get; set; } = string.Empty;

        public string AppointmentTime { get; set; } = string.Empty;

        public GdanskTakeAppointmentService Service { get; set; } = new GdanskTakeAppointmentService();

        public string TicketNumber { get; set; } = string.Empty;

        [JsonProperty("code")]
        public int Code {  get; set; }
    }

    public class GdanskTakeAppointmentService
    {
        public string Name { get; set; } = string.Empty;
    }


    public class GdanskAppointmentRequestWebModel
    {
        public int SedcoBranchID { get; set; }
        public int SedcoServiceID { get; set; }
        public int BranchID { get; set; }
        public int ServiceID { get; set; }
        public string AppointmentDay { get; set; } = string.Empty;
        public string AppointmentTime { get; set; } = string.Empty;

        public GdanskAppointmentCustomerInfoRequestWebModel CustomerInfo { get; set; } = new GdanskAppointmentCustomerInfoRequestWebModel();

        public string LanguagePrefix { get; set; } = "pl";
        public string SelectedLanguage { get; set; } = "pl";
        public string SegmentIdentification { get; set; } = "internet";

    }

    public class GdanskAppointmentCustomerInfoRequestWebModel
    {
        public GdanskAppointmentAdditionalInfoRequest AdditionalInfo { get; set; } = new GdanskAppointmentAdditionalInfoRequest();

    }
    public class GdanskAppointmentAdditionalInfoRequest
    {
        public string CustomerName_L2 { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        [JsonProperty("checkbox")]
        public bool Checkbox { get; set; } = true;
    }
}
