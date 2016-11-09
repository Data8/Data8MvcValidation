# Data8 Validation and Formatting for ASP.NET MVC

Apply Data8's advanced email and telephone number validation services to your ASP.NET MVC models with a simple attribute.
Ensure consistent formatting of common data types.

## NuGet

This package is available on [NuGet](https://www.nuget.org/packages/Data8.MvcValidation):

```powershell
Install-Package Data8.MvcValidation
```

## Prerequisites

This package makes use of the Data8 Validate suite of web services, and as such you must have a Data8 account with credits
available for the relevant services. If you do not already have a Data8 account, register at
https://www.data-8.co.uk/Membership/Register

Once you have registered you'll need to store your Data8 username and password in your web.config file in order for the
validation attributes to authenticate the calls to the relevant web services. Use the keys Data8Username and Data8Password, e.g.

```xml
<appSettings>
  <add key="Data8Username" value="your-username" />
  <add key="Data8Password" value="your-password" />
</appSettings>
```

## Usage - Email Address Validation

Add an `[EmailValidation]` attribute to any properties in your model class that should contain an email address, e.g.

```csharp
public class MyModel
{
  [EmailValidation]
  public string EmailAddress { get; set; }
}
```

The standard ASP.NET validation pipeline will cause the entered email address to be validated, and the ModelState.IsValid
property to be set to false in your controller action if an invalid email address is entered.

There are several levels of email validation available. The level to be applied can be configured within the attribute, e.g.

```csharp
[EmailValidation(Level = EmailValidationLevel.Address)]
```

The following are the available validation levels:

* Syntax gives the quickest response but will allow through a large number of invalid email addresses.
* MX (the default) gives a quick response and checks the domain on the right-hand side of the @ symbol is valid and configured 
  to receive email.
* Server gives a slower response and checks that the email servers to receive mail for the address are currently running
* Address gives the slowest response but will allow through the smallest number of invalid email addresses.

## Usage - Telephone Number Validation

Add a `[TelephoneValidation]` attribute to any properties in your model class that should contain a telephone number, e.g.

```csharp
public class MyModel
{
  [TelephoneValidation]
  public string TelephoneNumber { get; set; }
}
```

The standard ASP.NET validation pipeline will cause the entered telephone number to be validated, and the ModelState.IsValid
property to be set to false in your controller action if an invalid telephone number is entered.

Some types of telephone number can also be validated to a higher level, specifically mobile numbers and UK landline numbers.
These optional services validate the number in real time against the telephone networks. If you have credits for the relevant
web service you can enable these features in the attribute, e.g.

```csharp
[TelephoneValidation(UseMobileValidation = true, UseLandlineValidation = true)]
```

In order to be validated correctly, a telephone number must have the context of a country it is within. If a telephone number is
entered with a full international dialling code that is not a problem, but users will commonly enter their number without it. In
these cases, the country will be determined by either the DefaultCountry specified on the attribute itself, an appSetting value 
defined in the web.config file with a key of Data8DefaultCountry, or the value stored in another property in the same model class.

Using DefaultCountry:

```csharp
[TelephoneValidation(DefaultCountry = "GB")]
```

Using the web.config file:

```xml
<appSettings>
  <add key="Data8DefaultCountry" value="GB" />
</appSettings>
```

Using another property in the same model class:

```csharp
public class MyModel
{
  public string Country { get; set; }

  [TelephoneValidation(DefaultCountryProperty = "Country")]
  public string TelephoneNumber { get; set; }
}
```

In each case, the country value can be either:

* An ISO standard 2 character country code (e.g. "GB")
* An ISO standard English country name (e.g. "United Kingdom")
* An international telephone dialling code (e.g. "44")

## Usage - Standardizing Formatting

Add a `[StandardizeFormatting]` attribute to any action method you want to standardize any submitted values for, or add it globally
for all actions, e.g.

```csharp
public class MyController : Controller
{
  [HttpPost]
  [StandardizeFormatting]
  public ActionResult ActionName(MyModel model)
  {
    ...
  }
}
```

or

```csharp
public static void RegisterGlobalFilters(GlobalFilterCollection filters)
{
  filters.Add(new StandardizeFormattingAttribute());
}
```

Properties to be standardized within the model class are determined automatically based on the `[DataType]` attribute, e.g.

```csharp
public class MyModel
{
  [DataType(DataType.EmailAddress)]
  public string EmailAddress { get; set; }

  [DataType(DataType.PhoneNumber)]
  public string TelephoneNumber { get; set; }

  [DataType("Country")]
  public string CountryCode { get; set; }

  [DataType("FirstName")]
  public string FirstName { get; set; }

  [DataType("LastName")]
  public string LastName { get; set; }

  public string OtherStringProperty { get; set; }
}
```

In this example the values in the EmailAddress property would all be converted to lower case, the values in the TelephoneNumber
property would be formatted according to the conventions for the country specified in the CountryCode property, and the FirstName
and LastName properties would be proper (title) cased. The value in OtherStringProperty would be trimmed of any white space.

When formatting telephone numbers, if no property is available with a "Country" data type, the country specified in the 
Data8DefaultCountry application setting is used (as for telephone number validation above).