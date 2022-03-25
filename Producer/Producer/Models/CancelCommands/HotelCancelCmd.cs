using Producer.Models.BookingCommands;

namespace Producer.Models.CancelCommands
{
    public class HotelCancelCmd : Command
    {
        public string Name { get; }
        public Location Location { get; }
        public string Type => "HotelCancelCommand";
        
        public HotelCancelCmd(HotelBookingCmd hotel)
        {
            Name = hotel.Name;
            Location = hotel.Location;
        }
    }
}
