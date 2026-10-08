import { isPlatformBrowser } from '@angular/common';
import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable, PLATFORM_ID, signal, Signal } from '@angular/core';
import { catchError, finalize, of, tap } from 'rxjs';
import { ExtractResult, Recipe } from './recipe';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private http = inject(HttpClient);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
  // Cloudflare tunnel per IIS
  private readonly baseUrl = 'http://lemiericette.pm-softwareautomation.com/api';

  private _search = signal('');
  readonly search: Signal<string> = this._search;
  setSearch(v: string) {
    this._search.set(v);
  }

  readonly recipes = signal<Recipe[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  private api() {
    return this.baseUrl;
  }

  loadRecipes() {
    this.loading.set(true);
    this.error.set(null);
    const p = this.search() ? new HttpParams().set('search', this.search()) : new HttpParams();
    return this.http.get<Recipe[]>(this.api() + '/recipes', { params: p }).pipe(
      tap((list) => this.recipes.set(list ?? [])),
      catchError((e) => {
        this.error.set('Impossibile caricare le ricette: ' + (e?.error?.error ?? e?.message ?? e));
        return of([]);
      }),
      finalize(() => this.loading.set(false)),
    );
  }

  getRecipe(id: number) {
    return this.http.get<Recipe>(this.api() + '/recipes/' + id);
  }

  createRecipe(r: Recipe) {
    return this.http.post<Recipe>(this.api() + '/recipes', r);
  }

  updateRecipe(id: number, r: Recipe) {
    return this.http.put<Recipe>(this.api() + '/recipes/' + id, r);
  }

  deleteRecipe(id: number) {
    return this.http.delete<void>(this.api() + '/recipes/' + id);
  }

  uploadPhoto(id: number, file: File) {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<{ imageUrl: string }>(this.api() + '/recipes/' + id + '/photo', form);
  }

  removePhoto(id: number) {
    return this.http.delete<{ imageUrl: string | null }>(this.api() + '/recipes/' + id + '/photo');
  }

  extract(payload: { url?: string; text?: string }) {
    return this.http.post<ExtractResult>(this.api() + '/extract', payload);
  }

  imageUrl(path: string | null): string {
    if (!path) return '';
    if (path.startsWith('http')) return path;
    const base = this.baseUrl.replace('/api', '').replace(/\/$/, '');
    const clean = path.startsWith('/') ? path : '/uploads/' + path;
    return base + clean;
  }
}
