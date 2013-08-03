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

        protected SessionStateDocument Result { get; private set; }

        public ReleaseItemExclusive_when_lock_id_matches()
        {
            //call ReleaseItemExclusive with matching lockId  
           Subject.ReleaseItemExclusive(null, SessionId, LockIdExisting); 

            using (var session = DocumentStore.OpenSession())
            {
                Result = session.Load<SessionStateDocument>(SessionStateDocument.GenerateDocumentId(SessionId, ApplicationName));
            }
        }

        protected override IEnumerable<SessionStateDocument> PreExistingSessionState()
        {
            return new List<SessionStateDocument>
                {
                    new SessionStateDocument(SessionId, ApplicationName)
                        {
                            Locked = true, 
                            LockId = LockIdExisting,
                            Expires = ExpiryExisting
                        }
                };
        }

        [Fact]
        public void lock_is_removed()
        {
           Assert.False(Result.Locked); 
        }

        [Fact]
        public void expiry_is_extended()
        {
            Assert.True(Result.Expires >= ExpiryExisting.Add(Timeout));
        }
    }
}