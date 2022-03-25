using System;
using System.Threading.Tasks;
using Producer.Models.BookingCommands;
using Producer.Models.CancelCommands;

namespace Producer
{
    public class SagaHandler
    {
        public static BookingSaga CreateSaga(string hotelEast, string hotelArea)
        {
            var hotelEastCmd = new HotelBookingCmd(hotelEast, hotelArea.ToLower() == "east" ? Location.EAST : Location.WEST);
            return new BookingSaga(hotelEastCmd);
        }

        public static async Task ExecuteSaga(BookingSaga saga)
        {
             var rpcClient = new RpcClient();

            // This could be parallelized, could introduce race-condition concerns, however.
            
            // Hotel Book
            var hotelBookingCmd = saga.HotelBookingCmd;
            
            CommandLog(hotelBookingCmd.Type, hotelBookingCmd.Name);
            var hotelResponse = await rpcClient.CallAsync(hotelBookingCmd, hotelBookingCmd.Location == Location.EAST ? "HotelEastKey" : "HotelWestKey");
            ResponseLog(hotelResponse);

            if (hotelResponse == (hotelBookingCmd.Location == Location.EAST ? "HotelEastFailed" : "HotelWestFailed"))
            {
                var hotelCancelCmd = saga.HotelBookingCancelCmd;
                
                CommandLog(hotelCancelCmd.Type, hotelCancelCmd.Name);
                var hotelCancelResponse = await rpcClient.CallAsync(hotelCancelCmd, hotelCancelCmd.Location == Location.EAST ? "HotelEastKey" : "HotelWestKey");
                ResponseLog(hotelCancelResponse);

                Console.WriteLine("Failed and cancelled booking hotel :(");
                rpcClient.Close();
                return;

            }
            
            Console.WriteLine("Successfully booked a hotel in the " + hotelBookingCmd.Location);
            
            rpcClient.Close();
        }
        
        private static  void CommandLog(string type, string name)
        {
            Console.WriteLine("<-- Sending {0} for {1}", type, name);
        }
        
        private static void ResponseLog(string response)
        {
            Console.WriteLine("--> Received '{0}'", response);
        }
    }

    public class BookingSaga
    {
        public HotelBookingCmd HotelBookingCmd { get; set; }
        public HotelCancelCmd HotelBookingCancelCmd { get; set; }

        public BookingSaga(HotelBookingCmd hotelBookingCmd)
        {
            HotelBookingCmd = hotelBookingCmd;
            HotelBookingCancelCmd = new HotelCancelCmd(hotelBookingCmd);
        }
    }
}
