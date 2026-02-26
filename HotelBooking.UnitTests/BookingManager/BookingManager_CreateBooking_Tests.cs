using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotelBooking.Core;
using Moq;
using Xunit;

namespace HotelBooking.UnitTests.BookingManager
{
    /// <summary>
    /// THEORY:
    /// CreateBooking is an orchestration/use-case method.
    /// Good unit tests verify:
    /// 1) observable return value
    /// 2) observable state changes on the Booking entity (RoomId, IsActive)
    /// 3) interaction with the persistence port (AddAsync) when booking succeeds
    ///
    /// Moq Verify is appropriate here because "persist booking" is part of the use-case behavior.
    /// </summary>
    public class BookingManager_CreateBooking_Tests
    {
        [Fact]
        public async Task CreateBooking_WhenRoomAvailable_SetsFields_PersistsBooking_ReturnsTrue()
        {
            // Arrange
            var bookingRepo = new Mock<IRepository<Booking>>();
            var roomRepo = new Mock<IRepository<Room>>();

            roomRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Room>
            {
                new Room { Id = 101 }
            });

            bookingRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Booking>()); // no conflicts

            // Important for async mocks: if AddAsync is called, it returns a Task.
            //Extra note!: We are not testing whether the database can save. You’re testing whether BookingManager behaves correctly given that saving works (or fails).
            bookingRepo.Setup(r => r.AddAsync(It.IsAny<Booking>())).Returns(Task.CompletedTask); // Simulate successful save

            var sut = new HotelBooking.Core.BookingManager(bookingRepo.Object, roomRepo.Object); // System under test

            var booking = new Booking
            {
                StartDate = DateTime.Today.AddDays(5),
                EndDate = DateTime.Today.AddDays(6),
                CustomerId = 1,
                IsActive = false
            };

            // Act
            var result = await sut.CreateBooking(booking);

            // Assert
            Assert.True(result);
            Assert.True(booking.IsActive);
            Assert.Equal(101, booking.RoomId);

            // THEORY:
            // We verify the interaction because it is the outward effect of the use-case.
            // We don't verify internal loops/conditions, only the behavior visible at the boundary.
            bookingRepo.Verify(r => r.AddAsync(It.Is<Booking>(b =>
                b.CustomerId == 1 &&
                b.RoomId == 101 &&
                b.IsActive == true &&
                b.StartDate == booking.StartDate &&
                b.EndDate == booking.EndDate
            )), Times.Once);
        }

        [Fact]
        public async Task CreateBooking_WhenNoRoomAvailable_DoesNotPersist_ReturnsFalse()
        {
            // Arrange
            var bookingRepo = new Mock<IRepository<Booking>>();
            var roomRepo = new Mock<IRepository<Room>>();

            var start = DateTime.Today.AddDays(10);
            var end = DateTime.Today.AddDays(12);

            roomRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Room>
            {
                new Room { Id = 1 }
            });

            // A conflicting active booking blocks the only room
            bookingRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Booking>
            {
                new Booking { RoomId = 1, IsActive = true, StartDate = start.AddDays(-1), EndDate = end.AddDays(+1) }
            });

            var sut = new HotelBooking.Core.BookingManager(bookingRepo.Object, roomRepo.Object);

            var booking = new Booking
            {
                StartDate = start,
                EndDate = end,
                CustomerId = 123,
                IsActive = false
            };

            // Act
            var result = await sut.CreateBooking(booking);

            // Assert
            Assert.False(result);

            // It should NOT mark active or assign a room if booking failed
            Assert.False(booking.IsActive);
            Assert.Equal(0, booking.RoomId);

            // THEORY: negative interaction verification (should not persist on failure)
            bookingRepo.Verify(r => r.AddAsync(It.IsAny<Booking>()), Times.Never);
        }
    }
}
