using ChatGgtApp.Crawler.Extractors.Loopcv;

namespace ChatGgtApp.Crawler.Browser;

public class ChromiumFactory(LoopCvLogger loopCvLogger)
{
    public Chromium Create() => new(loopCvLogger);
}