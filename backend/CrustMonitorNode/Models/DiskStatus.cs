namespace CrustMonitorNode.Models
{
    public class DiskStatus
    {
        public double Total { get; set; }
        public double Available { get; set; }
        public bool Ready { get; set; } = false;
        public string VolumeLabel { get; set; }
    }
}