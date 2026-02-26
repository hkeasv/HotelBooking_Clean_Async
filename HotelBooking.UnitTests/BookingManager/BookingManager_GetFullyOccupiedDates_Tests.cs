using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotelBooking.Core;
using HotelBooking.UnitTests.TestData;
using Moq;
using Xunit;

namespace HotelBooking.UnitTests.BookingManager
{
    /// <summary>
    /// THEORY:
    /// GetFullyOccupiedDates supports the UI "calendar-like view".
    /// The rule: return dates where number of active bookings covering the date >= number of rooms.
    ///
    /// We test:
    /// - validation (start > end throws)
    /// - empty cases (no bookings => empty)
    /// - positive case (fully occupied days returned exactly)
    ///
    /// Async version: repository reads are awaited and mocked with ReturnsAsync.
    /// </summary>
    public class BookingManager_GetFullyOccupiedDates_Tests
    {
        [Theory]
        [MemberData(nameof(BookingManagerTestData.InvalidGetFullyOccupiedDatesRanges), MemberType = typeof(BookingManagerTestData))]
        public async Task GetFullyOccupiedDates_InvalidRange_ThrowsArgumentException(DateTime start, DateTime end)
        {
            var bookingRepo = new Mock<IRepository<Booking>>();
            var roomRepo = new Mock<IRepository<Room>>();
            var sut = new HotelBooking.Core.BookingManager(bookingRepo.Object, roomRepo.Object);

            Func<Task> act = () => sut.GetFullyOccupiedDates(start, end);

            await Assert.ThrowsAsync<ArgumentException>(act);
        }

        [Fact]
        public async Task GetFullyOccupiedDates_NoBookings_ReturnsEmpty()
        {
            var bookingRepo = new Mock<IRepository<Booking>>();
            var roomRepo = new Mock<IRepository<Room>>();

            roomRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Room>
            {
                new Room { Id = 1 }, new Room { Id = 2 }
            });

            bookingRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Booking>()); // none

            var sut = new HotelBooking.Core.BookingManager(bookingRepo.Object, roomRepo.Object);

            var start = DateTime.Today.AddDays(5);
            var end = DateTime.Today.AddDays(10);

            var result = await sut.GetFullyOccupiedDates(start, end);

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetFullyOccupiedDates_WhenAllRoomsBookedOnSpecificDates_ReturnsThoseDates()
        {
            // Arrange
            var bookingRepo = new Mock<IRepository<Booking>>();
            var roomRepo = new Mock<IRepository<Room>>();

            roomRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Room>
            {
                new Room { Id = 1 },
                new Room { Id = 2 }
            });

            // Window we query for fully occupied dates
            var windowStart = DateTime.Today.AddDays(10);
            var windowEnd = DateTime.Today.AddDays(14);

            // Fully occupied on day 12 and 13 (both rooms booked and active)
            var day12 = DateTime.Today.AddDays(12);
            var day13 = DateTime.Today.AddDays(13);

            bookingRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Booking>
            {
                new Booking { RoomId = 1, IsActive = true, StartDate = day12, EndDate = day13 },
                new Booking { RoomId = 2, IsActive = true, StartDate = day12, EndDate = day13 }
            });

            var sut = new HotelBooking.Core.BookingManager(bookingRepo.Object, roomRepo.Object);

            // Act
            var result = await sut.GetFullyOccupiedDates(windowStart, windowEnd);

            // Assert (strong: exact set of dates)
            Assert.Contains(day12, result);
            Assert.Contains(day13, result);

            // And ensure we didn't mark unrelated days
            Assert.DoesNotContain(DateTime.Today.AddDays(11), result);
            Assert.DoesNotContain(DateTime.Today.AddDays(14), result);

            // Optional: make it "exactly these two days"
            Assert.Equal(2, result.Count);
            Assert.True(result.SequenceEqual(new[] { day12, day13 }));
        }

        [Fact]
        public async Task GetFullyOccupiedDates_InactiveBookings_DoNotCount_TowardFullOccupancy()
        {
            // Arrange

            var bookingRepo = new Mock<IRepository<Booking>>();
            var roomRepo = new Mock<IRepository<Room>>();

            roomRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Room>
            {
                new Room { Id = 1 },
                new Room { Id = 2 }
            });

            var start = DateTime.Today.AddDays(10);
            var end = DateTime.Today.AddDays(10); // single day

            bookingRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Booking>
            {
                new Booking { RoomId = 1, IsActive = true,  StartDate = start, EndDate = end },
                new Booking { RoomId = 2, IsActive = false, StartDate = start, EndDate = end } // ignored
            });

            var sut = new HotelBooking.Core.BookingManager(bookingRepo.Object, roomRepo.Object);

            // Act
            var result = await sut.GetFullyOccupiedDates(start, end);

            // Assert
            Assert.Empty(result);
        }
    }
}
