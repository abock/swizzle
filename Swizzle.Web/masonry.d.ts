declare interface MasonryOptions {
  itemSelector?: string
  columnWidth?: string | number | HTMLElement
  gutter?: string | number | HTMLElement
  percentPosition?: boolean
  horizontalOrder?: boolean
  stamp?: string
  fitWidth?: boolean
  originLeft?: boolean
  originTop?: boolean
  containerStyle?: string
  transitionDuration?: string | number
}

declare class Masonry {
  constructor(elem: HTMLElement, options: MasonryOptions);
  layout(): void;
  addItems(elem: HTMLElement | HTMLElement[]): void;
}