using OpenQA.Selenium;
using Airline.SeleniumTests.Utils;

namespace Airline.SeleniumTests.Pages;

public class BaggagePage : BasePage
{
    private readonly By _pageTitle = By.CssSelector("h2.bt-title"); // "Real-time Baggage Log"
    private readonly By _scanInput = By.CssSelector("div.scan-input input");
    private readonly By _addBaggageButton = By.CssSelector("button.bt-add, button.fab-btn");
    private readonly By _baggageRows = By.CssSelector("tr.bag-row");
    private readonly By _statCards = By.CssSelector("div.stat-card");
    private readonly By _eventLogItems = By.CssSelector("div.el-item");

    public BaggagePage(IWebDriver driver) : base(driver) { }

    public string GetPageTitle() => GetText(_pageTitle);

    public int GetBaggageRowCount() => Driver.FindElements(_baggageRows).Count;

    public int GetStatCardCount() => Driver.FindElements(_statCards).Count;

    public void ClickAddBaggage() => Click(_addBaggageButton);

    public int GetEventLogCount() => Driver.FindElements(_eventLogItems).Count;

    public void EnterTrackingNumber(string number) => SendKeys(_scanInput, number);
}
