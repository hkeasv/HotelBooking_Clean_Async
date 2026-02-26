using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HotelBooking.Core
{
    /// <summary>
    /// Footnote (architecture): BookingManager is a "use-case/service" in the Core layer.
    /// Core should not depend on infrastructure details (DB/EF/Web) -> Clean Architecture.
    /// </summary>
    public class BookingManager : IBookingManager
    {
        // Footnote (DIP): depending on IRepository<T> keeps Core independent of EF/DB.
        // This also makes unit tests easy (swap in mocks/fakes).
        private IRepository<Booking> bookingRepository;
        private IRepository<Room> roomRepository;

        // Footnote (DI): constructor injection makes dependencies explicit and test-friendly.
        // Avoids hidden dependencies and supports mocking frameworks.
        public BookingManager(IRepository<Booking> bookingRepository, IRepository<Room> roomRepository)
        {
            // Footnote (SRP): constructor should only assign dependencies (no heavy logic).
            this.bookingRepository = bookingRepository;
            this.roomRepository = roomRepository;
        }

        public async Task<bool> CreateBooking(Booking booking)
        {
            // Footnote (async): I/O-bound work (repository access) should be awaited,
            // not blocked with .Result/.Wait() to avoid deadlocks and improve scalability.
            int roomId = await FindAvailableRoom(booking.StartDate, booking.EndDate);

            // Footnote (use-case orchestration): CreateBooking coordinates:
            // - check availability (rule)
            // - update booking state (entity changes)
            // - persist via repository port (interaction)
            if (roomId >= 0)
            {
                booking.RoomId = roomId;
                booking.IsActive = true;

                // Footnote (observable behavior): persistence is part of the use-case.
                // In unit tests, we verify this interaction (Moq Verify AddAsync called once).
                await bookingRepository.AddAsync(booking);
                return true;
            }

            // Footnote: returning false is a simple failure signal (no exception for "no rooms").
            return false;
        }

        public async Task<int> FindAvailableRoom(DateTime startDate, DateTime endDate)
        {
            // Footnote (preconditions): validate early to avoid meaningless repository work.
            // Test partition: invalid inputs -> throws ArgumentException.
            //
            // Footnote (testability): DateTime.Today is a hidden time dependency.
            // Ideal seam: inject IClock.Today so tests can control "today" deterministically.
            if (startDate <= DateTime.Today || startDate > endDate)
                throw new ArgumentException("The start date cannot be in the past or later than the end date.");

            // Footnote (ports): repositories return data; core applies business rules on it.
            var bookings = await bookingRepository.GetAllAsync();

            // Footnote (domain rule): only active bookings block availability.
            var activeBookings = bookings.Where(b => b.IsActive);

            var rooms = await roomRepository.GetAllAsync();

            // Footnote (algorithm): for each room, check if requested range overlaps any active booking.
            // Unit tests should focus on scenarios (overlap/non-overlap, active/inactive),
            // not the specific LINQ implementation.
            foreach (var room in rooms)
            {
                var activeBookingsForCurrentRoom = activeBookings.Where(b => b.RoomId == room.Id);

                // Footnote (interval semantics): overlap logic encodes the domain decision.
                // Be explicit in tests about whether endDate is inclusive/exclusive.
                if (activeBookingsForCurrentRoom.All(b =>
                        (startDate < b.StartDate && endDate < b.StartDate)  // request entirely before booking
                     || (startDate > b.EndDate && endDate > b.EndDate)))   // request entirely after booking
                {
                    return room.Id;
                }
            }

            // Footnote: -1 is a sentinel meaning "no room available".
            // CreateBooking depends on this contract -> good reason for unit tests.
            return -1;
        }

        public async Task<List<DateTime>> GetFullyOccupiedDates(DateTime startDate, DateTime endDate)
        {
            // Footnote (preconditions): invalid range -> exception. Test with data-driven [Theory].
            if (startDate > endDate)
                throw new ArgumentException("The start date cannot be later than the end date.");

            // Footnote: result is a pure data structure -> easy to unit test with strong assertions.
            List<DateTime> fullyOccupiedDates = new List<DateTime>();

            var rooms = await roomRepository.GetAllAsync();
            int noOfRooms = rooms.Count();

            var bookings = await bookingRepository.GetAllAsync();

            // Footnote (edge case): no bookings => no fully occupied dates.
            if (bookings.Any())
            {
                // Footnote: iterate each day; for each day count active bookings covering it.
                // Unit tests should include: "exactly which days are returned" (strong assertions).
                for (DateTime d = startDate; d <= endDate; d = d.AddDays(1))
                {
                    var noOfBookings = from b in bookings
                                       where b.IsActive && d >= b.StartDate && d <= b.EndDate
                                       select b;

                    // Footnote (business rule): day is "fully occupied" when bookings covering that day
                    // are at least the number of rooms.
                    if (noOfBookings.Count() >= noOfRooms)
                        fullyOccupiedDates.Add(d);
                }
            }

            return fullyOccupiedDates;
        }
    }
}
