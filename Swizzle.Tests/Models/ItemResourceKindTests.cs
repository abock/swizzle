using System;
using Xunit;

namespace Swizzle.Models
{
    public class ItemResourceKindTests
    {
        static void AssertContentType(
            ItemResourceKind expectedKind,
            string contentType)
        {
            Assert.True(ItemResourceKind.TryGetFromContentType(
                contentType,
                out var actualKind));
            Assert.Equal(
                expectedKind,
                actualKind);
        }

        static void AssertExtension(
            ItemResourceKind expectedKind,
            string extension)
        {
            Assert.True(ItemResourceKind.TryGetFromExtension(
                extension,
                out var actualKind));
            Assert.Equal(
                expectedKind,
                actualKind);
        }

        [Fact]
        public void Jpeg()
        {
            Assert.Equal(ItemResourceKind.Jpeg, ItemResourceKind.Jpeg);
            Assert.True(ItemResourceKind.Jpeg == ItemResourceKind.FromExtension("jpg"));
            Assert.True(ItemResourceKind.Jpeg == ItemResourceKind.FromExtension(".jpeg"));
            Assert.Equal("image/jpeg", ItemResourceKind.Jpeg.PreferredContentType);
            Assert.Equal(".jpg", ItemResourceKind.Jpeg.PreferredExtension);
            AssertContentType(ItemResourceKind.Jpeg, "IMAGE/JPEG");
            AssertContentType(ItemResourceKind.Jpeg, "image/jpeg");
            AssertExtension(ItemResourceKind.Jpeg, ".jpg");
            AssertExtension(ItemResourceKind.Jpeg, ".jpeg");
            AssertExtension(ItemResourceKind.Jpeg, "jpg");
            AssertExtension(ItemResourceKind.Jpeg, "jpeg");
            Assert.ThrowsAny<Exception>(() => AssertExtension(ItemResourceKind.Jpeg, ".JPEG"));
            Assert.ThrowsAny<Exception>(() => AssertExtension(ItemResourceKind.Jpeg, "jPg"));
            Assert.ThrowsAny<Exception>(() => AssertExtension(ItemResourceKind.Jpeg, "jPeG"));
        }

        [Fact]
        public void Gif()
        {
            Assert.Equal(ItemResourceKind.Gif, ItemResourceKind.Gif);
            Assert.True(ItemResourceKind.Gif == ItemResourceKind.FromExtension("gif"));
            Assert.True(ItemResourceKind.Gif == ItemResourceKind.FromExtension(".gif"));
            Assert.Equal("image/gif", ItemResourceKind.Gif.PreferredContentType);
            Assert.Equal(".gif", ItemResourceKind.Gif.PreferredExtension);
            AssertContentType(ItemResourceKind.Gif, "IMAGE/GIF");
            AssertContentType(ItemResourceKind.Gif, "image/gif");
            AssertExtension(ItemResourceKind.Gif, ".gif");
            AssertExtension(ItemResourceKind.Gif, "gif");
            Assert.ThrowsAny<Exception>(() => AssertExtension(ItemResourceKind.Jpeg, ".GiF"));
            Assert.ThrowsAny<Exception>(() => AssertExtension(ItemResourceKind.Jpeg, "GIF"));
        }

        [Fact]
        public void Mp4()
        {
            Assert.Equal(ItemResourceKind.Mp4, ItemResourceKind.Mp4);
            Assert.True(ItemResourceKind.Mp4 == ItemResourceKind.FromExtension("mp4"));
            Assert.True(ItemResourceKind.Mp4 == ItemResourceKind.FromExtension(".mp4"));
            Assert.Equal("video/mp4", ItemResourceKind.Mp4.PreferredContentType);
            Assert.Equal(".mp4", ItemResourceKind.Mp4.PreferredExtension);
            AssertContentType(ItemResourceKind.Mp4, "VIDEO/MP4");
            AssertContentType(ItemResourceKind.Mp4, "video/mp4");
            AssertExtension(ItemResourceKind.Mp4, ".mp4");
            AssertExtension(ItemResourceKind.Mp4, "mp4");
            Assert.ThrowsAny<Exception>(() => AssertExtension(ItemResourceKind.Mp4, ".Mp4"));
            Assert.ThrowsAny<Exception>(() => AssertExtension(ItemResourceKind.Mp4, "MP4"));
        }

        [Fact]
        public void Ogv()
        {
            Assert.Equal(ItemResourceKind.Ogv, ItemResourceKind.Ogv);
            Assert.True(ItemResourceKind.Ogv == ItemResourceKind.FromExtension("ogv"));
            Assert.True(ItemResourceKind.Ogv == ItemResourceKind.FromExtension(".ogv"));
            Assert.Equal("video/ogg", ItemResourceKind.Ogv.PreferredContentType);
            Assert.Equal(".ogv", ItemResourceKind.Ogv.PreferredExtension);
            AssertContentType(ItemResourceKind.Ogv, "VIDEO/OGG");
            AssertContentType(ItemResourceKind.Ogv, "video/ogg");
            AssertExtension(ItemResourceKind.Ogv, ".ogv");
            AssertExtension(ItemResourceKind.Ogv, "ogv");
            Assert.ThrowsAny<Exception>(() => AssertExtension(ItemResourceKind.Ogv, ".OgV"));
            Assert.ThrowsAny<Exception>(() => AssertExtension(ItemResourceKind.Ogv, "OGV"));
        }
    }
}
