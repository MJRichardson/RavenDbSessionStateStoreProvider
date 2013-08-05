using System;
using System.Collections.Generic;
using Raven.AspNet.SessionState;
using Xunit;

namespace Tests
{
    public class ReleaseItemExclusive_when_lock_id_matches : RavenSessionStateTest
    {
        protected const string SessionId = "XVB";
        protected const int LockIdExisting = 4;
        protected DateTime ExpiryExisting = DateTime.UtcNow.AddMilliseconds(6);

        protected SessionStateDocument PersistedSessionStateDocument { get; set; }
        protected SessionStateExpiryDocument PersistedExpiryDocument { get; set; }

        public ReleaseItemExclusive_when_lock_id_matches()
        {
            //call ReleaseItemExclusive with matching lockId  
           Subject.ReleaseItemExclusive(null, SessionId, LockIdExisting); 

            using (var session = DocumentStore.OpenSession())
            {
                PersistedSessionStateDocument = session.Load<SessionStateDocument>(SessionStateDocument.GenerateDocumentId(SessionId, ApplicationName));
                PersistedExpiryDocument = session.Load<SessionStateExpiryDocument>(SessionStateExpiryDocument.GenerateDocumentId(SessionId, ApplicationName));
            }
        }

        protected override SessionStateDocument PreExistingSessionState()
        {
            return
                new SessionStateDocument(SessionId, ApplicationName)
                    {
                        Locked = true,
                        LockId = LockIdExisting,
                    };
        }

        protected override SessionStateExpiryDocument PreExistingExpiry()
        {
            return new SessionStateExpiryDocument(SessionId, ApplicationName)
            {
                Expiry = ExpiryExisting
            }; 
        }

        [Fact]
        public void lock_is_removed()
        {
           Assert.False(PersistedSessionStateDocument.Locked); 
        }

        [Fact]
        public void expiry_is_extended()
        {
            Assert.True(PersistedExpiryDocument.Expiry >= ExpiryExisting.Add(Timeout));
        }
    }
}