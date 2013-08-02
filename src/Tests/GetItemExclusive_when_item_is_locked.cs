using System;
using Raven.AspNet.SessionState;
using Xunit;

namespace Tests
{
    public class GetItemExclusive_when_item_is_locked : GetItemExclusiveTest
    {
        public GetItemExclusive_when_item_is_locked()
        {
            LockDate = DateTime.Now.AddMinutes(-1);

            var item = new SessionState(SessionId, ApplicationName)
            {
                Locked = true,
                LockId = LockIdExisting,
                LockDate = LockDate 
            };

            using (var session = DocumentStore.OpenSession())
            {
                session.Store(item);
                session.SaveChanges();
            }
            
        }

        protected int LockIdExisting {get { return 66; }}
        protected DateTime LockDate { get; set; }

        protected override string SessionId
        {
            get { return "IEXIST"; }
        }

        [Fact]
        public void returns_null()
        {
           Assert.Null(Result); 
        }

        [Fact]
        public void outputs_lock_id()
        {
            Assert.Equal(LockIdExisting, LockId);
        }
    }
}