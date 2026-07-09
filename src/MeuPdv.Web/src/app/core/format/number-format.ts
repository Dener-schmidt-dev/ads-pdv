const brlFormatter = new Intl.NumberFormat('pt-BR', {
  style: 'currency',
  currency: 'BRL',
  minimumFractionDigits: 2,
  maximumFractionDigits: 2
});

export function formatBrl(value: number | null | undefined): string {
  if (value == null || Number.isNaN(value)) {
    return brlFormatter.format(0);
  }
  return brlFormatter.format(value);
}

export function parseBrl(text: string): number {
  const raw = text.trim();
  if (!raw) return 0;

  const normalized = raw
    .replace(/\s/g, '')
    .replace(/R\$/gi, '')
    .replace(/\./g, '')
    .replace(',', '.');

  const parsed = Number(normalized);
  return Number.isFinite(parsed) ? parsed : 0;
}

export function formatBrlInput(value: number | null | undefined): string {
  if (value == null || Number.isNaN(value)) return '';
  return value.toLocaleString('pt-BR', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  });
}

export function formatPercent(value: number | null | undefined, decimals = 2): string {
  if (value == null || Number.isNaN(value)) {
    return '0%';
  }
  return `${value.toLocaleString('pt-BR', {
    minimumFractionDigits: 0,
    maximumFractionDigits: decimals
  })}%`;
}

export function parsePercent(text: string): number {
  const raw = text.trim();
  if (!raw) return 0;

  const normalized = raw
    .replace(/\s/g, '')
    .replace(/%/g, '')
    .replace(/\./g, '')
    .replace(',', '.');

  const parsed = Number(normalized);
  return Number.isFinite(parsed) ? parsed : 0;
}

export function formatPercentInput(value: number | null | undefined): string {
  if (value == null || Number.isNaN(value)) return '';
  return value.toLocaleString('pt-BR', {
    minimumFractionDigits: 0,
    maximumFractionDigits: 2
  });
}
