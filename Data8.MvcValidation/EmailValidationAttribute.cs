using System;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Net.Http;
using System.Threading.Tasks;
using Data8.MvcValidation.EmailValidationWS;
using Newtonsoft.Json;

namespace Data8.MvcValidation
{
    /// <summary>
    /// Provides advanced validation of email addresses.
    /// </summary>
    /// <remarks>
    /// Uses the Data8 Email Validation web service to validate the supplied email address data.
    /// Email addresses will be validated to the level specified by the <see cref="Level"/> property.
    /// Non-string, null or empty values will be passed as valid.
    /// The web service needs a username and password to access it. These should be configured in the web.config
    /// file &lt;appSettings&gt; element as Data8Username and Data8Password.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
    public class EmailValidationAttribute : ValidationAttribute
    {
        /// <summary>
        /// Applies advanced email address validation to the property.
        /// </summary>
        public EmailValidationAttribute() : this(Properties.Resources.EmailValidation_ErrorMessage)
        {
        }

        /// <summary>
        /// Applies advanced email address validation to the property.
        /// </summary>
        /// <param name="errorMessage">The error message to use when the email address is found to be invalid.</param>
        public EmailValidationAttribute(string errorMessage) : this(() => errorMessage)
        {
        }

        /// <summary>
        /// Applies advanced email address validation to the property.
        /// </summary>
        /// <param name="errorMessageAccessor">The <see cref="Func{T}"/> that will return an error message.</param>
        public EmailValidationAttribute(Func<string> errorMessageAccessor) : base(errorMessageAccessor)
        {
            Level = EmailValidationLevel.MX;
        }

        /// <summary>
        /// Returns or sets the level to which the email address will be validated.
        /// </summary>
        /// <value>
        /// An enum value indicating the level to which the email address will be validated. Choose a level appropriate
        /// to your requirements in terms of speed of response and acceptable level of false negatives.
        /// </value>
        /// <remarks>
        /// The available values are:
        /// &lt;ol&gt;
        ///   &lt;li&gt;<see cref="EmailValidationLevel.Syntax"/> gives the quickest response but will allow through a large
        /// number of invalid email addresses.&lt;/li&gt;
        ///   &lt;li&gt;<see cref="EmailValidationLevel.MX"/> gives a quick response and checks the domain on the right-hand
        /// side of the @ symbol is valid and configured to receive email.&lt;/li&gt;
        ///   &lt;li&gt;<see cref="EmailValidationLevel.Server"/> gives a slower response and checks that the email servers
        /// to receive mail for the address are currently running.&lt;/li&gt;
        ///   &lt;li&gt;<see cref="EmailValidationLevel.Address"/> gives the slowest response but will allow through the
        /// smallest number of invalid email addresses.&lt;/li&gt;
        /// &lt;/ol&gt;
        /// </remarks>
        public EmailValidationLevel Level { get; set; }

        /// <summary>
        /// Validates the supplied email address to the configured <see cref="Level"/>.
        /// </summary>
        /// <param name="value">The email address to validate.</param>
        /// <returns><c>true</c> if the supplied <paramref name="value"/> is a valid email address, or <c>false</c> otherwise.</returns>
        public override bool IsValid(object value)
        {
            var str = value as string;

            if (String.IsNullOrEmpty(str))
                return true;

            var username = ConfigurationManager.AppSettings["Data8Username"];
            var password = ConfigurationManager.AppSettings["Data8Password"];
            var apikey = ConfigurationManager.AppSettings["Data8APIKey"];
            if (!String.IsNullOrEmpty(apikey))
            {
                username = "apikey-" + apikey;
                password = "";
            }

            var data = new
            {
                username,
                password,
                email = value.ToString(),
                level = Level,
                options = new
                {
                    ApplicationName = "MVC"
                }
            };

            ValidationResponses.EmailValidationResponse outcome = PerformValidation(JsonConvert.SerializeObject(data));

            if (outcome.Status.Success == false)
                return true;

            return outcome.Result != "Invalid";
        }

        private ValidationResponses.EmailValidationResponse PerformValidation(string data)
        {
            var url = "https://webservices.data-8.co.uk/EmailValidation/IsValid.json";
            using (var request = new HttpClient()) {
                var response = request.PostAsync(url, new StringContent(data, System.Text.Encoding.UTF8, "application/json")).Result;
                var emailResult = JsonConvert.DeserializeObject<ValidationResponses.EmailValidationResponse>(response.Content.ReadAsStringAsync().Result);
                return emailResult;
            }
        }
    }
}
