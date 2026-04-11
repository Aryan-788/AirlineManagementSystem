using OpenQA.Selenium;
using Airline.SeleniumTests.Utils;

namespace Airline.SeleniumTests.Pages;

/// <summary>
/// Page Object for the Booking screen.
///
/// Selectors are derived from the real Angular template:
///   src/app/features/passenger/booking/booking.html
///
/// Covers seat selection, passenger details form, and booking confirmation.
/// </summary>
public class BookingPage : BasePage
{
    // ─── Locators ──────────────────────────────────────────────────────────────

    // Page-level confirmation that booking page has loaded
    private readonly By _pageTitle        = By.CssSelector("h1.bk-title");

    // Seat map
    private readonly By _allSeatButtons   = By.CssSelector("button.seat-btn");
    private readonly By _availableSeat    = By.CssSelector("button.seat-btn:not(.seat-occupied):not(.seat-selected)");

    // Passenger form fields — Angular uses [name] attributes like name0, age0, aadhar0
    // We use a pattern-based selector for the first passenger (index 0)
    private readonly By _passengerNameInput   = By.CssSelector("input[name='name0']");
    private readonly By _passengerAgeInput    = By.CssSelector("input[name='age0']");
    private readonly By _passengerGenderSelect = By.CssSelector("select[name='gender0']");
    private readonly By _passengerAadharInput = By.CssSelector("input[name='aadhar0']");

    // Fare summary
    private readonly By _totalPriceElement = By.CssSelector("div.fare-total span.total-amount");

    // Action buttons
    private readonly By _confirmBookingButton = By.CssSelector("button.confirm-btn");
    private readonly By _cancelButton         = By.CssSelector("button.cancel-btn");

    // Seat class labels (First / Business / Economy)
    private readonly By _firstClassLabel    = By.XPath("//div[contains(@class,'class-label') and contains(text(),'First')]");
    private readonly By _businessClassLabel = By.XPath("//div[contains(@class,'class-label') and contains(text(),'Business')]");
    private readonly By _economyClassLabel  = By.XPath("//div[contains(@class,'class-label') and contains(text(),'Economy')]");

    // ─── Constructor ───────────────────────────────────────────────────────────

    public BookingPage(IWebDriver driver) : base(driver) { }

    // ─── Page Actions ──────────────────────────────────────────────────────────

    /// <summary>Waits for the booking page to fully load by checking the page title.</summary>
    public BookingPage WaitForPageLoad()
    {
        WaitHelper.WaitForVisible(Driver, _pageTitle, TimeSpan.FromSeconds(20));
        return this;
    }

    /// <summary>
    /// Selects the first available seat automatically.
    /// Returns the seat label text (e.g. "10A") or throws if none found.
    /// </summary>
    public string SelectFirstAvailableSeat()
    {
        var seat = WaitHelper.WaitForClickable(Driver, _availableSeat, TimeSpan.FromSeconds(10));
        var seatLabel = seat.Text.Trim();
        seat.Click();
        return seatLabel;
    }

    /// <summary>Selects a specific seat by its label (e.g. "10A").</summary>
    public BookingPage SelectSeatByLabel(string label)
    {
        var seats = Driver.FindElements(_allSeatButtons);
        var target = seats.FirstOrDefault(s => s.Text.Trim() == label)
            ?? throw new InvalidOperationException($"Seat '{label}' was not found on the seat map.");

        if (target.GetDomAttribute("class")!.Contains("seat-occupied"))
            throw new InvalidOperationException($"Seat '{label}' is already occupied.");

        target.Click();
        return this;
    }

    /// <summary>Fills in the first passenger's name.</summary>
    public BookingPage EnterPassengerName(string name)
    {
        SendKeysSlowly(_passengerNameInput, name);
        return this;
    }

    /// <summary>Sets the first passenger's age.</summary>
    public BookingPage EnterPassengerAge(int age)
    {
        var el = WaitHelper.WaitForVisible(Driver, _passengerAgeInput);
        ((IJavaScriptExecutor)Driver).ExecuteScript(
            "arguments[0].value = arguments[1]; arguments[0].dispatchEvent(new Event('input', {bubbles: true}));",
            el, age.ToString());
        return this;
    }

    /// <summary>Selects the first passenger's gender.</summary>
    public BookingPage SelectPassengerGender(string gender)
    {
        SelectByText(_passengerGenderSelect, gender);
        return this;
    }

    /// <summary>Enters the 12-digit Aadhaar card number for the first passenger.</summary>
    public BookingPage EnterAadharNumber(string aadhar)
    {
        SendKeysSlowly(_passengerAadharInput, aadhar);
        return this;
    }

    /// <summary>
    /// Fills in all required details for the first passenger in a single call.
    /// </summary>
    public BookingPage FillPassengerDetails(string name, int age, string gender, string aadhar)
    {
        EnterPassengerName(name);
        EnterPassengerAge(age);
        SelectPassengerGender(gender);
        EnterAadharNumber(aadhar);
        return this;
    }

    /// <summary>Clicks the Confirm Booking button.</summary>
    public BookingPage ClickConfirmBooking()
    {
        Click(_confirmBookingButton);
        return this;
    }

    /// <summary>Waits for navigation to the payment page after a confirmed booking.</summary>
    public bool WaitForPaymentRedirect(TimeSpan? timeout = null)
    {
        try
        {
            WaitHelper.WaitForUrlContains(Driver, "/payment", timeout ?? TimeSpan.FromSeconds(20));
            return true;
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }

    // ─── Assertions / Queries ──────────────────────────────────────────────────

    /// <summary>Returns the total price text shown in the fare summary.</summary>
    public string GetTotalPrice()
    {
        return WaitHelper.WaitForNonEmptyText(Driver, _totalPriceElement, TimeSpan.FromSeconds(10));
    }

    /// <summary>Returns true if the Confirm Booking button is currently enabled.</summary>
    public bool IsConfirmButtonEnabled()
    {
        try
        {
            var btn = Driver.FindElement(_confirmBookingButton);
            return btn.Enabled && btn.GetDomProperty("disabled") == null;
        }
        catch (NoSuchElementException)
        {
            return false;
        }
    }

    /// <summary>Returns the count of available (not occupied, not selected) seats.</summary>
    public int GetAvailableSeatCount()
    {
        return Driver.FindElements(_availableSeat).Count;
    }
}
