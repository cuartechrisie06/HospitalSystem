namespace HospitalSystem.Models
{
    public class Bed
    {
        public int Id { get; set; }
        public string RoomNo { get; set; }
        public string BedNo { get; set; }
        public string Ward { get; set; }
        public bool IsOccupied { get; set; }

        public string Label
        {
            get { return "Room " + RoomNo + " / Bed " + BedNo + " (" + Ward + ")"; }
        }

        public string Status
        {
            get { return IsOccupied ? "Occupied" : "Available"; }
        }

        public override string ToString()
        {
            return Label;
        }
    }
}
