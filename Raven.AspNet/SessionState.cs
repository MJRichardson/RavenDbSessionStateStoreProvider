using System;
using System.Web.SessionState;

namespace Raven.AspNet
{
    internal class SessionState
    {
        public SessionState(string sessionId, string applicationName)
        {
            SessionId = sessionId;
            ApplicationName = applicationName;
            Created = DateTime.UtcNow;
            SessionItems = string.Empty;
        }

        public string SessionId { get; set; }
        public string ApplicationName { get; set; }
        public DateTime Created { get; set; }
        public DateTime Expires { get; set; }
        public DateTime LockDate { get; set; }
        public int LockId { get; set; }
        public bool Locked { get; set; }
        public string SessionItems { get; set; }
        public SessionStateActions Flags { get; set; }


    }
}