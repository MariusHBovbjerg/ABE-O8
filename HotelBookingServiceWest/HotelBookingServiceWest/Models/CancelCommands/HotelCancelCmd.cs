using Consumer.Models.BookingCommands;

namespace Consumer.Models.CancelCommands
{
    public class HotelCancelCmd : Command
    {
        public new string Name { get; set; }
        public Location Location { get; }
        public new const string Type = "HotelCancelCommand";

        public HotelCancelCmd(string hotelName, Location location)
        {
            Name = hotelName;
            Location = location;
        }
    }
}