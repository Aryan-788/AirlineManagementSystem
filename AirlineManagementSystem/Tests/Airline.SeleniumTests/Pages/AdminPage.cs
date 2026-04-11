using OpenQA.Selenium;
using Airline.SeleniumTests.Utils;

namespace Airline.SeleniumTests.Pages;

public class AdminPage : BasePage
{
    private readonly By _pageTitle = By.CssSelector("h1.pg-title"); // "Fleet Control"
    private readonly By _addFlightButton = By.CssSelector("button.add-flight-btn");
    private readonly By _flightRegistryHeading = By.CssSelector("h3.ft-title"); // "Flight Registry"
    private readonly By _flightRows = By.CssSelector("table tbody tr:not(.empty-cell)");
    private readonly By _flightForm = By.CssSelector("div.form-card");
    private readonly By _activityLogItems = By.CssSelector("div.act-item");

    public AdminPage(IWebDriver driver) : base(driver) { }

    public string GetPageTitle() => GetText(_pageTitle);

    public bool IsFlightRegistryVisible() => IsDisplayed(_flightRegistryHeading);

    public int GetFlightCount() => Driver.FindElements(_flightRows).Count;

    public void ClickAddFlight() => Click(_addFlightButton);

    public bool IsFlightFormVisible() => IsDisplayed(_flightForm);

    public int GetActivityLogCount() => Driver.FindElements(_activityLogItems).Count;
}
