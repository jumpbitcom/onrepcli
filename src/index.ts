import Parser from 'rss-parser';
import * as cheerio from 'cheerio';

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
  // print how many items are in the feed
  console.log(feed.items.length);

  feed.items.forEach(async item => {
    console.log(item.title + ':' + item.link) // item will have a `bar` property type as a number
    
    // call web page item.link with a http client get request and print title of the page
    const response = await fetch(item.link);
    const body = await response.text();
    const $ = cheerio.load(body);
    console.log($('title').text());
  });
})();