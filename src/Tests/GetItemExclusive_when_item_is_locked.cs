using System;
using System.Collections.Generic;
using Raven.AspNet.SessionState;
using Xunit;

namespace Tests
{
    public class GetItemExclusive_when_item_is_locked : GetItemExclusiveTest
    {

        protected int LockIdExisting {get { return 66; }}
        protected DateTime LockDate = DateTime.UtcNow.AddMinutes(-1);

        protected override string SessionId
        {
            get { return "IEXIST"; }
        }

        protected override IEnumerable<SessionState> PreExistingSessionState()
        {
            return new List<SessionState>
                {

                    new SessionState(SessionId, ApplicationName)
                        {
                            Locked = true,
                            LockId = LockIdExisting,
                            LockDate = LockDate
                        }
                };
        }

        [Fact]
        public void returns_null()
        {
           Assert.Null(Result); 
        }

        [Fact]
        public void outputs_locked_true()
        {
            Assert.True(Locked);
        }

        [Fact]
        public void outputs_lock_id()
        {
            Assert.Equal(LockIdExisting, LockId);
        }

        [Fact]
        public void outputs_lock_age()
        {
           Assert.InRange(LockAge, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2)); 
        }
    }
}