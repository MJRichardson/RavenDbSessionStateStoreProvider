using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web.Configuration;
using Raven.AspNet.SessionState;
using Raven.Client;
using Raven.Client.Embedded;

namespace Tests
{
    public abstract class RavenSessionStateTest : IDisposable
    {
        protected RavenSessionStateTest()
        {
           DocumentStore = new EmbeddableDocumentStore
           {
              RunInMemory = true
           };
            DocumentStore.Initialize();

            Subject = new RavenSessionStateStoreProvider{ApplicationName = ApplicationName, SessionStateConfig = new SessionStateSection
                {
                    Timeout = Timeout
                }};
            Subject.Initialize("", new NameValueCollection(), DocumentStore);

            //persist pre-existing data
            using (var session = DocumentStore.OpenSession())
            {
                var existingSessionState = PreExistingSessionState();

                if (existingSessionState != null)
                    session.Store(existingSessionState);

                session.SaveChanges();
            }
        } 

        protected abstract SessionStateDocument PreExistingSessionState();

        protected IDocumentStore DocumentStore { get; private set; }
        protected RavenSessionStateStoreProvider Subject { get; private set; }
        protected string ApplicationName {get { return "DummyApplicationName"; }}

        protected TimeSpan Timeout {get { return TimeSpan.FromMinutes(20); }}

        public void Dispose()
        {
            if (DocumentStore != null)
                DocumentStore.Dispose();
        }
    }
}