using System;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Web.Mvc;
using Newtonsoft.Json;

namespace Data8.MvcValidation
{
    /// <summary>
    /// Applies consistent formatting to supplied data values.
    /// </summary>
    public class StandardizeFormattingAttribute : ActionFilterAttribute
    {
        public StandardizeFormattingAttribute()
        {
            DefaultCountry = ConfigurationManager.AppSettings["Data8DefaultCountry"];

            if (String.IsNullOrEmpty(DefaultCountry))
                DefaultCountry = "GB";
        }

        /// <summary>
        /// The country code to assume when formatting telephone numbers supplied without an explicit country code.
        /// </summary>
        /// <value>
        /// Specify either an ISO 2-character country code (GB, US, ...), ISO standard country name or international dialling code.
        /// </value>
        public string DefaultCountry { get; set; }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // Standardise formatting of supplied model data based on the metadata on each property.
            foreach (var parameter in filterContext.ActionParameters)
            {
                // Don't bother trying to do standardisation for null models or models of basic types.
                if (parameter.Value == null ||
                    parameter.Value is string ||
                    parameter.Value.GetType().IsPrimitive)
                    continue;

                // Get the metadata for the parameter type.
                var metadata = ModelMetadataProviders.Current.GetMetadataForType(() => parameter.Value, parameter.Value.GetType());

                foreach (var property in metadata.Properties)
                {
                    // Can't do anything for read-only properties
                    if (property.IsReadOnly)
                        continue;

                    var str = property.Model as string;

                    // Can't do any standardisation on null or non-string values
                    if (str == null)
                        continue;

                    // Trim values by default
                    var formattedValue = str.Trim();

                    // Get the reflected property
                    var prop = parameter.Value.GetType().GetProperty(property.PropertyName);

                    // Check what the data type is for this property
                    DataType dataType;
                    Enum.TryParse(property.DataTypeName, out dataType);

                    switch (dataType)
                    {
                        // Email addresses should be converted to lower case
                        case DataType.EmailAddress:
                            formattedValue = str.Trim().ToLower();
                            break;

                        // Phone numbers should be formatted according to the country-specific rules
                        // Find the country from the model, default to GB if not present
                        case DataType.PhoneNumber:
                            {
                                var country = (string)metadata.Properties.FirstOrDefault(p => p.DataTypeName == "Country")?.Model ?? DefaultCountry;
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
                                    number = str.Trim(),
                                    defaultCountry = country,
                                    options = new
                                    {
                                        DefaultCountryCode = country,
                                        ApplicationName = "MVC"
                                    }
                                };

                                TelephoneFormattingResponse result = PerformFormatting(JsonConvert.SerializeObject(data));

                                if (result.Status.Success)
                                    formattedValue = result.FormattedNumber;
                            }
                            break;

                        default:
                            switch (property.DataTypeName)
                            {
                                // Forenames and surnames should be in proper case, but handle Mc/Mac correctly in surnames
                                case "FirstName":
                                case "Forename":
                                    formattedValue = str.Trim().ToProper(false, CultureInfo.CurrentCulture);
                                    break;

                                case "LastName":
                                case "Surname":
                                    formattedValue = str.Trim().ToProper(true, CultureInfo.CurrentCulture);
                                    break;
                            }
                            break;
                    }

                    if (formattedValue != str)
                    {
                        prop.SetValue(parameter.Value, formattedValue, null);
                        ((Controller)filterContext.Controller).ModelState[property.PropertyName].Value = new ValueProviderResult(formattedValue, (string)formattedValue, CultureInfo.CurrentCulture);
                    }
                }
            }
        }

        private TelephoneFormattingResponse PerformFormatting(string data)
        {
            var url = "https://webservices.data-8.co.uk/TelephoneFormatting/FormatTelephoneNumber.json";
            using (var client = new HttpClient())
            {
                var response = client.PostAsync(url, new StringContent(data, System.Text.Encoding.UTF8, "application/json")).Result;
                var phoneResult = JsonConvert.DeserializeObject<TelephoneFormattingResponse>(response.Content.ReadAsStringAsync().Result);
                return phoneResult;
            }
        }
    }

    public class TelephoneFormattingResponse
    {
        public string FormattedNumber { get; set; }
        public Status Status { get; set; }
    }
}
