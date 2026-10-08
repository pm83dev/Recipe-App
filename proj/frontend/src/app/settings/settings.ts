import { Component, OnInit, signal } from '@angular/core';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [],
  template: `
    <div class="d-flex align-items-center gap-3 mb-4">
      <a href="javascript:void(0)" class="back-link text-decoration-none fw-semibold" (click)="goBack()">
        <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><path d="M19 12H5m0 0 6 6m-6-6 6-6"/></svg>
        Indietro
      </a>
      <h1 class="fs-4 fw-bold mb-0">Impostazioni</h1>
    </div>

    <div class="card p-4 mb-4">
      <h2 class="fs-6 fw-bold mb-3">Colori dell'app</h2>
      
      <div class="mb-4">
        <label class="form-label small fw-semibold text-muted">Colore principale</label>
        <input type="color" class="form-control form-control-color" [value]="accentColor()" (change)="updateAccentColor($event)" title="Scegli colore principale">
        <div class="small text-muted mt-1">Usato per i pulsanti, link e elementi principali</div>
      </div>

      <div class="mb-4">
        <label class="form-label small fw-semibold text-muted">Colore di sfondo</label>
        <input type="color" class="form-control form-control-color" [value]="bgColor()" (change)="updateBgColor($event)" title="Scegli colore di sfondo">
        <div class="small text-muted mt-1">Usato per lo sfondo dell'app</div>
      </div>

      <div class="mb-4">
        <label class="form-label small fw-semibold text-muted">Colore superficie</label>
        <input type="color" class="form-control form-control-color" [value]="surfaceColor()" (change)="updateSurfaceColor($event)" title="Scegli colore superficie">
        <div class="small text-muted mt-1">Usato per i card e gli elementi di interfaccia</div>
      </div>

      <div class="mb-4">
        <label class="form-label small fw-semibold text-muted">Colore bordo</label>
        <input type="color" class="form-control form-control-color" [value]="borderColor()" (change)="updateBorderColor($event)" title="Scegli colore bordo">
        <div class="small text-muted mt-1">Usato per i bordi degli elementi</div>
      </div>

      <div class="mb-4">
        <label class="form-label small fw-semibold text-muted">Colore testo</label>
        <input type="color" class="form-control form-control-color" [value]="textColor()" (change)="updateTextColor($event)" title="Scegli colore testo">
        <div class="small text-muted mt-1">Usato per il testo principale</div>
      </div>

      <div class="d-flex gap-2 mt-4">
        <button class="btn btn-sm ghost" (click)="resetColors()">Ripristina predefiniti</button>
        <button class="btn btn-sm" (click)="applyDemoColors()">Colori demo</button>
        <button class="btn btn-sm" (click)="applyAndSaveColors()">Applica modifiche</button>
      </div>
    </div>

    <div class="card p-4">
      <h2 class="fs-6 fw-bold mb-3">Anteprima</h2>
      <p class="small text-muted mb-3">Ecco come apparirà l'app con i colori selezionati:</p>
      
      <div class="d-flex gap-3 mb-3">
        <div class="card p-3" [style.background]="surfaceColor()" [style.border]="borderColor() ? '1px solid ' + borderColor() : '1px solid var(--border)'">
          <div class="fw-bold" [style.color]="textColor()">Card di anteprima</div>
          <p class="small mb-0" [style.color]="textColor()">Contenuto della card</p>
        </div>
        <div class="card p-3" [style.background]="surfaceColor()" [style.border]="borderColor() ? '1px solid ' + borderColor() : '1px solid var(--border)'">
          <button class="btn btn-sm" [style.background]="accentColor()" [style.color]="getContrastColor(accentColor())">Pulsante</button>
        </div>
      </div>
      
      <div class="d-flex gap-3 mt-3">
        <div class="p-3" [style.background]="bgColor()" [style.border]="borderColor() ? '2px solid ' + borderColor() : '1px solid var(--border)'" style="flex: 1; min-height: 60px; border-radius: 12px; display: flex; align-items: center; justify-content: center; color: var(--text-muted); font-size: 14px;">
          <span class="font-monospace">Sfondo</span>
        </div>
        <div class="p-3" [style.background]="surfaceColor()" [style.border]="borderColor() ? '2px solid ' + borderColor() : '1px solid var(--border)'" style="flex: 1; min-height: 60px; border-radius: 12px; display: flex; align-items: center; justify-content: center; color: var(--text-muted); font-size: 14px;">
          <span class="font-monospace">Superficie</span>
        </div>
      </div>
    </div>
  `,
  styles: `
    .form-control-color {
      width: 100%;
      max-width: 200px;
      height: 40px;
      padding: 0;
      border: 1px solid var(--border);
      border-radius: 12px;
      outline: none;
      cursor: pointer;
      transition: all 0.2s ease;
      box-shadow: 0 1px 3px rgba(0,0,0,0.1);
    }
    
    .form-control-color::-webkit-color-swatch {
      border: none;
      border-radius: 10px;
      height: 36px;
      transition: all 0.2s ease;
    }
    
    .form-control-color::-moz-color-swatch {
      border: none;
      border-radius: 10px;
      height: 36px;
      transition: all 0.2s ease;
    }
    
    .form-control-color:focus {
      box-shadow: 0 0 0 0.2rem color-mix(in srgb, var(--accent) 25%, transparent);
      outline: none;
    }
    

  `
})
export class Settings implements OnInit {
  accentColor = signal('#c0562b');
  bgColor = signal('#faf7f2');
  surfaceColor = signal('#ffffff');
  borderColor = signal('#e8e2d8');
  textColor = signal('#2d2a26');

  ngOnInit() {
    this.loadColors();
    // Apply the colors when the page loads
    this.applyColors();
  }

  loadColors() {
    // Load saved colors from localStorage or use defaults
    const savedAccent = localStorage.getItem('app-accent-color');
    const savedBg = localStorage.getItem('app-bg-color');
    const savedSurface = localStorage.getItem('app-surface-color');
    const savedBorder = localStorage.getItem('app-border-color');
    const savedText = localStorage.getItem('app-text-color');

    if (savedAccent) this.accentColor.set(savedAccent);
    if (savedBg) this.bgColor.set(savedBg);
    if (savedSurface) this.surfaceColor.set(savedSurface);
    if (savedBorder) this.borderColor.set(savedBorder);
    if (savedText) this.textColor.set(savedText);
  }

  updateAccentColor(event: any) {
    this.accentColor.set(event.target.value);
    localStorage.setItem('app-accent-color', this.accentColor());
  }

  updateBgColor(event: any) {
    this.bgColor.set(event.target.value);
    localStorage.setItem('app-bg-color', this.bgColor());
  }

  updateSurfaceColor(event: any) {
    this.surfaceColor.set(event.target.value);
    localStorage.setItem('app-surface-color', this.surfaceColor());
  }

  updateBorderColor(event: any) {
    this.borderColor.set(event.target.value);
    localStorage.setItem('app-border-color', this.borderColor());
  }

  updateTextColor(event: any) {
    this.textColor.set(event.target.value);
    localStorage.setItem('app-text-color', this.textColor());
  }

  resetColors() {
    // Reset to default values
    this.accentColor.set('#c0562b');
    this.bgColor.set('#faf7f2');
    this.surfaceColor.set('#ffffff');
    this.borderColor.set('#e8e2d8');
    this.textColor.set('#2d2a26');
    
    // Clear localStorage
    localStorage.removeItem('app-accent-color');
    localStorage.removeItem('app-bg-color');
    localStorage.removeItem('app-surface-color');
    localStorage.removeItem('app-border-color');
    localStorage.removeItem('app-text-color');
    
    // Apply the default colors
    this.applyColors();
  }

  applyDemoColors() {
    // Apply demo colors
    this.accentColor.set('#1b7a43');
    this.bgColor.set('#f8f9fa');
    this.surfaceColor.set('#ffffff');
    this.borderColor.set('#dee2e6');
    this.textColor.set('#212529');
    
    localStorage.setItem('app-accent-color', this.accentColor());
    localStorage.setItem('app-bg-color', this.bgColor());
    localStorage.setItem('app-surface-color', this.surfaceColor());
    localStorage.setItem('app-border-color', this.borderColor());
    localStorage.setItem('app-text-color', this.textColor());
    
    // Apply the demo colors
    this.applyColors();
  }

  applyColors() {
    // Apply the colors to CSS variables on both root and body for maximum compatibility
    const root = document.documentElement;
    const body = document.body;
    root.style.setProperty('--accent', this.accentColor());
    root.style.setProperty('--bg', this.bgColor());
    root.style.setProperty('--surface', this.surfaceColor());
    root.style.setProperty('--border', this.borderColor());
    root.style.setProperty('--text', this.textColor());
    body.style.setProperty('--accent', this.accentColor());
    body.style.setProperty('--bg', this.bgColor());
    body.style.setProperty('--surface', this.surfaceColor());
    body.style.setProperty('--border', this.borderColor());
    body.style.setProperty('--text', this.textColor());
    
    // Trigger reflow to ensure styles are applied
    setTimeout(() => {
      window.dispatchEvent(new Event('resize'));
    }, 0);
  }

  applyAndSaveColors() {
    // Apply the current colors to the app
    this.applyColors();
  }

  getContrastColor(hexColor: string): string {
    // Convert hex to RGB
    const r = parseInt(hexColor.substr(1, 2), 16);
    const g = parseInt(hexColor.substr(3, 2), 16);
    const b = parseInt(hexColor.substr(5, 2), 16);
    
    // Calculate luminance
    const luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255;
    
    // Return black or white based on luminance
    return luminance > 0.5 ? '#000000' : '#ffffff';
  }

  goBack() {
    // Apply the colors before going back
    this.applyColors();
    window.history.back();
  }
}