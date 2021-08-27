using System;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Net.Http;
using Newtonsoft.Json;
using ValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace Data8.MvcValidation
{
    /// <summary>
    /// Provides advanced validation of telephone numbers.
    /// </summary>
    /// <remarks>
    /// Uses the Data8 International Telephone Number Validation web service to validate the supplied telephone number data.
    /// Non-string, null or empty values will be passed as valid.
    /// The web service needs a username and password to access it. These should be configured in the web.config
    /// file &lt;appSettings&gt; element as Data8Username and Data8Password.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
    public class TelephoneValidationAttribute : ValidationAttribute
    {
        /// <summary>
        /// Applies advanced telephone number validation to the property.
        /// </summary>
        public TelephoneValidationAttribute() : this(Properties.Resources.TelephoneValidation_ErrorMessage)
        {
        }

        /// <summary>
        /// Applies advanced telephone number validation to the property.
        /// </summary>
        /// <param name="errorMessage">The error message to use when the telephone number is found to be invalid.</param>
        public TelephoneValidationAttribute(string errorMessage) : this(() => errorMessage)
        {
        }

        /// <summary>
        /// Applies advanced telephone number validation to the property.
        /// </summary>
        /// <param name="errorMessageAccessor">The <see cref="Func{T}"/> that will return an error message.</param>
        public TelephoneValidationAttribute(Func<string> errorMessageAccessor) : base(errorMessageAccessor)
        {
            DefaultCountry = ConfigurationManager.AppSettings["Data8DefaultCountry"];

            if (String.IsNullOrEmpty(DefaultCountry))
                DefaultCountry = "GB";
        }

        /// <summary>
        /// The country code to assume when validating telephone numbers supplied without an explicit country code.
        /// </summary>
        /// <value>
        /// Specify either an ISO 2-character country code (GB, US, ...), ISO standard country name or international dialling code.
        /// </value>
        public string DefaultCountry { get; set; }

        /// <summary>
        /// The name of a property within the same class as the telephone number property being validated to take the default
        /// country from.
        /// </summary>
        /// <value>
        /// The name of another property to take as the <see cref="DefaultCountry"/>.
        /// </value>
        public string DefaultCountryProperty { get; set; }

        /// <summary>
        /// Indicates whether unavailable mobile numbers (e.g. turned off for an extended period) should be treated as invalid numbers.
        /// </summary>
        /// <remarks>
        /// By default, unavailable mobile numbers are treated as valid.
        /// </remarks>
        public bool TreatUnavailableMobileAsInvalid { get; set; }

        /// <summary>
        /// Indicates whether unavailable mobile numbers (e.g. turned off for an extended period) should be treated as invalid numbers.
        /// </summary>
        /// <remarks>
        /// By default, telephone numbers from countries with no coverage are treated as valid.
        /// </remarks>
        public bool TreatNoCoverageAsInvalid { get; set; }

        /// <summary>
        /// Validates the supplied telephone number.
        /// </summary>
        /// <param name="value">The telephone number to validate.</param>
        /// <param name="validationContext">The context in which the telephone number is being validated.</param>
        /// <returns>
        /// When validation is valid, <see cref="ValidationResult.Success"/>.
        /// <para>
        /// When validation is invalid, an instance of <see cref="ValidationResult"/>.
        /// </para>
        /// </returns>
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var str = value as string;

            if (String.IsNullOrEmpty(str))
                return ValidationResult.Success;

            var username = ConfigurationManager.AppSettings["Data8Username"];
            var password = ConfigurationManager.AppSettings["Data8Password"];
            var apikey = ConfigurationManager.AppSettings["Data8APIKey"];
            if (!String.IsNullOrEmpty(apikey))
            {
                username = "apikey-" + apikey;
                password = "";
            }

            var country = DefaultCountry;

            if (!String.IsNullOrEmpty(DefaultCountryProperty))
            {
                var property = validationContext.ObjectType.GetProperty(DefaultCountryProperty);
                country = (string)property?.GetValue(validationContext.ObjectInstance, null) ?? country;
            }

            var data = new {
                username,
                password,
                telephoneNumber = str,
                defaultCountry = country,
                options = new {
                    TreatUnavailableMobileAsInvalid = TreatUnavailableMobileAsInvalid.ToString(),
                    ApplicationName = "MVC"
                }
            };

            PhoneValidationResponse outcome = PerformValidation(JsonConvert.SerializeObject(data));
            if (outcome.Status.Success == false)
                return ValidationResult.Success;

            if (outcome.Result.ValidationResult != "Invalid" && (outcome.Result.ValidationResult == "NoCoverage" && !TreatNoCoverageAsInvalid))
                return ValidationResult.Success;

            return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
        }

        private PhoneValidationResponse PerformValidation(string data) {
            var url = "https://webservices.data-8.co.uk/PhoneValidation/IsValid.json";
            using (var client = new HttpClient())
            {
                var response = client.PostAsync(url, new StringContent(data, System.Text.Encoding.UTF8, "application/json")).Result;
                var phoneResult = JsonConvert.DeserializeObject<PhoneValidationResponse>(response.Content.ReadAsStringAsync().Result);
                Console.WriteLine("Data8 Validation Result: " + phoneResult.Result.ValidationResult);
                return phoneResult;
            }
        }
    }

    public class PhoneValidationResponse
    {
        public PhoneResult Result { get; set; }
        public Status Status { get; set; }
    }

    public class Status
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public decimal CreditsRemaining { get; set; }
    }

    public class PhoneResult
    {
        public string TelephoneNumber { get; set; }
        public string ValidationResult { get; set; }
        public string ValidationLevel { get; set; }
        public string NumberType { get; set; }
        public string Location { get; set; }
        public string Provider { get; set; }
        public string CountryCode { get; set; }
        public string CountryName { get; set; }
    }

}
