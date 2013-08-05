﻿using System;
using System.Web.SessionState;
using Newtonsoft.Json;

namespace Raven.AspNet.SessionState
{
    public class SessionStateDocument
    {
        public SessionStateDocument(string sessionId, string applicationName)
        {
            SessionId = sessionId;
            ApplicationName = applicationName;
            Id = GenerateDocumentId(sessionId, applicationName);
            ExpiryDocumentId = SessionStateExpiryDocument.GenerateDocumentId(sessionId, applicationName);
            Created = DateTime.UtcNow;
            SessionItems = string.Empty;
        }

        public string Id { get; private set; }
        public string SessionId { get; private set; }
        public string ApplicationName { get; private set; }
        public DateTime Created { get; private set; }
        public DateTime LockDate { get; set; }
        public int LockId { get; set; }
        public bool Locked { get; set; }
        public string SessionItems { get; set; }
        public SessionStateActions Flags { get; set; }
        public string ExpiryDocumentId { get;  private set; }

        public static string GenerateDocumentId(string sessionId, string applicationName)
        {
            return "sessionState/" + applicationName + "/" + sessionId;
        }


    }
}