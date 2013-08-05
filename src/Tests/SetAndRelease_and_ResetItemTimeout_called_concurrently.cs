using System;
using System.Threading.Tasks;
using System.Web.SessionState;
using Raven.AspNet.SessionState;
using System.Linq;
using Xunit;

namespace Tests
{
    public class SetAndRelease_and_ResetItemTimeout_called_concurrently : RavenSessionStateTest
    {
        private const int Iterations = 10000;

        public SetAndRelease_and_ResetItemTimeout_called_concurrently()
        {
            var modifyTask = Task.Factory.StartNew(() =>
            {
                for (int i = 0; i < Iterations; i++)
                {
                    object lockId;
                    bool locked;
                    TimeSpan lockAge;
                    SessionStateActions actions;

                    var sessionState = Subject.GetItemExclusive(null, SessionId, out locked, out lockAge, out lockId, out actions);

                    Subject.SetAndReleaseItemExclusive(null, SessionId, sessionState, lockId, false );
                }

            });

            var resetTimeoutTask = Task.Factory.StartNew(() =>
            {
                for (int i = 0; i < Iterations; i++)
                {
                    Subject.ResetItemTimeout(null, SessionId);
                }
            });

            Task.WaitAll(modifyTask, resetTimeoutTask);
        }

        protected  string SessionId
        {
            get { return "BLAHBLAHBLAH"; }
        }

        protected override SessionStateDocument PreExistingSessionState()
        {
            var items = new SessionStateItemCollection();
            items["Name"] = "Roger Ramjet";

            return new SessionStateDocument(SessionId, ApplicationName)
                {
                    Locked = false,
                    LockId = 3,
                    SessionItems = Subject.Serialize(items),
                }; 
        }

        protected override SessionStateExpiryDocument PreExistingExpiry()
        {
            return new SessionStateExpiryDocument(SessionId, ApplicationName)
            {
                Expiry = DateTime.UtcNow.AddMinutes(1)
            }; 
        }

        [Fact]
        public void no_exception_is_thrown()
        {
            //so long as no exception is thrown, we're happy
        }
    }
}