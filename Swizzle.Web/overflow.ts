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

class ItemView {
  readonly item: ItemDto;
  readonly containerElem?: HTMLDivElement;
  
  private readonly mediaContainerElem?: HTMLDivElement;
  private readonly videoElem?: HTMLVideoElement;
  private readonly notifyLayoutChanged: (view: ItemView) => void;

  constructor(
    item: ItemDto,
    notifyLayoutChanged: (view: ItemView) => void) {
    this.item = item;
    this.notifyLayoutChanged = notifyLayoutChanged;

    let posterUri: string | null = null;
    let videoWidth: number = 0;
    let videoHeight: number = 0;
    const sourceElems: HTMLSourceElement[] = [];

    for (const resource of item.resources) {
      if (resource.contentType.startsWith('video/')) {
        if (videoWidth === 0)
          videoWidth = resource.width;
        if (videoHeight === 0)
          videoHeight = resource.height;
        const sourceElem = document.createElement('source');
        sourceElem.src = resource.uri;
        sourceElem.type = resource.contentType;
        sourceElems.push(sourceElem);
      } else if (resource.contentType === 'image/jpeg') {
        posterUri = resource.uri;
      }
    }

    if (sourceElems.length === 0 || !posterUri)
      return;

    this.containerElem = document.createElement('div');
    this.containerElem.setAttribute('data-slug', item.slug);
    this.containerElem.classList.add('grid-item');

    this.mediaContainerElem = document.createElement('div');
    this.mediaContainerElem.classList.add('media-container');
    this.containerElem.appendChild(this.mediaContainerElem);

    this.videoElem = document.createElement('video');
    this.videoElem.preload
    this.videoElem.loop = true;
    this.videoElem.muted = true;
    this.videoElem.defaultMuted = true;
    this.videoElem.playsInline = true;
    this.videoElem.onloadedmetadata = () => this.maybeUpdateLayout();
    this.videoElem.onloadeddata = () => this.maybeUpdateLayout();
    this.videoElem.onclick = () => this.togglePlay();

    this.videoElem.poster = posterUri;
    this.videoElem.style.backgroundImage = `url('${posterUri}')`;
    for (const sourceElem of sourceElems)
      this.videoElem.appendChild(sourceElem);

    this.mediaContainerElem.appendChild(this.videoElem);
  }

  private maybeUpdateLayout() {
    this.notifyLayoutChanged(this);
  }

  play() {
    this.videoElem?.play();
  }

  pause() {
    this.videoElem?.pause();
  }

  togglePlay() {
    if (!this.videoElem)
      return;
    if (this.videoElem.readyState === 0)
      this.videoElem.load();
    if (this.videoElem.paused) {
      this.play();
    } else {
      this.pause();
    }
  }
}

class Overflow {
  _loadingItems: boolean;
  _currentItemOffset: number;
  _totalItems: number;
  _itemBatchSize: number;
  _lastLoaderTop: number;
  _itemsContainerElem: HTMLElement;
  _loaderElem: HTMLElement;
  _masonry: Masonry;

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

    const unableToLoadItems = () => {
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
      }  
    };
    httpClient.onerror = e => unableToLoadItems();
    httpClient.send(null);
  }

  _renderItems(items: ItemDto[]) {
    for (const item of items) {
      this._renderItem(item);
    }
  }

  _renderItem(item: ItemDto) {
    const itemView = new ItemView(item, _ => this._masonry.layout());
    if (itemView.containerElem) {
      this._itemsContainerElem.appendChild(itemView.containerElem);
      this._masonry.addItems(itemView.containerElem);
      this._masonry.layout();
      this._totalItems++;
    }
  }
}

window.addEventListener('DOMContentLoaded', e => {
  const itemsElem = document.getElementById('items');
  const loaderElem = document.getElementById('loader');
  if (itemsElem && loaderElem) {
    (window as any)._overflowapp = new Overflow(itemsElem, loaderElem);
  }
});