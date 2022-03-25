namespace Producer.Models.BookingCommands
{
    public class HotelBookingCmd : Command
    {
        public string Name { get; }
        public Location Location { get; }
        public string Type => "HotelBookingCommand";

        public HotelBookingCmd(string hotelName, Location location)
        {
            Name = hotelName;
            Location = location;
        }
    }

    public enum Location
    {
        EAST, WEST
    }
}
