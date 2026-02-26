using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotelBooking.Core;
using HotelBooking.UnitTests.TestData;
using Moq;
using Xunit;

namespace HotelBooking.UnitTests.BookingManager
{
    /// <summary>
    /// THEORY (what these tests demonstrate):
    /// - Clean Architecture / DIP: BookingManager depends on IRepository<T> abstractions.
    ///   => Unit tests replace those ports with mocks (no DB, no EF).
    ///
    /// - Good unit tests: fast, isolated, deterministic, strong assertions.
    ///
    /// - Async version: tests are async Task and use ReturnsAsync / ThrowsAsync patterns.
    ///
    /// NOTE about time:
    /// BookingManager uses DateTime.Today internally, which is a hidden dependency.
    /// Ideally we inject an IClock to remove non-determinism (design-for-testability).
    /// For now, we keep dates relative to Today to avoid flakiness.
    /// </summary>
    public class BookingManager_FindAvailableRoom_Tests
    {
        [Theory]
        [MemberData(nameof(BookingManagerTestData.InvalidFindAvailableRoomRanges), MemberType = typeof(BookingManagerTestData))]
        public async Task FindAvailableRoom_InvalidDateRanges_ThrowsArgumentException(DateTime start, DateTime end)
        {
            // Arrange
            var bookingRepo = new Mock<IRepository<Booking>>();
            var roomRepo = new Mock<IRepository<Room>>();
            var sut = new HotelBooking.Core.BookingManager(bookingRepo.Object, roomRepo.Object);

            // Act (async exception testing pattern)
            Func<Task> act = () => sut.FindAvailableRoom(start, end);

            // Assert
            await Assert.ThrowsAsync<ArgumentException>(act);
        }

        [Fact]
        public async Task FindAvailableRoom_NoRooms_ReturnsMinusOne()
        {
            // THEORY:
            // This is an edge case partition: "there are zero rooms".
            // A good test suite includes boundary conditions.
            var bookingRepo = new Mock<IRepository<Booking>>();
            var roomRepo = new Mock<IRepository<Room>>();

            bookingRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Booking>());
            roomRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Room>()); // no rooms

            var sut = new HotelBooking.Core.BookingManager(bookingRepo.Object, roomRepo.Object);

            var start = DateTime.Today.AddDays(5);
            var end = DateTime.Today.AddDays(6);

            var roomId = await sut.FindAvailableRoom(start, end);

            Assert.Equal(-1, roomId);
        }

        [Fact]
        public async Task FindAvailableRoom_OneRoomNoBookings_ReturnsThatRoomId()
        {
            // Arrange
            var bookingRepo = new Mock<IRepository<Booking>>();
            var roomRepo = new Mock<IRepository<Room>>();

            bookingRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Booking>());
            roomRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Room>
            {
                new Room { Id = 101 }
            });

            var sut = new HotelBooking.Core.BookingManager(bookingRepo.Object, roomRepo.Object);

            var start = DateTime.Today.AddDays(5);
            var end = DateTime.Today.AddDays(6);

            // Act
            var roomId = await sut.FindAvailableRoom(start, end);

            // Assert (strong assertion: exact room id, not just "not -1")
            Assert.Equal(101, roomId);
        }

        [Fact]
        public async Task FindAvailableRoom_InactiveOverlappingBooking_IsIgnored_ReturnsRoom()
        {
            // THEORY:
            // Business rule: only active reservations should block booking.
            // This is a partition: overlapping booking exists but IsActive = false.

            // Arrange
            var bookingRepo = new Mock<IRepository<Booking>>();
            var roomRepo = new Mock<IRepository<Room>>();

            var start = DateTime.Today.AddDays(10);
            var end = DateTime.Today.AddDays(11);

            roomRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Room> { new Room { Id = 1 } });

            bookingRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Booking>
            {
                new Booking
                {
                    RoomId = 1,
                    IsActive = false,                // key point
                    StartDate = start.AddDays(-1),    // overlaps
                    EndDate = end.AddDays(+1)         // overlaps
                }
            });

            var sut = new HotelBooking.Core.BookingManager(bookingRepo.Object, roomRepo.Object);

            // Act
            var roomId = await sut.FindAvailableRoom(start, end);

            // Assert
            Assert.Equal(1, roomId);
        }

        [Fact]
        public async Task FindAvailableRoom_AllRoomsBooked_ReturnsMinusOne()
        {
            // THEORY:
            // This covers the main negative scenario partition: "no capacity".
            // We expect the use-case to return -1 (and CreateBooking will then return false).

            // Arrange
            var bookingRepo = new Mock<IRepository<Booking>>();
            var roomRepo = new Mock<IRepository<Room>>();

            var start = DateTime.Today.AddDays(10);
            var end = DateTime.Today.AddDays(12);

            roomRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Room>
            {
                new Room { Id = 1 },
                new Room { Id = 2 },
            });

            bookingRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Booking>
            {
                new Booking { RoomId = 1, IsActive = true, StartDate = start.AddDays(-1), EndDate = end.AddDays(+1) },
                new Booking { RoomId = 2, IsActive = true, StartDate = start,          EndDate = end }
            });

            var sut = new HotelBooking.Core.BookingManager(bookingRepo.Object, roomRepo.Object);

            // Act
            var roomId = await sut.FindAvailableRoom(start, end);
            
            // Assert
            Assert.Equal(-1, roomId);
        }
    }
}
