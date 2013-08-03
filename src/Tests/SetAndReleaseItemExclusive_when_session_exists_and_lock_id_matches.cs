using System;
using System.Web.SessionState;
using Raven.AspNet.SessionState;
using Xunit;

namespace Tests
{
    public class SetAndReleaseItemExclusive_when_session_exists_and_lock_id_matches : SetAndReleaseItemExclusiveTest
    {

        protected override string SessionId
        {
            get { return "BLAHBLAHBLAH"; }
        }

        protected override int LockId
        {
            get { return 3; }
        }

        protected override bool NewItem
        {
            get { return false; }
        }

        protected DateTime ExistingExpiry = DateTime.UtcNow.AddMinutes(3);

        protected override SessionStateItemCollection Items
        {
            get { 
                var items = new SessionStateItemCollection();
                items["Name"] = "Papa Smurf"; 
                return items;
            }
        }

        protected override SessionStateDocument PreExistingSessionState()
        {
            var items = new SessionStateItemCollection();
            items["Name"] = "Roger Ramjet";

            return new SessionStateDocument(SessionId, ApplicationName)
                {
                    Locked = true,
                    LockId = LockId,
                    SessionItems = Subject.Serialize(items),
                    Expires = DateTime.UtcNow.AddMinutes(1)
                }; 
        }

        [Fact]
        public void item_is_unlocked()
        {
            Assert.False(Result.Locked);
        }


        [Fact]
        public void data_is_modified()
        {
          Assert.Equal(Subject.Serialize(Items), Result.SessionItems );  
        }

        [Fact]
        public void expiry_is_extended()
        {
            Assert.True(Result.Expires > ExistingExpiry);
        }
    }
}