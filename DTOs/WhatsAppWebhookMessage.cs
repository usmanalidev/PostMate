using System.Text.Json.Serialization;

namespace PostmateAPI.DTOs
{
    public class WhatsAppWebhookMessage
    {
        [JsonPropertyName("object")]
        public string Object { get; set; } = string.Empty;

        [JsonPropertyName("entry")]
        public List<WhatsAppEntry> Entry { get; set; } = new();
    }

    public class WhatsAppEntry
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("changes")]
        public List<WhatsAppChange> Changes { get; set; } = new();
    }

    public class WhatsAppChange
    {
        [JsonPropertyName("value")]
        public WhatsAppValue Value { get; set; } = new();

        [JsonPropertyName("field")]
        public string Field { get; set; } = string.Empty;
    }

    public class WhatsAppValue
    {
        [JsonPropertyName("messaging_product")]
        public string MessagingProduct { get; set; } = string.Empty;

        [JsonPropertyName("metadata")]
        public WhatsAppMetadata Metadata { get; set; } = new();

        [JsonPropertyName("contacts")]
        public List<WhatsAppContact> Contacts { get; set; } = new();

        [JsonPropertyName("messages")]
        public List<WhatsAppMessage> Messages { get; set; } = new();
    }

    public class WhatsAppMetadata
    {
        [JsonPropertyName("display_phone_number")]
        public string DisplayPhoneNumber { get; set; } = string.Empty;

        [JsonPropertyName("phone_number_id")]
        public string PhoneNumberId { get; set; } = string.Empty;
    }

    public class WhatsAppContact
    {
        [JsonPropertyName("profile")]
        public WhatsAppProfile Profile { get; set; } = new();

        [JsonPropertyName("wa_id")]
        public string WaId { get; set; } = string.Empty;
    }

    public class WhatsAppProfile
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class WhatsAppMessage
    {
        [JsonPropertyName("from")]
        public string From { get; set; } = string.Empty;

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; } = string.Empty;

        [JsonPropertyName("text")]
        public WhatsAppText? Text { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
    }

    public class WhatsAppText
    {
        [JsonPropertyName("body")]
        public string Body { get; set; } = string.Empty;
    }

    public class WhatsAppSendMessageRequest
    {
        [JsonPropertyName("messaging_product")]
        public string MessagingProduct { get; set; } = "whatsapp";

        [JsonPropertyName("to")]
        public string To { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = "text";

        [JsonPropertyName("text")]
        public WhatsAppTextMessage Text { get; set; } = new();
    }

    public class WhatsAppTextMessage
    {
        [JsonPropertyName("body")]
        public string Body { get; set; } = string.Empty;
    }

    public class WhatsAppSendMessageResponse
    {
        [JsonPropertyName("messaging_product")]
        public string MessagingProduct { get; set; } = string.Empty;

        [JsonPropertyName("contacts")]
        public List<WhatsAppContact> Contacts { get; set; } = new();

        [JsonPropertyName("messages")]
        public List<WhatsAppMessageResponse> Messages { get; set; } = new();
    }

    public class WhatsAppMessageResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }
}