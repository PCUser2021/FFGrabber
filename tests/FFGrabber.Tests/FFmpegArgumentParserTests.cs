using FFGrabber.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FFGrabber.Tests;

[TestClass]
public sealed class FFmpegArgumentParserTests
{
    [TestMethod]
    public void Parse_KeepsLeadingUncPrefixLiteral()
    {
        var tokens = FFmpegArgumentParser.Parse(@"-passlogfile \\nas\share\log");

        Assert.AreEqual(2, tokens.Count);
        Assert.AreEqual("-passlogfile", tokens[0]);
        Assert.AreEqual(@"\\nas\share\log", tokens[1]);
    }

    [TestMethod]
    public void Parse_KeepsUncPrefixLiteralInsideQuotes()
    {
        var tokens = FFmpegArgumentParser.Parse("-hls_segment_filename \"\\\\nas\\share\\seg%03d.ts\"");

        Assert.AreEqual(2, tokens.Count);
        Assert.AreEqual(@"\\nas\share\seg%03d.ts", tokens[1]);
    }

    [TestMethod]
    public void Parse_StillUnescapesQuotedBackslashesAndQuotes()
    {
        var tokens = FFmpegArgumentParser.Parse("-metadata title=\"say \\\"hi\\\"\" -x a\\\\b");

        Assert.AreEqual(4, tokens.Count);
        Assert.AreEqual("title=say \"hi\"", tokens[1]);
        Assert.AreEqual(@"a\b", tokens[3]);
    }

    [TestMethod]
    public void Parse_RejectsUnmatchedQuote()
    {
        Assert.ThrowsExactly<ArgumentException>(() => FFmpegArgumentParser.Parse("-metadata \"oops"));
    }
}
