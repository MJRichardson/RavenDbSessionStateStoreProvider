using System;
using System.Collections.Generic;
using Raven.AspNet.SessionState;
using Xunit;

namespace Tests
{
    public class ReleaseItemExclusive_when_lock_id_does_not_match : RavenSessionStateTest
    {
        protected const string SessionId = "XVB";
        protected const int LockIdExisting = 4;
        protected DateTime ExpiryExisting = DateTime.UtcNow.AddMilliseconds(6);

        protected SessionStateDocument Result { get; private set; }

        public ReleaseItemExclusive_when_lock_id_does_not_match()
        {
            //call ReleaseItemExclusive with a lockId that does not match
           Subject.ReleaseItemExclusive(null, SessionId, 3); 

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
        public void item_remains_locked()
        {
            Assert.True(Result.Locked);
        }

        [Fact]
        public void lock_id_is_unchanged()
        {
            Assert.Equal(LockIdExisting, Result.LockId);
        }

        [Fact]
        public void expiry_is_unchanged()
        {
            Assert.Equal(ExpiryExisting, Result.Expires);
        }
    }
}