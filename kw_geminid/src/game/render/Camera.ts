export class Camera {
  public x: number = 0;
  public y: number = 0;
  public zoom: number = 1.0;
  public minZoom: number = 0.5;
  public maxZoom: number = 2.5;

  public viewportWidth: number = 800;
  public viewportHeight: number = 600;

  public mapWidthPixels: number = 1920;
  public mapHeightPixels: number = 480;

  constructor(viewportWidth: number, viewportHeight: number) {
    this.viewportWidth = viewportWidth;
    this.viewportHeight = viewportHeight;
  }

  public setMapBounds(widthPixels: number, heightPixels: number) {
    this.mapWidthPixels = widthPixels;
    this.mapHeightPixels = heightPixels;
    this.clamp();
  }

  public resize(width: number, height: number) {
    this.viewportWidth = width;
    this.viewportHeight = height;
    this.clamp();
  }

  public pan(dx: number, dy: number) {
    this.x -= dx / this.zoom;
    this.y -= dy / this.zoom;
    this.clamp();
  }

  public setZoom(newZoom: number, focalScreenX?: number, focalScreenY?: number) {
    const clampedZoom = Math.max(this.minZoom, Math.min(this.maxZoom, newZoom));
    if (clampedZoom === this.zoom) return;

    const fx = focalScreenX ?? this.viewportWidth / 2;
    const fy = focalScreenY ?? this.viewportHeight / 2;

    const worldBefore = this.screenToWorld(fx, fy);
    this.zoom = clampedZoom;
    const worldAfter = this.screenToWorld(fx, fy);

    this.x += worldBefore.x - worldAfter.x;
    this.y += worldBefore.y - worldAfter.y;

    this.clamp();
  }

  public focusOn(worldX: number, worldY: number) {
    this.x = worldX - (this.viewportWidth / (2 * this.zoom));
    this.y = worldY - (this.viewportHeight / (2 * this.zoom));
    this.clamp();
  }

  private clamp() {
    const visibleWidth = this.viewportWidth / this.zoom;
    const visibleHeight = this.viewportHeight / this.zoom;

    if (this.mapWidthPixels <= visibleWidth) {
      this.x = (this.mapWidthPixels - visibleWidth) / 2;
    } else {
      this.x = Math.max(-100, Math.min(this.mapWidthPixels - visibleWidth + 100, this.x));
    }

    if (this.mapHeightPixels <= visibleHeight) {
      this.y = (this.mapHeightPixels - visibleHeight) / 2;
    } else {
      this.y = Math.max(-100, Math.min(this.mapHeightPixels - visibleHeight + 100, this.y));
    }
  }

  public screenToWorld(sx: number, sy: number): { x: number; y: number } {
    return {
      x: this.x + sx / this.zoom,
      y: this.y + sy / this.zoom
    };
  }

  public worldToScreen(wx: number, wy: number): { x: number; y: number } {
    return {
      x: (wx - this.x) * this.zoom,
      y: (wy - this.y) * this.zoom
    };
  }
}
