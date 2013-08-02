using System;
using System.Collections.Generic;
using System.Web.SessionState;
using Raven.AspNet.SessionState;

namespace Tests
{
    public abstract class GetItemExclusiveTest : RavenSessionStateTest
    {
        protected GetItemExclusiveTest()
        {

            bool locked;
            TimeSpan lockAge;
            object lockId;
            SessionStateActions actions;
            Result = Subject.GetItemExclusive(null, SessionId, out locked, out lockAge, out lockId, out actions);
            Locked = locked;
            LockId = lockId;
            LockAge = lockAge;
        }

        protected abstract string SessionId { get; }


        
        protected SessionStateStoreData Result { get; set; }
        protected bool Locked { get; set; }
        protected object LockId { get; set; }
        protected TimeSpan LockAge { get; set; }
    }
}