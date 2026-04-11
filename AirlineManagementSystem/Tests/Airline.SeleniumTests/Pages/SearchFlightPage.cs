using OpenQA.Selenium;
using Airline.SeleniumTests.Utils;

namespace Airline.SeleniumTests.Pages;

/// <summary>
/// Page Object for the Flight Search screen.
///
/// Selectors are derived from the real Angular template:
///   src/app/features/passenger/search/search.html
///
/// Inputs use ngModel bindings without IDs; we target them by
/// placeholder text, input type, and parent CSS classes.
/// </summary>
public class SearchFlightPage : BasePage
{
    // ─── Locators ──────────────────────────────────────────────────────────────

    // Search bar inputs
    private readonly By _sourceInput      = By.CssSelector("div.search-bar input[placeholder='e.g. Mumbai']");
    private readonly By _destinationInput = By.CssSelector("div.search-bar input[placeholder='e.g. Delhi']");
    private readonly By _departureDateInput = By.CssSelector("div.search-bar input[type='date']");
    private readonly By _passengersInput  = By.CssSelector("div.search-bar input[type='number']");
    private readonly By _classSelect      = By.CssSelector("div.search-bar select");

    // Action buttons in the search bar
    private readonly By _findFlightsButton = By.CssSelector("button.search-btn");
    private readonly By _browseAllButton   = By.CssSelector("button.browse-btn");

    // Results area — appears only after a search
    private readonly By _resultsLayout    = By.CssSelector("div.results-layout");
    private readonly By _flightCards      = By.CssSelector(".flight-card, .result-card");

    // Loading / empty state indicators
    private readonly By _loadingIndicator = By.CssSelector(".loading, .spinner");
    private readonly By _noResultsMsg     = By.CssSelector(".no-results, .empty-state");

    // Dropdown suggestion items (autocomplete)
    private readonly By _sourceDropdownItems = By.CssSelector("div.search-bar div.custom-dropdown div.dropdown-item");
    private readonly By _destDropdownItems   = By.CssSelector("div.search-bar div.custom-dropdown div.dropdown-item");

    // ─── Constructor ───────────────────────────────────────────────────────────

    public SearchFlightPage(IWebDriver driver) : base(driver) { }

    // ─── Page Actions ──────────────────────────────────────────────────────────

    /// <summary>Waits for the search bar to be present, confirming the page has loaded.</summary>
    public SearchFlightPage WaitForPageLoad()
    {
        WaitHelper.WaitForVisible(Driver, _findFlightsButton, TimeSpan.FromSeconds(20));
        return this;
    }

    /// <summary>
    /// Enters the source city. Handles the autocomplete dropdown if it appears.
    /// </summary>
    public SearchFlightPage EnterSource(string city)
    {
        SendKeysSlowly(_sourceInput, city);
        TrySelectDropdownItem(_sourceDropdownItems, city);
        return this;
    }

    /// <summary>
    /// Enters the destination city. Handles the autocomplete dropdown if it appears.
    /// </summary>
    public SearchFlightPage EnterDestination(string city)
    {
        SendKeysSlowly(_destinationInput, city);
        TrySelectDropdownItem(_destDropdownItems, city);
        return this;
    }

    /// <summary>Sets the departure date (format: yyyy-MM-dd, e.g. "2025-12-25").</summary>
    public SearchFlightPage SelectDepartureDate(string date)
    {
        // Date inputs are manipulated via JavaScript for cross-browser reliability
        var dateEl = WaitHelper.WaitForVisible(Driver, _departureDateInput);
        ((IJavaScriptExecutor)Driver).ExecuteScript(
            "arguments[0].value = arguments[1]; arguments[0].dispatchEvent(new Event('input', {bubbles: true})); arguments[0].dispatchEvent(new Event('change', {bubbles: true}));",
            dateEl, date);
        return this;
    }

    /// <summary>Sets the number of passengers (1–9).</summary>
    public SearchFlightPage SetPassengerCount(int count)
    {
        var el = WaitHelper.WaitForVisible(Driver, _passengersInput);
        ((IJavaScriptExecutor)Driver).ExecuteScript(
            "arguments[0].value = arguments[1]; arguments[0].dispatchEvent(new Event('input', {bubbles: true})); arguments[0].dispatchEvent(new Event('change', {bubbles: true}));",
            el, count.ToString());
        return this;
    }

    /// <summary>Selects the travel class (Economy, Business, First Class).</summary>
    public SearchFlightPage SelectClass(string travelClass)
    {
        SelectByText(_classSelect, travelClass);
        return this;
    }

    /// <summary>Clicks the "Find Flights" button and waits for results to appear.</summary>
    public SearchFlightPage ClickFindFlights()
    {
        Click(_findFlightsButton);
        return this;
    }

    /// <summary>Clicks "Browse All" to load all available flights.</summary>
    public SearchFlightPage ClickBrowseAll()
    {
        Click(_browseAllButton);
        return this;
    }

    /// <summary>
    /// Waits until the results layout becomes visible after a search.
    /// Returns true if results appeared, false if the timeout was reached.
    /// </summary>
    public bool WaitForResults(TimeSpan? timeout = null)
    {
        try
        {
            WaitHelper.WaitForVisible(Driver, _resultsLayout, timeout ?? TimeSpan.FromSeconds(20));
            return true;
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }

    /// <summary>Returns the count of flight result cards rendered on the page.</summary>
    public int GetFlightCardCount()
    {
        try
        {
            return Driver.FindElements(_flightCards).Count;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>Returns true if the "no results" message is shown.</summary>
    public bool IsNoResultsMessageVisible()
    {
        return IsDisplayed(_noResultsMsg);
    }

    // ─── Private Helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Attempts to click a dropdown suggestion item whose text contains <paramref name="cityName"/>.
    /// Silently does nothing if no dropdown appears.
    /// </summary>
    private void TrySelectDropdownItem(By dropdownLocator, string cityName)
    {
        try
        {
            var wait = WaitHelper.CreateWait(Driver, TimeSpan.FromSeconds(3));
            wait.Until(d =>
            {
                var items = d.FindElements(dropdownLocator);
                foreach (var item in items)
                {
                    if (item.Displayed && item.Text.Contains(cityName, StringComparison.OrdinalIgnoreCase))
                    {
                        item.Click();
                        return true;
                    }
                }
                return false;
            });
        }
        catch (WebDriverTimeoutException)
        {
            // No dropdown appeared — typed value will be used directly
        }
    }
}
