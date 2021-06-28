/// <reference path="masonry.d.ts"/>

interface ItemResourceDto {
  contentType: string;
  uri: string;
  size: number;
  creationTime: string;
  lastWriteTime: string;
  width: number;
  height: number;
  duration: number;
}

interface ItemDto {
  slug: string;
  uri: string;
  creationTime: string;
  lastWriteTime: string;
  resources: ItemResourceDto[];
}

window.addEventListener('DOMContentLoaded', e => {
  const itemsElem = document.getElementById('items');
  const loaderElem = document.getElementById('loader');
  if (itemsElem && loaderElem) {
    (window as any)._overflowapp = new Overflow(itemsElem, loaderElem);
  }
});

class Overflow {
  _loadingItems: boolean
  _currentItemOffset: number;
  _totalItems: number;
  _itemBatchSize: number;
  _lastLoaderTop: number
  _itemsContainerElem: HTMLElement
  _loaderElem: HTMLElement
  _masonry: Masonry

  constructor(
    itemsContainerElem: HTMLElement,
    loaderElem: HTMLElement) {
    this._loadingItems = false;
    this._currentItemOffset = 0;
    this._totalItems = 0;
    this._itemBatchSize = 16;
    this._lastLoaderTop = loaderElem.offsetTop;

    this._itemsContainerElem = itemsContainerElem;
    this._loaderElem = loaderElem;

    this._masonry = new Masonry(
      itemsContainerElem,
      {
        itemSelector: '.grid-item',
        columnWidth: '.grid-item',
        gutter: '.grid-item-gutter-sizer',
        percentPosition: true,
        transitionDuration: 0
      });

    this._loadItems();

    document.addEventListener('scroll', e => this._maybeLoadMoreItems());
  }

  _areBoundsInViewport(bounds: DOMRect) {
    return (
      Math.ceil(bounds.top) >= 0 &&
      Math.ceil(bounds.left) >= 0 &&
      Math.floor(bounds.bottom) <= document.documentElement.clientHeight &&
      Math.floor(bounds.right) <= document.documentElement.clientWidth);
  }

  _maybeLoadMoreItems() {
    const bounds = this._loaderElem.getBoundingClientRect();
    if (this._loaderElem.offsetTop !== this._lastLoaderTop &&
      this._areBoundsInViewport(bounds)) {
      this._lastLoaderTop = this._loaderElem.offsetTop;
      this._loadItems();
      return true;
    }
    return false;
  }

  _startLoadItems() {
    this._loadingItems = true;
    this._loaderElem.classList.add('loading');
  }

  _endLoadItems() {
    this._loadingItems = false;
    this._loaderElem.classList.remove('loading');
  }

  _loadItems() {
    if (this._loadingItems) {
      return;
    }

    const unableToLoadItems = function() {
    };

    this._startLoadItems();

    const offset = this._currentItemOffset;
    const limit = this._itemBatchSize;

    const httpClient = new XMLHttpRequest();
    httpClient.open('GET', `/api/query?offset=${offset}&limit=${limit}`, true);
    httpClient.setRequestHeader('Accept', 'application/json');
    httpClient.onload = e => {
      if (httpClient.readyState !== 4)
        return;
  
      try {
        if (httpClient.status !== 200) {
          unableToLoadItems();
          return;
        }
  
        try {
          const items = JSON.parse(httpClient.responseText);
          if (items.length === 0) {
            this._currentItemOffset = 0;
          } else {
            this._currentItemOffset += items.length;
            this._renderItems(items);
          }
        } catch(ex) {
          console.error('Failed to query items: %O', ex);
          unableToLoadItems();
        }
      } finally {
        this._endLoadItems();
        setTimeout(() => this._maybeLoadMoreItems(), 1000);
      }  
    };
    httpClient.onerror = e => unableToLoadItems();
    httpClient.send(null);
  }

  async _renderItems(items: ItemDto[]) {
    for (const item of items) {
      for (const resource of item.resources) {
        if (resource.contentType === 'video/mp4') {
          this._renderItem(item, resource);
          break;
        }
      }
    }
  }

  _renderItem(item: ItemDto, resource: ItemResourceDto) {
    const itemElem = document.createElement('div');
    itemElem.classList.add('grid-item');

    const createVideoElement = () => {
      if ((window as any)?.safari) {
        const videoElem = document.createElement('img');
        videoElem.src = resource.uri;
        videoElem.onload = e => this._masonry.layout();

        return videoElem;
      } else {
        const videoElem = document.createElement('video');
  
        videoElem.autoplay = true;
        videoElem.loop = true;
        videoElem.muted = true;
        videoElem.defaultMuted = true;
        videoElem.playsInline = true;

        // Saner browsers (Safari, Chrome) update the DOM with video dimension
        // metadata as they should, so we can early layout...
        videoElem.onloadedmetadata = () => this._masonry.layout();

        // But oh no, not Firefox. Firefox does not update the DOM with any
        // video dimension metadata that came in via the above 'loadedmetadata'
        // event, so we have to layout again when it's ready to render the
        // first frame.
        videoElem.onloadeddata = () => this._masonry.layout();
  
        const sourceElem = document.createElement('source');
        sourceElem.src = resource.uri;
        sourceElem.type = resource.contentType;
        videoElem.appendChild(sourceElem);

        return videoElem;
      }
    };

    const videoElem = createVideoElement();
    videoElem.classList.add('media');
    itemElem.appendChild(videoElem);

    this._itemsContainerElem.appendChild(itemElem);
    this._masonry.appended(itemElem);
    this._masonry.layout();
    this._totalItems++;
  }
}