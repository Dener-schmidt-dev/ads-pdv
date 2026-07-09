import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ConfiguracaoPdv } from '../models/pdv-config.models';

@Injectable({ providedIn: 'root' })
export class PdvThemeService {
  private readonly http = inject(HttpClient);

  readonly config = signal<ConfiguracaoPdv | null>(null);

  carregar(): void {
    this.http.get<ConfiguracaoPdv>('/api/pdv/visual').subscribe({
      next: (c) => {
        this.config.set(c);
        this.aplicarCorPrimaria(c.corPrimaria);
      },
      error: () => {
        this.config.set({
          corPrimaria: '#2563eb',
          exibirNomeEmpresa: true,
          temImagemFundo: false
        });
      }
    });
  }

  aplicarCorPrimaria(cor: string): void {
    if (!cor?.match(/^#[0-9A-Fa-f]{6}$/)) return;

    const root = document.documentElement;
    root.style.setProperty('--mpdv-primary', cor);
    root.style.setProperty('--mpdv-primary-hover', escurecerCor(cor, 0.12));
    root.style.setProperty('--mpdv-primary-soft', clarearCor(cor, 0.92));
    root.style.setProperty('--mpdv-primary-muted', clarearCor(cor, 0.85));
    root.style.setProperty('--mpdv-primary-ring', `${cor}38`);
  }

  urlImagemFundo(): string | null {
    const c = this.config();
    if (!c?.temImagemFundo) return null;
    const v = c.imagemFundoVersao ? `?v=${c.imagemFundoVersao}` : '';
    return `/api/pdv/imagem-fundo${v}`;
  }

  tituloPdv(): string {
    const c = this.config();
    if (c?.exibirNomeEmpresa && c.nomeEmpresa?.trim()) {
      return c.nomeEmpresa.trim();
    }
    return 'MeuPDV';
  }
}

function escurecerCor(hex: string, fator: number): string {
  const rgb = hexParaRgb(hex);
  if (!rgb) return hex;
  return rgbParaHex(
    Math.round(rgb.r * (1 - fator)),
    Math.round(rgb.g * (1 - fator)),
    Math.round(rgb.b * (1 - fator))
  );
}

function clarearCor(hex: string, mix: number): string {
  const rgb = hexParaRgb(hex);
  if (!rgb) return hex;
  return rgbParaHex(
    Math.round(rgb.r + (255 - rgb.r) * mix),
    Math.round(rgb.g + (255 - rgb.g) * mix),
    Math.round(rgb.b + (255 - rgb.b) * mix)
  );
}

function hexParaRgb(hex: string): { r: number; g: number; b: number } | null {
  const m = /^#?([0-9A-Fa-f]{6})$/.exec(hex);
  if (!m) return null;
  const n = parseInt(m[1], 16);
  return { r: (n >> 16) & 255, g: (n >> 8) & 255, b: n & 255 };
}

function rgbParaHex(r: number, g: number, b: number): string {
  return `#${[r, g, b].map((v) => v.toString(16).padStart(2, '0')).join('')}`;
}
