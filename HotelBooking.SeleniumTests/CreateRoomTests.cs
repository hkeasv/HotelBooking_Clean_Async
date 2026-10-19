using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace HotelBooking.SeleniumTests;

public class CreateRoomTests : IDisposable
{
    private readonly IWebDriver driver;
    
    public CreateRoomTests()
    {
        ChromeOptions options = new ChromeOptions();
        options.AddArgument("headless");
        driver = new ChromeDriver(Environment.CurrentDirectory, options);
    }
    
    public void Dispose()
    {
        driver.Quit();
    }


    [Fact]
    public void CreateRoom_RoomIsCreatedAndDisplayedOnRoomsPage()
    {
        // Arrange
        var createRoomPage = new CreateRoomPage(driver);
        createRoomPage.Visit();
        var roomsPage = createRoomPage.CreateRoom("Test Room");
        
        // Act
        var rooms = roomsPage.GetRooms();
        
        // Assert
        Assert.NotEmpty(rooms);
        Assert.Equal("Test Room", rooms.Last());
    }
}