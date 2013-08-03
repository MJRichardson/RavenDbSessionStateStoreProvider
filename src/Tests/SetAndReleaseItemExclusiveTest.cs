using System.Web;
using System.Web.SessionState;
using Raven.AspNet.SessionState;

namespace Tests
{
    public abstract class SetAndReleaseItemExclusiveTest : RavenSessionStateTest
    {
        protected SetAndReleaseItemExclusiveTest()
        {
            Subject.SetAndReleaseItemExclusive(null, SessionId, new SessionStateStoreData(Items, new HttpStaticObjectsCollection(), (int)Timeout.TotalMinutes ), LockId, NewItem);

            using (var session = DocumentStore.OpenSession())
            {
                Result = session.Load<SessionStateDocument>(SessionStateDocument.GenerateDocumentId(SessionId, ApplicationName));
            }
        }

        protected abstract string SessionId { get; }
        protected abstract int LockId { get; }
        protected abstract bool NewItem { get; }
        protected abstract SessionStateItemCollection Items { get; }

        protected SessionStateDocument Result { get; set; }


    }
}