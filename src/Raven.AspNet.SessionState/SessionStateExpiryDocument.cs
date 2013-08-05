using System;
using Raven.Imports.Newtonsoft.Json;

namespace Raven.AspNet.SessionState
{
    public class SessionStateExpiryDocument
    {

        public SessionStateExpiryDocument(string sessionId, string applicationName )
        {
            SessionId = sessionId;
            ApplicationName = applicationName;
            Id = GenerateDocumentId(sessionId, applicationName);
        }

        public string Id { get; private set; }
        public string SessionId { get; private set; }
        public string ApplicationName { get; private set; }
        public DateTime Expiry { get; set; }

        [JsonIgnore]
        public bool IsExpired
        {
            get { return Expiry < DateTime.UtcNow; }
        }

        public static string GenerateDocumentId(string sessionId, string applicationName)
        {
            return "sessionStateExpiry" + applicationName + "/" + sessionId;
        }
    }
}