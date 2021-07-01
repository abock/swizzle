using System.IO;

using Xunit;

namespace Swizzle.InteropServices
{
    public sealed class LdSoConfParserTests
    {
        static readonly string s_rootPath = Path.GetFullPath(Path.Combine(
            "..", "..", "..", "..",
            "Swizzle.Tests",
            "ld.so.conf-root"));

        [Theory]
        [InlineData("ubuntu-20.04",
            // from /etc/ld.so.conf, it yields the following
            // via 'include /etc/ld.so.conf.d/*.conf':

            // from /etc/ld.so.conf.d/fakeroot-x86_64-linux-gnu.conf
            "/usr/lib/x86_64-linux-gnu/libfakeroot",

            // from /etc/ld.so.conf.d/libc.conf
            "/usr/local/lib",

            // from /etc/ld.so.conf.d/x86_64-linux-gnu.conf
            "/usr/local/lib/x86_64-linux-gnu",
            "/lib/x86_64-linux-gnu",
            "/usr/lib/x86_64-linux-gnu",

            // from /etc/ld.so.conf.d/zz_i386-biarch-compat.conf
            "/lib32",
            "/usr/lib32",

            // trusted built-ins:
            "/lib",
            "/lib64",
            "/usr/lib",
            "/usr/lib64")]

        [InlineData("opensuse-15.1",
            // from /etc/ld.so.conf
            "/usr/local/lib64",
            "/usr/local/lib",
            // then the following via 'include /etc/ld.so.conf.d/*.conf':

            // from /etc/ld.so.conf.d/mysql-x86_64.conf:
            //   this one is probably added by MySQL, which I
            //   happened to have installed on my system
            //   -abock, 2021-07-01
            "/usr/lib64/mysql",

            // trusted built-ins:
            "/lib",
            "/lib64",
            "/usr/lib",
            "/usr/lib64")]
        public void Test(string testBase, params string[] expectedSearchPaths)
            => Assert.Equal(
                expectedSearchPaths,
                new LdSoConfParser(
                    "/etc/ld.so.conf",
                    Path.Combine(s_rootPath, testBase)).Parse());
    }
}
