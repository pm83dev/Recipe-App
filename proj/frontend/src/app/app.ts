import { Component, OnInit } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  title = 'Le mie Ricette';

  ngOnInit() {
    const root = document.documentElement;
    const body = document.body;
    const apply = (varName: string, key: string) => {
      const val = localStorage.getItem(key);
      if (val) {
        root.style.setProperty(varName, val);
        body.style.setProperty(varName, val);
      }
    };
    apply('--accent', 'app-accent-color');
    apply('--bg', 'app-bg-color');
    apply('--surface', 'app-surface-color');
    apply('--border', 'app-border-color');
    apply('--text', 'app-text-color');
  }
}
