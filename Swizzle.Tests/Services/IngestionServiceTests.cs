using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.Extensions.Logging;

using Xunit;

using Swizzle.Models;

namespace Swizzle.Services
{
    public class IngestionServiceTests : IDisposable
    {
        static readonly string s_contentRootPath = Path.GetFullPath(
            Path.Combine("..", "..", "..", "test-content-root"));

        static IngestionService CreateService()
            => new(
                LoggerFactory
                    .Create(builder => builder.AddConsole())
                    .CreateLogger<IngestionService>(),
                s_contentRootPath);

        static string JoinPath(string collectionKey, string fileName)
            => Path.Combine(s_contentRootPath, collectionKey, fileName);

        public void Dispose()
        {
            GC.SuppressFinalize(this);

            foreach (var mutableDir in Directory.EnumerateDirectories(
                s_contentRootPath))
            {
                if (Path.GetFileName(mutableDir)[0] == '_')
                    Directory.Delete(mutableDir, recursive: true);
            }
        }

        static string CloneCollection(
            string sourceCollectionKey,
            Predicate<string>? predicate = null)
        {
            var destCollectionKey = $"_{Guid.NewGuid()}";
            var sourceCollectionPath = Path.Combine(s_contentRootPath, sourceCollectionKey);
            var destCollectionPath = Path.Combine(s_contentRootPath, destCollectionKey);

            try
            {
                Directory.Delete(destCollectionPath, recursive: true);
            }
            catch (DirectoryNotFoundException)
            {
            }

            Directory.CreateDirectory(destCollectionPath);
            foreach (var sourceFilePath in Directory.EnumerateFiles(
                sourceCollectionPath))
            {
                if (predicate is null || predicate(sourceFilePath))
                    File.Copy(
                        sourceFilePath,
                        Path.Combine(
                            destCollectionPath,
                            Path.GetFileName(sourceFilePath)));
            }

            return destCollectionKey;
        }

        [Theory]
        [InlineData(typeof(ArgumentNullException), null, null)]
        [InlineData(typeof(IllegalPathException), "collection1", "../BadPath")]
        [InlineData(typeof(IllegalPathException), "collection1", "/BadPath")]
        [InlineData(typeof(CollectionNotRegisteredException), "collection1", "NoExistPath")]
        [InlineData(typeof(IllegalSlugException), "collection1", "illegal-slug.mp4", true)]
        [InlineData(typeof(UnsupportedContentTypeException), "collection1", "NoExistPath", true)]
        [InlineData(typeof(UnsupportedContentTypeException), "collection1", "NoExistPath.GIF", true)]
        [InlineData(typeof(UnsupportedContentTypeException), "collection1", "NoExistPath.OGV", true)]
        [InlineData(typeof(UnsupportedContentTypeException), "collection1", "NoExistPath.MP4", true)]
        [InlineData(typeof(UnsupportedContentTypeException), "collection1", "NoExistPath.JPG", true)]
        [InlineData(typeof(UnsupportedContentTypeException), "collection1", "NoExistPath.JPEG", true)]
        [InlineData(typeof(FileNotFoundException), "collection1", "NoExistPath.gif", true)]
        [InlineData(typeof(FileNotFoundException), "collection1", "NoExistPath.ogv", true)]
        [InlineData(typeof(FileNotFoundException), "collection1", "NoExistPath.mp4", true)]
        [InlineData(typeof(FileNotFoundException), "collection1", "NoExistPath.jpg", true)]
        [InlineData(typeof(FileNotFoundException), "collection1", "NoExistPath.jpeg", true)]
        [InlineData(null, "collection1", "a5lZeeK.gif", true)]
        [InlineData(null, "collection1", "WkRmXJO.gif", true)]
        [InlineData(null, "collection1", "ReWy5RA.gif", true)]
        public void ValidateIngestionPath(
            Type? expectedException,
            string collectionKey,
            string fileName,
            bool registerCollection = false)
        {
            var service = CreateService();
            if (registerCollection)
                service.RegisterCollection(collectionKey);

            void Exec()
                => service.IngestFile(JoinPath(collectionKey, fileName));

            if (expectedException is null)
                Exec();
            else
                Assert.Throws(expectedException, () => Exec());
        }

        [Theory]
        [InlineData(typeof(ArgumentNullException), null)]
        [InlineData(typeof(ArgumentException), "")]
        [InlineData(null, "collection1")]
        [InlineData(null, "collection2")]
        [InlineData(null, "collection1", "collection2")]
        [InlineData(null, "collection1", "collection1")]
        [InlineData(null, "COLLECTION1", "collection1")]
        public void RegisterCollection(
            Type? expectedException,
            string collectionKey1,
            string? collectionKey2 = null)
        {
            var service = CreateService();

            void Exec()
            {
                service.RegisterCollection(collectionKey1);
                if (collectionKey2 is not null)
                    service.RegisterCollection(collectionKey2);
            }

            if (expectedException is null)
                Exec();
            else
                Assert.Throws(expectedException, () => Exec());
        }

        [Theory]
        [InlineData(typeof(ArgumentNullException), null)]
        [InlineData(typeof(CollectionNotRegisteredException), "blah")]
        public void UnregisteredCollection(
            Type expectedException,
            string collectionKey)
            => Assert.Throws(
                expectedException,
                () => CreateService().GetCollection(collectionKey));

        [Fact]
        public void MoreRegisterCollection()
        {
            var service = CreateService();

            var path = Path.Combine(s_contentRootPath, "collection1");

            Assert.Throws<IllegalPathException>(
                () => service.IngestFile(Path.Combine("a.gif")));

            Assert.Throws<CollectionNotRegisteredException>(
                () => service.IngestFile(Path.Combine(path, "a.gif")));

            service.RegisterCollection("collection1");

            Assert.Throws<CollectionNotRegisteredException>(
                () => service.IngestFile(Path.Combine(path, "..", "collection2", "a.gif")));

            service.RegisterCollection("collection2");

            var col2APath = Path.Combine(path, "..", "collection2", "a.gif");
            var itemA = service.IngestFile(col2APath);
            Assert.Equal("a", itemA.Slug);
            Assert.Equal(Path.GetFullPath(col2APath), itemA.Resources[0].PhysicalPath);
        }

        [Fact]
        public void MoreIllegalIngestionPaths()
        {
            Assert.Throws<IllegalPathException>(() => CreateService().IngestFile("a.gif"));
            Assert.Throws<IllegalPathException>(() => CreateService().IngestFile("/a.gif"));
            Assert.Throws<IllegalPathException>(() => CreateService().IngestFile("./a.gif"));
            Assert.Throws<IllegalPathException>(() => CreateService().IngestFile("../a.gif"));
            Assert.Throws<IllegalPathException>(() => CreateService().IngestFile("\\a.gif"));
            Assert.Throws<IllegalPathException>(() => CreateService().IngestFile(".\\a.gif"));
            Assert.Throws<IllegalPathException>(() => CreateService().IngestFile("..\\a.gif"));
        }

        [Fact]
        public void IngestSame()
        {
            var service = CreateService();
            var path = JoinPath("collection1", "a.gif");
            service.RegisterCollection("collection1");

            Assert.Empty(service.GetCollection("collection1"));
            Assert.Equal(0, service.GetCollection("collection1").Generation);

            for (var i = 0; i < 100; i++)
            {
                service.IngestFile(path);
                Assert.Equal(i + 1, service.GetCollection("collection1").Generation);
                Assert.Collection(
                    service.GetCollection("collection1"),
                    item => Assert.Collection(
                        item.Resources,
                        r => Assert.Equal(path, r.PhysicalPath)));
            }
        }

        [Fact]
        public void IngestMany()
        {
            static string JoinPath(string fileName)
                => IngestionServiceTests.JoinPath("collection1", fileName);

            var service = CreateService();
            service.RegisterCollection("collection1");

            Assert.Empty(service.GetCollection("collection1"));

            service.IngestFile(JoinPath("a.gif"));
            Assert.Collection(
                service.GetCollection("collection1"),
                item =>
                {
                    Assert.Equal("a", item.Slug);
                    Assert.Collection(
                        item.Resources,
                        r => Assert.Equal(JoinPath("a.gif"), r.PhysicalPath));
                });

            service.IngestFile(JoinPath("a.mp4"));
            Assert.Collection(
                service.GetCollection("collection1"),
                item =>
                {
                    Assert.Equal("a", item.Slug);
                    Assert.Collection(
                        item.Resources,
                        r => Assert.Equal(JoinPath("a.gif"), r.PhysicalPath),
                        r => Assert.Equal(JoinPath("a.mp4"), r.PhysicalPath));
                });

            service.IngestFile(JoinPath("a.ogv"));
            Assert.Collection(
                service.GetCollection("collection1"),
                item =>
                {
                    Assert.Equal("a", item.Slug);
                    Assert.Collection(
                        item.Resources,
                        r => Assert.Equal(JoinPath("a.gif"), r.PhysicalPath),
                        r => Assert.Equal(JoinPath("a.mp4"), r.PhysicalPath),
                        r => Assert.Equal(JoinPath("a.ogv"), r.PhysicalPath));
                });

            service.IngestFile(JoinPath("a.jpg"));
            Assert.Collection(
                service.GetCollection("collection1"),
                item =>
                {
                    Assert.Equal("a", item.Slug);
                    Assert.Collection(
                        item.Resources,
                        r => Assert.Equal(JoinPath("a.gif"), r.PhysicalPath),
                        r => Assert.Equal(JoinPath("a.mp4"), r.PhysicalPath),
                        r => Assert.Equal(JoinPath("a.ogv"), r.PhysicalPath),
                        r => Assert.Equal(JoinPath("a.jpg"), r.PhysicalPath));
                });

            service.IngestFile(JoinPath("b.mp4"));
            Assert.Collection(
                service.GetCollection("collection1"),
                item =>
                {
                    Assert.Equal("a", item.Slug);
                    Assert.Collection(
                        item.Resources,
                        r => Assert.Equal(JoinPath("a.gif"), r.PhysicalPath),
                        r => Assert.Equal(JoinPath("a.mp4"), r.PhysicalPath),
                        r => Assert.Equal(JoinPath("a.ogv"), r.PhysicalPath),
                        r => Assert.Equal(JoinPath("a.jpg"), r.PhysicalPath));
                },
                item =>
                {
                    Assert.Equal("b", item.Slug);
                    Assert.Collection(
                        item.Resources,
                        r => Assert.Equal(JoinPath("b.mp4"), r.PhysicalPath));
                });

            service.IngestFile(JoinPath("b.gif"));
            Assert.Collection(
                service.GetCollection("collection1"),
                item =>
                {
                    Assert.Equal("a", item.Slug);
                    Assert.Collection(
                        item.Resources,
                        r => Assert.Equal(JoinPath("a.gif"), r.PhysicalPath),
                        r => Assert.Equal(JoinPath("a.mp4"), r.PhysicalPath),
                        r => Assert.Equal(JoinPath("a.ogv"), r.PhysicalPath),
                        r => Assert.Equal(JoinPath("a.jpg"), r.PhysicalPath));
                },
                item =>
                {
                    Assert.Equal("b", item.Slug);
                    Assert.Collection(
                        item.Resources,
                        r => Assert.Equal(JoinPath("b.mp4"), r.PhysicalPath),
                        r => Assert.Equal(JoinPath("b.gif"), r.PhysicalPath));
                });

            service.IngestFile(JoinPath("b.jpg"));
            Assert.Collection(
                service.GetCollection("collection1"),
                item =>
                {
                    Assert.Equal("a", item.Slug);
                    Assert.Collection(
                        item.Resources,
                        r => Assert.Equal(JoinPath("a.gif"), r.PhysicalPath),
                        r => Assert.Equal(JoinPath("a.mp4"), r.PhysicalPath),
                        r => Assert.Equal(JoinPath("a.ogv"), r.PhysicalPath),
                        r => Assert.Equal(JoinPath("a.jpg"), r.PhysicalPath));
                },
                item =>
                {
                    Assert.Equal("b", item.Slug);
                    Assert.Collection(
                        item.Resources,
                        r => Assert.Equal(JoinPath("b.mp4"), r.PhysicalPath),
                        r => Assert.Equal(JoinPath("b.gif"), r.PhysicalPath),
                        r => Assert.Equal(JoinPath("b.jpg"), r.PhysicalPath));
                });

            service.IngestFile(JoinPath("b.ogv"));
            Assert.Collection(
                service.GetCollection("collection1"),
                item =>
                {
                    Assert.Equal("a", item.Slug);
                    Assert.Collection(
                        item.Resources,
                        r => Assert.Equal(JoinPath("a.gif"), r.PhysicalPath),
                        r => Assert.Equal(JoinPath("a.mp4"), r.PhysicalPath),
                        r => Assert.Equal(JoinPath("a.ogv"), r.PhysicalPath),
                        r => Assert.Equal(JoinPath("a.jpg"), r.PhysicalPath));
                },
                item =>
                {
                    Assert.Equal("b", item.Slug);
                    Assert.Collection(
                        item.Resources,
                        r => Assert.Equal(JoinPath("b.mp4"), r.PhysicalPath),
                        r => Assert.Equal(JoinPath("b.gif"), r.PhysicalPath),
                        r => Assert.Equal(JoinPath("b.jpg"), r.PhysicalPath),
                        r => Assert.Equal(JoinPath("b.ogv"), r.PhysicalPath));
                });


            service.IngestFile(JoinPath("ReWy5RA.gif"));
            Assert.Collection(
                service.GetCollection("collection1"),
                item =>
                {
                    Assert.Equal("a", item.Slug);
                    Assert.Collection(
                        item.Resources,
                        r => Assert.Equal(JoinPath("a.gif"), r.PhysicalPath),
                        r => Assert.Equal(JoinPath("a.mp4"), r.PhysicalPath),
                        r => Assert.Equal(JoinPath("a.ogv"), r.PhysicalPath),
                        r => Assert.Equal(JoinPath("a.jpg"), r.PhysicalPath));
                },
                item =>
                {
                    Assert.Equal("b", item.Slug);
                    Assert.Collection(
                        item.Resources,
                        r => Assert.Equal(JoinPath("b.mp4"), r.PhysicalPath),
                        r => Assert.Equal(JoinPath("b.gif"), r.PhysicalPath),
                        r => Assert.Equal(JoinPath("b.jpg"), r.PhysicalPath),
                        r => Assert.Equal(JoinPath("b.ogv"), r.PhysicalPath));
                },
                item =>
                {
                    Assert.Equal("ReWy5RA", item.Slug);
                    Assert.Collection(
                        item.Resources,
                        r => Assert.Equal(JoinPath("ReWy5RA.gif"), r.PhysicalPath));
                });
        }

        [Fact]
        public void IngestAlternateResources()
        {
            static string JoinPath(string fileName)
                => IngestionServiceTests.JoinPath("collection1", fileName);

            var service = CreateService();
            service.RegisterCollection("collection1");
            var item = service.IngestFile(
                JoinPath("a.gif"),
                IngestFileOptions.IngestAlternateResources);
            Assert.NotNull(item);
            Assert.Collection(
                service.GetCollection("collection1"),
                i =>
                {
                    Assert.Equal(item, i);
                    Assert.Equal(3, i.Generation);
                    Assert.Collection(
                        i.Resources,
                        // first should be the gif as we explicitly asked to ingest it
                        r => Assert.Equal(JoinPath("a.gif"), r.PhysicalPath),
                        // rest depend on the order of ItemResourceKind.All,
                        // then ItemResourceKind.Extensions
                        r => Assert.Equal(JoinPath("a.jpg"), r.PhysicalPath),
                        r => Assert.Equal(JoinPath("a.mp4"), r.PhysicalPath),
                        r => Assert.Equal(JoinPath("a.ogv"), r.PhysicalPath));
                });
        }

        [Fact]
        public void ProduceAlternateResources()
        {
            var collectionKey = CloneCollection(
                "collection1",
                path => Path.GetFileNameWithoutExtension(path)
                    is not ("a" or "b"));

            string JoinPath(string fileName)
                => IngestionServiceTests.JoinPath(collectionKey, fileName);

            var service = CreateService();
            service.RegisterCollection(collectionKey);

            void CollectionAssert(IEnumerable<Item> collection)
                => Assert.Collection(collection,
                    item => Assert.Collection(
                        item.Resources,
                        r => Assert.Equal(JoinPath("a5lZeeK.gif"), r.PhysicalPath),
                        r => Assert.Equal(JoinPath("a5lZeeK.jpg"), r.PhysicalPath),
                        r => Assert.Equal(JoinPath("a5lZeeK.mp4"), r.PhysicalPath),
                        r => Assert.Equal(JoinPath("a5lZeeK.ogv"), r.PhysicalPath)),
                    item => Assert.Collection(
                        item.Resources,
                        r => Assert.Equal(JoinPath("N2w1OR.mp4"), r.PhysicalPath),
                        r => Assert.Equal(JoinPath("N2w1OR.jpg"), r.PhysicalPath),
                        r => Assert.Equal(JoinPath("N2w1OR.gif"), r.PhysicalPath),
                        r => Assert.Equal(JoinPath("N2w1OR.ogv"), r.PhysicalPath)),
                    item => Assert.Collection(
                        item.Resources,
                        r => Assert.Equal(JoinPath("ReWy5RA.gif"), r.PhysicalPath),
                        r => Assert.Equal(JoinPath("ReWy5RA.jpg"), r.PhysicalPath),
                        r => Assert.Equal(JoinPath("ReWy5RA.mp4"), r.PhysicalPath),
                        r => Assert.Equal(JoinPath("ReWy5RA.ogv"), r.PhysicalPath)),
                    item => Assert.Collection(
                        item.Resources,
                        r => Assert.Equal(JoinPath("WkRmXJO.gif"), r.PhysicalPath),
                        r => Assert.Equal(JoinPath("WkRmXJO.jpg"), r.PhysicalPath),
                        r => Assert.Equal(JoinPath("WkRmXJO.mp4"), r.PhysicalPath),
                        r => Assert.Equal(JoinPath("WkRmXJO.ogv"), r.PhysicalPath)));

            CollectionAssert(service
                .IngestDirectory(
                    Path.Combine(s_contentRootPath, collectionKey),
                    IngestFileOptions.ProduceAlternateResources)
                .OrderBy(i => i.Slug));

            CollectionAssert(service.GetCollection(collectionKey)
                .OrderBy(i => i.Slug));
        }
    }
}
