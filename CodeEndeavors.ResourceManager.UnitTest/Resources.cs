using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using CodeEndeavors.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeEndeavors.ResourceManager.UnitTest
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class Resources
    {
        public Resources()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private static ResourceRepository _repo = null;
        private static string _userId = "1";
        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext) 
        {
            //_repo = new ResourceRepository("RavenDB", ResourceRepository.RepositoryType.RavenDb);
            
        }
        //
        // Use ClassCleanup to run code after all tests in a class have run
         [ClassCleanup()]
         public static void MyClassCleanup() 
         {
             //if (_repo != null)
             //    _repo.Dispose();
         }
        //
        // Use TestInitialize to run code before running each test 
         [TestInitialize()]
         public void MyTestInitialize() 
         {
             _repo = new ResourceRepository(ConfigurationManager.AppSettings.GetSetting("RepositoryConnection", @"{ type:'File', resourceDir:'~/App_Data/FileDb', cacheConnection: {cacheName: 'MyCache', cacheType: 'CodeEndeavors.Distributed.Cache.Client.InMemory.InMemoryCache'} }"));
         }
        //
        // Use TestCleanup to run code after each test has run
         [TestCleanup()]
         public void MyTestCleanup() 
         {
             if (_repo != null)
                 _repo.Dispose();

         }
        //
        #endregion

        [TestMethod]
        public void BasicGetPurge()
        {
            var type = "foo3";
            _repo.DeleteAll<string>(type);

            var resources = _repo.GetResources<string>(type);
            Assert.AreEqual(0, resources.Count);

            var resource = new DomainObjects.Resource<string>()
            {
                Type = type,
                Sequence = 1,
                Data = "BAR"
            };

            resource = _repo.StoreResource(resource, _userId);
            _repo.SaveChanges();

            Assert.IsNotNull(_repo.GetResourceById<string>(resource.Id));

            resources = _repo.GetResources<string>(type);
            Assert.AreEqual(1, resources.Count);

            _repo.Delete<string>(resource);
            _repo.SaveChanges();

            resources = _repo.GetResources<string>(type);
            Assert.AreEqual(0, resources.Count);
        }

        [TestMethod]
        public void BasicMatch()
       { 
            var type = "Widgets";
            var key = "";
            var pageId = 1;
            _repo.DeleteAll<Widget>(type);
            _repo.StoreResource(NewWidgetResource(type, key, 1, "TitleBar", pageId), _userId);
            _repo.StoreResource(NewWidgetResource(type, key, 2, "TextHtml", pageId), _userId);
            _repo.StoreResource(NewWidgetResource(type, key, 1, "TextHtml", 2), _userId);
            _repo.SaveChanges();

            var widgets = _repo.GetResources<Widget>(type);
            Assert.AreEqual(3, widgets.Count);

            widgets = _repo.GetResources<Widget>(type, key, w => w.Scope.PageId == -1);
            Assert.AreEqual(0, widgets.Count);

            widgets = _repo.GetResources<Widget>(type, key, w => w.Scope.PageId == pageId);
            Assert.AreEqual(2, widgets.Count);
        }

        [TestMethod]
        public void ExpirationMatch()
        {
            var type = "Widgets";
            var key = "";
            var pageId = 1;
            _repo.DeleteAll<Widget>(type);
            _repo.StoreResource(NewWidgetResource(type, key, 1, "TextHtml", pageId), _userId);
            _repo.StoreResource(NewWidgetResource(type, key, 1, "TextHtml", pageId, DateTimeOffset.MaxValue), _userId);
            _repo.StoreResource(NewWidgetResource(type, key, 1, "TextHtml", pageId, null, DateTimeOffset.MinValue), _userId);
            _repo.SaveChanges();

            var widgets = _repo.GetResources<Widget>(type, key, w => w.Scope.PageId == pageId);
            Assert.AreEqual(1, widgets.Count);
        }

        [TestMethod]
        public void MultiMatch()
        {
            var type = "Localization";
            var key = "Html.Text";
            _repo.DeleteAll<string>(type);
            _repo.StoreResource(new DomainObjects.Resource<string>(type, key, null, "DefaultText", new { locale = "" }), _userId);
            _repo.StoreResource(new DomainObjects.Resource<string>(type, key, null, "en Text", new { locale = "en" }), _userId);
            _repo.StoreResource(new DomainObjects.Resource<string>(type, key, null, "en-US Text", new { locale = "en-US" }), _userId);
            _repo.SaveChanges();

            Assert.AreEqual("NOT FOUND", _repo.GetResourceData<string>(type, key, l => l.Scope.locale == "jp-JP", "NOT FOUND"));

            Assert.AreEqual("DefaultText", _repo.GetResourceData<string>(type, key, GetLocalizationQueries("ja-JP"), "NOT FOUND"));
            Assert.AreEqual("en Text", _repo.GetResourceData<string>(type, key, GetLocalizationQueries("en-GB"), "NOT FOUND"));
            Assert.AreEqual("en-US Text", _repo.GetResourceData<string>(type, key, GetLocalizationQueries("en-US"), "NOT FOUND")); 
        }

        private List<DomainObjects.Query<DomainObjects.Resource<string>>> GetLocalizationQueries(string locale)
        {
            return new List<DomainObjects.Query<DomainObjects.Resource<string>>>()
                    {new DomainObjects.Query<DomainObjects.Resource<string>>(l => l.Scope.locale == locale, 3), 
                    new DomainObjects.Query<DomainObjects.Resource<string>>(l => l.Scope.locale == locale.Substring(0, 2), 2), 
                    new DomainObjects.Query<DomainObjects.Resource<string>>(l => l.Scope.locale == "", 1)};
        }

        [TestMethod]
        public void CacheHits()
        {
            var type = "Localization";
            var key = "Html.Text";
            _repo.DeleteAll<string>(type);
            _repo.StoreResource(new DomainObjects.Resource<string>(type, key, null, "DefaultText", new { locale = "" }), _userId);
            _repo.StoreResource(new DomainObjects.Resource<string>(type, key, null, "en Text", new { locale = "en" }), _userId);
            _repo.StoreResource(new DomainObjects.Resource<string>(type, key, null, "en-US Text", new { locale = "en-US" }), _userId);
            _repo.SaveChanges();

            var all = _repo.GetResources<string>(type);
            Assert.AreEqual(3, all.Count);
            var text = "";
            for (var i = 0; i < 100; i++)
            {
                text = _repo.GetResourceData<string>(type, key, l => l.Scope.locale == "en-US", "NOT FOUND");
            }
            Assert.AreEqual("en-US Text", text);

            _repo.StoreResource(new DomainObjects.Resource<string>(type, key, null, "jp-JP Text", new {locale =  "jp-JP"}), _userId);
            _repo.SaveChanges();

            text = _repo.GetResourceData<string>(type, key, l => l.Scope.locale == "en-JP", "NOT FOUND");
            Assert.AreEqual("NOT FOUND", text);
            //_repo.ExpireCache<List<DomainObjects.Resource<string>>>();
            text = _repo.GetResourceData<string>(type, key, l => l.Scope.locale == "jp-JP", "NOT FOUND");
            Assert.AreEqual("jp-JP Text", text);
        }


        [TestMethod]
        public void RequiredTest()
        {
            var type = "Localization";
            var key = "Hello.Text";
            _repo.DeleteAll<string>(type);
            _repo.StoreResource(new DomainObjects.Resource<string>(type, key, null, "Hi", new 
                {
                        Type = "Widgets",
                        Company = "10000001",
                        WidgetType = "MyWidget"
                }), _userId);

            _repo.StoreResource(new DomainObjects.Resource<string>(type, key, null, "Hola", new
                {
                    Type = "Widgets",
                    Company = "10000002",
                    WidgetType = "MyWidget"
                }), _userId);

            _repo.StoreResource(new DomainObjects.Resource<string>(type, key, null, "Hi Global World", new
            {
                Type = "Widgets"
            }), _userId);

            _repo.SaveChanges();

            //TODO:  convert these to multiple queries!

            //match only on Type - 3 hits!
            Assert.AreEqual(3, _repo.GetResources<string>(type, key, GetWidgetQueries("Widgets", "10000003", "MyWidget2"), false).Count);

            ////Missing match 
            Assert.AreEqual("NOT FOUND", _repo.GetResourceData<string>(type, key, GetWidgetQueries("Widgets2", "10000003", "MyWidget2"), "NOT FOUND"));
            Assert.AreEqual("NOT FOUND", _repo.GetResourceData<string>(type, key, l => l.Scope.Type == "Widgets" && l.Scope.Company == "10000003" && l.Scope.Type == "MyWidget2", "NOT FOUND"));

            ////Match on Type, Company
            Assert.AreEqual("Hi", _repo.GetResourceData<string>(type, key, GetWidgetQueries("Widgets", "10000001", "MyWidget2"), "NOT FOUND"));

            ////Match on Type, Company, WidgetType
            Assert.AreEqual("Hola", _repo.GetResourceData<string>(type, key, GetWidgetQueries("Widgets", "10000002", "MyWidget"), "NOT FOUND"));
        }

        private List<DomainObjects.Query<DomainObjects.Resource<string>>> GetWidgetQueries(string type, string company, string widgetType)
        {
            return new List<DomainObjects.Query<DomainObjects.Resource<string>>>()
                    {new DomainObjects.Query<DomainObjects.Resource<string>>(l => l.Scope.Type == type, 1), 
                    new DomainObjects.Query<DomainObjects.Resource<string>>(l => l.Scope.Company == company, 1), 
                    new DomainObjects.Query<DomainObjects.Resource<string>>(l => l.Scope.WidgetType == widgetType, 1)};
        }


        [TestMethod]
        public void EnsureGeneratedId()
        {
            var type = "Widgets";
            _repo.DeleteAll<Widget>(type);
            _repo.StoreResource(NewWidgetResource(type, "", 1, "TextHtml", 1), _userId);
            _repo.SaveChanges();

            var widgets = _repo.GetResources<Widget>(type, "", w => w.Scope.PageId == 1);
            Assert.AreEqual(1, widgets.Count);
            Assert.IsNotNull(widgets[0].Id);
        }

        [TestMethod]
        public void NewestEffectiveMatch()
        {
            var type = "Widgets";
            var key = "";
            var pageId = 1;
            var recentDate = DateTimeOffset.Now;
            var yesturday = DateTimeOffset.Now.AddDays(-1);
            _repo.DeleteAll<Widget>(type);
            _repo.StoreResource(NewWidgetResource(type, key, 1, "TextHtml", pageId, yesturday), _userId);
            _repo.StoreResource(NewWidgetResource(type, key, 1, "TextHtml", pageId, recentDate), _userId);
            _repo.StoreResource(NewWidgetResource(type, key, 1, "TextHtml", pageId, new DateTime(2000, 1, 1), null), _userId);
            _repo.SaveChanges();

            var widgets = _repo.GetResources<Widget>(type, key, w => w.Scope.PageId == pageId);
            Assert.AreEqual(1, widgets.Count);
            Assert.AreEqual(recentDate, widgets[0].EffectiveDate);
            _repo.ExpireResource(widgets[0], _userId);
            //_repo.SaveChanges();
            widgets = _repo.GetResources<Widget>(type, key, w => w.Scope.PageId == pageId);
            Assert.AreEqual(1, widgets.Count);
            Assert.AreEqual(yesturday, widgets[0].EffectiveDate);

        }

        [TestMethod]
        public void BinaryResourceTest()
        {
            var type = "Image";
            var key = "home/codeendeavors2.png";
            _repo.DeleteAll<BinaryResource>(type);
            var path = Path.Combine(Environment.CurrentDirectory, @"..\..\..\CodeEndeavors.ResourceManager.UnitTest");

            var binaryResource = new DomainObjects.Resource<BinaryResource>(type, key, null, new BinaryResource() 
                {
                    MimeType = "image/png",
                    Name = "codeendeavors2.png",
                    Path = Path.Combine(path, "codeendeavors2.png")
                },
                new {ModuleId = 123});
            binaryResource.Data.WriteStream(Path.Combine(path, "codeendeavors.png"));
            _repo.StoreResource(binaryResource, _userId);
            _repo.SaveChanges();

            Assert.IsTrue(System.IO.File.Exists(binaryResource.Data.Path));

            var image = _repo.GetResourceData<BinaryResource>(type, key, r => r.Scope.ModuleId == 123, null);
            Assert.AreEqual("codeendeavors2.png", image.Name);
            Assert.IsTrue(image.Size.HasValue);

            System.IO.File.Delete(image.Path);
            Assert.IsFalse(System.IO.File.Exists(image.Path));
        }

        private void CreateWidgets(string Type)
        {
            _repo.DeleteAll<Widget>(Type);
            _repo.StoreResource(NewWidgetResource(Type, "", 1, "TitleBar", 1), _userId);
            _repo.StoreResource(NewWidgetResource(Type, "", 2, "TextHtml", 1), _userId);
        }

        private DomainObjects.Resource<Widget> NewWidgetResource(string type, string key, int seq, string widgetName, int pageId)
        {
            return NewWidgetResource(type, key, seq, widgetName, pageId, null, null);
        }

        private DomainObjects.Resource<Widget> NewWidgetResource(string type, string key, int seq, string widgetName, int pageId, DateTimeOffset? effectiveDate = null, DateTimeOffset? expirationDate = null)
        {
            var resource = new DomainObjects.Resource<Widget>(type, key, seq, 
                new Widget() { Name = widgetName },
                    new 
                    {
                        PageId = pageId
                    });

            resource.EffectiveDate = effectiveDate;
            resource.ExpirationDate = expirationDate;
            return resource;
        }

    }
}
