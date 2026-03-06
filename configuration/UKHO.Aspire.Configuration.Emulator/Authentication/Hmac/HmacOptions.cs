using Microsoft.AspNetCore.Authentication;

namespace UKHO.Aspire.Configuration.Emulator.Authentication.Hmac
{
    public class HmacOptions : AuthenticationSchemeOptions
    {
        public string Credential { get; set; } = default!;

        public string Secret { get; set; } = default!;
    }
}