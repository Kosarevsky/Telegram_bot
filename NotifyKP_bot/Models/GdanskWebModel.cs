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
        public GdanskTakeAppointmentResponse RESPONSE { get; set; }
    }

    public class GdanskTakeAppointmentResponse
    {
        public TakeAppointmentResult TakeAppointmentResult { get; set; }

        public AppointmentTicketInfo AppointmentTicketInfo { get; set; }

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

        public GdanskTakeAppointmentService Service { get; set; }

        public string TicketNumber { get; set; } = string.Empty;

        [JsonProperty("code")]
        public string Code {  get; set; } = string.Empty;
    }

    public class GdanskTakeAppointmentService
    {
        public string Name { get; set; } = string.Empty;
    }
}
