using System;
using System.Collections.Specialized;
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
              DataDirectory = "Data"
           };
            DocumentStore.Initialize();

            Subject = new RavenSessionStateStoreProvider();
            Subject.Initialize("", new NameValueCollection(), DocumentStore);
        } 

        protected IDocumentStore DocumentStore { get; private set; }
        protected RavenSessionStateStoreProvider Subject { get; private set; }
        protected string ApplicationName {get { return "DummyApplicationName"; }}

        public void Dispose()
        {
            if (DocumentStore != null)
                DocumentStore.Dispose();
        }
    }
}