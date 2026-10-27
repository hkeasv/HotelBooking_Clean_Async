using OpenQA.Selenium;

namespace HotelBooking.SeleniumTests;

// This class represents the "Create Room" page in the hotel booking application.
public class CreateRoomPage
{
    // The locator for the "Description" input field on the "Create Room" page.
    private readonly By descriptionInputLocator = By.Id("Description");
    
    // The locator for the "Create" button on the "Create Room" page.
    private readonly By createButtonLocator =
        By.CssSelector("input[type='submit'][value='Create']");

    private readonly IWebDriver driver;

    public CreateRoomPage(IWebDriver driver)
    {
        ArgumentNullException.ThrowIfNull(driver);
        this.driver = driver;
    }
    
    // Navigates to the "Create Room" page.
    public void Visit()
    {
        driver.Navigate().GoToUrl("https://localhost:5001/Rooms/Create");
    }

    // Creates a new room with the specified description and returns the "Rooms" page.
    public RoomsPage CreateRoom(string description)
    {
        // Fill in the "Description" input field.
        driver.FindElement(descriptionInputLocator).SendKeys(description);
        
        // Click the "Create" button to submit the form. The RoomsController Create
        // action method will navigate to the "Rooms" page after successful creation.
        driver.FindElement(createButtonLocator).Click();

        // Return a new instance of the "Rooms" page after creating the room.
        return new RoomsPage(driver);
    }
}