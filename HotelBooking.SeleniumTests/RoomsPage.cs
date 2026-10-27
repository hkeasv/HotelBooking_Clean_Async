using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace HotelBooking.SeleniumTests;

// This class represents the "Rooms" page in the hotel booking application.
public class RoomsPage
{
    // The locator for the rows of the rooms table on the "Rooms" page.
    private readonly By roomRowsLocator = By.CssSelector("table.table tbody tr");
    
    // The locator for the first cell (description) of each room row in the rooms table.
    private readonly By roomDescriptionLocator = By.CssSelector("td:first-child");

    private readonly IWebDriver driver;

    public RoomsPage(IWebDriver driver)
    {
        ArgumentNullException.ThrowIfNull(driver);
        this.driver = driver;
        
        // Wait for the rooms table to be loaded and displayed on the page. Wait until at
        // least one room row is present in the table. This is an explicit wait to ensure
        // that the page has fully loaded before interacting with it.
        WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(2));
        wait.Until(d => d.FindElements(roomRowsLocator).Count > 0);
    }

    // Retrieves a read-only list of room descriptions displayed on the "Rooms" page.
    public IReadOnlyList<string> GetRooms()
    {
        // Find all the rows in the rooms table.
        var roomRows = driver.FindElements(roomRowsLocator);
        
        var rooms = new List<string>();

        // Iterate through each row and extract the room description from the first cell.
        foreach (var row in roomRows)
        {
            var description = row.FindElement(roomDescriptionLocator).Text;
            // Add the room description to the list of rooms.
            rooms.Add(description);
        }
        
        // Return the list of room descriptions.
        return rooms;
    }
}