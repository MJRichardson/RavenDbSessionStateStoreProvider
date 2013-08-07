using System.Collections.Generic;
using Raven.AspNet.SessionState;
using Xunit;

namespace Tests
{
    public class GetItemExclusive_when_item_does_not_exist : GetItemExclusiveTest
    {

        protected override string SessionId
        {
            get { return "XXX"; }
        }

        protected override SessionStateDocument PreExistingSessionState()
        {
            return null;
        }

        [Fact]
        public void returns_null()
        {
           Assert.Null(Result); 
        }

        [Fact]
        public void outputs_not_locked()
        {
           Assert.False(Locked); 
        }


    }
}