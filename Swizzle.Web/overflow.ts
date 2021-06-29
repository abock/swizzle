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
    this.videoElem.addEventListener('loadedmetadata', e => this.maybeUpdateLayout());
    this.videoElem.addEventListener('loadeddata', e => this.maybeUpdateLayout());
    this.videoElem.addEventListener('click', e => this.togglePlay());

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
  private readonly itemBatchSize: number = 16;
  private readonly itemsContainerElem: HTMLElement;
  private readonly loaderElem: HTMLElement;
  private readonly masonry: Masonry;

  private loadingItems: boolean = false;
  private currentItemOffset: number = 0;
  private totalItems: number = 0;
  private lastLoaderTop: number;

  constructor(
    itemsContainerElem: HTMLElement,
    loaderElem: HTMLElement) {
    this.lastLoaderTop = loaderElem.offsetTop;
    this.itemsContainerElem = itemsContainerElem;
    this.loaderElem = loaderElem;

    this.masonry = new Masonry(
      itemsContainerElem,
      {
        itemSelector: '.grid-item',
        columnWidth: '.grid-item',
        gutter: '.grid-item-gutter-sizer',
        percentPosition: true,
        transitionDuration: 0
      });

    this.loadItems();

    document.addEventListener('scroll', e => this.maybeLoadMoreItems());
  }

  private areBoundsInViewport(bounds: DOMRect) {
    return (
      Math.ceil(bounds.top) >= 0 &&
      Math.ceil(bounds.left) >= 0 &&
      Math.floor(bounds.bottom) <= document.documentElement.clientHeight &&
      Math.floor(bounds.right) <= document.documentElement.clientWidth);
  }

  private maybeLoadMoreItems() {
    const bounds = this.loaderElem.getBoundingClientRect();
    if (this.loaderElem.offsetTop !== this.lastLoaderTop &&
      this.areBoundsInViewport(bounds)) {
      this.lastLoaderTop = this.loaderElem.offsetTop;
      this.loadItems();
      return true;
    }
    return false;
  }

  private startLoadItems() {
    this.loadingItems = true;
    this.loaderElem.classList.add('loading');
  }

  private endLoadItems() {
    this.loadingItems = false;
    this.loaderElem.classList.remove('loading');
  }

  private loadItems() {
    if (this.loadingItems)
      return;

    const unableToLoadItems = () => {
    };

    this.startLoadItems();

    const offset = this.currentItemOffset;
    const limit = this.itemBatchSize;

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
            this.currentItemOffset = 0;
          } else {
            this.currentItemOffset += items.length;
            this.renderItems(items);
          }
        } catch(ex) {
          console.error('Failed to query items: %O', ex);
          unableToLoadItems();
        }
      } finally {
        this.endLoadItems();
      }  
    };
    httpClient.onerror = e => unableToLoadItems();
    httpClient.send(null);
  }

  private renderItems(items: ItemDto[]) {
    for (const item of items)
      this.renderItem(item);
  }

  private renderItem(item: ItemDto) {
    const itemView = new ItemView(item, _ => this.masonry.layout());
    if (itemView.containerElem) {
      this.itemsContainerElem.appendChild(itemView.containerElem);
      this.masonry.addItems(itemView.containerElem);
      this.masonry.layout();
      this.totalItems++;
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