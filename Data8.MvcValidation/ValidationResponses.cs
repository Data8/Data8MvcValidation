using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data8.MvcValidation
{
    public static class ValidationResponses
    {
        public class Status
        {
            public bool Success { get; set; }
            public string ErrorMessage { get; set; }
            public decimal CreditsRemaining { get; set; }
        }

        public class EmailValidationResponse
        {
            public string Result { get; set; }
            public Status Status { get; set; }
        }

        public enum EmailValidationLevel { 
            Syntax,
            MX,
            Server,
            Address
        }

        public class PhoneValidationResponse
        {
            public PhoneResult Result { get; set; }
            public Status Status { get; set; }
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

}
