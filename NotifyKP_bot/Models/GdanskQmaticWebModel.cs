namespace BezKolejki_bot.Models
{
    public class GdanskQmaticWebModel
    {
        public string branchName { get; set; }
        public string branchPublicId { get; set; }
        public List<GdanskQmaticServiceGroup> serviceGroups { get; set; }
    }
    public class GdanskQmaticService
    {
        public string publicId { get; set; }
        public string name { get; set; }
        public int duration { get; set; }
        public int additionalCustomerDuration { get; set; }
        public string custom { get; set; }
    }

    public class GdanskQmaticServiceGroup
    {
        public List<GdanskQmaticService> services { get; set; }
    }

    public class GdanskQmaticDateWebModel
    {
        public DateOnly Date { get; set; }
    }

}
