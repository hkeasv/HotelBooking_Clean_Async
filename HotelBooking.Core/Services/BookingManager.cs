using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;

namespace HotelBooking.Core
{
    public class BookingManager : IBookingManager
    {
        private IRepository<Booking> bookingRepository;
        private IRepository<Room> roomRepository;

        // Constructor injection
        public BookingManager(IRepository<Booking> bookingRepository, IRepository<Room> roomRepository)
        {
            this.bookingRepository = bookingRepository;
            this.roomRepository = roomRepository;
        }

        public async Task<bool> CreateBooking(Booking booking)
        {
            int roomId = await FindAvailableRoom(booking.StartDate, booking.EndDate);

            if (roomId >= 0)
            {
                booking.RoomId = roomId;
                booking.IsActive = true;
                await bookingRepository.AddAsync(booking);
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<int> FindAvailableRoom(DateTime startDate, DateTime endDate)
        {
            if (startDate <= DateTime.Today || startDate > endDate)
                throw new ArgumentException("The start date cannot be in the past or later than the end date.");

            var bookings = await bookingRepository.GetAllAsync();
            var activeBookings = bookings.Where(b => b.IsActive);
            var rooms = await roomRepository.GetAllAsync();
            foreach (var room in rooms)
            {
                var activeBookingsForCurrentRoom = activeBookings.Where(b => b.RoomId == room.Id);
                if (activeBookingsForCurrentRoom.All(b => startDate < b.StartDate &&
                    endDate < b.StartDate || startDate > b.EndDate && endDate > b.EndDate))
                {
                    return room.Id;
                }
            }
            return -1;
        }

        public async Task<List<DateTime>> GetFullyOccupiedDates(DateTime startDate, DateTime endDate)
        {
            if (startDate > endDate)
                throw new ArgumentException("The start date cannot be later than the end date.");

            List<DateTime> fullyOccupiedDates = new List<DateTime>();
            var rooms = await roomRepository.GetAllAsync();
            int noOfRooms = rooms.Count();
            var bookings = await bookingRepository.GetAllAsync();

            if (bookings.Any())
            {
                for (DateTime d = startDate; d <= endDate; d = d.AddDays(1))
                {
                    var bookedRoomIdsOnThisDate = from b in bookings
                                       where b.IsActive && d >= b.StartDate && d <= b.EndDate
                                       select b.RoomId;
                    if (bookedRoomIdsOnThisDate.Distinct().Count() >= noOfRooms) 
                        fullyOccupiedDates.Add(d);
                }
            }
            return fullyOccupiedDates;
        }

    }
}
