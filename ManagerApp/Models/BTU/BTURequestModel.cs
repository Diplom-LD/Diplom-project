using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ManagerApp.Models.BTU
{
    public class BTURequestModel : IValidatableObject
    {
        [JsonPropertyName("room_size")]
        [Required(ErrorMessage = "Room size is required.")]
        [Range(1, 500, ErrorMessage = "Room size must be between 1 and 500.")]
        public double RoomSize { get; set; }

        [JsonPropertyName("size_unit")]
        [Required(ErrorMessage = "Size unit is required.")]
        [RegularExpression("square meters|square feet", ErrorMessage = "Size unit must be either 'square meters' or 'square feet'.")]
        public string SizeUnit { get; set; } = "square meters";

        [JsonPropertyName("ceiling_height")]
        [Required(ErrorMessage = "Ceiling height is required.")]
        [Range(2, 10, ErrorMessage = "Ceiling height must be between 2 and 10.")]
        public double CeilingHeight { get; set; }

        [JsonPropertyName("height_unit")]
        [Required(ErrorMessage = "Height unit is required.")]
        [RegularExpression("meters|feet", ErrorMessage = "Height unit must be either 'meters' or 'feet'.")]
        public string HeightUnit { get; set; } = "meters";

        [JsonPropertyName("sun_exposure")]
        [Required(ErrorMessage = "Sun exposure is required.")]
        [RegularExpression("low|medium|high", ErrorMessage = "Sun exposure must be 'low', 'medium', or 'high'.")]
        public string SunExposure { get; set; } = "medium"; 

        [JsonPropertyName("people_count")]
        [Required(ErrorMessage = "People count is required.")]
        [Range(1, 100, ErrorMessage = "People count must be between 1 and 100.")]
        public int PeopleCount { get; set; }

        [JsonPropertyName("number_of_computers")]
        [Required(ErrorMessage = "Number of computers is required.")]
        [Range(0, 50, ErrorMessage = "Number of computers must be between 0 and 50.")]
        public int NumberOfComputers { get; set; }

        [JsonPropertyName("number_of_tvs")]
        [Required(ErrorMessage = "Number of TVs is required.")]
        [Range(0, 50, ErrorMessage = "Number of TVs must be between 0 and 50.")]
        public int NumberOfTVs { get; set; }

        [JsonPropertyName("other_appliances_kwattage")]
        [Required(ErrorMessage = "Other appliances wattage is required.")]
        [Range(0, 20, ErrorMessage = "Other appliances wattage must be between 0 and 20.")]
        public double OtherAppliancesKWattage { get; set; }

        [JsonPropertyName("has_ventilation")]
        public bool HasVentilation { get; set; } = false;

        [JsonPropertyName("air_exchange_rate")]
        [Range(0.5, 3.0, ErrorMessage = "Air exchange rate must be between 0.5 and 3.0.")]
        public double? AirExchangeRate { get; set; }

        [JsonPropertyName("guaranteed_20_degrees")]
        public bool Guaranteed20Degrees { get; set; } = false;

        [JsonPropertyName("is_top_floor")]
        public bool IsTopFloor { get; set; } = false;

        [JsonPropertyName("has_large_window")]
        public bool HasLargeWindow { get; set; } = false;

        [JsonPropertyName("window_area")]
        [Range(0, 100, ErrorMessage = "Window area must be between 0 and 100.")]
        public double? WindowArea { get; set; }

#pragma warning disable IDE0300
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (HasVentilation)
            {
                if (AirExchangeRate == null)
                    yield return new ValidationResult(
                        "If 'HasVentilation' is true, 'AirExchangeRate' must be specified.",
                        new[] { nameof(AirExchangeRate) });
            }
            else
            {
                AirExchangeRate = null;
            }

            if (HasLargeWindow)
            {
                if (WindowArea == null)
                    yield return new ValidationResult(
                        "If 'HasLargeWindow' is true, 'WindowArea' must be specified.",
                        new[] { nameof(WindowArea) });
            }
            else
            {
                WindowArea = null; 
            }
        }
#pragma warning restore IDE0300
    }
}
