using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Configuration;
using System.Web.SessionState;
using NLog;
using Raven.Abstractions.Exceptions;
using Raven.AspNet.SessionState.Infrastructure;
using Raven.Client;
using Raven.Client.Document;
using Raven.Json.Linq;

namespace Raven.AspNet.SessionState
{
    /// <summary>
    /// An ASP.NET session-state store-provider implementation (http://msdn.microsoft.com/en-us/library/ms178588.aspx) using 
    /// RavenDb (http://ravendb.net) for persistence.
    /// </summary>
    public class RavenSessionStateStoreProvider : SessionStateStoreProviderBase, IDisposable
    {

        private IDocumentStore _documentStore;
        private SessionStateSection _sessionStateConfig;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Public parameterless constructor
        /// </summary>
        public RavenSessionStateStoreProvider()
        {}

        /// <summary>
        /// Constructor accepting a document store instance, used for testing.
        /// </summary>
        public RavenSessionStateStoreProvider(IDocumentStore documentStore)
        {
            _documentStore = documentStore;
        }

        /// <summary>
        /// The name of the application.
        /// Session-data items will be stored against this name.
        /// If not set, defaults to System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath
        /// </summary>
        public string ApplicationName { get; set; }

        internal SessionStateSection SessionStateConfig
        {
            get { return _sessionStateConfig ?? (_sessionStateConfig = (SessionStateSection) ConfigurationManager.GetSection("system.web/sessionState")); }
            set { _sessionStateConfig = value; }
        }

        public override void Initialize(string name, System.Collections.Specialized.NameValueCollection config)
        {
            Initialize(name, config, null);
        }

        internal void Initialize(string name, System.Collections.Specialized.NameValueCollection config,
            IDocumentStore documentStore)
        {
            
            try
            {
                if (config == null)
                    throw new ArgumentNullException("config");

                Logger.Debug("Beginning Initialize. Name= {0}. Config={1}.", 
                    name, config.AllKeys.Aggregate("", (aggregate, next) => aggregate + next + ":" + config[next]));
           
               
                if (string.IsNullOrEmpty(name))
                    name = "RavenSessionStateStore";

                base.Initialize(name, config);

                if (string.IsNullOrEmpty(ApplicationName))
                    ApplicationName = System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath;
                
                if (documentStore != null)
                    _documentStore = documentStore;

                if (_documentStore == null)
                {
                    if (string.IsNullOrEmpty(config["connectionStringName"]))
                        throw new ConfigurationErrorsException("Must supply a connectionStringName.");

                    _documentStore = new DocumentStore
                                         {
                                             ConnectionStringName = config["connectionStringName"],
                                             Conventions = { JsonContractResolver = new PrivatePropertySetterResolver()},
                                         };

                    _documentStore.Initialize();

                }

                Logger.Debug("Completed Initalize.");

            }
            catch(Exception ex)
            {
                Logger.ErrorException("Error while initializing.", ex);
                throw;
            }
        }


       
        /// <summary>
        /// Retrieves session values and information from the session data store and locks the session-item data 
        /// at the data store for the duration of the request. 
        /// </summary>
        /// <param name="context">The HttpContext instance for the current request</param>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="locked">An output parameter indicating whether the item is currently exclusively locked.</param>
        /// <param name="lockAge">The age of the exclusive lock (if present)</param>
        /// <param name="lockId">The identifier of the exclusive lock (if present)</param>
        /// <param name="actions">Used with sessions whose Cookieless property is true, 
        /// when the regenerateExpiredSessionId attribute is set to true. 
        /// An actionFlags value set to InitializeItem (1) indicates that the entry in the session data store is a 
        /// new session that requires initialization.</param>
        /// <returns>The session data</returns>
        public override SessionStateStoreData GetItemExclusive(HttpContext context,
                                                               string sessionId,
                                                               out bool locked,
                                                               out TimeSpan lockAge,
                                                               out object lockId,
                                                               out SessionStateActions actions)
        {
            try
            {
                Logger.Debug("Beginning GetItemExclusive. SessionId={0}; Application={1}.", sessionId, ApplicationName);

                var item = GetSessionStoreItem(true, context, sessionId,  out locked,
                                           out lockAge, out lockId, out actions);

                Logger.Debug("Completed GetItemExclusive. SessionId={0}, Application={1}, locked={2}, lockAge={3}, lockId={4}, actions={5}.", 
                    sessionId, ApplicationName, locked, lockAge, lockId, actions);

                return item;
            }
            catch (Exception ex)
            {
                Logger.ErrorException(string.Format("Error during GetItemExclusive. SessionId={0}, Application={1}.", sessionId, ApplicationName), ex);
                throw;
            }

        }

        /// <summary>
        /// This method performs the same work as the GetItemExclusive method, except that it does not attempt to lock the session item in the data store.
        /// </summary>
        /// <param name="context">The HttpContext instance for the current request</param>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="locked">An output parameter indicating whether the item is currently exclusively locked.</param>
        /// <param name="lockAge">The age of the exclusive lock (if present)</param>
        /// <param name="lockId">The identifier of the exclusive lock (if present)</param>
        /// <param name="actions">Used with sessions whose Cookieless property is true, 
        /// when the regenerateExpiredSessionId attribute is set to true. 
        /// An actionFlags value set to InitializeItem (1) indicates that the entry in the session data store is a 
        /// new session that requires initialization.</param>
        /// <returns>The session data</returns>
        public override SessionStateStoreData GetItem(HttpContext context, string sessionId, out bool locked,
                                                      out TimeSpan lockAge, out object lockId,
                                                      out SessionStateActions actions)
        {
            try
            {
                Logger.Debug("Beginning GetItem. SessionId={0}, Application={1}.", sessionId, ApplicationName);

                var item =  GetSessionStoreItem(false, context, sessionId,  out locked, out lockAge, out lockId, out actions);

                Logger.Debug("Completed GetItem. SessionId={0}, Application={1}, locked={2}, lockAge={3}, lockId={4}, actions={5}.", 
                    sessionId, ApplicationName, locked, lockAge, lockId, actions);

                return item;
            }
            catch (Exception ex)
            {
                Logger.ErrorException(string.Format("Error during GetItem. SessionId={0}, Application={1}.", 
                    sessionId, ApplicationName), ex);
                throw;
            }
        }


        /// <summary>
        /// If the newItem parameter is true, the SetAndReleaseItemExclusive method inserts a new item into the data store with the supplied values. 
        /// Otherwise, the existing item in the data store is updated with the supplied values, and any lock on the data is released. 
        /// </summary>
        /// <param name="context">The HttpContext instance for the current request</param>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="item">The current session values to be stored</param>
        /// <param name="lockId">The lock identifier for the current request.</param>
        /// <param name="newItem">If true, a new item is inserted into the store.  Otherwise, the existing item in 
        /// the data store is updated with the supplied values, and any lock on the data is released. </param>
        public override void SetAndReleaseItemExclusive(HttpContext context, string sessionId, SessionStateStoreData item,
                                                        object lockId, bool newItem)
        {
            try
            {
                Logger.Debug(
                    " Beginning SetAndReleaseItemExclusive. SessionId={0}, Application: {1}, LockId={2}, newItem={3}.",
                    sessionId, ApplicationName, lockId, newItem);

                if ( item == null)
                    throw new ArgumentNullException("item");

                var serializedItems = Serialize((SessionStateItemCollection) item.Items);

                using (var documentSession = _documentStore.OpenSession())
                {
                    //don't tolerate stale data
                    documentSession.Advanced.AllowNonAuthoritativeInformation = false;

                    SessionStateDocument sessionStateDocument;

                    if (newItem) //if we are creating a new document
                    {
                        sessionStateDocument = new SessionStateDocument(sessionId, ApplicationName);

                        documentSession.Store(sessionStateDocument);

                    }
                    else //we are not creating a new document, so load it
                    {
                        sessionStateDocument =
                            documentSession .Load<SessionStateDocument>(SessionStateDocument.GenerateDocumentId( sessionId, ApplicationName));

                        //if the lock identifier does not match, then we don't modifiy the data
                        if (sessionStateDocument.LockId != (int) lockId)
                        {
                            Logger.Debug(
                                "Lock Id does not match, so data will not be modified. Session Id: {0}; Application: {1}; Lock Id {2}.",
                                sessionId, ApplicationName, lockId);
                            return;
                        }
                    }

                    sessionStateDocument.SessionItems = serializedItems;
                    sessionStateDocument.Locked = false;

                    //set the expiry
                    var expiry = DateTime.UtcNow.AddMinutes(SessionStateConfig.Timeout.TotalMinutes);
                    sessionStateDocument.Expiry = expiry;
                    documentSession.Advanced.GetMetadataFor(sessionStateDocument)["Raven-Expiration-Date"] = new RavenJValue(expiry);

                    documentSession.SaveChanges();
                }

                Logger.Debug("Completed SetAndReleaseItemExclusive. SessionId={0}; Application:{1}; LockId={2}; newItem={3}.", sessionId, ApplicationName, lockId, newItem);

            }
            catch(Exception ex)
            {
                Logger.ErrorException(string.Format("Error during SetAndReleaseItemExclusive. SessionId={0}; Application={1}; LockId={2}, newItem={3}.", sessionId, ApplicationName, lockId, newItem), ex);
                throw;
            }
        }

        /// <summary>
        /// Releases the lock on an item in the session data store.
        /// </summary>
        /// <param name="context">The HttpContext instance for the current request</param>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="lockId">The lock identifier for the current request.</param>
        public override void ReleaseItemExclusive(HttpContext context, string sessionId, object lockId)
        {
            try
            {
                Logger.Debug("Beginning ReleaseItemExclusive. SessionId={0}; Application={1}; LockId={2}.", sessionId, ApplicationName, lockId);

                using (var documentSession = _documentStore.OpenSession())
                {
                    //don't tolerate stale data
                    documentSession.Advanced.AllowNonAuthoritativeInformation = false;
                    
                    var sessionState =
                            documentSession
                                .Load<SessionStateDocument>(SessionStateDocument.GenerateDocumentId( sessionId, ApplicationName));

                    //if the session-state is not present (it may have expired and been removed) or
                    //the locked id does not match, then we do nothing
                    if (sessionState == null || sessionState.LockId != (int) lockId)
                    {
                        Logger.Debug(
                            "Session state was not present or lock id did not match. Session id: {0}; Application: {1}; Lock id: {2}.",
                            sessionId, ApplicationName, lockId);
                        return;
                    }

                    sessionState.Locked = false;

                    //update the expiry
                    var expiry = DateTime.UtcNow.AddMinutes(SessionStateConfig.Timeout.TotalMinutes);
                    sessionState.Expiry = expiry;
                    documentSession.Advanced.GetMetadataFor(sessionState)["Raven-Expiration-Date"] = new RavenJValue(expiry);

                    documentSession.SaveChanges();
                }

                Logger.Debug("Completed ReleaseItemExclusive. SessionId={0}; Application={1}; LockId={2}.", sessionId, ApplicationName, lockId);

            }
            catch(Exception ex)
            {
                Logger.ErrorException(string.Format("Error during ReleaseItemExclusive. SessionId={0}; Application={1}; LockId={2}.", sessionId, ApplicationName, lockId), ex);
                throw;
            }
        }


        /// <summary>
        /// deletes the session information from the data store where the data store item matches the supplied SessionID value, 
        /// the current application, and the supplied lock identifier.
        /// </summary>
        /// <param name="context">The HttpContext instance for the current request</param>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="lockId">The exclusive-lock identifier.</param>
        /// <param name="item"></param>
        public override void RemoveItem(HttpContext context, string sessionId, object lockId, SessionStateStoreData item)
        {
            try
            {
                Logger.Debug("Beginning RemoveItem. SessionId={0}; Application={1}; lockId={2}.", sessionId,
                    ApplicationName, lockId);

                using (var documentSession = _documentStore.OpenSession())
                {
                    //don't tolerate stale data
                    documentSession.Advanced.AllowNonAuthoritativeInformation = false;

                    var sessionStateDocument = documentSession
                                .Load<SessionStateDocument>(SessionStateDocument.GenerateDocumentId( sessionId, ApplicationName));


                    if (sessionStateDocument != null && sessionStateDocument.LockId == (int) lockId)
                    {
                        documentSession.Delete(sessionStateDocument);
                        documentSession.SaveChanges();
                    }
                }

                Logger.Debug("Completed RemoveItem. SessionId={0}; Application={1}; lockId={2}.", sessionId,
                    ApplicationName, lockId);
            }
            catch (Exception ex)
            {
                Logger.ErrorException(string.Format("Error during RemoveItem. SessionId={0}; Application={1}; lockId={2}", sessionId, ApplicationName, lockId), ex);
                throw;
            }
        }

        /// <summary>
        /// Resets the expiry timeout for a session item.
        /// </summary>
        /// <param name="context">The HttpContext instance for the current request</param>
        /// <param name="sessionId">The session identifier.</param>
        public override void ResetItemTimeout(HttpContext context, string sessionId)
        {
            try
            {
                Logger.Debug("Beginning ResetItemTimeout. SessionId={0}; Application={1}.", sessionId, ApplicationName);


                using (var documentSession = _documentStore.OpenSession())
                {
                    //we never want to over-write data with this method
                    documentSession.Advanced.UseOptimisticConcurrency = true;

                    var sessionStateDocument = documentSession
                        .Load<SessionStateDocument>(SessionStateDocument.GenerateDocumentId(sessionId, ApplicationName));


                    if (sessionStateDocument != null)
                    {
                        var expiry = DateTime.UtcNow.AddMinutes(SessionStateConfig.Timeout.TotalMinutes);
                        sessionStateDocument.Expiry = expiry;
                        documentSession.Advanced.GetMetadataFor(sessionStateDocument)["Raven-Expiration-Date"] =
                            new RavenJValue(expiry);

                        documentSession.SaveChanges();
                    }
                }

                Logger.Debug("Completed ResetItemTimeout. SessionId={0}; Application={1}.", sessionId, ApplicationName);
            }
            catch (ConcurrencyException ex)
            {
               //swallow, we don't care 
            }
            catch(Exception ex)
            {
                Logger.ErrorException("Error during ResetItemTimeout. SessionId=" + sessionId, ex);
                throw;
            }
        }


        /// <summary>
        /// Adds an uninitialized item to the session data store.
        /// </summary>
        /// <param name="context">The HttpContext instance for the current request</param>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="timeout">The expiry timeout in minutes.</param>
        public override void CreateUninitializedItem(HttpContext context, string sessionId, int timeout)
        {
            try
            {
                Logger.Debug("Beginning CreateUninitializedItem. SessionId={0}; Application={1}; timeout={1}.", sessionId, ApplicationName, timeout);

                using (var documentSession = _documentStore.OpenSession())
                {
                    var expiry = DateTime.UtcNow.AddMinutes(timeout);

                    var sessionStateDocument = new SessionStateDocument(sessionId, ApplicationName)
                        {
                            Expiry = expiry
                        };

                    documentSession.Store(sessionStateDocument);
                    documentSession.Advanced.GetMetadataFor(sessionStateDocument)["Raven-Expiration-Date"] =
                        new RavenJValue(expiry);

                    documentSession.SaveChanges();
                }

                Logger.Debug("Completed CreateUninitializedItem. Sessionid={0}; Application={1}; timeout={1}.", sessionId, ApplicationName, timeout);

            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error during CreateUninitializedItem.", ex);
                throw;
            }
        }


        /// <summary>
        ///  returns a new SessionStateStoreData object with an empty ISessionStateItemCollection object, 
        ///  an HttpStaticObjectsCollection collection, and the specified Timeout value.
        /// </summary>
        /// <param name="context">The HttpContext instance for the current request</param>
        /// <param name="timeout">The expiry timeout in minutes.</param>
        /// <returns>A newly created SessionStateStoreData object.</returns>
        public override SessionStateStoreData CreateNewStoreData(HttpContext context, int timeout)
        {
            return new SessionStateStoreData(new SessionStateItemCollection(),
                                             GetSessionStaticObjects(context),
                                             timeout);
        }

        /// <summary>
        /// Takes as input a delegate that references the Session_OnEnd event defined in the Global.asax file. 
        /// If the session-state store provider supports the Session_OnEnd event, a local reference to the 
        /// SessionStateItemExpireCallback parameter is set and the method returns true; otherwise, the method returns false.
        /// </summary>
        /// <param name="expireCallback">A callback.</param>
        /// <returns>False.</returns>
        public override bool SetItemExpireCallback(SessionStateItemExpireCallback expireCallback)
        {
            return false;
        }

        /// <summary>
        /// Performs any initialization required by your session-state store provider.
        /// </summary>
        /// <param name="context">The HttpContext instance for the current request</param>
        public override void InitializeRequest(HttpContext context)
        {
        }

        /// <summary>
        /// Performs any cleanup required by your session-state store provider.
        /// </summary>
        /// <param name="context">The HttpContext instance for the current request</param>
        public override void EndRequest(HttpContext context)
        {
        }

        public override void Dispose()
        {
            try
            {
                if (_documentStore != null)
                    _documentStore.Dispose();
            }
            catch(Exception ex)
            {
                Logger.ErrorException("An exception was thrown while disposing the DocumentStore: ", ex);
                //swallow the exception...nothing good can come from throwing it here!
            }
        }

        //
        // GetSessionStoreItem is called by both the GetItem and 
        // GetItemExclusive methods. GetSessionStoreItem retrieves the 
        // session data from the data source. If the lockRecord parameter
        // is true (in the case of GetItemExclusive), then GetSessionStoreItem
        // locks the record and sets a new LockId and LockDate.
        //
        private SessionStateStoreData GetSessionStoreItem(bool lockRecord,
                                                          HttpContext context,
                                                          string sessionId,
                                                          out bool locked,
                                                          out TimeSpan lockAge,
                                                          out object lockId,
                                                          out SessionStateActions actionFlags)
        {
            // Initial values for return value and out parameters.
            lockAge = TimeSpan.Zero;
            lockId = null;
            locked = false;
            actionFlags = 0;

            using (var documentSession = _documentStore.OpenSession())
            {
                //don't tolerate stale data
                documentSession.Advanced.AllowNonAuthoritativeInformation = false;

                Logger.Debug("Retrieving item from RavenDB. SessionId: {0}; Application: {1}.", sessionId, ApplicationName);

                    var sessionState = documentSession.Load<SessionStateDocument>(SessionStateDocument.GenerateDocumentId( sessionId, ApplicationName));

                if (sessionState == null)
                {
                    Logger.Debug("Item not found in RavenDB with SessionId: {0}; Application: {1}.",sessionId, ApplicationName);
                    return null;
                }

                //if the record is locked, we can't have it.
                if (sessionState.Locked)
                {
                    Logger.Debug("Item retrieved is locked. SessionId: {0}; Application: {1}.", sessionId, ApplicationName );

                    locked = true;
                    lockAge = DateTime.UtcNow.Subtract((DateTime) sessionState.LockDate);
                    lockId = sessionState.LockId;
                    return null;
                }

                //generally we shouldn't get expired items, as the expiration bundle should clean them up,
                //but just in case the bundle isn't installed, or we made the window, we'll delete expired items here.
                if (DateTime.UtcNow > sessionState.Expiry)
                {
                    Logger.Debug("Item retrieved has expired. SessionId: {0}; Application: {1}; Expiry (UTC): {2}", sessionId, ApplicationName, sessionState.Expiry);

                    try
                    {
                        documentSession.Delete(sessionState);
                        documentSession.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        //we never want this clean-up op to throw
                        Logger.DebugException("Exception thrown while attempting to remove expired item.", ex);
                    }

                    return null;
                }

                if (lockRecord)
                {
                    sessionState.Locked = true;
                    sessionState.LockId += 1;
                    sessionState.LockDate = DateTime.UtcNow;

                    documentSession.SaveChanges();
                }

                lockId = sessionState.LockId;
                return
                    sessionState.Flags == SessionStateActions.InitializeItem
                        ? new SessionStateStoreData(new SessionStateItemCollection(),
                                                    GetSessionStaticObjects(context),
                                                    (int)SessionStateConfig.Timeout.TotalMinutes)
                        : Deserialize(context, sessionState.SessionItems, (int)SessionStateConfig.Timeout.TotalMinutes);
            }
        }

        internal string Serialize(SessionStateItemCollection items)
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream))
                {
                    if (items != null)
                        items.Serialize(writer);

                    writer.Flush();
                    writer.Close();

                    return Convert.ToBase64String(stream.ToArray());
                }
            }
        }

        internal static SessionStateStoreData Deserialize(HttpContext context,
                                                         string serializedItems, int timeout)
        {
            using (var stream = new MemoryStream(Convert.FromBase64String(serializedItems)))
            {
                var sessionItems = new SessionStateItemCollection();

                if (stream.Length > 0)
                {
                    using (var reader = new BinaryReader(stream))
                    {
                        sessionItems = SessionStateItemCollection.Deserialize(reader);
                    }
                }

                return new SessionStateStoreData(sessionItems,
                                                 GetSessionStaticObjects(context),
                                                 timeout);
            }
        }

        //facilitates testing by allowing a null HTTP context to be supplied
        private static HttpStaticObjectsCollection  GetSessionStaticObjects(HttpContext context)
        {
            return context != null
                ? SessionStateUtility.GetSessionStaticObjects(context)
                : new HttpStaticObjectsCollection();
        }
    }
}