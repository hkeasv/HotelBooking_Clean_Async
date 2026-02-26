using System;
using System.Collections.Generic;

namespace HotelBooking.UnitTests.TestData
{
    /// <summary>
    /// THEORY:
    /// Data-driven testing reduces duplication and makes the "input partitions"
    /// explicit: invalid date ranges are a partition of inputs that must throw.
    ///
    /// Also: DateTime doesn't play nicely with [InlineData] (needs constants),
    /// so we use MemberData-style providers.
    /// </summary>
    public static class BookingManagerTestData
    {
        public static IEnumerable<object[]> InvalidFindAvailableRoomRanges()
        {
            var today = DateTime.Today;

            // Partition 1: start date not strictly in the future (the business rule says future bookings only)
            yield return new object[] { today, today.AddDays(1) };
            yield return new object[] { today.AddDays(-1), today.AddDays(1) };

            // Partition 2: start date after end date (invalid period)
            yield return new object[] { today.AddDays(10), today.AddDays(9) };
        }

        public static IEnumerable<object[]> InvalidGetFullyOccupiedDatesRanges()
        {
            var today = DateTime.Today;

            // Partition: start > end must throw
            yield return new object[] { today.AddDays(10), today.AddDays(9) };
        }
    }
}
