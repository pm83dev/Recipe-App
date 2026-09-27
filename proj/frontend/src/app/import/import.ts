import { Component, inject, signal, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ApiService } from '../api';
import { ExtractResult } from '../recipe';

@Component({
  selector: 'app-import',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './import.html',
  styleUrl: './import.css'
})
export class Import {
  readonly api = inject(ApiService);
  private router = inject(Router);

  mode = signal<'url' | 'text'>('url');
  url = signal('');
  text = signal('');
  loading = signal(false);
  error = signal<string | null>(null);
  result = signal<ExtractResult | null>(null);
  readonly resultBox = viewChild.required<HTMLElement>('resultBox');

  extract() {
    const payload = this.mode() === 'url'
      ? { url: this.url().trim() }
      : { text: this.text().trim() };
    if (!payload.url && !payload.text) {
      this.error.set('Inserisci un URL o un testo');
      return;
    }
    this.loading.set(true);
    this.error.set(null);
    this.result.set(null);
    this.api.extract(payload).subscribe({
      next: r => {
        this.result.set(r);
        this.loading.set(false);
        setTimeout(() => this.resultBox().scrollIntoView({ behavior: 'smooth', block: 'start' }), 50);
      },
      error: e => {
        this.error.set('Estrazione: ' + (e?.error?.error ?? e?.message ?? e));
        this.loading.set(false);
      }
    });
  }

  save() {
    const r = this.result();
    if (!r) return;
    this.loading.set(true);
    this.api.createRecipe(r.recipe).subscribe({
      next: created => this.router.navigate(['/recipes', created.id]),
      error: e => {
        this.error.set('Salvataggio: ' + (e?.message ?? e));
        this.loading.set(false);
      }
    });
  }
}
