import Parser from 'rss-parser';

type CustomFeed = {foo: string};
type CustomItem = {bar: number};

const parser: Parser<CustomFeed, CustomItem> = new Parser({
  customFields: {
    feed: ['foo'],
    item: ['bar']
  }
});

(async () => {

  const feed = await parser.parseURL('https://www.onlinereports.ch/RSS-Feed.122+M5ea6a5a56eb.0.html');
  console.log(feed.title); // feed will have a `foo` property, type as a string

  feed.items.forEach(item => {
    console.log(item.title + ':' + item.link) // item will have a `bar` property type as a number
  });
})();