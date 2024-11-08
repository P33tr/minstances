namespace minstances.Models
{
    public class InstanceStatusVm
    {
        public InstanceStatusVm()
        {
            InstanceStatuses = new List<InstanceStatuss>();
        }
        public string Error { get; set; }

        public List<InstanceStatuss> InstanceStatuses { get; set; }
    }
}